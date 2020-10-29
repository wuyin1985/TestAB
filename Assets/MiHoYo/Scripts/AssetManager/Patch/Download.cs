using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace NewWarMap.Patch
{
    public class Download : DownloadHandlerScript
    {
        #region ICloneable implementation

        public Download Clone()
        {
            return new Download
            {
                id = id,
                md5 = md5,
                url = url,
                len = len,
                savePath = savePath,
            };
        }

        #endregion

        public int id { get; set; }

        public string error { get; private set; }

        public long len { get; set; }

        public MD5Creater.MD5Struct md5 { get; set; }

        public string url { get; set; }

        public long position { get; private set; }

        public string tempPath
        {
            get
            {
                var dir = Path.GetDirectoryName(savePath);
                return $"{dir}/{md5.GetMD5Str(false)}";
            }
        }

        public string savePath;

        private UnityWebRequest _request;

        private FileStream _stream;

        private bool _running;

        private bool _finished;

        protected override float GetProgress()
        {
            return position * 1f / len;
        }

        protected override byte[] GetData()
        {
            return null;
        }

        protected override void ReceiveContentLength(int contentLength)
        {
        }

        protected override bool ReceiveData(byte[] buffer, int dataLength)
        {
            if (!string.IsNullOrEmpty(_request.error))
            {
                error = _request.error;
                Complete();
                return true;
            }

            _stream.Write(buffer, 0, dataLength);
            position += dataLength;
            return _running;
        }

        protected override void CompleteContent()
        {
            Complete();
        }

        public override string ToString()
        {
            return $"{url}, size:{len}, hash:{md5}";
        }

        public void Start()
        {
            if (_running)
            {
                return;
            }

            error = null;
            finished = false;
            _running = true;
            _stream = new FileStream(tempPath, FileMode.OpenOrCreate, FileAccess.Write);
            position = _stream.Length;
            if (position < len)
            {
                _stream.Seek(position, SeekOrigin.Begin);
                _request = UnityWebRequest.Get(url);
                _request.SetRequestHeader("Range", "bytes=" + position + "-");
                _request.downloadHandler = this;
                _request.SendWebRequest();
                CommonLog.Log(MAuthor.WY, "Start Download：" + url);
            }
            else
            {
                Complete();
            }
        }

        public void Update()
        {
            if (_running && error == null)
            {
                if (_request.isDone && _request.downloadedBytes < (ulong) len)
                {
                    error = "unknown error: downloadedBytes < len";
                }

                if (!string.IsNullOrEmpty(_request.error))
                {
                    error = _request.error;
                }
            }
        }

        public void Remove()
        {
            if (_stream != null)
            {
                _stream.Close();
                _stream.Dispose();
                _stream = null;
            }

            if (_request != null)
            {
                _request.Abort();
                _request.Dispose();
                _request = null;
            }

            Dispose();
        }

        public void Complete(bool stop = false)
        {
            if (_finished) return;
            _running = false;
            finished = true;
            Remove();
            if (!stop)
            {
                CheckError();
            }

            if (error != null && File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }

        private void CheckError()
        {
            if (File.Exists(tempPath))
            {
                if (error == null)
                {
                    var info = new FileInfo(tempPath);
                    if (info.Length != len)
                    {
                        error = $"download file length error {len} -> {info.Length}";
                    }

                    var assetBundleBytes = FileUtils.ReadAllBytes(tempPath);
                    var md5 = MD5Creater.Md5Struct(assetBundleBytes);
                    if (md5.MD51 != this.md5.MD51 || md5.MD52 != this.md5.MD52)
                    {
                        error = $"download file md5 error {this.md5.GetMD5Str(false)} -> {md5.GetMD5Str(false)}";
                    }
                }

                if (error == null)
                {
                    try
                    {
                        File.Copy(tempPath, savePath, true);
                        File.Delete(tempPath);
                        CommonLog.Log($"Complete Download: {url}");
                    }
                    catch (Exception e)
                    {
                        CommonLog.Error(MAuthor.WY, $"Copy and delete temp file {tempPath} occur error : {e.Message}");
                        error = "copy temp file failed";
                    }
                }
            }
            else
            {
                error = $"file {tempPath} not exist";
            }
        }

        public bool finished
        {
            get { return _finished; }
            private set { _finished = value; }
        }
    }
}