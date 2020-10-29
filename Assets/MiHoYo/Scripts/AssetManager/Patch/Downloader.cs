using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NewWarMap.Patch
{
    public class Downloader : MonoBehaviour
    {
        private const float BYTES_2_MB = 1f / (1024 * 1024);
        public int maxDownloads = 3;
        private readonly List<Download> _downloads = new List<Download>();
        private readonly List<Download> _tostart = new List<Download>();
        private readonly List<Download> _progressing = new List<Download>();
        public Action<long, long, float> onUpdate;

        private int _finishedIndex;
        private float _startTime;
        private float _lastTime;
        private long _lastSize;
        private bool _started;

        public long size { get; private set; }

        public long position { get; private set; }

        public float speed { get; private set; }

        public List<Download> downloads => _downloads;

        public bool IsFinished()
        {
            return _downloads.Count == _finishedIndex;
        }

        private long GetDownloadSize()
        {
            var len = 0L;
            var downloadSize = 0L;
            foreach (var download in _downloads)
            {
                downloadSize += download.position;
                len += download.len;
            }

            return downloadSize - (len - size);
        }

        [SerializeField] private float sampleTime = 0.5f;

        public void StartDownload()
        {
            _tostart.Clear();
            _finishedIndex = 0;
            _lastSize = 0L;
            Restart();
        }

        public void Restart()
        {
            _startTime = Time.realtimeSinceStartup;
            _lastTime = 0;
            _started = true;
            for (var i = _finishedIndex; i < _downloads.Count; i++)
            {
                var item = _downloads[i];
                _tostart.Add(item);
            }
        }

        public void Stop()
        {
            _tostart.Clear();
            foreach (var download in _progressing)
            {
                download.Complete(true);
                _downloads[download.id] = download.Clone();
            }

            _progressing.Clear();
            _started = false;
        }

        public void Clear()
        {
            size = 0;
            position = 0;

            _finishedIndex = 0;
            _lastTime = 0f;
            _lastSize = 0L;
            _startTime = 0;
            _started = false;
            foreach (var item in _progressing)
            {
                item.Complete(true);
            }

            _progressing.Clear();
            _downloads.Clear();
            _tostart.Clear();
        }

        public void AddDownload(string url, string savePath, MD5Creater.MD5Struct md5, long len)
        {
            var download = new Download
            {
                id = _downloads.Count,
                url = url,
                md5 = md5,
                len = len,
                savePath = savePath,
            };
            _downloads.Add(download);
            var info = new FileInfo(download.tempPath);
            if (info.Exists)
            {
                size += len - info.Length;
            }
            else
            {
                size += len;
            }
        }

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

        private void Update()
        {
            if (!_started)
                return;

            while (_progressing.Count < maxDownloads && _tostart.Count > 0)
            {
                var item = _tostart[0];
                item.Start();
                _tostart.RemoveAt(0);
                _progressing.Add(item);
            }

            for (var index = 0; index < _progressing.Count; index++)
            {
                var download = _progressing[index];
                download.Update();
                if (download.error != null && !download.finished)
                {
                    download.Complete(true);
                }

                if (download.finished)
                {
                    CommonLog.Log(MAuthor.WY, $"OnFinished:{_finishedIndex} {download.url},");

                    //下载失败文件重新下载
                    if (download.error != null)
                    {
                        CommonLog.Log(MAuthor.WY, $"start reDownload {download.url} because error:{download.error}");
                        var reDownload = download.Clone();
                        _downloads[download.id] = reDownload;
                        reDownload.Start();
                        _progressing[index] = reDownload;
                    }
                    else
                    {
                        _progressing.RemoveAt(index);
                        index--;
                        _finishedIndex++;
                    }
                }
            }

            if (IsFinished())
            {
                _started = false;
            }

            position = GetDownloadSize();

            var elapsed = Time.realtimeSinceStartup - _startTime;
            if (elapsed - _lastTime < sampleTime)
                return;

            var deltaTime = elapsed - _lastTime;
            speed = Mathf.Abs(position - _lastSize) / deltaTime;
            onUpdate?.Invoke(position, size, speed);

            _lastTime = elapsed;
            _lastSize = position;
        }
    }
}