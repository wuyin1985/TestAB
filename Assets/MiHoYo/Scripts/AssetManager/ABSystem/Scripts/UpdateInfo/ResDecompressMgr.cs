//using YZL.Compress.LZMA;
/**
//安装包里压缩包进行解压控制器
public class ResDecompressMgr : MonoBehaviour
{
    //放在streamingAsset下面的更新文件
    public AssetBundleUpdateInfo StreamUpdate;
    //放在streamingAsset下面的更新文件的MD5
    public string StreamUpdateMd5;
    //放在stream下面的更新文件的文本
    public string StreamUpdateXml;
    //存储放在是stream下面的更新文件的更新信息
    public Dictionary<string, string> StreamUpdateDic = new Dictionary<string, string>();
    //存储着需要更新的bundle
    public static Dictionary<string, string> StreamNotUpdate = new Dictionary<string, string>();
    //是否资源在resource中
    public static bool IsAllResources = false;
    //是否拷貝所有的文件
    public static bool IsCopyAllAB = false;
    //全部解压更新文件信息
    public AssetBundleUpdateInfo CompressStreamUpdate;
    //当前解压到何处
    public int CurIndexUncompress = 0;
    //是否解压完成
    public bool IsUncompressComplete = false;
    //真正需要解压的数量
    public int RealLoad = 0;
    //是否是第一次解压
    public bool IsFirstUncompress = false;
    //放在streamingAsset下面的压缩更新文件的MD5
    public string CompressUnpdateMd5 = "";
    //资源更新控制器
    public static ResUpdateManager UpdateMgr;
    //解压的总数量
    private int LoadCount = 0;
    //streamingAsset目录地址
    private string StreamingPath = "";
    //persisten目录地址
    private string PersistenPath = "";
    //第一次需要解压的场景
    public List<string> FirstUncompress = null;
    //检测列表
    private List<AssetBundleUpdateInfo.AssetBundleUpdateFileInfo> CheckList = new List<AssetBundleUpdateInfo.AssetBundleUpdateFileInfo>();
    //开始检测
    private bool CheckBegin = false;
    //检测进度
    private float CheckIndex = 0;
    //检测的总长度
    private int CheckNum = 0;
    //是否完全解压完成检测
    private bool IsCheckRealComplete = false;
    //当前保存的AB位置
    private string CurSaveABPath = "";
    //当前剩余的空间
    private static float DiskRemainMB = 0;
    //是否在解压中
    private bool ISUnCompressing = false;
    //加载的上限
    private int UncompressNum = 2;
    //当前加载的次数
    private int CurCompressNum = 0;
    //保存解压数据信息
    private static Dictionary<string, AssetBundleUpdateInfo.AssetBundleUpdateFileInfo> InfoDic = new Dictionary<string, AssetBundleUpdateInfo.AssetBundleUpdateFileInfo>();

    //设置解压缩次数上线，避免出现解压内存过大而闪退
    private void SetUnompressNum()
    {
#if UNITY_IOS
        if (NativeAPIProxy.Instance.IsMoreOneG)
        {
            UncompressNum = 12;
        }
#endif     
    }

    //拷贝并解压所有文件
    public IEnumerator CopyAllCompressFiles()
    {
        SetUnompressNum();
        DiskRemainMB = NativeAPIProxy.Instance.GetCachePathRemainStorageSizeMB();
        InfoDic = new Dictionary<string, AssetBundleUpdateInfo.AssetBundleUpdateFileInfo>();
        IsCheckRealComplete = false;
        FirstUncompress = new List<string>();
        LoadCount = 0;
        yield return new WaitForSeconds(1);
        StreamingPath = AssetBundlePathResolver.GetStreamingBundlePath("");
        PersistenPath = AssetBundlePathResolver.GetBundleSourceFile("");
        //拷贝更新文件
        string infoPath = AssetBundlePathResolver.GetStreamingBundlePath(AssetBundlePathResolver.UpdateInfoFileName);
        yield return GetUpdateInfoMd5(infoPath);
        //拷贝更新信息文件
        string infoSavePath = AssetBundlePathResolver.GetBundleSourceFile(AssetBundlePathResolver.UpdateInfoFileName);
        yield return CopyXmlToPersisten(infoPath, infoSavePath, true);
        //是否空间满足
        foreach (var fi in CompressStreamUpdate.AssetBundleInfoList)
        {
            //needDownloadMB += (fi.sizeKB / 1024f);
            if (!InfoDic.ContainsKey(fi.fileName))
            {
                InfoDic.Add(fi.fileName, fi);
            }
        }
        if (!string.IsNullOrEmpty(CompressUnpdateMd5) && (Directory.GetFiles(AssetBundlePathResolver.GetBundleCacheDir()).Length <= 0 || CheckPrefsMd5() == false))
        {
            if (Directory.GetFiles(AssetBundlePathResolver.GetBundleCacheDir()).Length > 0)
            {
                FileUtils.DeleteFiles(AssetBundlePathResolver.GetBundleCacheDir(), SearchOption.AllDirectories,"");
            }
            IsFirstUncompress = true;
            PlayerPrefs.DeleteKey("FirstResUncompress"+NativeAPIProxy.Instance.GetAppVersion());
            DecodeMainfest();
            yield return GetFirstDecodeData(false);
            UILoginManager.Instance.View.Set_Update_State(UI_Login_ViewFunc.eUpdateState.Loading);
            UpdateMgr.SetViewProgress(UILoginManager.Instance.View, 0);
            UILoginManager.Instance.View.Set_Update_Text(Codel18nUtils.Get("正在解压资源中(不消耗流量)...")); 
          
            LoadCount = FirstUncompress.Count;
            yield return IThreadDecode(CompressStreamUpdate.AssetBundleInfoList, false);
        }
        else
        {
            yield return CheckBundle();
            //UpdateMgr.StartUpdateRes();
        }
    }
    //获取压缩后的文件大小
    public static int GetFileInfoSize(string _name)
    {
        if (InfoDic.ContainsKey(_name))
        {
            return InfoDic[_name].compressSize;
        }
        return 0;
    }
    //检测空间是否充足
    public static bool CheckDiskEnough(string _name, bool isPop = true)
    {
        bool isEnoughSpace = true;
        float remainMB = 0;
        float needDownloadMB = 0;
        float needMB = 0;
        try
        {
            if (InfoDic.ContainsKey(_name))
            {
                //remainMB = NativeAPIProxy.Instance.GetCachePathRemainStorageSizeMB();
                needDownloadMB += (InfoDic[_name].sizeKB / 1024f);
                needMB = needDownloadMB + 10f;
                isEnoughSpace = DiskRemainMB > needMB;
                DiskRemainMB -= needDownloadMB;
            }
        }
        catch (Exception e)
        {
            UpdateMgr.LogEHelper(e);
            UpdateMgr.OnWriteDiskError(Codel18nUtils.Get("缓存文件夹"));
        }
        if (!isEnoughSpace && isPop)
        {
            //空间不足
            UpdateMgr.OnNoEnoughSpaceError(needMB);
        }
        return isEnoughSpace;
    }

    //是否解壓過了
    public static bool IsDecompressDone
    {
        get
        {
            if (IsCopyAllAB && string.IsNullOrEmpty(PlayerPrefs.GetString("FirstResUncompress"+NativeAPIProxy.Instance.GetAppVersion())))
            {
                return false;
            }
            return true;
        }
    }
    //如果解压过，则进行检测MD5
    IEnumerator CheckBundle()
    {
        LoadCount = 0;
        CheckBegin = true;
        yield return GetFirstDecodeData(true);
        UpdateMgr.SetViewProgress(UILoginManager.Instance.View, 0);
        UILoginManager.Instance.View.Set_Update_Text(Codel18nUtils.Get("正在检查资源中(不消耗流量)..."));
        string _path = AssetBundlePathResolver.GetStreamingBundlePath(AssetBundlePathResolver.UpdateInfoFileName);
        ParameterizedThreadStart _start = new ParameterizedThreadStart(ThreadCheck);
        Thread _itemThread = new Thread(_start);
        string _txt = GetResByStreaming.Instance.GetStringAllPlatform(_path);
        _itemThread.IsBackground = true;
        _itemThread.Start(_txt);
        while (CheckBegin)
        {
            yield return null;
        }
        if (LoadCount > 0)
        {
            UpdateMgr.SetViewProgress(UILoginManager.Instance.View, 0);
            UILoginManager.Instance.View.Set_Update_Text(Codel18nUtils.Get("正在解压资源中(不消耗流量)...")); 
            yield return IThreadDecode(CheckList, true);

        }
        yield return null;
    }
    //多线程检测
    void ThreadCheck(object _path)
    {
        List<AssetBundleUpdateInfo.AssetBundleUpdateFileInfo> _list = new List<AssetBundleUpdateInfo.AssetBundleUpdateFileInfo>();
        string _txt = (string)_path;
        if (!string.IsNullOrEmpty(_txt))
        {
            AssetBundleUpdateInfo xml = _txt.FromXML<AssetBundleUpdateInfo>();
            if (xml != null)
            {
                int bundleNum = xml.AssetBundleInfoList.Count;
                CheckNum = bundleNum;
                //检查xml对应文件
                for (int i = 0; i < bundleNum; i++)
                {
                    AssetBundleUpdateInfo.AssetBundleUpdateFileInfo _info = xml.AssetBundleInfoList[i];
                    CheckIndex = (float)i;
                    if (_info.fileName == AssetBundlePathResolver.DependFileName)
                    {
                        continue;
                    }
                    string path = "";
                    if (_info.fileName == AssetBundlePathResolver.AssetbundleManifestName)
                    {
                        path = PersistenPath + "streaming/" + _info.fileName;
                    }
                    else
                    {
                        path = PersistenPath + _info.fileName;
                    }
                    if (!AssetBundleManager.Instance.ServerVerBundle.Contains(_info.fileName) && (FirstUncompress.Contains(_info.fileName)))
                    {
                        if (!AssetBundleManager.Instance.CheckBunldeByPath(path, _info.fileMD5))
                        {
                            LoadCount++;
                            _list.Add(_info);
                        }
                    }
                }
            }
        }
        CheckList = _list;
        CheckBegin = false;
    }
    //先解析主mainfest，不然无法获取首次解压的数据
    void DecodeMainfest()
    {
        string _streamingPath = AssetBundlePathResolver.GetBundleSourceFile(
                    "streaming/" + AssetBundlePathResolver.AssetbundleManifestName, false);
        if (!File.Exists(_streamingPath))
        {
            string _path = AssetBundlePathResolver.GetStreamingBundlePath(
            AssetBundlePathResolver.AssetbundleManifestName);
            byte[] _byts = GetResByStreaming.Instance.GetByteAllPlatform(_path);
            if (_byts != null)
            {
                byte[] _allByte = null;
                _allByte = lzma.decompressBuffer(_byts);
                if (_allByte != null && _allByte.Length > 0)
                {
                    AssetBundlePathResolver.CreatDependDir();
                    File.WriteAllBytes(_streamingPath, _allByte);
                    AssetBundleManager.Instance.LoadStreamingMainfest(true);
                }
            }
        }
        else
        {
            AssetBundleManager.Instance.LoadStreamingMainfest(true);
        }
    }
    //获取首次解压的数据
    IEnumerator GetFirstDecodeData(bool isCheck)
    {
        string _firstArr = null;
        TextAsset _firstTxt = Resources.Load("DecodeFirst/DecodeFirst") as TextAsset;
        if (_firstTxt != null)
        {
            _firstArr = _firstTxt.text;
            Resources.UnloadAsset(_firstTxt);
        }
        string xmlPath = AssetBundlePathResolver.GetStreamingBundlePath(AssetBundlePathResolver.DependFileName);
        string _str = GetResByStreaming.Instance.GetStringAllPlatform(xmlPath);
        string[] _depends = null;
        List<string> leftList = new List<string>();
        AssetBundleManager.Instance.LoadStreamingMainfest(true);
        if (!string.IsNullOrEmpty(_str))
        {
            var table = _str.FromXML<AssetBundleTable>();
            int _index = 0;
            if (table != null)
            {
                if (isCheck)
                {
                    int _len = table.AssetBundleInfoList.Count;
                    for (int a = 0; a < _len; a++)
                    {
                        AssetBundleTable.AssetBundleXMLData _data = table.AssetBundleInfoList[a];
                        if (
                            _data.assetFullName.IndexOf(
                                @"Development\Script\Scripts\PBStaticConfigure\Resources\PBStaticConfigure") != -1)
                        {
                            if (!FirstUncompress.Contains(_data.bundleName))
                            {
                                FirstUncompress.Add(_data.bundleName);
                            }
                            _depends = AssetBundleManager.Instance.GetDependencies(_data.bundleName);
                            int _dependslen = _depends.Length;
                            for (int c = 0; c < _dependslen; c++)
                            {
                                if (!FirstUncompress.Contains(_depends[c]))
                                {
                                    FirstUncompress.Add(_depends[c]);
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (var x in table.AssetBundleInfoList)
                    {
                        if (!FirstUncompress.Contains(x.bundleName))
                        {
                            FirstUncompress.Add(x.bundleName);
                        }
                        _depends = AssetBundleManager.Instance.GetDependencies(x.bundleName);
                        int _len = _depends.Length;
                        for (int a = 0; a < _len; a++)
                        {
                            if (!FirstUncompress.Contains(_depends[a]))
                            {
                                FirstUncompress.Add(_depends[a]);
                            }
                        }
                        _index++;
                        if (_index > 40)
                        {
                            _index = 0;
                            yield return null;
                        }
                    }
                    if (_firstArr != "0")
                    {
                        foreach (var y in table.AssetBundleSceneList)
                        {
                            if ((string.IsNullOrEmpty(_firstArr) || _firstArr.IndexOf(y.assetFullName) != -1))
                            {
                                if (!FirstUncompress.Contains(y.bundleName))
                                {
                                    FirstUncompress.Add(y.bundleName);
                                }
                                _depends = AssetBundleManager.Instance.GetDependencies(y.bundleName);
                                int _len = _depends.Length;
                                for (int a = 0; a < _len; a++)
                                {
                                    if (!FirstUncompress.Contains(_depends[a]))
                                    {
                                        leftList.Add(_depends[a]);
                                        FirstUncompress.Add(_depends[a]);
                                    }
                                }
                            }
                            else
                            {
                                _depends = AssetBundleManager.Instance.GetDependencies(y.bundleName);
                                int _len = _depends.Length;
                                for (int a = 0; a < _len; a++)
                                {
                                    if (FirstUncompress.Contains(_depends[a]) && !leftList.Contains(_depends[a]))
                                    {
                                        FirstUncompress.Remove(_depends[a]);
                                    }
                                }
                            }
                            _index++;
                            if (_index > 40)
                            {
                                _index = 0;
                                yield return null;
                            }
                        }
                    }
                }
            }
        }
        yield return null;
    }
    //多线程检测是否需要下载或者本地读取
    IEnumerator IThreadDecode(List<AssetBundleUpdateInfo.AssetBundleUpdateFileInfo> uncompressList, bool isCheck)
    {
        int indexCount = 0;
        RealLoad = LoadCount;
        CurCompressNum = 0;
        foreach (
                    AssetBundleUpdateInfo.AssetBundleUpdateFileInfo _info in uncompressList)
        {
            if (_info.fileName == AssetBundlePathResolver.DependFileName)
            {
                continue;
            }
            if (isCheck == false)
            {
                if (!FirstUncompress.Contains(_info.fileName))
                {
                    continue;
                }
            }
            if (!CheckDiskEnough(_info.fileName))
            {
                //break;
                while (true)
                {
                    yield return null;
                }
            }
#if UNITY_IOS
            while (CurCompressNum >= UncompressNum)
            {
                yield return null;
            }
            CurCompressNum++;
#endif
#if UNITY_ANDROID
            while (CurCompressNum >= UncompressNum)
            {
                yield return null;
            }
            CurCompressNum++;
#endif
            Thread.Sleep(1);
            string abPath = StreamingPath + _info.fileName;
            string abSavePath = PersistenPath + _info.fileName;
            if (_info.fileName == AssetBundlePathResolver.AssetbundleManifestName)
            {
                abSavePath = PersistenPath + "streaming/" + _info.fileName;
            }
            //RealLoad++;
            indexCount++;
            string abStreamingPath = AssetBundlePathResolver.GetBundleStreamingUrl(_info.fileName);
            if (ResZipRead.Instance.Exist(abStreamingPath) == false)
            {
                CurSaveABPath = abSavePath;
                yield return
                        ABDecodeFromStream.Instance.LoadByPath(
                            AssetBundlePathResolver.GetWebCacheBundlePath(AssetBundlePathResolver.ServiceBundlePath,
                                _info.fileName) + "?v=" + UnityEngine.Random.Range(1, 10000) + "" + Global.time, 5, LoadDecodeOut);
            }
            else
            {
                ISUnCompressing = true;
                LZMADecodeOut(abPath, abSavePath);
#if UNITY_ANDROID
                if (indexCount > 2)
                {
                    indexCount = 0;
                    yield return new WaitForEndOfFrame();
                }
#endif
            }
        }
        while (CurCompressNum>0)
        {
            yield return null;
        }
        IsUncompressComplete = true;
        while (IsCheckRealComplete == false)
        {
            yield return null;
        }
        yield return null;
    }
    //通过服务器加载模式更新bundle
    void LoadDecodeOut(byte[] _data)
    {
        if (_data != null)
        {
            ByteFile _byteFile = new ByteFile();
            _byteFile.PathName = CurSaveABPath;
            _byteFile.Bytes = _data;
            //GetABFileThread(_byteFile);
            ParameterizedThreadStart _start = new ParameterizedThreadStart(GetABFileThread);
            Thread _itemThread = new Thread(_start);
            _itemThread.IsBackground = true;
            _itemThread.Start(_byteFile);
        }
        else
        {
            UpdateMgr.OnDownloadError();
        }
    }
    //走新的解压缩导出
    void LZMADecodeOut(string filePath, string savepath)
    {
        byte[] fileBytes = null;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        fileBytes = File.ReadAllBytes(filePath);
#elif UNITY_ANDROID
        fileBytes = GetResByStreaming.Instance.GetByteByFile(filePath);
#else
        if (File.Exists(filePath))
        {
            fileBytes = File.ReadAllBytes(filePath);
        }
#endif
        if (fileBytes == null)
        {
#if UNITY_EDITOR
            CommonLog.Error("bytes null:" + filePath);
#endif
        }
        else
        {
            ByteFile _byteFile = new ByteFile();
            _byteFile.PathName = savepath;
            _byteFile.Bytes = fileBytes;
            ParameterizedThreadStart _start = new ParameterizedThreadStart(GetABFileThread);
            Thread _itemThread = new Thread(_start);
            _itemThread.IsBackground = true;
            _itemThread.Start(_byteFile);
        }
    }
    //多线程解压AB
    void GetABFileThread(object _path)
    {
        ByteFile _byteFile = _path as ByteFile;
        if (_byteFile != null && _byteFile.Bytes!=null && _byteFile.Bytes.Length>0)
        {
            byte[] _allByte = null;
            _allByte = lzma.decompressBuffer(_byteFile.Bytes);
            if (_allByte != null && _allByte.Length > 0)
            {
                File.WriteAllBytes(_byteFile.PathName, _allByte);
            }
            CurIndexUncompress++;
        }
        CurCompressNum--;
        ISUnCompressing = false;
    }
    //解壓並拷貝單個文件
    IEnumerator CopyCompressOut(string filePath, string savepath)
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        WWW _www = new WWW("file://" + filePath);
#else
         WWW _www = new WWW(filePath);
#endif
        yield return _www;
        if (string.IsNullOrEmpty(_www.error))
        {
            Stream _stream = new MemoryStream();
            _stream.Write(_www.bytes, 0, _www.bytes.Length);
            _stream.Position = 0;
            //解压资源
            LZMAFile.UnCompressByStreamAsync(_stream, savepath, UncompresAsyn);
            for (int a = 0; a < 4; a++)
            {
                yield return null;
            }
        }
        else
        {
#if UNITY_EDITOR
            Log.Error(_www.error);
#endif
        }
        _www.Dispose();
        _www = null;
    }
    //异步解压返回
    private void UncompresAsyn(long data1, long data2)
    {
        if (data1 <= data2)
        {
            CurIndexUncompress++;
        }
    }
    //拷貝xml文件
    IEnumerator CopyXmlToPersisten(string filePath, string savepath, bool isUpdate)
    {
        string _str = GetResByStreaming.Instance.GetStringAllPlatform(filePath);
        if (!string.IsNullOrEmpty(_str))
        {
            //File.WriteAllText(savepath, _str);
            if (isUpdate)
            {
                CompressStreamUpdate = _str.FromXML<AssetBundleUpdateInfo>();
            }
        }
        yield return new WaitForEndOfFrame();
    }
    //获取更新文件MD5
    IEnumerator GetUpdateInfoMd5(string path)
    {
        byte[] _byts = GetResByStreaming.Instance.GetByteAllPlatform(path);
        if (_byts != null && _byts.Length > 0)
        {
            CompressUnpdateMd5 = Lua_API_Helper.MD5Stream(Lua_API_Helper.BytesToStream(_byts));
        }
        yield return new WaitForEndOfFrame();
    }
    //检测初始游戏是否解压完成
    public static void CheckFirstCompress()
    {
        if (IsCopyAllAB && string.IsNullOrEmpty(PlayerPrefs.GetString("FirstResUncompress"+NativeAPIProxy.Instance.GetAppVersion())))
        {
            string path = AssetBundlePathResolver.GetBundleSourceFile("");
#if UNITY_IOS
            path+= "streaming/iOS";
#endif
#if UNITY_ANDROID
            path+="streaming/Android";
#endif
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception)
            {

            }
        }
    }

    //检测两边更新文件的MD5是否相同
    private bool CheckPrefsMd5()
    {
        if (string.IsNullOrEmpty(PlayerPrefs.GetString("FirstResUncompress"+NativeAPIProxy.Instance.GetAppVersion())))
        {
            return false;
        }
        else
        {
            if (PlayerPrefs.GetString("FirstResUncompress"+NativeAPIProxy.Instance.GetAppVersion()) != CompressUnpdateMd5)
            {
                return false;
            }
        }
        return true;
    }
    //帧频调用
    public void UpdateHandler()
    {
        if (IsUncompressComplete)
        {
            if (CurIndexUncompress >= RealLoad - 1)
            {
                //UIMessageBoxManager.Instance.ShowMessageBox_OK(string.Format("解压时间花费了 {0} 毫秒！", ProfileTimeUtil.instance.stopTime(), 0));
                //CommonLog.LogFormat(string.Format("解压时间花费了 {0} 毫秒！", ProfileTimeUtil.instance.stopTime(), 0));
                LoadCount = 0;
                UpdateMgr.SetViewProgress(UILoginManager.Instance.View, 0.99f);
                IsUncompressComplete = false;
                PlayerPrefs.SetString("FirstResUncompress"+NativeAPIProxy.Instance.GetAppVersion(), CompressUnpdateMd5);
                PlayerPrefs.Save();
                AssetBundleManager.Instance.ClearAllData();
                AssetBundleManager.Instance.ClearMainFest();
                AssetBundleManager.Instance.LoadAssetBundleConfig();
                UILoginManager.Instance.View.Set_Update_State(UI_Login_ViewFunc.eUpdateState.Loading);
                UpdateMgr.SetViewProgress(UILoginManager.Instance.View, 0);
                IsCheckRealComplete = true;
                //UpdateMgr.StartUpdateRes();
            }
        }
        else
        {
            if (CheckBegin && CheckNum > 0)
            {
                UpdateMgr.SetViewProgress(UILoginManager.Instance.View, CheckIndex / CheckNum);
            }
            if (LoadCount > 0)
            {
                float _process = (float)CurIndexUncompress / LoadCount;
                if (_process > 0.99)
                {
                    _process = 0.99f;
                }
                UILoginManager.Instance.View.Set_Update_Text(Codel18nUtils.Get("正在解压资源中(不消耗流量)...") + Math.Round(_process * 100, 1) + "%");
                UpdateMgr.SetViewProgress(UILoginManager.Instance.View, _process);
            }
        }
    }

    //存储stream下面的更新文件信息
    private void ParseStreamUpdate()
    {
        foreach (AssetBundleUpdateInfo.AssetBundleUpdateFileInfo _info in StreamUpdate.AssetBundleInfoList)
        {
            StreamUpdateDic.Add(_info.fileName, _info.fileMD5);
            StreamNotUpdate.Add(_info.fileName, _info.fileMD5);
        }
    }
}

    */
