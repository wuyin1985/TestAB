//using YZL.Compress.LZMA;
/**
//加载更新解压控制器
public class LoadDecompressMgr : MonoBehaviour
{
    //是否完全解压完成
    public bool UnCompressAll = false;
    //总共需要下载的字节数
    public int AllNeedLoadByts = 0;
    //当前已经下载的字节数
    public int CurLoadBtys = 0;
    //是否正在下载资源中
    public bool IsBytsLoading = false;
    //当前的下载进程
    public WWW BytsWWW = null;
    //当前下载进程所需下载的字节数
    public int CurNeedLoadByts = 0;
    //当前加载的文件名称
    public string CurLoadFileName = "";
    //当前下载的进度
    public float CurLoadProcess = 0;
    //先前下载的字节数
    public float LastLoadByts = 0;
    //重试次数
    public int LoadRepeatIndex = 5;
    //字体bundle名称
    public static string FontBundleName = "font";
    //字体bundle文件数据
    private byte[] FontBundleByts = null;
    //归零次数
    private int LoadProcessIndex = 0;
    //间隔多久检测一次网速
    private int GetProcessFrame = 0;
    //当前加载的速度
    private double CurLoadSpeed = 0;


    void Start()
    {
    } 
    //检测本地是否有字体的bundle
    private byte[] FontBundleExit()
    {
        string _fontPath = "android/" + FontBundleName;
#if UNITY_IPHONE
        _fontPath="ios/" + FontBundleName;
#endif
        byte[] _bytes =
                GetResByStreaming.Instance.GetByteAllPlatform(
                    AssetBundlePathResolver.GetStreamingPath(_fontPath));
        if (_bytes == null || _bytes.Length <= 0)
        {
            return null;
        }
        return _bytes;
    }
    //发现有更新的资源则删除老的更新xml
    private void DeleteUpdateXML()
    {
        string cachePath = AssetBundlePathResolver.GetBundleCacheDir() + AssetBundlePathResolver.DependFileName;
        try
        {
            if (File.Exists(cachePath))
            {
                File.Delete(cachePath);
            }
        }
        catch (Exception e)
        {

        }
    }
    /// <param name="webresp"></param>
    /// <returns></returns>
    public IEnumerator IDownloadABEnumerator(UI_Login_ViewFunc view, WebServerGroupInfo webresp, List<AssetBundleUpdateInfo.AssetBundleUpdateFileInfo> needDownloadFiles)
    {
        LoadProcessIndex = 8;
        if (needDownloadFiles.Count > 0)
        {
            //DeleteUpdateXML();
            NativeAPIProxy.Instance.ShowPermiss();
            bool isEnoughSpace = false;
            float needDownloadMB = 0f;
            float remainMB = 0;
            float needMB = 0 + 50f;
            float compressloadMB = 0f;
            string bundleCachePath = null;
            AllNeedLoadByts = 0;
            CurLoadBtys = 0;
            try
            {
                //TODO 提示下载内容大小，检查是否有足够磁盘空间+50mb
                bundleCachePath = AssetBundlePathResolver.GetBundleCacheDir();
                remainMB = NativeAPIProxy.Instance.GetCachePathRemainStorageSizeMB();
                foreach (var fi in needDownloadFiles)
                {
                    if (fi.fileName.IndexOf(FontBundleName) != -1)
                    {
                        FontBundleByts = FontBundleExit();
                        if (FontBundleByts != null && FontBundleByts.Length > 0)
                        {
                            continue;
                        }
                    }
                    if (UpdateMgr.IsCompress)
                    {
                        compressloadMB += (fi.compressSize / 1024f);
                        AllNeedLoadByts += fi.compressSize;
                    }
                    else
                    {
                        AllNeedLoadByts += fi.sizeKB;
                    }
                    needDownloadMB += (fi.sizeKB / 1024f);
                }
                needMB = needDownloadMB + 50f;
                isEnoughSpace = remainMB > needMB;
                if (UpdateMgr.IsCompress)
                {
                    needDownloadMB = compressloadMB;
                }
            }
            catch (Exception e)
            {
                UpdateMgr.LogEHelper(e);
                UpdateMgr.OnWriteDiskError(Codel18nUtils.Get("缓存文件夹")); //缓存文件夹
            }
            //UI上最小显示下载数据为0.01MB大小
            var uiNeedDownloadMB = 0.01f;
            if (uiNeedDownloadMB < needDownloadMB)
            {
                uiNeedDownloadMB = needDownloadMB;
            }
            var res =
                UIMessageBoxManager.Instance.ShowMessageBox_OK(Codel18nUtils.Get(""),
                    string.Format(Codel18nUtils.Get("本次更新需要下载 {0} mb文件内容,是否继续?")
                        , uiNeedDownloadMB.ToString("f2"), remainMB),
                       ""
                    ); //本次更新需要下载 {0} mb文件内容,是否继续?
            yield return res;
            if (isEnoughSpace)
            {
                int bundleNum = needDownloadFiles.Count;
                LoadRepeatIndex = 5;
                //下载更新 
                for (int i = 0; i < bundleNum; i++)
                {
                    if (LoadRepeatIndex <= 1)
                    {
                        break;
                    }
                    var file = needDownloadFiles[i];

                    var filename = file.fileName;
                    if (UpdateMgr.IsCompress)
                    {
                        CurNeedLoadByts = file.compressSize;
                    }
                    else
                    {
                        CurNeedLoadByts = file.sizeKB;
                    }
                    if (string.IsNullOrEmpty(file.RealFileName))
                    {
                        file.RealFileName = filename;
                    }
                    if (filename.IndexOf(DLLName) != -1)
                    {
                        CurLoadFileName = file.fileMD5;
                        view.Set_Update_Text(string.Format(Codel18nUtils.Get("更新文件{0}"), file.fileMD5)); 
                    }
                    else
                    {
                        CurLoadFileName = file.RealFileName;
                        view.Set_Update_Text(string.Format(Codel18nUtils.Get("更新文件{0}"), filename));
                    }
                    NativeAPIProxy.Instance.ShowPermiss();
                    
                    yield return LoadAssetBundleSingle(webresp, filename, bundleCachePath, file);
                }
            }
            else
            {
                //空间不足
                UpdateMgr.OnNoEnoughSpaceError(needMB);
            }
        }
    }
    //单个下载bundle
    private IEnumerator LoadAssetBundleSingle(WebServerGroupInfo webresp, string filename, string bundleCachePath,
        AssetBundleUpdateInfo.AssetBundleUpdateFileInfo file)
    {
        if (LoadRepeatIndex <= 0)
        {
            UpdateMgr.OnDownloadError();
        }
        else
        {
            List<byte[]> _list = new List<byte[]>();
            byte[] _curByt = null;
            if (filename.IndexOf(FontBundleName) != -1 && FontBundleByts != null && FontBundleByts.Length > 0)
            {
                _curByt = FontBundleByts;
                _list.Add(FontBundleByts);
            }
            else
            {
                string LoadPath = AssetBundlePathResolver.GetWebBundlePath(webresp.updateFilePath, filename) + "?v=" +
                                  file.fileMD5 + UnityEngine.Random.Range(1, 1000);
                WWW www = new WWW(LoadPath);
                BytsWWW = www;
                IsBytsLoading = true;
                yield return www;
                if (string.IsNullOrEmpty(www.error))
                {
                    try
                    {
                        var filePath = bundleCachePath + filename;
                        //如果存在原文件，则删除
                        if (File.Exists(@filePath))
                        {
                            File.Delete(@filePath);
                        }
                    }
                    catch (Exception e)
                    {
                        UpdateMgr.LogEHelper(e);
                    }
                    yield return null;
                    _curByt = www.bytes;
                    _list.Add(www.bytes);
                }
                else
                {
#if UNITY_EDITOR
                    Log.Error(www.error);
#endif
                    LoadRepeatIndex--;
                    yield return LoadAssetBundleSingle(webresp, filename, bundleCachePath, file);
                }
            }
            if (_list.Count > 0 && _curByt != null)
            {
                UnCompressAll = false;
              
                    DownLoadAsyncHelper(_list, bundleCachePath + filename, _curByt, file.fileMD5);
               
                if (UpdateMgr.IsCompress && filename.IndexOf(AssetBundlePathResolver.DependFileName) == -1)
                {
                    yield return IsUncompressAll();
                }
                //等待0.3秒避免文件读写冲突
                yield return new WaitForSeconds(0.3f);
                if (BytsWWW != null)
                {
                    BytsWWW.Dispose();
                    BytsWWW = null;
                    CurLoadBtys += CurNeedLoadByts;
                }
                yield return new WaitForSeconds(0.1f);
                //MD5校验
                if (!AssetBundleManager.Instance.CheckBundleUpdateFileExistCorrect(file))
                {
                    if (LoadRepeatIndex <= 1)
                    {
                        if (NativeAPIProxy.Instance.GetSDExit() == false)
                        {
                            UpdateMgr.OnWriteDiskError(Codel18nUtils.Get("SD卡")); 
                        }
                        else
                        {
                            UpdateMgr.OnWriteDiskError(CurLoadFileName);
                        }
                    }
                    else
                    {

                        if (File.Exists(bundleCachePath + filename))
                        {
                            File.Delete(bundleCachePath + filename);
                        }

                        CurLoadBtys -= CurNeedLoadByts;
                        LoadRepeatIndex--;
                        yield return null;
                        yield return LoadAssetBundleSingle(webresp, filename, bundleCachePath, file);
                    }
                }
                else
                {
                    CurNeedLoadByts = 0;
                    LoadRepeatIndex = 5;
                    LastLoadByts = 0;
                    IsBytsLoading = false;
                    yield return null;
                }
            }
        }
    }

    //是否全部解压完成
    private IEnumerator IsUncompressAll()
    {
        while (UnCompressAll == false)
        {
            yield return null;
        }
    }
    //辅助写入磁盘
    private void DownLoadAsyncHelper(List<byte[]> fragments, string savepath, byte[] _bytes, string Md5)
    {
        //写入磁盘 
        try
        {
            if (fragments != null)
            {
                if (UpdateMgr.IsCompress && savepath.IndexOf(AssetBundlePathResolver.DependFileName) == -1)
                {
                    ByteFile _byteFile = new ByteFile();
                    _byteFile.PathName = savepath;
                    _byteFile.Bytes = _bytes;
                    _byteFile.FileMd5 = Md5;
                    //GetABFileThread(_byteFile);
                    ParameterizedThreadStart _start = new ParameterizedThreadStart(GetABFileThread);
                    Thread _itemThread = new Thread(_start);
                    _itemThread.IsBackground = true;
                    _itemThread.Start(_byteFile);
                }
                else
                {
                    try
                    {
                        File.WriteAllBytes(savepath, _bytes);
                    }
                    catch (Exception)
                    {
                    }
                    UnCompressAll = true;
                }
            }
        }
        catch (Exception e)
        {
            UpdateMgr.LogEHelper(e);
            UpdateMgr.OnWriteDiskError("@" + savepath);
        }
    }
    //多线程解压ab文件
    void GetABFileThread(object _path)
    {
        ByteFile _byteFile = _path as ByteFile;
        if (_byteFile != null)
        {
            byte[] _allByte = null;
            try
            {
                _allByte = lzma.decompressBuffer(_byteFile.Bytes);
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                CommonLog.Error(e.Message);
#endif
            }
            if (_allByte != null && _allByte.Length > 0)
            {
                if (Lua_API_Helper.MD5Stream(Lua_API_Helper.BytesToStream(_allByte)) == _byteFile.FileMd5)
                {
                    try
                    {
                        File.WriteAllBytes(_byteFile.PathName, _allByte);
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }
        UnCompressAll = true;
    }
    // Update is called once per frame
    public void UpdateHandler()
    {
        if (IsBytsLoading && AllNeedLoadByts > 0)
        {
            if (BytsWWW != null && CurNeedLoadByts > 0)
            {
                float _process = CurNeedLoadByts * BytsWWW.progress;

                CurLoadProcess = (float)(CurLoadBtys + _process) / AllNeedLoadByts;
                if (CurLoadProcess > 1)
                {
                    CurLoadProcess = 1;
                }
                UpdateMgr.SetViewProgress(UILoginManager.Instance.View, CurLoadProcess);
                //UILoginManager.Instance.View.Set_Update_Text(string.Format("更新文件{0}", CurLoadFileName + " " + Math.Round(CurLoadProcess * 100, 1) + "%"+" "+ _loadbyts+"K/s"));
                if (CurLoadProcess < 1)
                {
                    if (GetProcessFrame <= 0 || (CurLoadSpeed <= 0 && GetProcessFrame <= 30))
                    {
                        double _loadbyts = Math.Round((_process - LastLoadByts) * 16);
                        CurLoadSpeed = _loadbyts;
                        GetProcessFrame = 35;
                    }
                    UILoginManager.Instance.View.Set_Update_Text(string.Format(Codel18nUtils.Get("更新文件{0} {1}% {2}K/s"), CurLoadFileName, Math.Round(CurLoadProcess * 100, 1), CurLoadSpeed)); 
                }
                GetProcessFrame--;

                //UILoginManager.Instance.View.Set_Update_Text(string.Format("更新文件{0}", CurLoadFileName + "  " + Math.Round(CurLoadProcess * 100, 1) + "%"));
                LastLoadByts = _process;
            }
            else
            {
                LastLoadByts = 0;
                CurLoadProcess = (float)CurLoadBtys / AllNeedLoadByts;
                if (CurLoadProcess > 1)
                {
                    CurLoadProcess = 1;
                }
                UpdateMgr.SetViewProgress(UILoginManager.Instance.View, CurLoadProcess);
                UILoginManager.Instance.View.Set_Update_Text(string.Format(Codel18nUtils.Get("更新文件{0}  {1}%"), CurLoadFileName, Math.Round(CurLoadProcess * 100, 1)));
            }
        }
    }
}
//多线程字节数据存储模型
public class ByteFile
{
    //保存路径
    public string PathName;
    //字节数据
    public byte[] Bytes;
    //Md5
    public string FileMd5;
}

    */
