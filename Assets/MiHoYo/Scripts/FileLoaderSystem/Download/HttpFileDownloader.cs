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
    private const string TempExtension = ".download";

    int m_nDownThreadNumb = 0; // 下载线程数量
    //int m_nWriteThreadNumb = 0;

    bool stopAllDownload; // 停止所有下载标识
    Thread[] m_runThreads;

    private readonly WWWFileDownloader.DownloaderProgress _progress = new WWWFileDownloader.DownloaderProgress();

    //Thread m_runWriteThread;

    //long m_nLimitDownSize; // 每秒限制下载的大小

    public WWWFileDownloader.DownloaderProgress GetProgress()
    {
        return _progress;
    }

    public HttpFileDownloader(Uri baseUri, string saveDir)
    {
        ServicePointManager.ServerCertificateValidationCallback = (o, certificate, chain, errors) => true;
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

    public void AddDownLoad(WWWFileDownloader.DownloadFileInfo download)
    {
        m_DownList.Add(download);
    }

    public long GetToDownloadSize()
    {
        long ret = 0;
        foreach (var downloadFileInfo in m_DownList)
        {
            var info = new FileInfo(
                UnityPersistFileHelper.GetPersistAssetFilePath(SavedDir, downloadFileInfo.FileName + TempExtension));
            if (info.Exists)
            {
                ret += downloadFileInfo.FileSize - info.Length;
            }
            else
            {
                ret += downloadFileInfo.FileSize;
            }
        }

        return ret;
    }

    public void StartDownloadFile()
    {
        long totalSize = 0;
        foreach (var file in m_DownList)
        {
            totalSize += file.FileSize;
        }

        long downloadedSize = 0;
        _progress.InitCompletedSize(totalSize, downloadedSize);

        m_nDownThreadNumb = 1; //SystemInfo.processorCount * 2;
        //if (m_nDownThreadNumb > downList.Count)
        //    m_nDownThreadNumb = downList.Count;

        m_nDownCount = m_DownList.Count;
        m_nNextDownIndex = 0;
        m_runThreads = new Thread[m_nDownThreadNumb];
        for (int i = 0; i < m_nDownThreadNumb; i++)
        {
            var t = new Thread(ThreadFunc) {Priority = System.Threading.ThreadPriority.Lowest};
            t.Start(_progress);
            m_runThreads[i] = t;
        }
    }

    public void Release()
    {
        this.stopAllDownload = true;

        if (m_runThreads != null)
        {
            foreach (var t in m_runThreads)
            {
                t?.Abort();
            }

            m_runThreads = null;
        }
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

    const int BUFFER_SIZE = 1024;

    private void RespCallback(IAsyncResult result)
    {
        var myRequestState = result.AsyncState as RequestState;
        try
        {
            var myHttpWebRequest = myRequestState.request;
            // response 不能忘记close
            myRequestState.response = (HttpWebResponse) myHttpWebRequest.EndGetResponse(result);

            if (myRequestState.response.StatusCode != HttpStatusCode.OK &&
                myRequestState.response.StatusCode != HttpStatusCode.PartialContent)
            {
                CommonLog.Error($"{myRequestState.fileInfo.FileName} StatusCode : {myRequestState.response.StatusCode}");
            }

            var responseStream = myRequestState.response.GetResponseStream();
            myRequestState.streamResponse = responseStream;
            responseStream.BeginRead(myRequestState.BufferRead, 0, BUFFER_SIZE, ReadCallBack, myRequestState);
        }
        catch (Exception ex)
        {
            myRequestState.m_file_stream?.Close();
            CommonLog.Error(MAuthor.WY, $"download {myRequestState.fileInfo.FileName} resp exception {ex.Message}");
            myRequestState.progress.SetException(ex, WWWFileDownloader.eWWWErrorType.WriteFileError);
            myRequestState.allDone.Set();
        }
    }


    private void CheckMd5AndReplaceTempFile(RequestState myRequestState)
    {
        var ms = new MemoryStream();
        myRequestState.m_file_stream.Position = 0;
        myRequestState.m_file_stream.CopyTo(ms);
        var md5Struct = MD5Creater.GenerateMd5Code(ms.ToArray());
        myRequestState.m_file_stream.Close();
        ms.Dispose();

        var pathRoot = UnityPersistFileHelper.GetPersistAssetFilePath(SavedDir, "");
    

        if (md5Struct.MD51 == myRequestState.fileInfo.MapedFileName_MD51
            && md5Struct.MD52 == myRequestState.fileInfo.MapedFileName_MD52)
        {
            CommonLog.Log($"complete download file {myRequestState.fileInfo.FileName}");
            myRequestState.progress.RealCompletedSize -= myRequestState.downloadedSize;
            myRequestState.progress.RealCompletedSize +=
                myRequestState.fileInfo.FileSize; //myRequestState.fileSize;
            myRequestState.progress.UpdateProgress();

            FileUtils.RenameFile(pathRoot, myRequestState.fileInfo.FileName + TempExtension,
                myRequestState.fileInfo.FileName);
        }
        else
        {
            var tempFilePath = pathRoot + myRequestState.fileInfo.FileName + TempExtension;
            CommonLog.Error(MAuthor.WY, $"file {myRequestState.fileInfo.FileName} md5 error, delete file {tempFilePath}");
            FileUtils.DeleteFile(tempFilePath);
            myRequestState.progress.SetException("md5 check failed",
                WWWFileDownloader.eWWWErrorType.WriteFileError);
        }
    }

    private void ReadCallBack(IAsyncResult asyncResult)
    {
        RequestState myRequestState = (RequestState) asyncResult.AsyncState;
        //try
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
                // 校验md5
                CheckMd5AndReplaceTempFile(myRequestState);
                responseStream.Close();
                myRequestState.allDone.Set();
            }
        }
        /*catch (Exception e)
        {
            myRequestState.m_file_stream?.Close();
            CommonLog.Error(MAuthor.WY, $"download {myRequestState.fileInfo.FileName} read exception {e.Message}");
            myRequestState.progress.SetException(e, WWWFileDownloader.eWWWErrorType.WriteFileError);
            myRequestState.allDone.Set();
        }*/
    }

    public static byte[] ToBytes(object obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));

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
            var myRequestState = state as RequestState;
            myRequestState.request?.Abort();
            CommonLog.Log(MAuthor.WY, $"download {myRequestState.fileInfo.FileName} timeout");
            myRequestState.progress.SetException("timeout ", WWWFileDownloader.eWWWErrorType.DownloadNetError);
        }
    }

    const int DefaultTimeout = 2 * 60 * 1000; // 2 minutes timeout

    private bool DownloadFile(RequestState myRequestState)
    {
        //try
        {
            myRequestState.allDone.Reset();
            var pathRoot = UnityPersistFileHelper.GetPersistAssetFilePath(SavedDir, "");
            FileUtils.DeleteFile(pathRoot + myRequestState.fileInfo.FileName);

            if (Directory.Exists(pathRoot) == false)
                Directory.CreateDirectory(pathRoot);

            string tempName = myRequestState.fileInfo.FileName + TempExtension;
            myRequestState.m_file_stream =
                new FileStream(pathRoot + tempName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            var position = myRequestState.m_file_stream.Length;
            if (position < myRequestState.fileInfo.FileSize)
            {
                myRequestState.m_file_stream.Seek(position, SeekOrigin.Begin);
                var uri = new Uri(GetAbsoluteUri(myRequestState.fileInfo.FileName));
                var httpWebRequest = (HttpWebRequest) WebRequest.Create(uri);
                httpWebRequest.Method = "GET";
                httpWebRequest.KeepAlive = true;
                httpWebRequest.ReadWriteTimeout = 5 * 60 * 1000;
                httpWebRequest.AddRange(position);
                myRequestState.request = httpWebRequest;

                CommonLog.Log(MAuthor.WY, $"start Download {uri} from position {position}");
                // Start the asynchronous request.
                IAsyncResult result =
                    httpWebRequest.BeginGetResponse(RespCallback, myRequestState);
                ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, new WaitOrTimerCallback(TimeoutCallback),
                    myRequestState, DefaultTimeout, true);

                // The response came in the allowed time. The work processing will happen in the 
                // callback function.
                bool waitDone;
                do
                {
                    waitDone = myRequestState.allDone.WaitOne(100);
                    myRequestState.progress.UpdateProgress();

                    //== TODO
                    if (stopAllDownload || myRequestState.progress.IsStop)
                    {
                        myRequestState.request.Abort();
                        myRequestState.response.Close();
                        myRequestState.streamResponse?.Close();
                        myRequestState.m_file_stream.Close();
                        return false;
                    }
                } while (!waitDone);

                // Release the HttpWebResponse resource.
                myRequestState.response.Close();
                myRequestState.streamResponse?.Close();
            }
            else
            {
                CheckMd5AndReplaceTempFile(myRequestState);
            }
        }
        /*catch (Exception ex)
        {
            myRequestState.m_file_stream?.Close();
            CommonLog.Error(MAuthor.WY, $"download {myRequestState.fileInfo.FileName} cause exception {ex.Message}");
            myRequestState.progress.SetException(ex, WWWFileDownloader.eWWWErrorType.DownloadNetError);
            return false;
        }*/

        return true;
    }

    private bool PopDownFileInfo(WWWFileDownloader.DownloaderProgress progress,
        out WWWFileDownloader.DownloadFileInfo resInfo)
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
        while (!progress.IsStop && !stopAllDownload)
        {
            while (progress.IsPause)
                Thread.Sleep(100);

            if (PopDownFileInfo(progress, out var resInfo))
            {
                // 将下载的内容提交到写线程
                var myRequestState = new RequestState {progress = progress, fileInfo = resInfo};
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

        progress.SetComplete();

        // 线程退出，线程数减一
        Interlocked.Decrement(ref m_nDownThreadNumb);
    }
}