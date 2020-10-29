/**
//AB从streamingAsset解压到persistent
public class ABDecodeFromStream : UnitySingleton<ABDecodeFromStream>
{
    //是否解压完成
    private bool DecodeComplete = false;
    //当前需要保存的AB地址
    private string CurSaveABPath = "";
    //当前正在执行的加载器
    private WWW _CurWww;
    //当前需要加载的字节数
    private int CurNeedLoad = 0;
    //总共需要下载的字节数
    private int AllNeedLoad = 0;
    //当前下载的进度
    public float CurLoadProcess = 0;
    //先前下载的字节数
    public float LastLoadByts = 0;
    //间隔多久检测一次网速
    private int GetProcessFrame = 0;
    //当前加载的速度
    private double CurLoadSpeed = 0;
    //当前已经加载的字节数
    private float CurLoadBtys = 0;

    //检测AB是否已经解压
    public IEnumerator CheckABDecode(string[] fileData)
    {
        int _len = fileData.Length;
        bool _isLoad = false;
        GetAllNeedLoad(fileData);
        CurLoadBtys = 0;
        for (int a = 0; a < _len; a++)
        {
            if (string.IsNullOrEmpty(fileData[a]))
            {
                continue;
            }
            string abPath = AssetBundlePathResolver.GetBundleStreamingUrl(fileData[a]);
            string abSavePath = AssetBundlePathResolver.GetBundleSourceFile(fileData[a]);
            if (!File.Exists(abSavePath))
            {
                _isLoad = true;
                DecodeComplete = false;
                if (!ResDecompressMgr.CheckDiskEnough(fileData[a], false))
                {
                    StopAllCoroutines();
                    UIMessageBoxManager.Instance.ShowMessageBox_OK(string.Format(LocalizationUtils.Get("ABDecodeFromStream_1539333705512"))) //存储空间不足
                        .AddOnSelectOKOrYes(result =>
                        {
                            ApplicationUtils.Quit();
                        });
                    while (true)
                    {
                        yield return null;
                    }
                }
                else
                {
                    if (ResZipRead.Instance.Exist(abPath))
                    {
                        LZMADecodeOut(abPath, abSavePath);
                    }
                    else
                    {
                        LastLoadByts = 0;
                        CurSaveABPath = abSavePath;
                        CurNeedLoad = ResDecompressMgr.GetFileInfoSize(fileData[a]);
                        yield return
                                LoadByPath(
                                    AssetBundlePathResolver.GetWebCacheBundlePath(AssetBundlePathResolver.ServiceBundlePath,
                                        fileData[a]) + "?v=" + UnityEngine.Random.Range(1, 10000) + "" + Global.time, 8, LoadDecodeOut);
                    }
                    while (DecodeComplete == false)
                    {
                        yield return null;
                    }
                }
            }
        }
        CurNeedLoad = 0;
        _CurWww = null;
        if (_isLoad)
        {
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void GetAllNeedLoad(string[] fileData)
    {
        AllNeedLoad = 0;
        int _len = fileData.Length;
        bool _isLoad = false;
        for (int a = 0; a < _len; a++)
        {
            if (string.IsNullOrEmpty(fileData[a]))
            {
                continue;
            }
            AllNeedLoad += ResDecompressMgr.GetFileInfoSize(fileData[a]);
        }
    }

    //通过服务器加载模式更新bundle
    void LoadDecodeOut(byte[] _data)
    {
        if (_data != null)
        {
            ByteFile _byteFile = new ByteFile();
            _byteFile.PathName = CurSaveABPath;
            _byteFile.Bytes = _data;
            ParameterizedThreadStart _start = new ParameterizedThreadStart(GetABFileThread);
            Thread _itemThread = new Thread(_start);
            _itemThread.IsBackground = true;
            _itemThread.Start(_byteFile);
        }
        else
        {
            StopAllCoroutines();
            UIMessageBoxManager.Instance.ShowMessageBox_OK(LocalizationUtils.Get("ABDecodeFromStream_1539333714239")).AddOnSelectOKOrYes(result => //网络异常,请重试
            {
                NativeAPIProxy.Instance.RestartGame();
            });
        }
    }
    //按照次数来加载对应的资源
    public IEnumerator LoadByPath(string _path, int _times, Action<byte[]> _callback)
    {
        WWW _www = new WWW(_path);
        _CurWww = _www;
        yield return _www;
        if (!string.IsNullOrEmpty(_www.error))
        {
            if (_times > 0)
            {
                _www.Dispose();
                yield return LoadByPath(_path + "" + UnityEngine.Random.Range(1, 1000), _times - 1, _callback);
            }
            else
            {
#if UNITY_EDITOR
                CommonLog.Error(_www.error + " , " + _www.url);
#endif
                _www.Dispose();
                _callback.Invoke(null);
            }
        }
        else
        {
            CurLoadBtys += _www.bytes.Length;
            _callback.Invoke(_www.bytes);
            _www.Dispose();
        }
    }
    //帧调用
    void Update()
    {
        if (_CurWww != null && CurNeedLoad > 0)
        {
            float _process = CurNeedLoad * _CurWww.progress;
            if (CurLoadProcess < 1)
            {
                if (GetProcessFrame <= 0 || (CurLoadSpeed <= 0 && GetProcessFrame <= 30))
                {
                    double _loadbyts = Math.Round((_process - LastLoadByts) * 16);
                    CurLoadSpeed = _loadbyts;
                    GetProcessFrame = 35;
                }
                CurLoadProcess = (float)(CurLoadBtys + _process) / AllNeedLoad;
                if (CurLoadProcess > 1)
                {
                    CurLoadProcess = 1;
                }
                UIFadeManager.Instance.SetLoadingText(string.Format(LocalizationUtils.Get("ABDecodeFromStream_1539334118288"), Math.Round(CurLoadProcess * 100, 1), CurLoadSpeed)); //下载场景资源中...{0}% ({1}K/s)

            }
            GetProcessFrame--;
            LastLoadByts = _process;
        }
    }

    //解压的具体方法
    void LZMADecodeOut(string filePath, string savepath)
    {
        byte[] fileBytes = null;
#if UNITY_IOS
        fileBytes = GetResByStreaming.Instance.GetByteAllPlatform(Application.streamingAssetsPath+"/"+filePath);
#else 
        fileBytes = GetResByStreaming.Instance.GetByteAllPlatform(filePath);
#endif
        if (fileBytes == null)
        {
            DecodeComplete = true;
            //CommonLog.Error("bytes null:" + filePath);
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
        if (_byteFile != null)
        {
            byte[] _allByte = null;
            _allByte = lzma.decompressBuffer(_byteFile.Bytes);
            if (_allByte != null && _allByte.Length > 0)
            {
                File.WriteAllBytes(_byteFile.PathName, _allByte);
            }
        }
        DecodeComplete = true;
    }

}

    **/
