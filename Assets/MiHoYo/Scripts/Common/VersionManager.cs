using LitJson;
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class VersionData
{
    /// <summary>
    /// 游戏版本号
    /// </summary>
    public string appVersion;
    /// <summary>
    /// 资源版本号
    /// </summary>
    public string resVersion;
    /// <summary>
    /// 配置版本号
    /// </summary>
    public string cfgVersion;
    /// <summary>
    /// App更新地址
    /// </summary>
    public string appUpdateUrl;
    /// <summary>
    /// 资源更新地址
    /// </summary>
    public string resUpdateUrl;
}

public class VersionManager : SimpleSingletonProvider<VersionManager>
{
    private VersionData versionData;

    private VersionData newVersionData;

#if UNITY_ANDROID
    /// <summary>
    /// apk 版本配置地址
    /// </summary>
    private readonly string versionCfgUrl = "http://download.thewar2061.com/version.xml";
#elif UNITY_IOS
    /// <summary>
    /// ios 版本配置地址
    /// </summary>
    private readonly string versionCfgUrl = "http://download.thewar2061.com/version.xml";
#else
    /// <summary>
    /// pc 版本配置地址
    /// </summary>
    private readonly string versionCfgUrl = "http://download.thewar2061.com/version.xml";
#endif



    /// <summary>
    /// 当前游戏App版本号
    /// </summary>
    public string AppVersion
    {
        get
        {
            return versionData?.appVersion;
        }
    }

    /// <summary>
    /// 当前游戏资源版本号
    /// </summary>
    public string ResVersion
    {
        get
        {
            return versionData?.resVersion;
        }
    }

    /// <summary>
    /// 游戏App新版本号
    /// </summary>
    public string NewAppVersion
    {
        get
        {
            if (newVersionData == null)
                return versionData?.appVersion;
            return newVersionData?.appVersion;
        }
    }

    /// <summary>
    /// 游戏资源新版本号
    /// </summary>
    public string NewResVersion
    {
        get
        {
            if (newVersionData == null)
                return versionData?.resVersion;
            return newVersionData?.resVersion;
        }
    }

    /// <summary>
    /// 游戏App更新地址
    /// </summary>
    public string AppUpdateUrl
    {
        get
        {
            if (newVersionData == null)
                return versionData?.appUpdateUrl;
            return newVersionData?.appUpdateUrl;
        }
    }

    /// <summary>
    /// 游戏资源更新地址
    /// </summary>
    public string ResUpdateUrl
    {
        get
        {
            if (newVersionData == null)
                return versionData?.resUpdateUrl;
            return newVersionData?.resUpdateUrl;
        }
    }

    /// <summary>
    /// 加载本地版本配置文件
    /// </summary>
    public void LoadVersionConfig()
    {
        CommonLog.Log(MAuthor.ZX, "Load VersionConfig");
        string localVersionPath = UnityFileLoaderHelper.StreamingAssetsPath + "/version.xml";
        if (GameAssetManager.USED_AB_MODE)
        {
            localVersionPath = "version.xml";
        }
        var xmlStr = UnityFileLoaderHelper.ReadFileAllText(localVersionPath);
        versionData = xmlStr.FromXML<VersionData>();
    }


    /// <summary>
    /// 下载远程版本配置文件
    /// </summary>
    /// <returns></returns>
    /*
    public Task<int> DownloadVersionCfg()
    {
        CommonLog.Log(MAuthor.ZX, "Download VersionConfig");
        TaskCompletionSource<int> source = new TaskCompletionSource<int>();
        DownloadVersionConfig(source).StartCoroutine();
        return source.Task;
    }
    */

    /// <summary>
    /// 下载版本配置文件
    /// </summary>
    private IEnumerator DownloadVersionConfig(TaskCompletionSource<int> source)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(versionCfgUrl))
        {
            yield return webRequest.SendWebRequest();
            if (webRequest.isNetworkError)
            {
                CommonLog.Error(webRequest.error);
                source.TrySetResult(-1);
            }
            else
            {
                var downloadHandler = webRequest.downloadHandler;
                var xmlStr = webRequest.downloadHandler.text;
                newVersionData = xmlStr.FromXML<VersionData>();
                source.TrySetResult(0);
            }
        }
    }

    /// <summary>
    /// 检测是否需要更新App
    /// </summary>
    public bool IsUpdateApp
    {
        get
        {
            return AppVersion != NewAppVersion;
        }
    }

    /// <summary>
    /// 检测是否需要更新资源
    /// </summary>
    public bool IsUpdateRes
    {
        get
        {
            return ResVersion != NewResVersion;
        }
    }

    /// <summary>
    /// 保存最新的版本配置文件
    /// </summary>
    private void SaveNewVersionConfig()
    {
        if (newVersionData == null)
        {
            CommonLog.Error(MAuthor.ZX, "下载服务器未配置version.json");
            return;
        }
        var jsonStr = JsonMapper.ToJson(newVersionData);
        var savePath = UnityFileLoaderHelper.PersistenPath + "/version.json";
        FileUtils.WriteAllText(savePath, savePath);
    }

    /// <summary>
    /// 检测是否强制更新App
    /// </summary>
    public int CheckUpdateApp()
    {
        if (IsUpdateApp)
        {
#if UNITY_ANDROID
            ShowAndroidUpdateDialog();
            return -1;
#elif UNITY_IOS
            ShowIOSUpdateDialog();
            return -1;
#else
            return 0;
#endif           
        }

        return 0;
    }

    /// <summary>
    /// 显示IOS升级弹窗
    /// </summary>
    private void ShowIOSUpdateDialog()
    {

    }

    /// <summary>
    /// 显示Android升级弹窗
    /// </summary>
    private void ShowAndroidUpdateDialog()
    {
        CommonLog.Log(MAuthor.ZX, "Open Android Update Dialog");
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        AndroidAlertDialog alertDialog = new AndroidAlertDialog(activity);
        alertDialog.SetTitle("游戏版本检测");
        alertDialog.SetMessage("检测到有新的游戏版本，是否更新？");
        alertDialog.SetPositiveButton("是", new AlertDialogClickListener(OnConfirm));
        alertDialog.SetNegativeButton("否", new AlertDialogClickListener(OnCancel));
        alertDialog.Create();
        alertDialog.Show();
    }

    /// <summary>
    /// 确认
    /// </summary>
    /// <param name="dialog"></param>
    /// <param name="which"></param>
    void OnConfirm(AndroidJavaObject dialog, int which)
    {
        //跳转App更新界面
        Application.OpenURL(AppUpdateUrl);
        //强制退出
        Application.Quit();
    }

    /// <summary>
    /// 取消
    /// </summary>
    /// <param name="dialog"></param>
    /// <param name="which"></param>
    void OnCancel(AndroidJavaObject dialog, int which)
    {
        //强制退出
        Application.Quit();
    }
}
