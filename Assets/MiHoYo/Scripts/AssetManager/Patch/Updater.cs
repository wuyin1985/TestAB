using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BytesTools;
using FileMapSystem;
using libx;
using Res.ABSystem;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace NewWarMap.Patch
{
    public interface IUpdater
    {
        void OnStart();

        void OnMessage(string msg);

        void OnProgress(float progress);

        void OnVersion(string ver);

        void OnClear();
    }

    [RequireComponent(typeof(Downloader))]
    [RequireComponent(typeof(NetworkMonitor))]
    public class Updater : MonoBehaviour, IUpdater, INetworkMonitorListener
    {
        enum Step
        {
            Wait,
            Versions,
            Prepared,
            Download,
            Refresh,
        }

        private Step _step;

        [SerializeField] private string baseURL = "http://127.0.0.1:7888/DLC/";
        public const string VersionInfoFileName = "version.json";

        public IUpdater listener { get; set; }

        private Downloader _downloader;
        private NetworkMonitor _monitor;
        private string _platform;
        private string _savePath;
        private string _tempDownloadPath;
        private Dictionary<string, FileMapGroupDesc> _currentDownloadingGroupDesc;
        private AssetBundleTable _currentDownloadAssetBundleTable;


        public void OnStart()
        {
        }

        public void OnMessage(string msg)
        {
            listener?.OnMessage(msg);
        }

        public void OnProgress(float progress)
        {
            listener?.OnProgress(progress);
        }

        public void OnVersion(string ver)
        {
            listener?.OnVersion(ver);
        }

        private void Start()
        {
            _downloader = gameObject.GetComponent<Downloader>();
            _downloader.onUpdate = OnUpdate;

            _monitor = gameObject.GetComponent<NetworkMonitor>();
            _monitor.listener = this;

            _savePath = $"{Application.persistentDataPath}/{AssetBundlePathResolver.BundleSaveDirName}/";
            _tempDownloadPath = $"{Application.persistentDataPath}/TempDownload/";
            _platform = GetPlatformForAssetBundles(Application.platform);
            _step = Step.Wait;

            var version = AssetBundleManager.Instance.GetFileMapSystem().Version.ToString();
            OnVersion(version);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (_reachabilityChanged || _step == Step.Wait)
            {
                return;
            }

            if (hasFocus)
            {
                MessageBox.CloseAll();
                if (_step == Step.Download)
                {
                    _downloader.Restart();
                }
                else
                {
                    StartUpdate();
                }
            }
            else
            {
                if (_step == Step.Download)
                {
                    _downloader.Stop();
                }
            }
        }

        private bool _reachabilityChanged;

        public void OnReachablityChanged(NetworkReachability reachability)
        {
            if (_step == Step.Wait)
            {
                return;
            }

            _reachabilityChanged = true;
            if (_step == Step.Download)
            {
                _downloader.Stop();
            }

            if (reachability == NetworkReachability.NotReachable)
            {
                MessageBox.Show("提示！", "找不到网络，请确保手机已经联网", "确定", "退出").onComplete += delegate(MessageBox.EventId id)
                {
                    if (id == MessageBox.EventId.Ok)
                    {
                        if (_step == Step.Download)
                        {
                            _downloader.Restart();
                        }
                        else
                        {
                            StartUpdate();
                        }

                        _reachabilityChanged = false;
                    }
                    else
                    {
                        Quit();
                    }
                };
            }
            else
            {
                if (_step == Step.Download)
                {
                    _downloader.Restart();
                }
                else
                {
                    StartUpdate();
                }

                _reachabilityChanged = false;
                MessageBox.CloseAll();
            }
        }

        private void OnUpdate(long progress, long size, float speed)
        {
            OnMessage(
                $"下载中...{Downloader.GetDisplaySize(progress)}/{Downloader.GetDisplaySize(size)}, 速度：{Downloader.GetDisplaySpeed(speed)}");

            OnProgress(progress * 1f / size);
        }

        public void Clear()
        {
            MessageBox.Show("提示", "清除数据后所有数据需要重新下载，请确认！", "清除").onComplete += id =>
            {
                if (id != MessageBox.EventId.Ok)
                    return;
                OnClear();
            };
        }

        public void OnClear()
        {
            OnMessage("数据清除完毕");
            OnProgress(0);
            _downloader.Clear();
            _step = Step.Wait;
            _reachabilityChanged = false;

            Assets.Clear();

            listener?.OnClear();

            if (Directory.Exists(_savePath))
            {
                Directory.Delete(_savePath, true);
            }
        }

        public static void DeleteDirectory(FileSystemInfo fileSystemInfo)
        {
            if (fileSystemInfo is DirectoryInfo directoryInfo)
            {
                foreach (FileSystemInfo childInfo in directoryInfo.GetFileSystemInfos())
                {
                    DeleteDirectory(childInfo);
                }
            }

            fileSystemInfo.Attributes = FileAttributes.Normal;
            fileSystemInfo.Delete();
        }

        public static void DeleteFile(string path)
        {
            var info = new FileInfo(path);
            if (info.Exists)
            {
                info.Attributes = FileAttributes.Normal;
                info.Delete();
            }
        }

        private IEnumerator _checking;

        public void StartUpdate()
        {
            listener?.OnStart();

            if (_checking != null)
            {
                StopCoroutine(_checking);
            }

            _checking = Checking();

            StartCoroutine(_checking);
        }

        private void PrepareDownloads(FileMapSystem.FileMapSystem newMap, string versionStr)
        {
            var currentMap = AssetBundleManager.Instance.GetFileMapSystem();
            var misses = currentMap.GetMissFileMaps(newMap);
            CommonLog.Log(MAuthor.WY, $"{misses.Count} files miss in current file map");
            foreach (var fileMapGroupDescIter in misses)
            {
                var fileName = fileMapGroupDescIter.Key;
                var desc = fileMapGroupDescIter.Value;
                _downloader.AddDownload(GetDownloadURL(fileName, versionStr), _savePath + fileName,
                    new MD5Creater.MD5Struct {MD51 = desc.Md51, MD52 = desc.Md52},
                    desc.Len);
            }
        }

        private static string GetPlatformForAssetBundles(RuntimePlatform target)
        {
            switch (target)
            {
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
                case RuntimePlatform.WebGLPlayer:
                    return "WebGL";
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return "Windows";
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    return "iOS"; // OSX
                default:
                    return null;
            }
        }

        private string GetDownloadURL(string filename, string version = null)
        {
            return version != null ? $"{baseURL}{_platform}/{version}/{filename}" : $"{baseURL}{_platform}/{filename}";
        }

        private IEnumerator Checking()
        {
            if (!Directory.Exists(_savePath))
            {
                Directory.CreateDirectory(_savePath);
            }

            if (!Directory.Exists(_tempDownloadPath))
            {
                Directory.CreateDirectory(_tempDownloadPath);
            }

            _currentDownloadingGroupDesc = null;
            _currentDownloadAssetBundleTable = null;

            _step = Step.Versions;

            if (_step == Step.Versions)
            {
                yield return RequestVersions();
            }

            if (_step == Step.Prepared)
            {
                OnMessage("正在检查版本信息...");
                var totalSize = _downloader.size;
                if (totalSize > 0)
                {
                    var tips = $"发现内容更新，总计需要下载 {Downloader.GetDisplaySize(totalSize)} 内容";
                    var mb = MessageBox.Show("提示", tips, "下载", "退出");
                    yield return mb;
                    if (mb.isOk)
                    {
                        _downloader.StartDownload();
                        _step = Step.Download;
                    }
                    else
                    {
                        Quit();
                    }
                }
                else
                {
                    OnComplete();
                }
            }
        }


        private void ShowAndroidUpdateDialog(string appUpdateUrl)
        {
            CommonLog.Log(MAuthor.ZX, "Open Android Update Dialog");
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidAlertDialog alertDialog = new AndroidAlertDialog(activity);
            alertDialog.SetTitle("游戏版本检测");
            alertDialog.SetMessage("检测到有新的游戏版本，是否更新？");
            alertDialog.SetPositiveButton("是", new AlertDialogClickListener((dialog, which) =>
            {
                //跳转App更新界面
                Application.OpenURL(appUpdateUrl);
                //强制退出
                Application.Quit();
            }));
            alertDialog.SetNegativeButton("否", new AlertDialogClickListener((dialog, which) =>
            {
                //强制退出
                Application.Quit();
            }));
            alertDialog.Create();
            alertDialog.Show();
        }

        /// <summary>
        /// 显示IOS升级弹窗
        /// </summary>
        private void ShowIOSUpdateDialog(string appUpdateUrl)
        {
        }


        private class SingleFileDownloadRequest
        {
            public string url;
            public string targetPath;
            public bool success;

            public void Reset(string url, string targetPath)
            {
                this.url = url;
                this.targetPath = targetPath;
                success = false;
            }
        }

        private IEnumerator DownloadSingleFile(SingleFileDownloadRequest sr)
        {
            DeleteFile(sr.targetPath);

            var request = UnityWebRequest.Get(sr.url);
            request.downloadHandler = new DownloadHandlerFile(sr.targetPath);
            yield return request.SendWebRequest();
            var error = request.error;
            request.Dispose();

            if (string.IsNullOrEmpty(error) && !File.Exists(sr.targetPath))
            {
                error = $"download finish but {sr.targetPath} not exist";
            }

            if (!string.IsNullOrEmpty(error))
            {
                var mb = MessageBox.Show("提示", $"获取服务器文件{sr.url} 失败：{error}", "重试", "退出");
                yield return mb;
                if (mb.isOk)
                {
                    StartUpdate();
                }
                else
                {
                    Quit();
                }

                sr.success = false;
                yield break;
            }

            sr.success = true;
        }

        private IEnumerator RequestVersions()
        {
            OnMessage("正在获取版本信息...");
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                var mb = MessageBox.Show("提示", "请检查网络连接状态", "重试", "退出");
                yield return mb;
                if (mb.isOk)
                {
                    StartUpdate();
                }
                else
                {
                    Quit();
                }

                yield break;
            }

            var versionFilePath = _tempDownloadPath + VersionInfoFileName;
            var singleFileDownloadRequest = new SingleFileDownloadRequest();
            singleFileDownloadRequest.Reset(GetDownloadURL(VersionInfoFileName), versionFilePath);
            yield return DownloadSingleFile(singleFileDownloadRequest);

            if (!singleFileDownloadRequest.success)
            {
                yield break;
            }

            VersionInfo versionInfo;
            try
            {
                var versionFileStr = File.ReadAllText(versionFilePath);
                versionInfo = versionFileStr.FromXML<VersionInfo>();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                MessageBox.Show("提示", "版本文件加载失败", "重试", "退出").onComplete +=
                    delegate(MessageBox.EventId id)
                    {
                        if (id == MessageBox.EventId.Ok)
                        {
                            StartUpdate();
                        }
                        else
                        {
                            Quit();
                        }
                    };

                yield break;
            }

            var state = versionInfo.CheckUpdateState(AssetBundleManager.Instance.GetVersion());
            var versionStr = versionInfo.DumpVersion();
            CommonLog.Log(MAuthor.WY, $"check version {versionStr} result {state}");
            if (state == VersionInfo.State.NeedUpdate)
            {
                //下载xmf
                var xmfFileName = AssetBundlePathResolver.BundleSaveDirName + FileMapInfo.FileExtension;
                var xmfTargetPath = _tempDownloadPath + xmfFileName;
                singleFileDownloadRequest.Reset(GetDownloadURL(xmfFileName, versionStr), xmfTargetPath);
                yield return DownloadSingleFile(singleFileDownloadRequest);
                if (!singleFileDownloadRequest.success)
                {
                    yield break;
                }

                //下载AssetBundleXMLData.xml 
                var xmlPath = _tempDownloadPath + AssetBundlePathResolver.DependFileName;
                singleFileDownloadRequest.Reset(
                    GetDownloadURL(AssetBundlePathResolver.DependFileName, versionStr),
                    xmlPath);
                yield return DownloadSingleFile(singleFileDownloadRequest);

                if (!singleFileDownloadRequest.success)
                {
                    yield break;
                }

                var tableStr = File.ReadAllText(xmlPath);
                _currentDownloadAssetBundleTable = tableStr.FromXML<AssetBundleTable>();

                var newFileMap = new FileMapSystem.FileMapSystem(_tempDownloadPath);
                var bs = File.ReadAllBytes(xmfTargetPath);
                newFileMap.InitFileMapInfo(bs);

                PrepareDownloads(newFileMap, versionStr);
                _step = Step.Prepared;
            }
            else
            {
                switch (state)
                {
                    case VersionInfo.State.MustDownloadAppAgain:
                    {
#if UNITY_ANDROID
                        ShowAndroidUpdateDialog(versionInfo.AppUpdateUrl);
                        yield break;
#elif UNITY_IOS
                        ShowIOSUpdateDialog(versionInfo.AppUpdateUrl);
                        yield break;
#else
                        var mb = MessageBox.Show("提示", $"需要重新下载app");
                        yield return mb;
                        Quit();
#endif
                        break;
                    }
                    case VersionInfo.State.NotNeedUpdate:
                    {
                        OnComplete();
                        break;
                    }
                }
            }
        }

        private void MergeArray<T>(ref T[] target, T[] addition)
        {
            var originCount = target.Length;
            Array.Resize(ref target, originCount + addition.Length);
            Array.Copy(addition, 0, target, originCount, addition.Length);
        }

        private void MergeUpdateFileMaps()
        {
            if (_currentDownloadAssetBundleTable == null)
            {
                throw new Exception($"{nameof(_currentDownloadAssetBundleTable) is null}");
            }

            if (_currentDownloadingGroupDesc == null)
            {
                throw new Exception($"{nameof(_currentDownloadingGroupDesc) is null}");
            }

            var watch = Stopwatch.StartNew();

            var assetBundleFromTableDic = new Dictionary<string, AssetBundleTable.AssetBundleBundleData>();
            var updatedAssetBundles = new List<AssetBundleTable.AssetBundleBundleData>();
            foreach (var info in _currentDownloadAssetBundleTable.BundleInfos)
            {
                var xbundleMD5Name = info.bundleNameMD5Struct.GetMD5Str(!info.isComplexName) +
                                     AssetBundleTable.AssetBundleBundleData.BundleExtension;
                //todo need add language extension name
                assetBundleFromTableDic.Add(xbundleMD5Name, info);
            }

            var newFileMapInfos = new List<FileMapInfo>();
            foreach (var fileMapGroupDescIter in _currentDownloadingGroupDesc)
            {
                var infos = fileMapGroupDescIter.Value.FileMapInfos;
                foreach (var fileMapInfo in infos)
                {
                    if (assetBundleFromTableDic.TryGetValue(fileMapInfo.FileName, out var bundleData))
                    {
                        updatedAssetBundles.Add(bundleData);
                    }
                    else
                    {
                        CommonLog.Error(MAuthor.WY,
                            $"AssetBundle {fileMapInfo.FileName} in map infos but not in bundleinfos");
                    }
                }

                newFileMapInfos.AddRange(infos);
            }

            CommonLog.Log(MAuthor.WY, $"diff updated asset bundle cost time {watch.ElapsedMilliseconds} ms");
            watch.Restart();

            var originTable = AssetBundleManager.Instance.GetTable();
            MergeArray(ref originTable.BundleInfos, updatedAssetBundles.ToArray());
            var xml = originTable.ToXML();
            File.WriteAllText(_savePath + AssetBundlePathResolver.DependFileName, xml);

            CommonLog.Log(MAuthor.WY, $"write AssetBundleDataXml cost time {watch.ElapsedMilliseconds} ms");
            watch.Restart();

            var map = AssetBundleManager.Instance.GetFileMapSystem();
            MergeArray(ref map.FileInfo.AllFileMapInfo, newFileMapInfos.ToArray());
            var mapperBs = new ByteBuf(10000);
            map.FileInfo.WriteToByteBuf(mapperBs);

            var xmfPath = _savePath + AssetBundlePathResolver.BundleSaveDirName +
                          FileMapInfo.FileExtension;
            DeleteFile(xmfPath);

            var writeFileMapStream = File.Create(xmfPath);
            writeFileMapStream.Write(mapperBs.GetRaw(), 0, mapperBs.WriterIndex);
            writeFileMapStream.Close();
            CommonLog.Log(MAuthor.WY, $"write AssetbundlesCache.xmf cost time {watch.ElapsedMilliseconds} ms");
        }

        private void Update()
        {
            if (_step == Step.Download)
            {
                if (_downloader.IsFinished())
                {
                    _step = Step.Refresh;
                    MergeUpdateFileMaps();

                    OnProgress(1);
                    OnMessage("更新完成");

                    StartCoroutine(ReloadResources());
                }
            }
        }

        private IEnumerator ReloadResources()
        {
            CommonLog.Log(MAuthor.WY, "start reload resource");
            Destroy(gameObject);
            yield return null;

            if (GameAssetManager.Instance.CheckAllAssetsDone())
            {
                yield return null;
            }

            CommonLog.Log(MAuthor.WY, "start safe dispose asset");
            GameAssetManager.Instance.SafeDisposeAllAsset();
            AssetBundleManager.Instance.SafeDisposeAll();
            yield return Resources.UnloadUnusedAssets();

            CommonLog.Log(MAuthor.WY, "start reload asset bundles");
            AssetBundleManager.Instance.LoadAssetBundleConfig();
            
            OnComplete();
        }

        private void OnComplete()
        {
            ToLoginScene();
        }

        private void ToLoginScene()
        {
        }

        private void OnDestroy()
        {
            MessageBox.Dispose();
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}