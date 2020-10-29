/**
//游戏全局更新资源控制器
public class ResUpdateManager : MonoBehaviour
{
    #region 更新流程
    [HideInInspector]
    public float Progress = 0f;
    //是否是压缩
    public bool IsCompress = false;
    //从streaming下全部解压控制器
    public static ResDecompressMgr DecompressMgr = null;
    //加载更新解压控制器
    public static LoadDecompressMgr LoadDeCodeMgr = null;
    //是否多线程检测完成
    private bool IsThreadCheck = false;
    //需要加载的文件量
    private List<AssetBundleUpdateInfo.AssetBundleUpdateFileInfo> needDownloadFilesName;
    //检测当前MD5文件的进度
    private int CheckMd5Index = 0;
    //是否删除ab缓存文件夹
    private static bool ISDeleteCacheFile = false;
    //是否拥有lua脚本的热更资源 
    private bool HasLuaBundle = false;
    //是否更新后重启游戏
    private bool IsRestart = true;

    //重新检测文件更新
    private void ReqRestartProgress()
    {
        StartUpdateProgress();
    }
    //开始检测更新
    public void StartUpdateProgress()
    {
        IsRestart = true;
        ISDeleteCacheFile = false;
        HasLuaBundle = false;
        //初始化解压控制器
        if (DecompressMgr == null)
        {
            DecompressMgr = this.gameObject.AddComponent<ResDecompressMgr>();
            ResDecompressMgr.UpdateMgr = this;
            LoadDeCodeMgr = this.gameObject.AddComponent<LoadDecompressMgr>();
            LoadDeCodeMgr.UpdateMgr = this;
        }
        SetViewProgress(UILoginManager.Instance.View, 0);
        Progress = 0;

#if AB_MODE
        //检查缓存文件夹是否可以读写
        try
        {
            AssetBundlePathResolver.CreateBundleCacheDir();
        }
        catch (Exception e)
        {
            OnWriteDiskError("缓存文件夹:" + AssetBundlePathResolver.GetBundleCacheDir() + e.Message);
        }
#endif
        StartUpdateRes();
    }
    //判断是否删除缓存文件
    public static void DeleteCacheCheck(string version)
    {
        string versionCache = "";
        string nowVersion = "Version:" + version;
        versionCache = PlayerPrefs.GetString("XunYinCacheVersion") + "";
        if (versionCache == nowVersion)
        {
            return;
        }
        PlayerPrefs.SetString("XunYinCacheVersion", nowVersion);
        PlayerPrefs.Save();
        DeleteCacheFile();
    }
    //删除缓存ab目录
    static void DeleteCacheFile()
    {
        if (ISDeleteCacheFile)
        {
            string cachePath = AssetBundlePathResolver.GetBundleCacheDir();
            if (Directory.Exists(cachePath))
            {
                FileUtils.DeleteFiles(cachePath, SearchOption.AllDirectories, "");
            }
        }
    }
    //帧调用
    void Update()
    {
        if (LoadDeCodeMgr != null)
        {
            LoadDeCodeMgr.UpdateHandler();
        }
        if (DecompressMgr != null)
        {
            DecompressMgr.UpdateHandler();
        }
    }
    //开始检查热更
    public void StartUpdateRes()
    {
        if (_IsTest)
        {
            WebServerGroupInfo notice_xml = new WebServerGroupInfo();
            //notice_xml.updateFileMd5 = "dea5464f67d2c96d7cd0092ab3caec38";
            notice_xml.updateFilePath = TestUpdateUrl;
            StartCoroutine(IDownloadEnumerator(notice_xml));
        }
        else
        {
            //更新进程
            StartCoroutine(INoticeProcess());
        }
    }

    //日志显示
    public void LogEHelper(Exception e)
    {
#if UNITY_EDITOR
        Log.Error(e);
#endif
    }

    /// <summary>
    /// 设置进度条
    /// </summary>
    public void SetViewProgress(UI_Login_ViewFunc view, float progress)
    {
        Progress = progress;
        if (Progress >= 1)
        {
            Progress = 1;
        }
        view.Set_Update_Percent(Progress);
    }


    #region 下载流程



    //检测更新文件的md5值是否比对
    private bool CheckUpdateMd5(string _md5)
    {
        string _path = AssetBundlePathResolver.GetBundleCacheDir() + AssetBundlePathResolver.UpdateInfoFileName;
        if (DecompressMgr.StreamUpdate != null && !string.IsNullOrEmpty(DecompressMgr.StreamUpdateMd5) && !File.Exists(@_path))
        {
            if (_md5 != DecompressMgr.StreamUpdateMd5)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        if (File.Exists(@_path))
        {
            var md5 = Lua_API_Helper.MD5File(@_path);
#if UNITY_EDITOR
            //CommonLog.Log($"Now Md5:" + md5 + "," + _md5);
#endif
            return md5.Equals(_md5);
        }
        return false;
    }
    //比对文件MD5是否需要加载
    private bool CheckStreamMd5(AssetBundleUpdateInfo.AssetBundleUpdateFileInfo info)
    {
        if (DecompressMgr.StreamUpdateDic.ContainsKey(info.fileName))
        {
            if (DecompressMgr.StreamUpdateDic[info.fileName] == info.fileMD5)
            {
                //StreamNotUpdate.Add(info.fileName, info.fileMD5);
                return true;
            }
            else
            {
                //如果MD5不相等的話，则从persistent文件中获取bundle
                if (ResDecompressMgr.StreamNotUpdate.ContainsKey(info.fileName))
                {
                    ResDecompressMgr.StreamNotUpdate.Remove(info.fileName);
                }
            }
        }
        return AssetBundleManager.Instance.CheckBundleUpdateFileExistCorrect(info);
    }
    /// <summary>
    /// 下载过程逻辑
    /// </summary>
    /// <returns></returns>
    private IEnumerator IDownloadEnumerator(WebServerGroupInfo webresp)
    {
        UI_Login_ViewFunc view = UILoginManager.Instance.View;
        view.Set_Update_Text(LocalizationUtils.Get("ResUpdateManager_1539155259637")); //获取更新内容信息..
        float startTime = Global.time;
        if (ResDecompressMgr.IsAllResources)
        {
            yield return DecompressMgr.CopyABMainfestXml();
            //如果是部分资源在resource中，则获取streaming下面的更新信息
            yield return DecompressMgr.GetStreamUpdateInfo();
        }
        yield return null;
        if (webresp != null)
        {
            //下载XML
            string updateFile = AssetBundlePathResolver.GetWebBundlePath(webresp.updateFilePath, AssetBundlePathResolver.UpdateInfoFileName);
            // CommonLog.Log($"www:" + updateFile);
            if (!string.IsNullOrEmpty(updateFile))
            {
                string xmlstr = "";
                bool _isUpdateSame = CheckUpdateMd5(webresp.updateFileMd5);
                if (string.IsNullOrEmpty(webresp.updateFileMd5) || _isUpdateSame == false)
                {
                    var url = Settings.ISTestUpdate
                        ? updateFile
                        : updateFile + "?v=" + UnityEngine.Random.Range(1, 1000);
                    WWW www = new WWW(url);
                    yield return www;
                    if (string.IsNullOrEmpty(www.error))
                    {
                        xmlstr = www.text;
                        www.Dispose();
                        www = null;
                    }
                    else
                    {
#if UNITY_EDITOR
                        CommonLog.Error("error:" + www.error + "," + AssetBundlePathResolver.GetWebBundlePath(webresp.updateFilePath, AssetBundlePathResolver.UpdateInfoFileName));
#endif
                    }
                }
                else
                {
                    if (!File.Exists(AssetBundlePathResolver.GetBundleCacheDir() + AssetBundlePathResolver.UpdateInfoFileName))
                    {
                        if (DecompressMgr.StreamUpdate != null && !string.IsNullOrEmpty(DecompressMgr.StreamUpdateMd5))
                        {
                            xmlstr = DecompressMgr.StreamUpdateXml;
                        }
                    }
                    else
                    {
                        xmlstr =
                            File.ReadAllText(AssetBundlePathResolver.GetBundleCacheDir() +
                                             AssetBundlePathResolver.UpdateInfoFileName);
                    }
                }
                if (!string.IsNullOrEmpty(xmlstr))
                {
                    AssetBundleUpdateInfo xml = null;
                    try
                    {
                        xml = xmlstr.FromXML<AssetBundleUpdateInfo>();
                    }
                    catch (Exception e)
                    {
                        LogEHelper(e);
                    }
                    if (xml != null)
                    {
                        SetViewProgress(view, 0.2f);
                        AssetBundlePathResolver.AssetbundleManifestName = xml.ManifestName;
                        AssetBundlePathResolver.ResourceVersion = xml.Version_Major + "." + xml.Version_Minor + "." +
                                                                  xml.Version_Build;
                        IsCompress = xml.isZipCompress;
                        view.Set_Update_Text(Codel18nUtils.Get("正在检查文件完整性(不消耗流量)...")); 
                        //下载更新文件
                        needDownloadFilesName = new List<AssetBundleUpdateInfo.AssetBundleUpdateFileInfo>();
                        int bundleNum = xml.AssetBundleInfoList.Count;
                        //检查首次更新的情况,如果没有任何ab配置则无需下载，避免第一次打开游戏需要下载配置
                        if (bundleNum == 1
                            && xml.AssetBundleInfoList[0].fileName == AssetBundlePathResolver.DependFileName
                            && !AssetBundleManager.HasBundleConfig)
                        {
                            //不用做任何事情
                        }
                        else
                        {
                            File.WriteAllText(AssetBundlePathResolver.GetBundleCacheDir() + AssetBundlePathResolver.UpdateInfoFileName, xmlstr);
                            if (DecompressMgr.IsFirstUncompress && _isUpdateSame)
                            {
                                //SetViewProgress_Sub(view, Progress, 0.3f, (float)i / bundleNum);
                            }
                            else
                            {
                                IsThreadCheck = false;
                                CheckMd5Index = 0;
                                ParameterizedThreadStart _start = new ParameterizedThreadStart(ThreadCheckMd5);
                                Thread _itemThread = new Thread(_start);
                                _itemThread.Start(xml);
                                _itemThread.IsBackground = true;
                                while (IsThreadCheck == false)
                                {
                                    float _process = (float)CheckMd5Index / bundleNum;
                                    view.Set_Update_Text(string.Format(Codel18nUtils.Get("正在检查文件完整性(不消耗流量)...{0}%"), Math.Round(_process * 100, 1))); 
                                    SetViewProgress(UILoginManager.Instance.View, Mathf.Min(0.2f + 0.5f * _process, 0.8f));
                                    yield return null;
                                }
                                yield return null;
                            }
                        }
                        if (needDownloadFilesName.Count > 0)
                        {
                            view.Set_Update_Text(Codel18nUtils.Get("开始更新文件.."));
                            SetViewProgress(UILoginManager.Instance.View, 0);
                            yield return StartCoroutine(LoadDeCodeMgr.IDownloadABEnumerator(view, webresp, needDownloadFilesName));
                        }
                        else
                        {
                            SetViewProgress(UILoginManager.Instance.View, 0.8f);
                        }
                        view.Set_Update_Text(Codel18nUtils.Get("加载配置文件(大约需要30秒到1分钟)"));

                        //等待黑屏结束
                        if (Global.time - startTime < 3f)
                        {
                            yield return new WaitForSeconds(3 + startTime - Global.time);
                        }
                        //预加载shader
                        if (ShaderVar != null)
                        {
                            ShaderVar.WarmUp();
                        }
                        SetViewProgress(view, 1f);
                        bool _isNeedRestare = CheckIsRestart();
                        //如果有需要更新的文件，则需要重新启动游戏
                        if (needDownloadFilesName.Count > 0 || ISDeleteDll)
                        {
                            if (_isNeedRestare == false)
                            {
                                AssetBundleManager.Instance.ClearAllData();
                                AssetBundleManager.Instance.ClearMainFest();
                                AssetBundleManager.Instance.LoadAssetBundleConfig();
                                LuaFixManager.Instance.init();

                            }
                            else
                            {
                                //重启
                                OnRestartGame();
                            }
                        }
                    }
                    else
                    {
                        //xml字符串无法转换为对象
                        OnDownloadError();
                    }
                }
                else
                {
                    //下载的xml字符串为空
                    OnDownloadError();
                }
            }
            else //开始下载
            {
#if UNITY_EDITOR
                CommonLog.Error("Web服务器没有热更新文件信息");
#endif
                OnDownloadError();
            }
        }
        else
        {

#if UNITY_EDITOR
            CommonLog.Error("Web服务器没有热更新文件信息");
#endif
            OnDownloadError();
        }
    }

    //检测是否需要重启 TODO:做在Bundle目录
    private bool CheckIsRestart()
    {
        bool _isNeedRestare = true;
        
        return _isNeedRestare;
    }

    /// <summary>
    /// 当Bundle更新完成，做一下清理
    /// </summary>
    private void DoClearWhenBundleUpdated()
    {

    }
    //多线程检测文件MD5值
    private void ThreadCheckMd5(object data)
    {
        AssetBundleUpdateInfo xml = data as AssetBundleUpdateInfo;
        int bundleNum = xml.AssetBundleInfoList.Count;
        //检查xml对应文件
        for (int i = 0; i < bundleNum; i++)
        {
            var x = xml.AssetBundleInfoList[i];
            if (x.fileName.IndexOf("977dc379d4fe2580f1ed1db51ea26ff") != -1)
            {
                x.fileName = "Assembly-CSharp.dll";
            }
            if (DecompressMgr.StreamUpdate != null && !string.IsNullOrEmpty(DecompressMgr.StreamUpdateMd5))
            {
                if (!CheckStreamMd5(x))
                {
                    needDownloadFilesName.Add(x);
                    if (x.fileName.IndexOf("Assembly-CSharp.dll") != -1)
                    {
                        NeedUpdateDLL = true;
                    }
                }
            }
            else
            {
                if (!AssetBundleManager.Instance.CheckBundleUpdateFileExistCorrect(x))
                {
                    needDownloadFilesName.Add(x);
                    if (x.fileName.IndexOf("Assembly-CSharp.dll") != -1)
                    {
                        NeedUpdateDLL = true;
                    }
                }
            }
            if (x.fileName.IndexOf("hotfix") != -1)
            {
                HasLuaBundle = true;
            }
            CheckMd5Index++;
        }
        IsThreadCheck = true;
    }
    /// <summary>
    /// 出现下载错误的时候的调用
    /// </summary>
    private void OnServerIPError(string s = null, params object[] p)
    {
#if UNITY_EDITOR
        CommonLog.Error("下载失败");
        if (s != null) { CommonLog.Error(s, p); }
#endif
        StopAllCoroutines();
        UIMessageBoxManager.Instance.ShowMessageBox_OK(""
            ,Codel18nUtils.Get("服务器异常,请重试")
            ,"").AddOnSelectOKOrYes(result => 
            {
                ReqRestartProgress();
            });
    }
    /// <summary>
    /// 出现下载错误的时候的调用
    /// </summary>
    public void OnDownloadError(string s = null, params object[] p)
    {
#if UNITY_EDITOR
        CommonLog.Error("下载失败");
        if (s != null) { CommonLog.Error(s, p); }
#endif
        StopAllCoroutines();
        UIMessageBoxManager.Instance.ShowMessageBox_OK("", Codel18nUtils.Get("网络异常,请重试"),"").AddOnSelectOKOrYes(result =>  
            {
                ReqRestartProgress();
            });
    }
    /// <summary>
    /// 出现下载错误的时候的调用
    /// </summary>
    public void OnNoEnoughSpaceError(float mb)
    {
#if UNITY_EDITOR
        CommonLog.Error("空间不足");
#endif
        StopAllCoroutines();
        UIMessageBoxManager.Instance.ShowMessageBox_OKCANCEL(""
            ,string.Format(Codel18nUtils.Get("空间不足，请至少保留{0}mb空间，或没有写入权限"), mb)
            ,""
            ).AddOnSelectOKOrYes(result => 
            {
                ReqRestartProgress();
            }).AddOnSelectCancelOrNo(result =>
                {
                    var res = GameManager.Instance.OpenExitGameMessageBox();
                    res.AddOnSelectCancelOrNo((_res) =>
                        {
                            OnNoEnoughSpaceError(mb);
                        });
                });
    }
    /// <summary>
    /// 出现下载错误的时候的调用
    /// </summary>
    public void OnWriteDiskError(string path)
    {
#if UNITY_EDITOR
        CommonLog.Error("写入文件失败");
#endif
        StopAllCoroutines();
        UIMessageBoxManager.Instance.ShowMessageBox_OKCANCEL(
            "",
            string.Format(Codel18nUtils.Get("写入{0}失败"), path)
            ,""
            ).AddOnSelectOKOrYes(result => 
            {
                ReqRestartProgress();
            }).AddOnSelectCancelOrNo(result =>
                {
                    var res = GameManager.Instance.OpenExitGameMessageBox();
                    res.AddOnSelectCancelOrNo((_res) =>
                        {
                            OnWriteDiskError(path);
                        });
                });
    }
    //重启游戏
    private void OnRestartGame()
    {
        StopAllCoroutines();
        UIMessageBoxManager.Instance.ShowMessageBox_OK(
            "",
            string.Format(Codel18nUtils.Get("ResUpdateManager_1539155399377")),
            ""
            ).AddOnSelectOKOrYes(result => //更新完成，请重启游戏
            {
                NativeAPIProxy.Instance.RestartGame();
            });
    }
    #endregion
    #endregion
}
    */
