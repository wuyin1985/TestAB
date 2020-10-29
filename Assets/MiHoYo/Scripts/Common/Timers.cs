using System;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SignalTools;
using UnityEngine.Profiling;

namespace TimerUtils
{
    public enum FollowTargetMode
    {
        TargetDisactiveDoStop,
        TargetDisactiveDoPause,
        TargetDisactiveDoNothing,
    }

    public enum eFollowTargetType
    {
        None = 0,
        Target1 = 1,
        Target2 = 2,
    }

    #region Task

    public interface TimerTaskRunner
    {
        void Run();
    }


    public class TimerTask
    {
        Action _fun;
        private TimerTaskRunner _runner;

        Action NextFun;

#if DEBUG_PERFORMANCE_TRACE
        private string stackInfo;
        private string stackInfoNextFun;
#endif
        //是否跟随对象生命周期
        protected eFollowTargetType followTargetType = eFollowTargetType.None;

        protected MonoBehaviour target;
        protected MonoBehaviour target2;

        protected FollowTargetMode followTargetMode = FollowTargetMode.TargetDisactiveDoStop;

        protected float _delayTime;

        protected float _loopDuration;

        protected int _loopCount;

        public bool Stop { get; private set; }

        public bool Running { get; private set; }

        public bool Paused { get; private set; }

        public bool IgnoreTimeScale { get; set; }

        //创建对象设置的变量
        private float _savedDelayTime;

        private bool _savedNextFrame;

        //创建对象设置的变量
        private int _savedLoopCount;

        //创建对象设置的变量
        private float _savedLoopDuration;

        private bool addedUpdateListener = false;

        //public event FinishedHandler Finished;
        //public Signal  Finished;

        public object SavedParam1;
        public object SavedParam2;
        public object SavedParam3;
        public object SavedParam4;

        public TimerTask()
        {
        }

        public TimerTask(float delayTime, System.Action fun, int loopCount, float loopDuration
#if DEBUG_PERFORMANCE_TRACE
            , string memberName = null
            , string path = null
            , int sourceLineNumber = default(int) 
#endif
        )
        {
            Init(delayTime, fun, loopCount, loopDuration
#if DEBUG_PERFORMANCE_TRACE
                , memberName
                , path
                , sourceLineNumber
#endif
            );
        }

        public TimerTask(float delayTime, TimerTaskRunner runner, int loopCount, float loopDuration
#if DEBUG_PERFORMANCE_TRACE
            , string memberName = null
            , string path = null
            , int sourceLineNumber = default(int) 
#endif
        )
        {
            Init(delayTime, runner, loopCount, loopDuration
#if DEBUG_PERFORMANCE_TRACE
                , memberName
                , path
                , sourceLineNumber
#endif
            );
        }

        /// <summary>
        /// 初始化，如果调用Clear再调用Init等价于创建一个新的对象
        /// </summary>
        /// <param name="delayTime"></param>
        /// <param name="fun"></param>
        /// <param name="loopCount"></param>
        /// <param name="loopDuration"></param>
        /// <returns></returns>
        public void Init(float delayTime, System.Action fun, int loopCount, float loopDuration
#if DEBUG_PERFORMANCE_TRACE
            , string memberName = null
            , string path = null
            , int sourceLineNumber = default(int) 
#endif
        )
        {
            _savedDelayTime = delayTime;
            //至少一次
            _savedLoopCount = Mathf.Clamp(loopCount, 1, int.MaxValue);
            _savedLoopDuration = loopDuration;
            _fun = fun;
            _runner = null;
#if DEBUG_PERFORMANCE_TRACE
            stackInfo = $"{path} : {memberName}:Line({sourceLineNumber})"; 
#endif
            DoReset(false);
        }

        public void Init(bool nextFrame, System.Action fun, int loopCount, float loopDuration
#if DEBUG_PERFORMANCE_TRACE
            , string memberName = null
            , string path = null
            , int sourceLineNumber = default(int) 
#endif
        )
        {
            _savedDelayTime = 0.001f;
            _savedNextFrame = nextFrame;
            //至少一次
            _savedLoopCount = Mathf.Clamp(loopCount, 1, int.MaxValue);
            _savedLoopDuration = loopDuration;
            _fun = fun;
            _runner = null;
#if DEBUG_PERFORMANCE_TRACE
            stackInfo = $"{path} : {memberName}:Line({sourceLineNumber})"; 
#endif
            DoReset(false);
        }

        /// <summary>
        /// 初始化，如果调用Clear再调用Init等价于创建一个新的对象
        /// </summary>
        /// <param name="delayTime"></param>
        /// <param name="fun"></param>
        /// <param name="loopCount"></param>
        /// <param name="loopDuration"></param>
        /// <returns></returns>
        public void Init(float delayTime, TimerTaskRunner runner, int loopCount, float loopDuration
#if DEBUG_PERFORMANCE_TRACE
            , string memberName = null
            , string path = null
            , int sourceLineNumber = default(int) 
#endif
        )
        {
            _savedDelayTime = delayTime;
            //至少一次
            _savedLoopCount = Mathf.Clamp(loopCount, 1, int.MaxValue);
            _savedLoopDuration = loopDuration;
            _fun = null;
            _runner = runner;
#if DEBUG_PERFORMANCE_TRACE
            stackInfo = $"{path} : {memberName}:Line({sourceLineNumber})"; 
#endif
            DoReset(false);
        }

        /// <summary>
        /// 将当前Timers重置到刚创建出来的状态
        /// </summary>
        public virtual void Clear()
        {
            _fun = null;
            _runner = null;
            Running = false;
            Stop = false;
            Paused = false;

            followTargetType = eFollowTargetType.None;

            target = null;
            target2 = null;

            followTargetMode = FollowTargetMode.TargetDisactiveDoStop;

            _delayTime = 0;

            _loopDuration = 0;

            _loopCount = 0;

            IgnoreTimeScale = false;

            _savedDelayTime = 0;

            _savedLoopCount = 0;

            _savedLoopDuration = 0;

            
#if DEBUG_PERFORMANCE_TRACE
            stackInfo = null; 
#endif
            if (addedUpdateListener)
            {
                TimerTaskManager.Instance.RemoveUpdateTimer(this);
            }

            addedUpdateListener = false;

            SavedParam1 = null;
            SavedParam2 = null;
            SavedParam3 = null;
            SavedParam4 = null;
        }


        public TimerTask DoPause()
        {
            Paused = true;
            return this;
        }

        private TimerTask DoUnpause()
        {
            Paused = false;
            return this;
        }

        public TimerTask DoStart(bool resetOnStart = false)
        {
            if (resetOnStart) DoReset(false);
            if (Paused) DoUnpause();
            if (Running) return this;
            if (Stop) return this;

            //Finished = new Signal();
            Running = true;
            //第一次立刻调用
            if (_delayTime <= 0)
            {
                //_loopCount--;
                OnTimeCallBack();
                if (_loopCount > 0)
                {
                    if (!addedUpdateListener)
                    {
                        addedUpdateListener = true;
                        TimerTaskManager.Instance.AddUpdateTimer(this);
                    }
                }
            }
            else
            {
                if (!addedUpdateListener)
                {
                    addedUpdateListener = true;
                    TimerTaskManager.Instance.AddUpdateTimer(this);
                }
            }
            return this;
        }

        /// <summary>
        /// 计时器重置到默认创建好的状态，参数保留
        /// </summary>
        /// <returns>The reset.</returns>
        protected TimerTask DoReset(bool autoStart = true)
        {
            Paused = false;
            Stop = false;
            Running = false;

            _delayTime = _savedDelayTime;
            _loopCount = _savedLoopCount;
            _loopDuration = _savedLoopDuration;
            this.target = null;
            this.target2 = null;
            followTargetType = eFollowTargetType.None;
            if (autoStart) DoStart();
            return this;
        }

        public void DoStop()
        {
            if (Stop) return;
            Stop = true;
            Running = false;
            if (addedUpdateListener)
            {
                TimerTaskManager.Instance.RemoveUpdateTimer(this);
            }
            addedUpdateListener = false;
            //调用下一个
            if (NextFun != null)
            {
                try
                {
#if DEBUG_PERFORMANCE_TRACE 
                    Profiler.BeginSample(stackInfoNextFun);
#endif
                    NextFun();
#if DEBUG_PERFORMANCE_TRACE 
                    Profiler.EndSample();
#endif
                }
                catch (Exception e)
                {
                    //TimerLogger.Error(e.ToString());
                    CommonLog.Error(e.ToString());
                }
            }
        }


        /// <summary>
        /// 生命周期跟随一个对象
        /// </summary>
        /// <param name="target"></param>
        /// <param name="ignoreTargetActive">忽略对象激活状态变化</param>
        public TimerTask DoFollow(MonoBehaviour target, FollowTargetMode followMode)
        {
            if (target == null) return this;
            this.target = target;
            followTargetType = eFollowTargetType.Target1;
            this.followTargetMode = followMode;
            return this;
        }


        /// <summary>
        /// 生命周期跟随一个对象
        /// </summary>
        /// <param name="target"></param>
        /// <param name="ignoreTargetActive">忽略对象激活状态变化</param>
        public TimerTask DoFollow(MonoBehaviour target, MonoBehaviour target2, FollowTargetMode followMode)
        {
            if (target == null || target2 == null) return this;
            this.target = target;
            this.target2 = target2;
            followTargetType = eFollowTargetType.Target2;
            this.followTargetMode = followMode;
            return this;
        }

        public void DoUpdate()
        {
            //停止
            if (!Running) return;

            //处理生命周期跟随
            if (followTargetType != eFollowTargetType.None)
            {
                if (Target_IsAllNotNull())
                {
                    if (Target_IsAnyNoActive())
                    {
                        if (followTargetMode == FollowTargetMode.TargetDisactiveDoNothing)
                        {

                        }
                        else if (followTargetMode == FollowTargetMode.TargetDisactiveDoPause)
                        {
                            return;
                        }
                        else if (followTargetMode == FollowTargetMode.TargetDisactiveDoStop)
                        {
                            DoStop();
                        }
                    }
                }
                else
                {
                    DoStop();
                    return;
                }
            }

            //暂停
            if (Paused)
            {
                return;
            }
			
            //计时
            float deltaTime = IgnoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;

            if (_savedNextFrame)
            {
                _savedNextFrame = false;
                return;
            }

            _delayTime = Mathf.Clamp(_delayTime - deltaTime, 0, float.MaxValue);

            if (_delayTime <= 0)
            {
                OnTimeCallBack();
            }
        }

        //是否所有target可以认定不NUll
        private bool Target_IsAllNotNull()
        {
            if (followTargetType == eFollowTargetType.None)
            {
                return true;
            }
            else if (followTargetType == eFollowTargetType.Target1)
            {
                return (target != null);
            }
            else if (followTargetType == eFollowTargetType.Target2)
            {
                if (target != null && target2 != null) { return true; }
                else { return false; }
            }
            else { return true; }
        }
        //是否有任何target不是激活的
        private bool Target_IsAnyNoActive()
        {
            if (followTargetType == eFollowTargetType.None)
            {
                return false;
            }
            else if (followTargetType == eFollowTargetType.Target1)
            {
                return (!(target.enabled && target.gameObject.activeSelf));
            }
            else if (followTargetType == eFollowTargetType.Target2)
            {
                return (!(target.enabled && target.gameObject.activeSelf)) ||
                       (!(target2.enabled && target2.gameObject.activeSelf));

            }
            else { return true; }
        }

        private void OnTimeCallBack()
        {
            RunCallBack();
            _loopCount--;
            if (_loopCount <= 0)
            {
                DoStop();
                TimerTaskManager.Instance.ReturnToPool(this);
            }
            else
            {
                _delayTime = _loopDuration;
            }
        }

        protected virtual void RunCallBack()
        {
            if (_runner != null)
            {
#if DEBUG_PERFORMANCE_TRACE
                Profiler.BeginSample(stackInfo); 
#endif
                _runner.Run();
#if DEBUG_PERFORMANCE_TRACE
                Profiler.EndSample(); 
#endif
            }
            else if (_fun != null)
            {
                try
                {
#if DEBUG_PERFORMANCE_TRACE
                    Profiler.BeginSample(stackInfo); 
#endif
                    _fun();
#if DEBUG_PERFORMANCE_TRACE
                    Profiler.EndSample(); 
#endif
                }
                catch (Exception e)
                {
                    //TimerLogger.Error(e.ToString());
                    CommonLog.Error(e.ToString());
                }
            }
            else
            {
                DoStop();
            } 
        }

        /// <summary>
        /// 设置调用CallBack后的回调，可以更容易实现调用链，Loop的话每次都会调用,如果Timer已经结束了，会立刻调用
        /// </summary>
        public void DoAfterTimer(System.Action f
#if DEBUG_PERFORMANCE_TRACE
            , string memberName = null
            , string path = null
            , int sourceLineNumber = default(int) 
#endif
        )
        {
#if DEBUG_PERFORMANCE_TRACE
            stackInfoNextFun = $"DoAffterTimer {path} : {memberName}:Line({sourceLineNumber})"; 
#endif
            NextFun = f;
            if (Stop)
            {
                try
                {
#if DEBUG_PERFORMANCE_TRACE
                    Profiler.BeginSample(stackInfoNextFun);                    
#endif
                    NextFun();
#if DEBUG_PERFORMANCE_TRACE
                    Profiler.EndSample();
#endif
                }
                catch (Exception e)
                {
                    //TimerLogger.Error(e.ToString());
                    CommonLog.Error(e.ToString());
                }
            }
        }
    }

    #endregion

    public class TimerTaskManager : SimpleSingletonProvider<TimerTaskManager>
    {
        public SimpleFirstList<TimerTask> HandleList = new SimpleFirstList<TimerTask>(16);
        public TimerTask[] ReleaseList = new TimerTask[4];//用于回收延迟一帧的池，不用特别多

        protected override void InstanceInit()
        {
            base.InstanceInit();
            HandleList.Clear();
            HandleList.IsAutoShrink = false;
            //警告！！！！！！！！！！！！！！！！！！！这里绝对不能用其他Update！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！
            BaseMonoManagerForUnitySingleton.Instance.BaseUpdate.AddListener(DoUpdate);
        }


        protected void DoUpdate()
        {
            //延迟一帧回收
            for (int i = 0; i < ReleaseList.Length; i++)
            {
                var hand = ReleaseList[i];
                if (hand != null)
                {
                    ReleaseList[i] = null;
                    Timers.Pool.ReleaseATimer(hand);
                }
            }
            ////Remove 
            //for (int i = 0; i < RemoveList.Count; i++)
            //{
            //    HandleList.Add(RemoveList[i]);
            //}
            //RemoveList.Clear();
            ////Add
            //for (int i = 0; i < AddList.Count; i++)
            //{
            //    HandleList.Add(AddList[i]);
            //}
            //AddList.Clear();
            //Update
            int handleCount = HandleList.capacity;
            for (int i = 0; i < handleCount; i++)
            {
                var hand = HandleList.buffer[i];
                if (hand != null)
                {
                    hand.DoUpdate();
                }
            }
            HandleList.DoShrinkHalf();

        }

        public void AddUpdateTimer(TimerTask task)
        {
            // AddList.Add(task);
            HandleList.Add(task);
        }

        public void RemoveUpdateTimer(TimerTask task)
        {
            //RemoveList.Remove(task);
            HandleList.Remove(task);
        }

        public void ReturnToPool(TimerTask task)
        {
            //for (int i = 0; i < ReleaseList.Length; i++)
            //{
            //    var hand = ReleaseList[i];
            //    if (hand == null)
            //    { 
            //        ReleaseList[i] = task;
            //        return;
            //    }
            //}
        }
    }

    /// <summary>
    /// 一个相当风骚完美Perfect的计时器回调函数工具
    /// Author:Eric
    /// </summary>
    public class Timers
    {
        //  池没啥特殊用处，暂时屏蔽
        public static TimerTaskPool Pool = new TimerTaskPool(10).Preload(10);

        /// <summary>
        /// 触发计时器
        /// </summary>
        /// <param name="delay">第一次延迟事件</param>
        /// <param name="func">触发函数</param>
        /// <param name="ignoreTimeScale">是否忽视时间缩放</param>
        /// <param name="loopCount">计时器触发次数</param>
        /// <param name="loopduration">计时器触发之间间隔(注意不是总时长)</param>
        public static TimerTask TimerStart(float delay, System.Action func, bool ignoreTimeScale = true, int loopCount = 1, float loopduration = 0f
#if DEBUG_PERFORMANCE_TRACE
            ,[CallerMemberName] string memberName = null
            ,[CallerFilePath] string path = null
            ,[CallerLineNumber] int sourceLineNumber = default(int) 
#endif
        )
        {
            var task = Pool.GetATimer();
            task.Init(delay, func, loopCount, loopduration
#if DEBUG_PERFORMANCE_TRACE
                , memberName
                , path
                , sourceLineNumber
#endif
            );
            task.IgnoreTimeScale = ignoreTimeScale;
            return task.DoStart();
            //return new TimerTask(delay, func, loopCount, loopduration).DoFollow(followtarget, followMode).DoStart();
        }

        public static TimerTask TimerTaskNextFrame(Action func, bool ignoreTimeScale = true, int loopCount = 1, float loopduration = 0f
#if DEBUG_PERFORMANCE_TRACE
            ,[CallerMemberName] string memberName = null
            ,[CallerFilePath] string path = null
            ,[CallerLineNumber] int sourceLineNumber = default(int) 
#endif
        )
        {
            var task = Pool.GetATimer();
            task.Init(true, func, loopCount, loopduration
#if DEBUG_PERFORMANCE_TRACE
                , memberName
                , path
                , sourceLineNumber
#endif
            );
            task.IgnoreTimeScale = ignoreTimeScale;
            return task.DoStart();
        }


        public static TimerTask TimerStart(float delay, System.Action func, MonoBehaviour followtarget, FollowTargetMode followMode = FollowTargetMode.TargetDisactiveDoStop, bool ignoreTimeScale = true, int loopCount = 1, float loopduration = 0f
#if DEBUG_PERFORMANCE_TRACE
            ,[CallerMemberName] string memberName = null
            ,[CallerFilePath] string path = null
            ,[CallerLineNumber] int sourceLineNumber = default(int) 
#endif
        )
        {
            var task = Pool.GetATimer();
            task.Init(delay, func, loopCount, loopduration
#if DEBUG_PERFORMANCE_TRACE
                , memberName
                , path
                , sourceLineNumber
#endif
            );
            task.IgnoreTimeScale = ignoreTimeScale;
            return task.DoStart().DoFollow(followtarget, followMode);
            //return new TimerTask(delay, func, loopCount, loopduration).DoFollow(followtarget, followMode).DoStart();
        }

        public static TimerTask TimerStart(float delay, System.Action func, MonoBehaviour followtarget, MonoBehaviour followtarget2, FollowTargetMode followMode = FollowTargetMode.TargetDisactiveDoStop, bool ignoreTimeScale = true, int loopCount = 3, float loopduration = 0f
#if DEBUG_PERFORMANCE_TRACE
            ,[CallerMemberName] string memberName = null
            ,[CallerFilePath] string path = null
            ,[CallerLineNumber] int sourceLineNumber = default(int) 
#endif
        )
        {
            var task = Pool.GetATimer();
            task.Init(delay, func, loopCount, loopduration
#if DEBUG_PERFORMANCE_TRACE
                , memberName
                , path
                , sourceLineNumber
#endif
            );
            task.IgnoreTimeScale = ignoreTimeScale;
            return task.DoStart().DoFollow(followtarget, followtarget2, followMode);
            //return new TimerTask(delay, func, loopCount, loopduration).DoFollow(followtarget, followMode).DoStart();
        }

        public static TimerTask TimerStart(float delay, TimerTaskRunner runner, MonoBehaviour followtarget, FollowTargetMode followMode = FollowTargetMode.TargetDisactiveDoStop, bool ignoreTimeScale = true, int loopCount = 1, float loopduration = 0f
#if DEBUG_PERFORMANCE_TRACE
            ,[CallerMemberName] string memberName = null
            ,[CallerFilePath] string path = null
            ,[CallerLineNumber] int sourceLineNumber = default(int) 
#endif
        )
        {
            var task = Pool.GetATimer();
            task.Init(delay, runner, loopCount, loopduration
#if DEBUG_PERFORMANCE_TRACE
                , memberName
                , path
                , sourceLineNumber
#endif
            );
            task.IgnoreTimeScale = ignoreTimeScale;
            return task.DoStart().DoFollow(followtarget, followMode);
            //return new TimerTask(delay, runner, loopCount, loopduration).DoFollow(followtarget, followMode).DoStart();
        }

        public static TimerTask TimerStart(float delay, TimerTaskRunner runner, MonoBehaviour followtarget, MonoBehaviour followtarget2, FollowTargetMode followMode = FollowTargetMode.TargetDisactiveDoStop, bool ignoreTimeScale = true, int loopCount = 1, float loopduration = 0f
#if DEBUG_PERFORMANCE_TRACE
            ,[CallerMemberName] string memberName = null
            ,[CallerFilePath] string path = null
            ,[CallerLineNumber] int sourceLineNumber = default(int) 
#endif
        )
        {
            var task = Pool.GetATimer();
            task.Init(delay, runner, loopCount, loopduration
#if DEBUG_PERFORMANCE_TRACE
                , memberName
                , path
                , sourceLineNumber
#endif
            );
            task.IgnoreTimeScale = ignoreTimeScale;
            return task.DoStart().DoFollow(followtarget, followMode);
            //return new TimerTask(delay, runner, loopCount, loopduration).DoFollow(followtarget, followMode).DoStart();
        }
        //  public static TimerTask TimerStart<T>(float delay, System.Action<T> func, T param, MonoBehaviour followtarget, FollowTargetMode followMode = FollowTargetMode.TargetDisactiveDoStop, int loopCount = 1, float loopduration = 0f)
        //  {
        //      return new TimerTaskT<T>(delay, func, loopCount, loopduration).SetParams(param).DoFollow(followtarget, followMode).DoStart();
        //  }


        //	    public static TimerTask TimerStart<T1, T2>(float delay, System.Action<T1, T2> func, T1 param1, T2 param2, int loopCount = 1, float loopduration = 0f)
        //	    {
        //	        return new TimerTaskT2<T1, T2>(delay, func, loopCount, loopduration).SetParams(param1, param2).DoStart();
        //	    }


        /// <summary>
        /// AudioItem的缓冲池   池容量固定，起到一定的消减GC作用
        /// Author:Eric
        /// </summary>
        public class TimerTaskPool
        {

            private TimerTask[] EmptyTimerArr;
            public TimerTaskPool(int cap)
            {
                if (cap < 1) cap = 1;
                EmptyTimerArr = new TimerTask[cap];
            }

            public TimerTaskPool Preload(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    var t = new TimerTask();
                    ReleaseATimer(t);
                }
                return this;
            }

            /// <summary>
            /// 从池里拿个，如果没有就创个
            /// </summary>
            /// <returns></returns>
            public TimerTask GetATimer()
            {
                for (int i = 0; i < EmptyTimerArr.Length; i++)
                {
                    var t = EmptyTimerArr[i];
                    if (t != null)
                    {
                        EmptyTimerArr[i] = null;
                        return t;
                    }
                }
                var item = new TimerTask();
                return item;
            }

            /// <summary>
            /// 归还timer,如果塞不下，就不要了
            /// </summary>
            /// <param name="tim"></param>
            public void ReleaseATimer(TimerTask tim)
            {
                tim.Clear();
                for (int i = 0; i < EmptyTimerArr.Length; i++)
                {
                    var t = EmptyTimerArr[i];
                    if (t == null)
                    {
                        EmptyTimerArr[i] = tim;
                        return;
                    }
                }
            }

        }

    }
}