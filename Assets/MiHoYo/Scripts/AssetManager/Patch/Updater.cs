using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BytesTools;
using FileMapSystem;
using Res.ABSystem;
using SimpleDiskUtils;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

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

    [RequireComponent(typeof(NetworkMonitor))]
    public class Updater : MonoBehaviour, INetworkMonitorListener
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

        [SerializeField] private string baseURL = "http://139.196.48.246/";
        public const string VersionInfoFileName = "versioninfo.xml";

        public IUpdater listener { get; set; }

        private HttpFileDownloader _downloader;

        private NetworkMonitor _monitor;
        private string _platform;
        private string _savePath;
        private string _tempDownloadPath;
        private Dictionary<string, FileMapGroupDesc> _currentDownloadingGroupDesc;
        private AssetBundleTable _currentDownloadAssetBundleTable;
        private VersionInfo _currentTargetVersion;

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
            _monitor = gameObject.GetComponent<NetworkMonitor>();
            _monitor.listener = this;

            _savePath = $"{Application.persistentDataPath}/{AssetBundlePathResolver.BundleSaveDirName}/";
            _tempDownloadPath = $"{Application.persistentDataPath}/TempDownload/";
            _platform = GetPlatformForAssetBundles(Application.platform);
            _step = Step.Wait;

            var version = AssetBundleManager.Instance.GetFileMapSystem().Version.ToString();
            OnVersion(version);

            StartUpdate();
        }

        private void OnApplicationQuit()
        {
            _downloader.Release();
        }

        /*private void OnApplicationFocus(bool hasFocus)
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
        }*/

        private bool _reachabilityChanged;

        private bool CheckDiskSpaceEnough(long needBytes)
        {
            var needMb = needBytes / (1024 * 1024);
            var currentMb = 0;

#if UNITY_EDITOR_WIN
            currentMb = DiskUtils.CheckAvailableSpace("C:/");
#elif UNITY_IOS || UNITY_EDITOR_OSX
            currentMb = DiskUtils.CheckAvailableSpace();
#elif UNITY_ANDROID
            currentMb = DiskUtils.CheckAvailableSpace(true);
#endif

            CommonLog.Log(MAuthor.WY, $"need {needMb}mb space , current available disk space is {currentMb}mb");
            return needMb < currentMb;
        }

        public void OnReachablityChanged(NetworkReachability reachability)
        {
            if (_step == Step.Wait)
            {
                return;
            }

            _reachabilityChanged = true;

            /*if (reachability == NetworkReachability.NotReachable)
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
            }*/
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


        private IEnumerator _checking;

        public void StartUpdate()
        {
            listener?.OnStart();

            if (_checking != null)
            {
                StopCoroutine(_checking);
            }

            _checking = Processing();

            StartCoroutine(_checking);
        }

        private void PrepareDownloads(FileMapSystem.FileMapSystem newMap, string versionStr)
        {
            if (_downloader != null)
            {
                _downloader.Release();
                _downloader = null;
            }

            _downloader = new HttpFileDownloader(new Uri(GetDownloadBaseURL(versionStr)),
                AssetBundlePathResolver.BundleSaveDirName);
            var currentMap = AssetBundleManager.Instance.GetFileMapSystem();
            var misses = currentMap.GetMissFileMaps(newMap);
            _currentDownloadingGroupDesc = misses;
            CommonLog.Log(MAuthor.WY, $"{misses.Count} files miss in current file map");

            foreach (var fileMapGroupDescIter in misses)
            {
                var fileName = fileMapGroupDescIter.Key;
                var desc = fileMapGroupDescIter.Value;
                var fileSavePath = _savePath + fileName;

                if (File.Exists(fileSavePath))
                {
                    var bytes = FileUtils.ReadAllBytes(fileSavePath);
                    if (bytes != null && bytes.Length > 0)
                    {
                        var md5 = MD5Creater.Md5Struct(bytes);
                        if (md5.MD51 == desc.Md51 && md5.MD52 == desc.Md52)
                        {
                            CommonLog.Log(MAuthor.WY, $"file {fileSavePath} already exist, skip");
                            continue;
                        }
                    }
                }

                _downloader.AddDownLoad(new WWWFileDownloader.DownloadFileInfo
                {
                    FileName = fileName,
                    FileSize = desc.Len,
                    MapedFileName_MD51 = desc.Md51,
                    MapedFileName_MD52 = desc.Md52,
                });
            }
        }
        
        private const float BYTES_2_MB = 1f / (1024 * 1024);
        
        public static string GetDisplaySpeed(float downloadSpeed)
        {
            if (downloadSpeed >= 1024 * 1024)
            {
                return $"{downloadSpeed * BYTES_2_MB:f2}MB/s";
            }

            if (downloadSpeed >= 1024)
            {
                return $"{downloadSpeed / 1024:f2}KB/s";
            }

            return $"{downloadSpeed:f2}B/s";
        }

        public static string GetDisplaySize(long downloadSize)
        {
            if (downloadSize >= 1024 * 1024)
            {
                return $"{downloadSize * BYTES_2_MB:f2}MB";
            }

            return downloadSize >= 1024 ? $"{downloadSize / 1024:f2}KB" : $"{downloadSize:f2}B";
        }

        private static string GetPlatformForAssetBundles(RuntimePlatform target)
        {
            switch (target)
            {
                case RuntimePlatform.Android:
                    return "android";
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return "pc";
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    return "ios"; // OSX
                default:
                    return null;
            }
        }

        private string GetDownloadURL(string filename, string version = null)
        {
            return $"{GetDownloadBaseURL(version)}{filename}";
        }

        private string GetDownloadBaseURL(string version = null)
        {
            return version != null ? $"{baseURL}{_platform}/{version}/" : $"{baseURL}{_platform}/";
        }

        private IEnumerator Processing()
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
            _currentTargetVersion = null;

            _step = Step.Versions;

            if (_step == Step.Versions)
            {
                yield return RequestVersions();
            }

            if (_step == Step.Prepared)
            {
                OnMessage("正在检查版本信息...");
                var totalSize = _downloader.GetToDownloadSize();
                if (totalSize > 0)
                {
                    var tips = $"发现内容更新，总计需要下载 {GetDisplaySize(totalSize)} 内容";
                    var mb = MessageBox.Show("提示", tips, "下载", "退出");
                    yield return mb;
                    if (mb.isOk)
                    {
                        _downloader.StartDownloadFile();
                        _step = Step.Download;
                    }
                    else
                    {
                        Quit();
                        yield break;
                    }
                }
                else
                {
                    _step = Step.Download;
                }
            }

            if (_step == Step.Download)
            {
                var progress = _downloader.GetProgress();
                do
                {
                    OnMessage(
                        $"下载中...{GetDisplaySize(progress.CompletedSize)}/{GetDisplaySize(progress.TotalSize)}, 速度：{GetDisplaySpeed(progress.Speed)}");
                    OnProgress(progress.CompletedSize * 1f / progress.TotalSize);
                    CommonLog.Log($"complete size {progress.CompletedSize}");
                    yield return null;
                } while (!progress.IsCompleted);

                if (progress.IsError)
                {
                    var tips = $"下载文件时发生错误(错误码:00{(int) progress.ErrorType}), 是否重新下载";
                    var mb = MessageBox.Show("提示", tips, "下载", "退出");
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

                _step = Step.Refresh;
                MergeUpdateFileMaps();
                OnProgress(1);
                OnMessage("更新完成");
                StartCoroutine(ReloadResources());
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
            FileUtils.DeleteFile(sr.targetPath);

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
                CommonLog.Error(e.Message);
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
            _currentTargetVersion = versionInfo;
            var versionStr = versionInfo.DumpVersion();
            CommonLog.Log(MAuthor.WY, $"check version {versionStr} result {state}");
            if (state == VersionInfo.State.NeedUpdate)
            {
                //下载xmf
                var xmfFileName = AssetBundlePathResolver.BundleSaveDirName + FileMapGroupInfo.FileExtension;
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

        private void MergeAssetBundleTable(AssetBundleTable table,
            Dictionary<string, AssetBundleTable.AssetBundleBundleData> append)
        {
            var origin = new Dictionary<string, AssetBundleTable.AssetBundleBundleData>();
            foreach (var assetBundleBundleData in table.BundleInfos)
            {
                origin.Add(
                    assetBundleBundleData.bundleFileName + AssetBundleTable.AssetBundleBundleData.BundleExtension,
                    assetBundleBundleData);
            }

            foreach (var assetBundleBundleData in append)
            {
                origin[assetBundleBundleData.Key] = assetBundleBundleData.Value;
            }

            var values = origin.Values;
            Array.Resize(ref table.BundleInfos, values.Count);
            values.CopyTo(table.BundleInfos, 0);
        }

        private void MergeUpdateFileMaps()
        {
            if (_currentDownloadAssetBundleTable == null)
            {
                throw new Exception($"{nameof(_currentDownloadAssetBundleTable)}is null");
            }

            if (_currentDownloadingGroupDesc == null)
            {
                throw new Exception($"{nameof(_currentDownloadingGroupDesc)} is null");
            }

            if (_currentTargetVersion == null)
            {
                throw new Exception($"{nameof(_currentTargetVersion)} is null");
            }

            var watch = Stopwatch.StartNew();

            var assetBundleFromTableDic = new Dictionary<string, AssetBundleTable.AssetBundleBundleData>();
            var updatedAssetBundles = new Dictionary<string, AssetBundleTable.AssetBundleBundleData>();
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
                        updatedAssetBundles.Add(fileMapInfo.FileName, bundleData);
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
            MergeAssetBundleTable(originTable, updatedAssetBundles);
            var xml = originTable.ToXML();
            File.WriteAllText(_savePath + AssetBundlePathResolver.DependFileName, xml);

            CommonLog.Log(MAuthor.WY, $"write AssetBundleDataXml cost time {watch.ElapsedMilliseconds} ms");
            watch.Restart();

            var map = AssetBundleManager.Instance.GetFileMapSystem();
            MergeArray(ref map.FileInfo.AllFileMapInfo, newFileMapInfos.ToArray());
            var mapperBs = new ByteBuf(10000);
            map.FileInfo.Ver = new FileMapSystem.Version
            {
                Version_Build = _currentTargetVersion.BuildVersion,
                Version_Major = _currentTargetVersion.MajorVersion,
                Version_Minor = _currentTargetVersion.MinorVersion
            };
            map.FileInfo.WriteToByteBuf(mapperBs);

            var xmfPath = _savePath + AssetBundlePathResolver.BundleSaveDirName +
                          FileMapGroupInfo.FileExtension;
            FileUtils.DeleteFile(xmfPath);

            var writeFileMapStream = File.Create(xmfPath);
            writeFileMapStream.Write(mapperBs.GetRaw(), 0, mapperBs.WriterIndex);
            writeFileMapStream.Close();
            CommonLog.Log(MAuthor.WY, $"write AssetbundlesCache.xmf cost time {watch.ElapsedMilliseconds} ms");
        }


        private IEnumerator ReloadResources()
        {
            CommonLog.Log(MAuthor.WY, "start reload resource");
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
            SceneManager.LoadScene(1, LoadSceneMode.Single);
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