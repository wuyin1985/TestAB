using System;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TimerUtils;
using UnityEngine.Networking;

public partial class WWWFileDownloader
{
    private Uri baseUri;
    private int maxTaskCount;

    public virtual Uri BaseUri
    {
        get { return this.baseUri; }
        set
        {
            if (value == null)
                throw new NotSupportedException(string.Format("Invalid uri:{0}", value == null ? "null" : value.OriginalString));

            this.baseUri = value;
        }
    }
    //存储的位置,是Persist+Dir组成，由/结尾
    public string SavedDir;

    public virtual int MaxTaskCount
    {
        get { return this.maxTaskCount; }
        set { this.maxTaskCount = Mathf.Max(value > 0 ? value : SystemInfo.processorCount * 2, 1); }
    }

    public WWWFileDownloader() : this(null, "")
    {
    }

    //存储的位置,是Persist+Dir组成，由/结尾
    public WWWFileDownloader(Uri baseUri, string savedDir)
    {
        this.BaseUri = baseUri;
        this.maxTaskCount = SystemInfo.processorCount * 2;
        this.SavedDir = savedDir;
    }

    protected virtual string GetAbsoluteUri(string relativePath)
    {
        string path = this.BaseUri.AbsoluteUri;
        if (this.BaseUri.Scheme.Equals("jar") && !path.StartsWith("jar:file://"))
            path = path.Replace("jar:file:", "jar:file://");

        if (path.EndsWith("/"))
            return path + relativePath;
        return path + "/" + relativePath;
    }

    /// <summary>
    /// 允许传入progress用于添加回调函数
    /// </summary>
    /// <param name="bundles"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public DownloaderProgress StartDownloadFile(List<DownloadFileInfo> bundles, DownloaderProgress progress = null)
    {
        if (progress == null) progress = new DownloaderProgress();
        //TaskManager.Instance.CreateTask(IDownloadAllFiles(bundles, progress));
        return progress;
    }
    /// <summary>
    /// 允许传入progress用于添加回调函数
    /// </summary>
    /// <param name="bundles"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    /*public DownloaderProgress StartDownloadFile(List<DownloadFileInfo> bundles, MonoBehaviour followedTarget
        , FollowTargetMode followMode = FollowTargetMode.TargetDisactiveDoNothing, DownloaderProgress progress = null)
    {
        if (progress == null) progress = new DownloaderProgress();
        GameTaskManager.Instance.CreateTask(IDownloadAllFiles(bundles, progress), followedTarget, followMode);
        return progress;
    }

    public DownloaderProgress StartHttpGetText(string fileName, DownloaderProgress progress = null)
    {
        if (progress == null) progress = new DownloaderProgress();
        GameTaskManager.Instance.CreateTask(IHTTPGetFile(fileName, progress));
        return progress;
    }
    public DownloaderProgress StartHttpGetText(string fileName, MonoBehaviour followedTarget
        , FollowTargetMode followMode = FollowTargetMode.TargetDisactiveDoNothing, DownloaderProgress progress = null)
    {
        if (progress == null) progress = new DownloaderProgress();
        GameTaskManager.Instance.CreateTask(IHTTPGetFile(fileName, progress), followedTarget, followMode);
        return progress;
    }

    public DownloaderProgress StartHttpGetRaw(string fileName, DownloaderProgress progress = null)
    {
        if (progress == null) progress = new DownloaderProgress();
        GameTaskManager.Instance.CreateTask(IHTTPGetFile(fileName, progress));
        return progress;
    }
    public DownloaderProgress StartHttpGetRaw(string fileName, MonoBehaviour followedTarget
        , FollowTargetMode followMode = FollowTargetMode.TargetDisactiveDoNothing, DownloaderProgress progress = null)
    {
        if (progress == null) progress = new DownloaderProgress();
        GameTaskManager.Instance.CreateTask(IHTTPGetFile(fileName, progress), followedTarget, followMode);
        return progress;
    }*/

    protected virtual IEnumerator IHTTPGetFile(string relativePath, DownloaderProgress outProgress, bool isSaveResult = false)
    {
        outProgress.UpdateProgress();
        byte[] data;
        string path = this.GetAbsoluteUri(relativePath);

        using (UnityWebRequest www = new UnityWebRequest(path))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SendWebRequest();
            while (!www.isDone)
            {
                if (www.downloadProgress >= 0)
                {
                    if (outProgress.TotalSize <= 0)
                    {
                        outProgress.InitCompletedSize((long)(www.downloadedBytes / www.downloadProgress), (long)www.downloadedBytes);
                    }
                    outProgress.RealCompletedSize = (long)www.downloadedBytes;
                    outProgress.UpdateProgress();
                }
                yield return null;
            }

            if (!string.IsNullOrEmpty(www.error))
            {
                outProgress.SetException(www.error, eWWWErrorType.DownloadNetError);
                yield break;
            }

            data = www.downloadHandler.data;
        }
        outProgress.SetRaw(data);
    }

    static int resDownloadGcCounter = 0;
    /// <summary>
    /// 每下载20个文件后gc一次
    /// </summary>
    readonly int resDownloadGcTriggerNum = 20;
    protected IEnumerator IDownloadAllFiles(List<DownloadFileInfo> files, DownloaderProgress outprogress)
    {
        long totalSize = 0;
        long downloadedSize = 0;
        List<DownloadFileInfo> list = new List<DownloadFileInfo>();
        for (int i = 0; i < files.Count; i++)
        {
            var info = files[i];
            totalSize += info.FileSize;

            if (UnityPersistFileHelper.IsPersistAssetFileExist(SavedDir, info.FileName))
            {
                downloadedSize += info.FileSize;
                continue;
            }

            list.Add(info);
        }

        outprogress.InitCompletedSize(totalSize, downloadedSize); 

        List<KeyValuePair<DownloadFileInfo, UnityWebRequest>> tasks = new List<KeyValuePair<DownloadFileInfo, UnityWebRequest>>();
        for (int i = 0; i < list.Count; i++)
        {
            var bundleInfo = list[i];

            UnityWebRequest www;
            //我们的Bundle是基于Hash命名的，无需在意Cache
            www = new UnityWebRequest(GetAbsoluteUri(bundleInfo.FileName));
            www.downloadHandler = new DownloadHandlerBuffer();

            www.SendWebRequest();
            tasks.Add(new KeyValuePair<DownloadFileInfo, UnityWebRequest>(bundleInfo, www));

            while (tasks.Count >= this.MaxTaskCount || (i == list.Count - 1 && tasks.Count > 0))
            {
                //判断暂停
                while (outprogress.IsPause)
                {
                    yield return new WaitForSeconds(0.1f);//WaitingForSecondConst.RWaitMS100;
                }

                if (outprogress.IsStop)
                {
                    yield break;
                }

                long tmpSize = 0;
                for (int j = tasks.Count - 1; j >= 0; j--)
                {
                    var task = tasks[j];
                    var _bundleInfo = task.Key;
                    UnityWebRequest _www = task.Value;

                    if (!_www.isDone)
                    {
                        tmpSize += Math.Max(0, (long)(_www.downloadProgress * _bundleInfo.FileSize));

                        continue;
                    }

                    tasks.RemoveAt(j);
                    downloadedSize += _bundleInfo.FileSize;
                    if (!string.IsNullOrEmpty(_www.error))
                    {
                        outprogress.SetException((_www.error), eWWWErrorType.DownloadNetError);

                        CommonLog.Error("Downloads File '{0}' failure from the address '{1}'.Reason:{2}", _bundleInfo.FileName, GetAbsoluteUri(_bundleInfo.FileName), _www.error);
                        yield break;
                    }

                    try
                    {
                        {
                            string fileName = _bundleInfo.FileName;
                            string tempName = new Guid().ToString() + ".download";

                            var pathRoot = UnityPersistFileHelper.GetPersistAssetFilePath(SavedDir, "");
                            //删了旧文件
                            FileUtils.DeleteFile(pathRoot + fileName);
                            //写到临时文件
                            FileUtils.WriteAllBytes(pathRoot + tempName, _www.downloadHandler.data);
                            //写完改名字
                            FileUtils.RenameFile(pathRoot, tempName, fileName);
                        }
                        if (resDownloadGcCounter++ >= resDownloadGcTriggerNum)
                        {
                            resDownloadGcCounter = 0;
                            GC.Collect();
                        }
                    }
                    catch (Exception e)
                    {
                        outprogress.SetException(e, eWWWErrorType.WriteFileError);
                        CommonLog.Error("Downloads File '{0}' failure from the address '{1}'.Reason:{2}", _bundleInfo.FileName, GetAbsoluteUri(_bundleInfo.FileName), e);
                        yield break;
                    }
                    finally
                    {
                        // 卸载资源
                        if (_www != null)
                        {
                            _www.Dispose();
                            _www = null;
                        }
                    }
                }

                outprogress.RealCompletedSize = downloadedSize + tmpSize;
                outprogress.UpdateProgress();

                yield return new WaitForSeconds(0.1f);//WaitingForSecondConst.RWaitMS100;
            }
        }
        outprogress.SetRaw(null);
    }
}
