using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using System.Web;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 基于 HttpWebRequest 的多线程下载库
/// Created on 2020.4.20
/// Auchor: laibing.sun
/// </summary>
public class HttpFileDownloader
{
    public class RequestState
    {
        // This class stores the State of the request.
        const int BUFFER_SIZE = 1024;
        public byte[] BufferRead;
        public HttpWebRequest request;
        public HttpWebResponse response;
        public Stream streamResponse;
        public int downloadedSize = 0;
        public WWWFileDownloader.DownloadFileInfo fileInfo;
        public ManualResetEvent allDone = new ManualResetEvent(false);

        public FileStream m_file_stream;
        public WWWFileDownloader.DownloaderProgress progress;
        public RequestState()
        {
            BufferRead = new byte[BUFFER_SIZE];
            request = null;
            streamResponse = null;
        }
    }
    private List<WWWFileDownloader.DownloadFileInfo> m_DownList = new List<WWWFileDownloader.DownloadFileInfo>();

    public Uri BaseUri { get; private set; }

    public string SavedDir = "";

    int m_nNextDownIndex = 0;
    int m_nDownCount = 0;
    
    int m_nDownThreadNumb = 0; // 下载线程数量
    //int m_nWriteThreadNumb = 0;

    bool stopAllDownload = false; // 停止所有下载标识
    Thread[] m_runThreads;
    //Thread m_runWriteThread;
    
    //long m_nLimitDownSize; // 每秒限制下载的大小

    private HttpFileDownloader()
    {
    }

    public HttpFileDownloader(Uri baseUri, string saveDir)
    {
        ServicePointManager.ServerCertificateValidationCallback = (o, certificate, chain, errors) => true;
        // 提前调用persitentPath让其缓存，否则线程那边会报错
//#if UNITY_EDITOR
//        Debug.Log(UnityFileLoaderHelper.PersistenPath);
//#endif

        this.BaseUri = baseUri;
        this.SavedDir = saveDir;
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += ChangePlayModeState;
#endif
    }

#if UNITY_EDITOR
    private void ChangePlayModeState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            Release();
        }
    }
#endif

    /*public WWWFileDownloader.DownloaderProgress StartHttpGetRaw(string fileName, WWWFileDownloader.DownloaderProgress _progress = null)
    {
        WWWFileDownloader.DownloaderProgress progress = null;
        if (_progress == null)
            progress = new WWWFileDownloader.DownloaderProgress();
        else
            progress = _progress;
        GameTaskManager.Instance.CreateTask(IHTTPGetFile(fileName, progress));
        return progress;
    }*/

    protected virtual IEnumerator IHTTPGetFile(string relativePath, WWWFileDownloader.DownloaderProgress outProgress, bool isSaveResult = false)
    {
        System.Net.ServicePointManager.DefaultConnectionLimit = 50;

        outProgress.UpdateProgress();
        byte[] data;
        string path = this.GetAbsoluteUri(relativePath);
        //Debug.Log("IHTTPGetFile " + path);

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
                outProgress.SetException(www.error, WWWFileDownloader.eWWWErrorType.DownloadNetError);
                yield break;
            }

            data = www.downloadHandler.data;
        }
        outProgress.SetRaw(data);
    }

    public WWWFileDownloader.DownloaderProgress StartDownloadFile(List<WWWFileDownloader.DownloadFileInfo> downList,
        WWWFileDownloader.DownloaderProgress _progress = null)
    {
        WWWFileDownloader.DownloaderProgress progress = null;
        if (_progress == null)
            progress = new WWWFileDownloader.DownloaderProgress();
        else
            progress = _progress;

        long totalSize = 0;
        foreach (var file in downList)
        {
            totalSize += file.FileSize;
        }
        long downloadedSize = 0;
        progress.InitCompletedSize(totalSize, downloadedSize);

        m_nDownThreadNumb = 1;//SystemInfo.processorCount * 2;
        //if (m_nDownThreadNumb > downList.Count)
        //    m_nDownThreadNumb = downList.Count;

        m_DownList = downList;
        m_nDownCount = downList.Count;
        m_nNextDownIndex = 0;
        m_runThreads = new Thread[m_nDownThreadNumb];
        for (int i = 0; i < m_nDownThreadNumb; i++)
        {
            Thread t = new Thread(ThreadFunc);
            t.Priority = System.Threading.ThreadPriority.Lowest;
            t.Start(progress);
            m_runThreads[i] = t;
        }

        // 启动写线程
        //m_nWriteThreadNumb = 1;
        //Thread tw = new Thread(WriteThreadFunc);
        //m_runWriteThread = tw;
        return progress;
    }

    void Release()
    {
        this.stopAllDownload = true;

        if (m_runThreads != null)
        {
            foreach (var t in m_runThreads)
            {
                if (t != null)
                    t.Abort();
            }
        }
        //if (m_runWriteThread != null)
        //    m_runWriteThread.Abort();
    }

    ~HttpFileDownloader()
    {
        Debug.Log("~HttpFileDownloader()");
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= ChangePlayModeState;
#endif
        Release();
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

    private bool DownloadFile(RequestState requestState)//, int nLastDownSize)
    {
        bool bSuc = DownPart(requestState, 0, 0);
        return bSuc;
    }

    const int BUFFER_SIZE = 1024;

    private void RespCallback(IAsyncResult result)
    {
        RequestState myRequestState = result.AsyncState as RequestState;
        try
        {
            HttpWebRequest myHttpWebRequest = myRequestState.request;

            // response 不能忘记close
            myRequestState.response = (HttpWebResponse)myHttpWebRequest.EndGetResponse(result);

            if (myRequestState.response.StatusCode != HttpStatusCode.OK)
            {
                CommonLog.Error("StatusCode : {myRequestState.response.StatusCode}");
            }

            Stream responseStream = myRequestState.response.GetResponseStream();
            myRequestState.streamResponse = responseStream;

            responseStream.BeginRead(myRequestState.BufferRead, 0, BUFFER_SIZE, ReadCallBack, myRequestState);
        }
        catch (Exception ex)
        {
            if (myRequestState.m_file_stream != null)
                myRequestState.m_file_stream.Close();
            myRequestState.progress.SetException(ex, WWWFileDownloader.eWWWErrorType.WriteFileError);
            myRequestState.allDone.Set();
        }
    }

    private void ReadCallBack(IAsyncResult asyncResult)
    {
        RequestState myRequestState = (RequestState)asyncResult.AsyncState;
        try
        {
            Stream responseStream = myRequestState.streamResponse;
            int read = responseStream.EndRead(asyncResult);
            if (read > 0)
            {
                myRequestState.m_file_stream.Write(myRequestState.BufferRead, 0, read);

                lock (myRequestState.progress)
                {
                    myRequestState.downloadedSize += read;
                    myRequestState.progress.RealCompletedSize += read;
                    myRequestState.progress.UpdateProgress();
                }

                responseStream.BeginRead(myRequestState.BufferRead, 0, BUFFER_SIZE, ReadCallBack, myRequestState);
            }
            else
            {
                //if (myRequestState.fileSize != myRequestState.downloadedSize)
                //{
                //    myRequestState.progress.UpdateTotalSize(myRequestState.m_file_stream.Length - myRequestState.fileSize);
                //    Debug.LogError($"{myRequestState.fileName} {myRequestState.fileSize} {myRequestState.downloadedSize} {myRequestState.m_file_stream.Length}");
                //}
                //else
                //    Debug.LogWarning($"{myRequestState.fileName} {myRequestState.fileSize} {myRequestState.downloadedSize} {myRequestState.m_file_stream.Length}");

                // 校验md5
                myRequestState.m_file_stream.Position = 0;
                //var md5Struct = MD5Creater.GenerateMd5Code(myRequestState.m_file_stream);
                var md5Struct = MD5Creater.GenerateMd5Code(ToBytes(myRequestState.m_file_stream));

                // close fileStream
                myRequestState.m_file_stream.Close();

                var pathRoot = UnityPersistFileHelper.GetPersistAssetFilePath(SavedDir, "");
                string tempFilePath = myRequestState.fileInfo.FileName + ".download";

                if (md5Struct.MD51 == myRequestState.fileInfo.MapedFileName_MD51
                    && md5Struct.MD52 == myRequestState.fileInfo.MapedFileName_MD52)
                {
                    myRequestState.progress.RealCompletedSize -= myRequestState.downloadedSize;
                    myRequestState.progress.RealCompletedSize += myRequestState.fileInfo.FileSize;//myRequestState.fileSize;
                    myRequestState.progress.UpdateProgress();

                    FileUtils.RenameFile(pathRoot, myRequestState.fileInfo.FileName+".download", myRequestState.fileInfo.FileName);
                }
                else
                {
                    FileUtils.DeleteFile(tempFilePath);
                    myRequestState.progress.SetException("md5 check failed", WWWFileDownloader.eWWWErrorType.WriteFileError);
                }
                responseStream.Close();

                // 要写完文件再设置信号量，否则回调直接访问文件会出错
                myRequestState.allDone.Set();

            }
        }
        catch (Exception e)
        {
            if (myRequestState.m_file_stream != null)
                myRequestState.m_file_stream.Close();

            myRequestState.progress.SetException(e, WWWFileDownloader.eWWWErrorType.WriteFileError);
            myRequestState.allDone.Set();
        }
    }
    
    public static byte[] ToBytes(object obj)
    {
        if (obj == null) throw new ArgumentNullException("obj");

        BinaryFormatter serializer = new BinaryFormatter();
        using (MemoryStream memStream = new MemoryStream())
        {
            serializer.Serialize(memStream, obj);
            byte[] bytes = memStream.GetBuffer();
            memStream.Close();
            return bytes;
        }
    }

    // Abort the request if the timer fires.
    private static void TimeoutCallback(object state, bool timedOut)
    {
        if (timedOut)
        {
            RequestState myRequestState = state as RequestState;
            if (myRequestState.request != null)
            {
                myRequestState.request.Abort();
            }

            myRequestState.progress.SetException("timeout ", WWWFileDownloader.eWWWErrorType.DownloadNetError);
        }
    }

    const int DefaultTimeout = 2 * 60 * 1000; // 2 minutes timeout
    private bool DownPart(RequestState myRequestState, 
        int nFileOffset, int nDownSize)
    {
        try
        {
            myRequestState.allDone.Reset();
            Uri uri = new Uri(GetAbsoluteUri(myRequestState.fileInfo.FileName));
            //Debug.Log("DownPart " + uri);
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpWebRequest.Method = "GET";
            httpWebRequest.KeepAlive = true;
            httpWebRequest.ReadWriteTimeout = 5*60*1000;

            myRequestState.request = httpWebRequest;

            var pathRoot = UnityPersistFileHelper.GetPersistAssetFilePath(SavedDir, "");
            //Debug.Log($"DownPart delete {pathRoot + fileName}");
            //删了旧文件
            FileUtils.DeleteFile(pathRoot + myRequestState.fileInfo.FileName);

            if (Directory.Exists(pathRoot) == false)
                Directory.CreateDirectory(pathRoot);

            //Debug.LogError($"begin write {pathRoot + downloadResFile.fileName}");
            string tempName = myRequestState.fileInfo.FileName + ".download";
            myRequestState.m_file_stream = new FileStream(pathRoot + tempName, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            // Start the asynchronous request.
            IAsyncResult result =
              (IAsyncResult)httpWebRequest.BeginGetResponse(RespCallback, myRequestState);
            // this line implements the timeout, if there is a timeout, the callback fires and the request becomes aborted
            ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, new WaitOrTimerCallback(TimeoutCallback), myRequestState, DefaultTimeout, true);

            // The response came in the allowed time. The work processing will happen in the 
            // callback function.
            bool waitDone = false;
            do
            {
                waitDone = myRequestState.allDone.WaitOne(100);
                myRequestState.progress.UpdateProgress();

                //== TODO
                if (stopAllDownload || myRequestState.progress.IsStop)
                {
                    myRequestState.request.Abort();
                    myRequestState.response.Close();
                    if (myRequestState.streamResponse != null)
                        myRequestState.streamResponse.Close();
                    myRequestState.m_file_stream.Close();
                    return false;
                }
            }
            while (!waitDone);

            // Release the HttpWebResponse resource.
            myRequestState.response.Close();
            if (myRequestState.streamResponse != null)
                myRequestState.streamResponse.Close();
        }
        catch (Exception ex)
        {
            if (myRequestState.m_file_stream != null)
                myRequestState.m_file_stream.Close();
            myRequestState.progress.SetException(ex, WWWFileDownloader.eWWWErrorType.DownloadNetError);
            return false;
        }

        return true;
    }

    private bool PopDownFileInfo(WWWFileDownloader.DownloaderProgress progress, out WWWFileDownloader.DownloadFileInfo resInfo)
    {
        resInfo = null;
        if (stopAllDownload || progress.IsStop)
            return false;
        lock (this)
        {
            if (m_nNextDownIndex < m_nDownCount)
            {
                resInfo = m_DownList[m_nNextDownIndex++];
            }
        }

        return resInfo != null;
    }

    private void ThreadFunc(object obj)
    {
        WWWFileDownloader.DownloaderProgress progress = obj as WWWFileDownloader.DownloaderProgress;
        WWWFileDownloader.DownloadFileInfo resInfo = null;
        while (!progress.IsStop && !stopAllDownload)
        {
            while (progress.IsPause)
                Thread.Sleep(100);

            if (PopDownFileInfo(progress, out resInfo))
            {
                // 将下载的内容提交到写线程
                RequestState myRequestState = new RequestState();
                myRequestState.progress = progress;
                myRequestState.fileInfo = resInfo;

                DownloadFile(myRequestState);

                // 发生错误，则跳出下载线程
                if (progress.IsError)
                    break;
            }
            else
            {
                break;
            }
        }
        progress.SetRaw(null);

        // 线程退出，线程数减一
        Interlocked.Decrement(ref m_nDownThreadNumb);
    }

}
