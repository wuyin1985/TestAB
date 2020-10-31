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
    public enum UNIT
    {
        Auto,
        BYTE,
        KB,
        MB,
        GB,
    }

    public class DownloaderGroupProgress : IDownloaderProgress
    {
        public DownloaderProgress[] AllProgress;

        public override string ErrorMsg
        {
            get
            {
                foreach (var p in AllProgress)
                {
                    if (p.IsError) return p.ErrorMsg;
                }
                return null;
            }
            protected set { }
        }


        public override bool IsCompleted
        {
            get
            {
                foreach (var p in AllProgress)
                {
                    if (!p.IsCompleted) return false;
                }
                return true;
            }
            protected set { }
        }
        public override bool IsError
        {
            get
            {
                foreach (var p in AllProgress)
                {
                    if (p.IsError) return true;
                }
                return false;
            }
            protected set { }
        }
        public override long CompletedSize
        {
            get
            {
                var sumSize = 0L;
                foreach (var p in AllProgress)
                {
                    sumSize += p.CompletedSize;
                }
                return sumSize;
            }
        }

        public override long TotalSize
        {
            get
            {
                var sumSize = 0L;
                foreach (var p in AllProgress)
                {
                    sumSize += p.TotalSize;
                }
                return sumSize;
            }
        }

        public override float Speed
        {
            get
            {
                var sumSp = 0f;
                foreach (var p in AllProgress)
                {
                    sumSp += p.Speed;
                }
                return sumSp;
            }
            protected set
            {
            }
        }



        public override bool keepWaiting
        {
            get
            {
                foreach (var p in AllProgress)
                {
                    if (!p.IsCompleted) return false;
                }
                return true;
            }
        }

        public void Init(params DownloaderProgress[] progress)
        {
            AllProgress = progress;
            foreach (var p in AllProgress)
            {
                //p.FinishSignal.AddListener(OnAnyProgressFinish);
            }
        }

        private void OnAnyProgressFinish(IDownloaderProgress p)
        {
            if (IsCompleted)
            {
                //BaseFinishSignal.Dispatch();
            }
        }
    }

    public abstract class IDownloaderProgress : CustomYieldInstruction
    {
        public virtual bool IsCompleted { get; protected set; }
        public virtual bool IsError { get; protected set; }
        public virtual long BeforeCompletedSize { get; }
        public virtual long CompletedSize { get; }
        //与上面区别是，暂停的时候，可能还多下载了一点点
        public virtual long RealCompletedSize { get; set; }
        public virtual long TotalSize { get; }

        public virtual float Speed { get; protected set; }
        public virtual string ErrorMsg { get; protected set; }

        public static UNIT GetSuitedByteUnit(long len)
        {
            if (len < 1048576f) return UNIT.KB;
            else if (len < 1073741824f) return UNIT.MB;
            else return UNIT.GB;
        }


        public virtual float GetCompletedSize(UNIT unit = UNIT.BYTE)
        {
            switch (unit)
            {
                case UNIT.KB:
                    return this.CompletedSize / 1024f;
                case UNIT.MB:
                    return this.CompletedSize / 1048576f;
                case UNIT.GB:
                    return this.CompletedSize / 1073741824f;
                default:
                    return (float)this.CompletedSize;
            }
        }
        public virtual float GetTotalSize(UNIT unit = UNIT.BYTE)
        {
            switch (unit)
            {
                case UNIT.KB:
                    return this.TotalSize / 1024f;
                case UNIT.MB:
                    return this.TotalSize / 1048576f;
                case UNIT.GB:
                    return this.TotalSize / 1073741824f;
                default:
                    return (float)this.TotalSize;
            }
        }

        public virtual float GetSpeed(UNIT unit = UNIT.BYTE)
        {
            float clampSpeed = Mathf.Clamp(Speed, 0, 50.0f);
            switch (unit)
            {
                case UNIT.KB:
                    return clampSpeed / 1024f;
                case UNIT.MB:
                    return clampSpeed / 1048576f;
                case UNIT.GB:
                    return clampSpeed / 1073741824f;
                default:
                    return clampSpeed;
            }
        }
        public virtual void UpdateProgress()
        {
        }

    }


    public class DownloaderProgress : IDownloaderProgress
    {
        private long beforeCompletedSize = 0;
        private long completedSize = 0;
        //与上面区别是，暂停的时候，可能还多下载了一点点
        private long realCompletedSize = 0;

        private long totalSize = 0;

        private long lastTime = -1;
        private long lastValue = -1;
        private long lastTime2 = -1;
        private long lastValue2 = -1;
        private object result;
        private byte[] rawData;
        //是否暂停下载中
        public bool IsPause { get; private set; }
        //是否被设置停止了
        public bool IsStop { get; private set; }


        public eWWWErrorType ErrorType = eWWWErrorType.None;


        public DownloaderProgress()
        {
            InitCompletedSize(0, 0);
        }

        //public void UpdateTotalSize(long changedSized)
        //{
        //    this.totalSize += changedSized;
        //}

        public void InitCompletedSize(long totalSize, long completedSize)
        {
            this.totalSize = totalSize;
            this.completedSize = completedSize;
            this.realCompletedSize = completedSize;
            this.beforeCompletedSize = 0;

            lastTime = DateTime.UtcNow.Ticks / 10000;
            lastValue = this.completedSize;

            lastTime2 = lastTime;
            lastValue2 = lastValue;
        }

        //与上面区别是，暂停的时候，可能还多下载了一点点
        public override long RealCompletedSize
        {
            get { return this.realCompletedSize; }
            set
            {
                this.realCompletedSize = value;
                if (!IsPause)
                {
                    this.OnUpdate();
                }
            }
        }

        public override long BeforeCompletedSize { get { return this.beforeCompletedSize; } }

        public override long CompletedSize
        {
            get
            {
                return this.completedSize + this.beforeCompletedSize;
            }
        }

        public override long TotalSize { get { return this.totalSize + this.beforeCompletedSize; } }
         
        private void OnUpdate()
        {
            //刷新UI完成值
            this.completedSize = this.realCompletedSize;

            long now = DateTime.UtcNow.Ticks / 10000;

            if ((now - lastTime) >= 1000) //1秒刷新1次
            {
                lastTime2 = lastTime;
                lastValue2 = lastValue;

                this.lastTime = now;
                this.lastValue = this.completedSize;

                float dt = (now - lastTime2) / 1000f;
                Speed = (this.completedSize - this.lastValue2) / dt;
            }
            else if ((now - lastTime) < 0)
            {
                this.lastTime = now;
                this.lastValue = this.completedSize;
            }

        }

        public virtual float Value
        {
            get
            {
                if (this.TotalSize <= 0)
                    return 0f;

                return this.CompletedSize / (float)this.TotalSize;
            }
        }

        public override bool keepWaiting
        {
            get { return !IsCompleted; }
        }

        /// <summary>
        /// 设置之前已下载内容量
        /// </summary>
        /// <param name="beforeCompletedSize"></param>
        public void SetBeforeDownloadSize(long beforeCompletedSize)
        {
            this.beforeCompletedSize = beforeCompletedSize;
        }

        public void SetPause(bool p)
        {
            if (!IsStop)
            {
                IsPause = p;
                if (!IsPause)
                {
                    OnUpdate();
                }
            }
        }


        public void SetStop()
        {
            if (!IsStop)
            {
                IsPause = true;
                IsStop = true;
                SetException(new Exception("Stop Download"), eWWWErrorType.PlayerStop);
            }
        }

        public void SetException(Exception e, eWWWErrorType type)
        {
            SetException(e.Message, type);
        }
        public void SetException(string e, eWWWErrorType type)
        {
            CommonLog.Log($"SetException {e} {type}");
            //Debug.LogError(StackTraceUtility.ExtractStackTrace());
            ErrorType = type;
            if (e == null)
            {
                IsError = false;
                ErrorMsg = "";
            }
            else
            {
                IsError = true;
                ErrorMsg = e;
            }
            IsCompleted = true;
        }
        
        private string _Text;
        public string Text
        {
            get
            {
                if (_Text == null)
                {
                    try
                    {
                        _Text = Encoding.UTF8.GetString(rawData);
                    }
                    catch (Exception e)
                    {
                        CommonLog.Error(e);
                        _Text = "";
                    }
                }
                return _Text;
            }
        }

        public byte[] Raw
        {
            get { return rawData; }
        }

        public void SetRaw(byte[] data)
        {
            rawData = data;
            IsCompleted = true;
            //FinishSignal.Dispatch(this);
            //base.BaseFinishSignal.Dispatch();
        }

        public void SetComplete()
        {
            IsCompleted = true;
        }

        public override void UpdateProgress()
        {
            base.UpdateProgress();
            OnUpdate();
        }
    }

    public class DownloadFileInfo
    {
        public long FileSize;
        public string FileName;
        public long MapedFileName_MD51;
        public long MapedFileName_MD52;
    }

    public enum eWWWErrorType { None, WriteFileError, NoSpaceError, DownloadNetError, FileFormatError, PlayerStop }



}
