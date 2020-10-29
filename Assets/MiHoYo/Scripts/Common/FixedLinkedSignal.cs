using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TimerUtils;
using UnityEngine;
using UnityEngine.Profiling;
using System.Runtime.CompilerServices;
using Debug = UnityEngine.Debug;

namespace SignalTools
{

    public class SignalFollowInfo
    {
        public MonoBehaviour Followed;
        public eFollowTargetMode Mode;
    }
    public enum eFollowTargetMode
    {
        TargetNoneDoNothing,
        TargetNoneDoStop,
        TargetDisactiveDoNothing,//None的时候，默认移除
        TargetDisactiveDoJump,//None的时候，默认移除
        TargetDisactiveDoStop,//None的时候，默认移除
    }
    /// <summary>
    /// 基于LinkedList和HashSet优化后的Signal，能有效减少大量调用的GC
    /// PS.已经不是基于上述结构了，基于FixedArrayList,LinkedList有GC，固定48
    /// </summary>
    public class FixedLinkedSignal
    {
        /// <summary>
        /// 如果Once有添加，则需要调用清除
        /// </summary>
        public SafeFirstList<EventVoid> Listener;
        public SafeFirstList<EventVoid> OnceListener;
        public Dictionary<EventVoid, SignalFollowInfo> ListenerFollowInfo;

        public FixedLinkedSignal(int capcity = 8)
        {
            if (capcity <= 0)
            {
                capcity = 8;
            }
            Listener = new SafeFirstList<EventVoid>(capcity);
            OnceListener = new SafeFirstList<EventVoid>(capcity);
        }
        
#if !DEBUG_PERFORMANCE_TRACE
        public void AddListener(EventVoid callback)
#else
        public void AddListener(EventVoid callback,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string path = null,
            [CallerLineNumber] int sourceLineNumber = default(int) )
#endif
        {
#if !DEBUG_PERFORMANCE_TRACE
            AddUnique(Listener, callback);
#else
            AddUnique(Listener, callback, null,eFollowTargetMode.TargetNoneDoNothing ,memberName, path, sourceLineNumber);
#endif
        }

#if !DEBUG_PERFORMANCE_TRACE
        public void AddListener(EventVoid callback, MonoBehaviour followed, eFollowTargetMode mode = eFollowTargetMode.TargetDisactiveDoStop)
#else
        public void AddListener(EventVoid callback, MonoBehaviour followed, eFollowTargetMode mode = eFollowTargetMode.TargetDisactiveDoStop,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string path = null,
            [CallerLineNumber] int sourceLineNumber = default(int) )
#endif
        {
#if !DEBUG_PERFORMANCE_TRACE
            AddUnique(Listener, callback, followed, mode);
#else
            AddUnique(Listener, callback, followed, mode, memberName, path, sourceLineNumber);
#endif
        }
#if !DEBUG_PERFORMANCE_TRACE
        public void AddOnce(EventVoid callback)
        {
            AddUnique(OnceListener, callback);
        }
        public void AddOnce(EventVoid callback, MonoBehaviour followed , eFollowTargetMode mode = eFollowTargetMode.TargetDisactiveDoStop)
        {
            AddUnique(OnceListener, callback, followed, mode);
        }
#else
        public void AddOnce(EventVoid callback,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string path = null,
            [CallerLineNumber] int sourceLineNumber = default(int) )
        {
            AddUnique(OnceListener, callback,null, eFollowTargetMode.TargetNoneDoNothing, memberName, path, sourceLineNumber);
        }

        public void AddOnce(EventVoid callback, MonoBehaviour followed , eFollowTargetMode mode = eFollowTargetMode.TargetDisactiveDoStop,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string path = null,
            [CallerLineNumber] int sourceLineNumber = default(int) )
        {
            AddUnique(OnceListener, callback, followed, mode, memberName, path, sourceLineNumber);
        }
#endif

        public void RemoveListener(EventVoid callback)
        {
            Listener.Remove(callback);
#if DEBUG_PERFORMANCE_TRACE
            FixedLinkedEventTrace<EventVoid>.RemoveTrace(callback);
#endif
            if (ListenerFollowInfo != null)
            {
                ListenerFollowInfo.Remove(callback);
            }
        }

        public void RemoveAllListener()
        {
            Listener.Clear();
#if DEBUG_PERFORMANCE_TRACE
            FixedLinkedEventTrace<EventVoid>.RemoveAll();
#endif
            if (ListenerFollowInfo != null)
            {
                ListenerFollowInfo.Clear();
            }
        }

#if !DEBUG_PERFORMANCE_TRACE
        public void Dispatch()
#else
        public void Dispatch(
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string path = null,
            [CallerLineNumber] int sourceLineNumber = default(int) )
#endif
        {
#if !DEBUG_PERFORMANCE_TRACE
            Profiler.BeginSample("DispatchListener");
#else
            Profiler.BeginSample($"DispatchListener: {path}:{memberName}:{sourceLineNumber}");
#endif
            DispatchList(Listener,false);
            Profiler.EndSample();
#if !DEBUG_PERFORMANCE_TRACE
            Profiler.BeginSample("DispatchOnceListener");
#else
            Profiler.BeginSample($"DispatchOnceListener: {path}:{memberName}:{sourceLineNumber}");
#endif
            DispatchList(OnceListener,true);
            Profiler.EndSample();
        }
          
        private void DispatchList(SafeFirstList<EventVoid> listeners,bool isClear=false)
        {
            listeners.LockForeach();
            int len = listeners.Capacity;
            for (int i = 0; i < len; i++)
            {
                var item = listeners[i];
                if (item != null)
                {
#if DEBUG_PERFORMANCE_TRACE
                    bool bSampled = FixedLinkedEventTrace<EventVoid>.BeginSample(item);
#endif
                    if (listeners == Listener)
                    {
                        CheckAndDispatch(item);
                    }
                    else if (listeners == OnceListener)
                    {
                        item.Invoke();
                    }
#if DEBUG_PERFORMANCE_TRACE
                    if(bSampled)
                        FixedLinkedEventTrace<EventVoid>.EndSample();
#endif
                }
            }
            if(isClear){listeners.Clear(true);}
            listeners.UnlockForeach();
        }

        private void CheckAndDispatch(EventVoid item )
        {
            if (ListenerFollowInfo == null)
            {
                item.Invoke();
                return;
            }
            SignalFollowInfo followInfo;
            if (ListenerFollowInfo.TryGetValue(item, out followInfo))
            {
                if (followInfo.Followed == null)
                {
                    if (followInfo.Mode == eFollowTargetMode.TargetNoneDoNothing)
                    {
                        item.Invoke();
                        return;
                    }
                    else if(followInfo.Mode == eFollowTargetMode.TargetNoneDoStop)
                    {
                        //因为有锁定API，所以还好还好，可以循环的时候调用
                        RemoveListener(item);
                        return;
                    }
                    else 
                    {
                        //默认就是NoDoStop
                        RemoveListener(item);
                        return;
                    }
                }
                //如果Disactive
                else if (!followInfo.Followed.enabled || !followInfo.Followed.gameObject.activeSelf)
                {
                    if (followInfo.Mode == eFollowTargetMode.TargetDisactiveDoNothing)
                    {
                        item.Invoke();
                        return;
                    }
                    else if (followInfo.Mode == eFollowTargetMode.TargetDisactiveDoStop)
                    {
                        //因为有锁定API，所以还好还好，可以循环的时候调用
                        RemoveListener(item);
                        return;
                    }
                    else if (followInfo.Mode == eFollowTargetMode.TargetDisactiveDoJump)
                    {
                        return;
                    }
                }
                else//一切正常
                {
                    item.Invoke();
                    return;
                }
            }//没有配置的情况，直接Invok
            else
            {
                item.Invoke();
                return;
            }
        }



        private void AddUnique(SafeFirstList<EventVoid> listeners, EventVoid callback, MonoBehaviour followed = null, eFollowTargetMode mode = eFollowTargetMode.TargetNoneDoNothing, string memberName = null, string path = null,  int sourceLineNumber = default(int) )
        {
            bool contains = !listeners.Contains(callback);
            if (contains)
            {
#if DEBUG_PERFORMANCE_TRACE
                FixedLinkedEventTrace<EventVoid>.AddTrace(callback, memberName, path, sourceLineNumber);
#endif
                listeners.Add(callback);
                //添加追踪事件
                if (mode != eFollowTargetMode.TargetNoneDoNothing)
                {
                    if (listeners == Listener)
                    {
                        if (ListenerFollowInfo == null)
                        {
                            ListenerFollowInfo = new Dictionary<EventVoid, SignalFollowInfo>((int)(listeners.Capacity));
                        }
                        ListenerFollowInfo[callback] = new SignalFollowInfo()
                        {
                            Followed = followed,
                            Mode = mode
                        };
                    } 
                }
            }
        }
    }


    /// <summary>
    /// 基于LinkedList和HashSet优化后的Signal，能有效减少大量调用的GC
    /// PS.已经不是基于上述结构了，基于FixedArrayList,LinkedList有GC，固定48
    /// </summary>
    public class FixedLinkedSignal<T>
    {
        /// <summary>
        /// 如果Once有添加，则需要调用清除
        /// </summary> 
        public SafeFirstList<EventT<T>> Listener;
        public SafeFirstList<EventT<T>> OnceListener;
        public Dictionary<EventT<T>, SignalFollowInfo> ListenerFollowInfo;

        public bool HasListener { get { return Listener.Count > 0 || OnceListener.Count > 0; } }

        public FixedLinkedSignal(int capcity = 4)
        {
            if (capcity <= 0)
            {
                capcity = 8;
            }
            Listener = new SafeFirstList<EventT<T>>(capcity);
            OnceListener = new SafeFirstList<EventT<T>>(capcity);
        }

#if !DEBUG_PERFORMANCE_TRACE
        public void AddListener(EventT<T> callback, MonoBehaviour followed = null, eFollowTargetMode mode = eFollowTargetMode.TargetNoneDoStop)
#else
        public void AddListener(EventT<T> callback, MonoBehaviour followed = null, eFollowTargetMode mode = eFollowTargetMode.TargetNoneDoStop, 
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string path = null,
            [CallerLineNumber] int sourceLineNumber = default(int))
#endif
        {
            if (null == followed)
            {
                mode = eFollowTargetMode.TargetNoneDoNothing;
            }
#if !DEBUG_PERFORMANCE_TRACE
            AddUnique(Listener, callback, followed, mode);
#else
            AddUnique(Listener, callback, followed, mode, memberName, path, sourceLineNumber);
#endif
        }

#if !DEBUG_PERFORMANCE_TRACE
        public void AddOnce(EventT<T> callback)
        {
            AddUnique(OnceListener, callback);
        }
#else
        public void AddOnce(EventT<T> callback,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string path = null,
            [CallerLineNumber] int sourceLineNumber = default(int))
        {
            AddUnique(OnceListener, callback, null, eFollowTargetMode.TargetNoneDoNothing,memberName, path, sourceLineNumber);
        }
#endif
        public void RemoveListener(EventT<T> callback)
        {
            Listener.Remove(callback);
#if DEBUG_PERFORMANCE_TRACE
            FixedLinkedEventTrace<EventT<T>>.RemoveTrace(callback);
#endif
            if (ListenerFollowInfo != null)
            {
                ListenerFollowInfo.Remove(callback);
            }
        }

        public void RemoveAllListener()
        {
            Listener.Clear();
#if DEBUG_PERFORMANCE_TRACE
            FixedLinkedEventTrace<EventT<T>>.RemoveAll();
#endif
            if (ListenerFollowInfo != null)
            {
                ListenerFollowInfo.Clear();
            }
        }

#if !DEBUG_PERFORMANCE_TRACE
        public void Dispatch(T data)
#else
        public void Dispatch(T data,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string path = null,
            [CallerLineNumber] int sourceLineNumber = default(int) )
#endif
        {
#if !DEBUG_PERFORMANCE_TRACE
            Profiler.BeginSample("DispatchListener");
#else
            Profiler.BeginSample($"DispatchListener: {path}:{memberName}:{sourceLineNumber}");
#endif
            DispatchList(Listener, data,false);
            Profiler.EndSample();
#if !DEBUG_PERFORMANCE_TRACE
            Profiler.BeginSample("DispatchOnceListener");
#else
            Profiler.BeginSample($"DispatchOnceListener: {path}:{memberName}:{sourceLineNumber}");
#endif
            DispatchList(OnceListener, data,true);
            Profiler.EndSample();
        }
          
        private void DispatchList(SafeFirstList<EventT<T>> listeners, T data,bool isClear=false)
        {
            listeners.LockForeach();
            int len = listeners.Capacity;
            for (int i = 0; i < len; i++)
            {
                var item = listeners[i];
                if (item != null)
                {
#if DEBUG_PERFORMANCE_TRACE
                    bool bSampled = FixedLinkedEventTrace<EventT<T>>.BeginSample(item);
#endif
                    if (listeners == Listener)
                    {
                        CheckAndDispatch(item, data);
                    }
                    else if (listeners == OnceListener)
                    {
                        item.Invoke(data);
                    }
#if DEBUG_PERFORMANCE_TRACE
                    if(bSampled)
                        FixedLinkedEventTrace<EventT<T>>.EndSample();
#endif
                }
            }
            if(isClear){listeners.Clear(true);}
            listeners.UnlockForeach();
        }

        private void CheckAndDispatch(EventT<T> item, T data)
        {
            if (ListenerFollowInfo == null)
            {
                item.Invoke(data);
                return;
            }
            SignalFollowInfo followInfo;
            if (ListenerFollowInfo.TryGetValue(item, out followInfo))
            {
                if (followInfo.Followed == null)
                {
                    if (followInfo.Mode == eFollowTargetMode.TargetNoneDoNothing)
                    {
                        item.Invoke(data);
                        return;
                    }
                    else
                    {
                        //因为有锁定API，所以还好还好，可以循环的时候调用
                        RemoveListener(item);
                        return;
                    }
                }
                //如果Disactive
                else if (!followInfo.Followed.enabled || !followInfo.Followed.gameObject.activeSelf)
                {
                    if (followInfo.Mode == eFollowTargetMode.TargetDisactiveDoNothing)
                    {
                        item.Invoke(data);
                        return;
                    }
                    else if (followInfo.Mode == eFollowTargetMode.TargetDisactiveDoStop)
                    {
                        //因为有锁定API，所以还好还好，可以循环的时候调用
                        RemoveListener(item);
                        return;
                    }
                    else if (followInfo.Mode == eFollowTargetMode.TargetDisactiveDoJump)
                    {
                        return;
                    }
                }
                else//一切正常
                {
                    item.Invoke(data);
                    return;
                }
            }//没有配置的情况，直接Invok
            else
            {
                item.Invoke(data);
                return;
            }
        }


        private void AddUnique(SafeFirstList<EventT<T>> listeners, EventT<T> callback, MonoBehaviour followed = null, eFollowTargetMode mode = eFollowTargetMode.TargetNoneDoNothing, string memberName = null, string path = null,  int sourceLineNumber = default(int) )
        {
            if (!listeners.Contains(callback))
            {
#if DEBUG_PERFORMANCE_TRACE
                FixedLinkedEventTrace<EventT<T>>.AddTrace(callback, memberName, path, sourceLineNumber);
#endif
                listeners.Add(callback);
                //添加追踪事件
                if (mode != eFollowTargetMode.TargetNoneDoNothing)
                {
                    if (listeners == Listener)
                    {
                        if (ListenerFollowInfo == null)
                        {
                            ListenerFollowInfo = new Dictionary<EventT<T>, SignalFollowInfo>((int)(listeners.Capacity));
                        }
                        ListenerFollowInfo[callback] = new SignalFollowInfo()
                        {
                            Followed = followed,
                            Mode = mode
                        };
                    }
                }
            }
        }
    }


    /// <summary>
    /// 基于LinkedList和HashSet优化后的Signal，能有效减少大量调用的GC
    /// PS.已经不是基于上述结构了，基于FixedArrayList,LinkedList有GC，固定48
    /// </summary>
    public class FixedLinkedSignal<T1, T2>
    {
        /// <summary>
        /// 如果Once有添加，则需要调用清除
        /// </summary> 
        public SafeFirstList<EventT<T1, T2>> Listener;
        public SafeFirstList<EventT<T1, T2>> OnceListener;
        public Dictionary<EventT<T1, T2>, SignalFollowInfo> ListenerFollowInfo;

        public FixedLinkedSignal(int capcity = 8)
        {
            if (capcity <= 0)
            {
                capcity = 8;
            }
            Listener = new SafeFirstList<EventT<T1, T2>>(capcity);
            OnceListener = new SafeFirstList<EventT<T1, T2>>(capcity);
        }

#if !DEBUG_PERFORMANCE_TRACE
        public void AddListener(EventT<T1, T2> callback, MonoBehaviour followed = null, eFollowTargetMode mode = eFollowTargetMode.TargetNoneDoNothing)
        {
            AddUnique(Listener, callback, followed, mode);
        }
        
        public void AddOnce(EventT<T1, T2> callback)
        {
            AddUnique(OnceListener, callback);
        }
#else
        public void AddListener(EventT<T1, T2> callback, MonoBehaviour followed = null, eFollowTargetMode mode = eFollowTargetMode.TargetNoneDoNothing,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string path = null,
            [CallerLineNumber] int sourceLineNumber = default(int))
        {
            AddUnique(Listener, callback, followed, mode, memberName, path, sourceLineNumber);
        }
        public void AddOnce(EventT<T1, T2> callback,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string path = null,
            [CallerLineNumber] int sourceLineNumber = default(int))
        {
            AddUnique(OnceListener, callback, null, eFollowTargetMode.TargetNoneDoNothing, memberName, path, sourceLineNumber);
        }
#endif


        public void RemoveListener(EventT<T1, T2> callback)
        {
            Listener.Remove(callback);
#if DEBUG_PERFORMANCE_TRACE
            FixedLinkedEventTrace<EventT<T1, T2>>.RemoveTrace(callback);
#endif
            if (ListenerFollowInfo != null)
            {
                ListenerFollowInfo.Remove(callback);
            }
        }

        public void RemoveAllListener()
        {
            Listener.Clear();
#if DEBUG_PERFORMANCE_TRACE
            FixedLinkedEventTrace<EventT<T1, T2>>.RemoveAll();
#endif
            if (ListenerFollowInfo != null)
            {
                ListenerFollowInfo.Clear();
            }
        }

#if !DEBUG_PERFORMANCE_TRACE
        public void Dispatch(T1 data1, T2 data2)
#else
        public void Dispatch(T1 data1, T2 data2,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string path = null,
            [CallerLineNumber] int sourceLineNumber = default(int) )
#endif
        {
#if !DEBUG_PERFORMANCE_TRACE
            Profiler.BeginSample("DispatchListener");
#else
            Profiler.BeginSample($"DispatchListener: {path}:{memberName}:{sourceLineNumber}");
#endif
            DispatchList(Listener, data1, data2,false);
            Profiler.EndSample();
#if !DEBUG_PERFORMANCE_TRACE
            Profiler.BeginSample("DispatchOnceListener");
#else
            Profiler.BeginSample($"DispatchOnceListener: {path}:{memberName}:{sourceLineNumber}");
#endif
            DispatchList(OnceListener, data1, data2,true);
            Profiler.EndSample();
        }
         
        private void DispatchList(SafeFirstList<EventT<T1, T2>> listeners, T1 data1, T2 data2,bool isClear=false)
        {
            try
            {
                listeners.LockForeach();
                int len = listeners.Capacity;
                for (int i = 0; i < len; i++)
                {
                    var item = listeners[i];
                    if (item != null)
                    {
#if DEBUG_PERFORMANCE_TRACE
                    bool bSampled = FixedLinkedEventTrace<EventT<T1, T2>>.BeginSample(item);
#endif
                        if (listeners == Listener)
                        {
                            CheckAndDispatch(item, data1, data2);
                        }
                        else if (listeners == OnceListener)
                        {
                            item.Invoke(data1, data2);
                        }
#if DEBUG_PERFORMANCE_TRACE
                    if(bSampled)
                        FixedLinkedEventTrace<EventT<T1, T2>>.EndSample();
#endif
                    }
                }
                if (isClear) { listeners.Clear(true); }
                listeners.UnlockForeach();
            }
            catch (Exception e)
            {
                CommonLog.Error(e.ToString());
            } 
        }

        private void CheckAndDispatch(EventT<T1, T2> item, T1 data1, T2 data2)
        {
            if (ListenerFollowInfo == null)
            {
                item.Invoke(data1, data2);
                return;
            }
            SignalFollowInfo followInfo;
            if (ListenerFollowInfo.TryGetValue(item, out followInfo))
            {
                if (followInfo.Followed == null)
                {
                    if (followInfo.Mode == eFollowTargetMode.TargetNoneDoNothing)
                    {
                        item.Invoke(data1, data2);
                        return;
                    }
                    else
                    {
                        //因为有锁定API，所以还好还好，可以循环的时候调用
                        RemoveListener(item);
                        return;
                    }
                }
                //如果Disactive
                else if (!followInfo.Followed.enabled || !followInfo.Followed.gameObject.activeSelf)
                {
                    if (followInfo.Mode == eFollowTargetMode.TargetDisactiveDoNothing)
                    {
                        item.Invoke(data1, data2);
                        return;
                    }
                    else if (followInfo.Mode == eFollowTargetMode.TargetDisactiveDoStop)
                    {
                        //因为有锁定API，所以还好还好，可以循环的时候调用
                        RemoveListener(item);
                        return;
                    }
                    else if (followInfo.Mode == eFollowTargetMode.TargetDisactiveDoJump)
                    {
                        return;
                    }
                }
                else//一切正常
                {
                    item.Invoke(data1, data2);
                    return;
                }
            }//没有配置的情况，直接Invok
            else
            {
                item.Invoke(data1, data2);
                return;
            }
        }

        private void AddUnique(SafeFirstList<EventT<T1, T2>> listeners, EventT<T1, T2> callback, MonoBehaviour followed = null, eFollowTargetMode mode = eFollowTargetMode.TargetNoneDoNothing, string memberName = null, string path = null,  int sourceLineNumber = default(int) )
        {
            if (!listeners.Contains(callback))
            {
                listeners.Add(callback);
#if DEBUG_PERFORMANCE_TRACE
                FixedLinkedEventTrace<EventT<T1, T2>>.AddTrace(callback, memberName, path, sourceLineNumber);
#endif
                //添加追踪事件
                if (mode != eFollowTargetMode.TargetNoneDoNothing)
                {
                    if (listeners == Listener)
                    {
                        if (ListenerFollowInfo == null)
                        {
                            ListenerFollowInfo = new Dictionary<EventT<T1, T2>, SignalFollowInfo>((int)(listeners.Capacity));
                        }
                        ListenerFollowInfo[callback] = new SignalFollowInfo()
                        {
                            Followed = followed,
                            Mode = mode
                        };
                    } 
                }
            }
        }
    }


    /// <summary>
    /// 基于LinkedList和HashSet优化后的Signal，能有效减少大量调用的GC
    /// PS.已经不是基于上述结构了，基于FixedArrayList,LinkedList有GC，固定48
    /// </summary>
    public class FixedLinkedSignal<T1, T2, T3>
    {
        /// <summary>
        /// 如果Once有添加，则需要调用清除
        /// </summary>
        //private bool _NeedClearOnce = false;
        public SafeFirstList<EventT<T1, T2, T3>> Listener;
        public SafeFirstList<EventT<T1, T2, T3>> OnceListener;
        public Dictionary<EventT<T1, T2, T3>, SignalFollowInfo> ListenerFollowInfo;

        public FixedLinkedSignal(int capcity = 8)
        {
            if (capcity <= 0)
            {
                capcity = 8;
            }
            Listener = new SafeFirstList<EventT<T1, T2, T3>>(capcity);
            OnceListener = new SafeFirstList<EventT<T1, T2, T3>>(capcity);
        }

#if !DEBUG_PERFORMANCE_TRACE
        public void AddListener(EventT<T1, T2, T3> callback, MonoBehaviour followed = null, eFollowTargetMode mode = eFollowTargetMode.TargetNoneDoNothing)
        {
            AddUnique(Listener, callback, followed, mode);
        }

        public void AddOnce(EventT<T1, T2, T3> callback)
        {
            AddUnique(OnceListener, callback);
        }
#else
        public void AddListener(EventT<T1, T2, T3> callback, MonoBehaviour followed = null, eFollowTargetMode mode = eFollowTargetMode.TargetNoneDoNothing,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string path = null,
            [CallerLineNumber] int sourceLineNumber = default(int))
        {
            AddUnique(Listener, callback, followed, mode, memberName, path, sourceLineNumber);
        }

        public void AddOnce(EventT<T1, T2, T3> callback,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string path = null,
            [CallerLineNumber] int sourceLineNumber = default(int))
        {
            AddUnique(OnceListener, callback, null, eFollowTargetMode.TargetNoneDoNothing, memberName, path, sourceLineNumber);
        }
#endif

        public void RemoveListener(EventT<T1, T2, T3> callback)
        {
            Listener.Remove(callback);
#if DEBUG_PERFORMANCE_TRACE
            FixedLinkedEventTrace<EventT<T1, T2, T3>>.RemoveTrace(callback);
#endif
            if (ListenerFollowInfo != null)
            {
                ListenerFollowInfo.Remove(callback);
            }
        }

        public void RemoveAllListener()
        {
            Listener.Clear();
#if DEBUG_PERFORMANCE_TRACE
            FixedLinkedEventTrace<EventT<T1, T2, T3>>.RemoveAll();
#endif
            if (ListenerFollowInfo != null)
            {
                ListenerFollowInfo.Clear();
            }
        }

#if !DEBUG_PERFORMANCE_TRACE
        public void Dispatch(T1 data1, T2 data2, T3 data3)
#else
        public void Dispatch(T1 data1, T2 data2,T3 data3,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string path = null,
            [CallerLineNumber] int sourceLineNumber = default(int) )
#endif
        {
#if !DEBUG_PERFORMANCE_TRACE
            Profiler.BeginSample("DispatchListener");
#else
            Profiler.BeginSample($"DispatchListener: {path}:{memberName}:{sourceLineNumber}");
#endif
            DispatchList(Listener, data1, data2, data3,false);
            Profiler.EndSample();
#if !DEBUG_PERFORMANCE_TRACE
            Profiler.BeginSample("DispatchOnceListener");
#else
            Profiler.BeginSample($"DispatchOnceListener: {path}:{memberName}:{sourceLineNumber}");
#endif
            DispatchList(OnceListener, data1, data2, data3,true);
            Profiler.EndSample();

        } 

        private void DispatchList(SafeFirstList<EventT<T1, T2, T3>> listeners, T1 data1, T2 data2, T3 data3,bool isClear=false)
        {
            listeners.LockForeach();
            int len = listeners.Capacity;
            for (int i = 0; i < len; i++)
            {
                var item = listeners[i];
                if (item != null)
                {
#if DEBUG_PERFORMANCE_TRACE
                    bool bSampled = FixedLinkedEventTrace<EventT<T1, T2, T3>>.BeginSample(item);
#endif
                    if (listeners == Listener)
                    {
                        CheckAndDispatch(item, data1, data2, data3);
                    }
                    else if (listeners == OnceListener)
                    {
                        item.Invoke(data1, data2, data3);
                    }
#if DEBUG_PERFORMANCE_TRACE
                    if(bSampled)
                        FixedLinkedEventTrace<EventT<T1, T2, T3>>.EndSample();
#endif
                }
            }
            if(isClear){listeners.Clear(true);}
            listeners.UnlockForeach();
        }


        private void CheckAndDispatch(EventT<T1, T2, T3> item, T1 data1, T2 data2, T3 data3)
        {
            if (ListenerFollowInfo == null)
            {
                item.Invoke(data1, data2, data3);
                return;
            }
            SignalFollowInfo followInfo;
            if (ListenerFollowInfo.TryGetValue(item, out followInfo))
            {
                if (followInfo.Followed == null)
                {
                    if (followInfo.Mode == eFollowTargetMode.TargetNoneDoNothing)
                    {
                        item.Invoke(data1, data2, data3);
                        return;
                    }
                    else
                    {
                        //因为有锁定API，所以还好还好，可以循环的时候调用
                        RemoveListener(item);
                        return;
                    }
                }
                //如果Disactive
                else if (!followInfo.Followed.enabled || !followInfo.Followed.gameObject.activeSelf)
                {
                    if (followInfo.Mode == eFollowTargetMode.TargetDisactiveDoNothing)
                    {
                        item.Invoke(data1, data2, data3);
                        return;
                    }
                    else if (followInfo.Mode == eFollowTargetMode.TargetDisactiveDoStop)
                    {
                        //因为有锁定API，所以还好还好，可以循环的时候调用
                        RemoveListener(item);
                        return;
                    }
                    else if (followInfo.Mode == eFollowTargetMode.TargetDisactiveDoJump)
                    {
                        return;
                    }
                }
                else//一切正常
                {
                    item.Invoke(data1, data2, data3);
                    return;
                }
            }//没有配置的情况，直接Invok
            else
            {
                item.Invoke(data1, data2, data3);
                return;
            }
        }

        private void AddUnique(SafeFirstList<EventT<T1, T2, T3>> listeners, EventT<T1, T2, T3> callback, MonoBehaviour followed = null, eFollowTargetMode mode = eFollowTargetMode.TargetNoneDoNothing, string memberName = null, string path = null,  int sourceLineNumber = default(int) )
        {
            if (!listeners.Contains(callback))
            {
                listeners.Add(callback);
                //添加追踪事件
                if (mode != eFollowTargetMode.TargetNoneDoNothing)
                {
#if DEBUG_PERFORMANCE_TRACE
                    FixedLinkedEventTrace<EventT<T1, T2, T3>>.AddTrace(callback, memberName, path, sourceLineNumber);
#endif
                    if (listeners == Listener)
                    {
                        if (ListenerFollowInfo == null)
                        {
                            ListenerFollowInfo = new Dictionary<EventT<T1, T2, T3>, SignalFollowInfo>((int)(listeners.Capacity));
                        }
                        ListenerFollowInfo[callback] = new SignalFollowInfo()
                        {
                            Followed = followed,
                            Mode = mode
                        };
                    } 
                }
            }
        }
    }


    /// <summary>
    /// 基于LinkedList和HashSet优化后的Signal，能有效减少大量调用的GC
    /// PS.已经不是基于上述结构了，基于FixedArrayList,LinkedList有GC，固定48
    /// </summary>
    public class FixedLinkedSignal<T1, T2, T3, T4>
    {
        /// <summary>
        /// 如果Once有添加，则需要调用清除
        /// </summary> 
        public SafeFirstList<EventT<T1, T2, T3, T4>> Listener;
        public SafeFirstList<EventT<T1, T2, T3, T4>> OnceListener;
        public Dictionary<EventT<T1, T2, T3, T4>, SignalFollowInfo> ListenerFollowInfo;

        public FixedLinkedSignal(int capcity = 8)
        {
            if (capcity <= 0)
            {
                capcity = 8;
            }
            Listener = new SafeFirstList<EventT<T1, T2, T3, T4>>(capcity);
            OnceListener = new SafeFirstList<EventT<T1, T2, T3, T4>>(capcity);
        }

#if !DEBUG_PERFORMANCE_TRACE
        public void AddListener(EventT<T1, T2, T3, T4> callback, MonoBehaviour followed = null, eFollowTargetMode mode = eFollowTargetMode.TargetNoneDoNothing)
        {
            AddUnique(Listener, callback, followed, mode);
        }

        public void AddOnce(EventT<T1, T2, T3, T4> callback)
        {
            AddUnique(OnceListener, callback); 
        }
#else
        public void AddListener(EventT<T1, T2, T3, T4> callback, MonoBehaviour followed = null, eFollowTargetMode mode = eFollowTargetMode.TargetNoneDoNothing,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string path = null,
            [CallerLineNumber] int sourceLineNumber = default(int) )
        {
            AddUnique(Listener, callback, followed, mode, memberName, path, sourceLineNumber);
        }

        public void AddOnce(EventT<T1, T2, T3, T4> callback,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string path = null,
            [CallerLineNumber] int sourceLineNumber = default(int) )
        {
            AddUnique(OnceListener, callback, null, eFollowTargetMode.TargetNoneDoNothing, memberName, path , sourceLineNumber); 
        }
#endif

        public void RemoveListener(EventT<T1, T2, T3, T4> callback)
        {
            Listener.Remove(callback);
#if DEBUG_PERFORMANCE_TRACE
            FixedLinkedEventTrace<EventT<T1, T2, T3, T4>>.RemoveTrace(callback);
#endif
            if (ListenerFollowInfo != null)
            {
                ListenerFollowInfo.Remove(callback);
            }
        }

        public void RemoveAllListener()
        {
            Listener.Clear();
#if DEBUG_PERFORMANCE_TRACE
            FixedLinkedEventTrace<EventT<T1, T2, T3, T4>>.RemoveAll();
#endif
            if (ListenerFollowInfo != null)
            {
                ListenerFollowInfo.Clear();
            }
        }
#if !DEBUG_PERFORMANCE_TRACE
        public void Dispatch(T1 data1, T2 data2, T3 data3, T4 data4)
#else
        public void Dispatch(T1 data1, T2 data2,T3 data3, T4 data4,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string path = null,
            [CallerLineNumber] int sourceLineNumber = default(int) )
#endif
        {
#if !DEBUG_PERFORMANCE_TRACE
            Profiler.BeginSample("DispatchListener");
#else
            Profiler.BeginSample($"DispatchListener: {path}:{memberName}:{sourceLineNumber}");
#endif
            DispatchList(Listener, data1, data2, data3, data4,false);
            Profiler.EndSample();
#if !DEBUG_PERFORMANCE_TRACE
            Profiler.BeginSample("DispatchOnceListener");
#else
            Profiler.BeginSample($"DispatchOnceListener: {path}:{memberName}:{sourceLineNumber}");
#endif
            DispatchList(OnceListener, data1, data2, data3, data4,true); 
            Profiler.EndSample();
        }
         
        private void DispatchList(SafeFirstList<EventT<T1, T2, T3, T4>> listeners, T1 data1, T2 data2, T3 data3, T4 data4,bool isClear=false)
        {
            listeners.LockForeach();
            int len = listeners.Capacity;
            for (int i = 0; i < len; i++)
            {
                var item = listeners[i];
                if (item != null)
                {
#if DEBUG_PERFORMANCE_TRACE
                    bool bSampled = FixedLinkedEventTrace<EventT<T1, T2, T3, T4>>.BeginSample(item);
#endif
                    if (listeners == Listener)
                    {
                        CheckAndDispatch(item, data1, data2, data3, data4);
                    }
                    else if (listeners == OnceListener)
                    {
                        item.Invoke(data1, data2, data3, data4);
                    }
#if DEBUG_PERFORMANCE_TRACE
                    if(bSampled)
                        FixedLinkedEventTrace<EventT<T1, T2, T3, T4>>.EndSample();
#endif
                }
            }
            if(isClear){listeners.Clear(true);}
            listeners.UnlockForeach();
        }


        private void CheckAndDispatch(EventT<T1, T2, T3, T4> item, T1 data1, T2 data2, T3 data3, T4 data4)
        {
            if (ListenerFollowInfo == null)
            {
                item.Invoke(data1, data2, data3, data4);
                return;
            }
            SignalFollowInfo followInfo;
            if (ListenerFollowInfo.TryGetValue(item, out followInfo))
            {
                if (followInfo.Followed == null)
                {
                    if (followInfo.Mode == eFollowTargetMode.TargetNoneDoNothing)
                    {
                        item.Invoke(data1, data2, data3, data4);
                        return;
                    }
                    else //其他状态，默认移除不执行
                    {
                        //因为有锁定API，所以还好还好，可以循环的时候调用
                        RemoveListener(item);
                        return;
                    }
                }
                //如果Disactive
                else if (!followInfo.Followed.enabled || !followInfo.Followed.gameObject.activeSelf)
                {
                    if (followInfo.Mode == eFollowTargetMode.TargetDisactiveDoNothing)
                    {
                        item.Invoke(data1, data2, data3, data4);
                        return;
                    }
                    else if (followInfo.Mode == eFollowTargetMode.TargetDisactiveDoStop)
                    {
                        //因为有锁定API，所以还好还好，可以循环的时候调用
                        RemoveListener(item);
                        return;
                    }
                    else if (followInfo.Mode == eFollowTargetMode.TargetDisactiveDoJump)
                    {
                        return;
                    }
                }
                else//一切正常
                {
                    item.Invoke(data1, data2, data3, data4);
                    return;
                }
            }//没有配置的情况，直接Invok
            else
            {
                item.Invoke(data1, data2, data3, data4);
                return;
            }
        }


        private void AddUnique(SafeFirstList<EventT<T1, T2, T3, T4>> listeners, EventT<T1, T2, T3, T4> callback, MonoBehaviour followed = null, eFollowTargetMode mode = eFollowTargetMode.TargetNoneDoNothing, string memberName = null, string path = null,  int sourceLineNumber = default(int) )
        {
            if (!listeners.Contains(callback))
            {
                listeners.Add(callback);
#if DEBUG_PERFORMANCE_TRACE
                FixedLinkedEventTrace<EventT<T1, T2, T3, T4>>.AddTrace(callback, memberName, path, sourceLineNumber);
#endif
                //添加追踪事件
                if (mode != eFollowTargetMode.TargetNoneDoNothing)
                {
                    if (ListenerFollowInfo == null)
                    {
                        ListenerFollowInfo = new Dictionary<EventT<T1, T2, T3, T4>, SignalFollowInfo>((int)(listeners.Capacity));
                    }
                    ListenerFollowInfo[callback] = new SignalFollowInfo()
                    {
                        Followed = followed,
                        Mode = mode
                    };
                }
            }
        }
    }


    public class SafeFirstList<T> where T : class
    {
        public SimpleFirstList<T> Data;

        public bool IsLockForeach = false;

        // public BetterList<T> AddCache;
        //
        // public BetterList<T> RemoveCache;

        public BetterList<OperateTask<T>> OperateQueue;

        /// <summary>
        /// 在Foreach时候并非安全的Count
        /// </summary>
        public int Count
        {
            get
            {
                if (!IsLockForeach)
                {
                    return Data.size;
                }
                else
                {
#if PROJECT_DEVELOP
               DebugX.E("禁止Foreach时候访问Count!");
#endif
                    return Data.size;
                }
            }
        }

        public class OperateTask<V>
        {
            public V Data;
            public bool IsAdd = false;
#if UNITY_EDITOR
            public string TestLog = null;//区分调用来源的测试用LOG数据
#endif

            public OperateTask(V data, bool isAdd)
            {
                Data = data;
                IsAdd = isAdd;
            }
#if UNITY_EDITOR
            public OperateTask(V data, bool isAdd, string testlog)
            {
                Data = data;
                IsAdd = isAdd;
                TestLog = testlog;
            }
#endif
        }


        public SafeFirstList(int capacity = 8)
        {
            if (capacity > 0)
            {
                Data = new SimpleFirstList<T>(capacity);
            }
        }

        public void LockForeach()
        {
            IsLockForeach = true;
            // Profiler.BeginSample("Begin Dispatch");
        }

        public void UnlockForeach()
        {
            IsLockForeach = false;
            // Profiler.EndSample();
            
            ////减
            //if (RemoveCache != null && RemoveCache.size > 0)
            //{
            //    for (int i = 0; i < RemoveCache.size; i++)
            //    {
            //        if (!Data.Remove(RemoveCache[i]))
            //        {
            //            DebugX.E("减了不存在的东西，调用顺序有误");
            //        }
            //    }
            //    RemoveCache.Clear();
            //}
            ////加
            //if (AddCache != null && AddCache.size > 0)
            //{
            //    for (int i = 0; i < AddCache.size; i++)
            //    {
            //        Data.Add(AddCache[i]);
            //    }
            //    AddCache.Clear();
            //}

            Profiler.BeginSample("Check OperationQueue");
            if (OperateQueue != null && OperateQueue.size > 0)
            {
                int queueNum = OperateQueue.size;
                int dataNum = Data.size;
                for (int i = 0; i < queueNum; i++)
                {
                    var operate = OperateQueue[i];
                    if (operate.IsAdd)
                    {
                        Data.Add(operate.Data);
                    }
                    else
                    {
                        if (!Data.Remove(operate.Data))
                        {
#if PROJECT_DEVELOP && UNITY_EDITOR
                        DebugX.E("移除错误，不存在该脚本:{0},----堆栈:{1}", operate.Data, operate.TestLog);
#endif
                        }
                    }
                }
                OperateQueue.Clear();
                //DebugX.E("原有{0}个脚本，添加删除共{1}个脚本，现在{2}个脚本",dataNum,queueNum,Data.size );
            }
            Profiler.EndSample();

        }

        public void Add(T item)
        {
            if (item == null) return;
            if (IsLockForeach)
            {
                //if (AddCache == null)
                //{
                //    AddCache = new BetterList<T>();
                //}
                //    AddCache.Add(item);
                if (OperateQueue == null)
                {
                    OperateQueue = new BetterList<OperateTask<T>>();
                }
#if PROJECT_DEVELOP && UNITY_EDITOR
            OperateQueue.Add(new OperateTask<T>(item, true, UnityEngine.StackTraceUtility.ExtractStackTrace()));
#else
                OperateQueue.Add(new OperateTask<T>(item, true));
#endif

            }
            else
            {
                Data.Add(item);
            }
        }


        public void Remove(T item)
        {
            if (item == null) return;
            if (IsLockForeach)
            {
                //if (RemoveCache == null)
                //{
                //    RemoveCache = new BetterList<T>();
                //}
                //    RemoveCache.Add(item);

                if (OperateQueue == null)
                {
                    OperateQueue = new BetterList<OperateTask<T>>();
                }
#if PROJECT_DEVELOP && UNITY_EDITOR
            OperateQueue.Add(new OperateTask<T>(item, false, UnityEngine.StackTraceUtility.ExtractStackTrace()));
#else
                OperateQueue.Add(new OperateTask<T>(item, false));
#endif
            }
            else
            {
                Data.Remove(item);
            }
        }


        public void Clear(bool ignoreForeach = false)
        {
            if (!ignoreForeach&&IsLockForeach)
            {
                //CommonLog.Error("不能再同一帧中同时调用清空，大概。。。");
            }
            else
            {
                Data.Clear();
            }
        }

        public bool Contains(T item)
        {
            return Data.Contains(item);
        }

        /// <summary>
        /// Convenience function. I recommend using .buffer instead.
        /// </summary>

        [DebuggerHidden]
        public T this[int i]
        {
            get { return Data.buffer[i]; }
            set { Data.buffer[i] = value; }
        }

        public int Capacity
        {
            get
            {
                return Data.capacity;
            }
        }

    }


    /// <summary>
    /// 一个没有顺序，可能为空的List,正常情况Remove和Add没有GC，自动扩容，非常危险，如果不是作者指导，请勿乱用
    /// Author:Eric
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SimpleFirstList<T> where T : class
    {
        /// <summary>
        /// 自动收缩容量
        /// </summary>
        public bool IsAutoShrink = true;

        /// <summary>
        /// Direct access to the buffer. Note that you should not use its 'Length' parameter, but instead use BetterList.size.
        /// </summary>
        public T[] buffer;

        /// <summary>
        /// 实际数据量
        /// </summary> 
        public int size = 0;

        /// <summary>
        /// 槽容量
        /// </summary>
        public int capacity
        {
            get
            {
                if (buffer == null)
                {
                    return 0;
                }
                else
                {
                    return buffer.Length;
                }
            }
        }

        public SimpleFirstList()
        {
        }

        public SimpleFirstList(int capacity)
        {
            if (capacity > 0)
            {
                buffer = new T[capacity];
            }
        }

        [DebuggerHidden]
        [DebuggerStepThrough]
        public IEnumerator<T> GetEnumerator()
        {
            if (buffer != null)
            {
                for (int i = 0; i < size; ++i)
                {
                    yield return buffer[i];
                }
            }
        }

        /// <summary>
        /// Convenience function. I recommend using .buffer instead.
        /// </summary>

        [DebuggerHidden]
        public T this[int i]
        {
            get { return buffer[i]; }
            set { buffer[i] = value; }
        }


        /// <summary>
        /// Helper function that expands the size of the array, maintaining the content.
        /// </summary>

        void AllocateMore()
        {
            T[] newList = (buffer != null) ? new T[Mathf.Max(buffer.Length << 1, 32)] : new T[8];
            if (buffer != null && size > 0) buffer.CopyTo(newList, 0);
            buffer = newList;
        }

        /// <summary>
        /// 容量缩小一半
        /// </summary>
        public void DoShrinkHalf()
        {
            if (buffer == null) return;
            //当容量大于128,并且容量大于内容4倍才计算缩小一半的可能性，否则无需考虑
            if (capacity > 128 && capacity > size * 4)
            {
                T[] newList = new T[buffer.Length >> 1];
                int len = buffer.Length;
                int j = newList.Length;
                //拷贝旧数据到新数据内
                for (int i = 0; i < len; i++)
                {
                    var d = buffer[i];
                    if (d != null)
                    {
                        j--;
                        newList[j] = d;
                    }
                }
                buffer = newList;
            }
        }

        /// <summary>
        /// 容量缩小一半
        /// </summary>
        void ShrinkHalf()
        {
            if (!IsAutoShrink) return;
            DoShrinkHalf();
        }

        /// <summary>
        /// Clear the array by resetting its size to zero. Note that the memory is not actually released.
        /// </summary>

        public void Clear()
        {
            if (buffer == null) return;
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = null;
            }
            size = 0;
        }


        /// <summary>
        /// Clear the array and release the used memory.
        /// </summary>

        public void Release() { size = 0; buffer = null; }

        /// <summary>
        /// Add the specified item to the end of the list.
        /// </summary>

        public void Add(T item)
        {
            if (buffer == null || size == buffer.Length) AllocateMore();
            buffer[FindAEmptyIndex()] = item;
            size++;
        }

        /// <summary>
        /// 找一个空槽位
        /// </summary>
        /// <returns></returns>
        public int FindAEmptyIndex()
        {
            if (buffer == null || size == buffer.Length) AllocateMore();
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] == null)
                {
                    return i;
                }
            }
            return -1;
        }

        public int Find(T item)
        {
#if PROJECT_DEVELOP
        //查找次数计数器，用于统计Find性能问题
        int count = 0;
#endif
            if (item == null) return -1;
            if (buffer != null)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i] == null)
                    {
                        continue;
                    }
#if PROJECT_DEVELOP
                count++;
#endif
                    if (item.Equals(buffer[i]))
                    {
#if PROJECT_DEVELOP
                    if (count > 3000)
                    {
                        CommonLog.Log($"HashLinkedFind性能急剧下降,Count为：" + count);
                    }
#endif
                        return i;
                    }
                }
            }

            return -1;
        }

        public bool Contains(T item)
        {
            return Find(item) >= 0;
        }

        /// <summary>
        /// Remove the specified item from the list. Note that RemoveAt() is faster and is advisable if you already know the index.
        /// </summary>

        public bool Remove(T item)
        {
            if (item == null) return false;
            if (buffer != null)
            {
                int index = Find(item);
                if (index >= 0)
                {
                    buffer[index] = null;
                    size--;
                    //收缩容量
                    ShrinkHalf();
                    return true;
                }
            }
            return false;
        }



        /// <summary>
        /// 随机吐出一个,不是随机，只是最前面的那个，模拟可以当队列用
        /// </summary>
        /// <returns></returns>
        public T GetA(bool remove = false)
        {
            if (buffer != null)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    var item = buffer[i];
                    if (item != null)
                    {
                        if (remove)
                        {
                            buffer[i] = null;
                            size--;
                            //收缩容量
                            ShrinkHalf();
                        }
                        return item;
                    }
                }
            }
            return null;
        }
    }

#if DEBUG_PERFORMANCE_TRACE
    public static class FixedLinkedEventTrace<EventTypeT> 
    {
        private static Dictionary<EventTypeT, string> eventCallTraceDic = new Dictionary<EventTypeT, string>();
        public static void AddTrace(EventTypeT callback, string memberName = null, string path = null,  int sourceLineNumber = default(int))
        {
            if (eventCallTraceDic.ContainsKey(callback))
            {
                return;
            }
            
            var stackInfo = $"{path} : {memberName}:Line({sourceLineNumber})"; //parameInfos[-1].ParameterType.Name
                
            // Debug.Log("Add Listener " +  stackInfo);
            eventCallTraceDic[callback] =  stackInfo;
        }

        public static void RemoveTrace(EventTypeT callback)
        {
            if (eventCallTraceDic.ContainsKey(callback))
            {
                eventCallTraceDic.Remove(callback);
            }
        }

        public static void RemoveAll()
        {
            eventCallTraceDic.Clear();
        }
        public static bool BeginSample(EventTypeT callback)
        {
            if(eventCallTraceDic.ContainsKey(callback))
            {
                Profiler.BeginSample(eventCallTraceDic[callback]);
                return true;
            }

            return false;
        }

        public static void EndSample()
        {
            Profiler.EndSample();
        }
    }
#endif
}