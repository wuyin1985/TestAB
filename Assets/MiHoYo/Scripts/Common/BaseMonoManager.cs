using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SignalTools;
using TimerUtils;
using UnityEngine.Profiling;


/// <summary>
/// Unity全生命周期都存在的一个代理Update和FixedUpdate,普通对象可以注册它来实现Update
/// </summary>
public class BaseMonoManagerForUnitySingleton : UnitySingleton<BaseMonoManagerForUnitySingleton>
{
    public readonly FixedLinkedSignal BaseAwake = new FixedLinkedSignal();
    public readonly FixedLinkedSignal PreBaseUpdate = new FixedLinkedSignal();
    public readonly FixedLinkedSignal BaseUpdate = new FixedLinkedSignal(48);
    public readonly FixedLinkedSignal AfterBaseUpdate = new FixedLinkedSignal();
    public readonly FixedLinkedSignal LaterBaseUpdate = new FixedLinkedSignal();
    public readonly FixedLinkedSignal PreBaseFixedUpdate = new FixedLinkedSignal();
    public readonly FixedLinkedSignal BaseFixedUpdate = new FixedLinkedSignal();
    public readonly FixedLinkedSignal AfterBaseFixedUpdate = new FixedLinkedSignal();
    public readonly FixedLinkedSignal<bool> BaseOnApplicationPause = new FixedLinkedSignal<bool>();
    public readonly FixedLinkedSignal BaseOnApplicationQuit = new FixedLinkedSignal();
    //网络回调，每1/10秒回调,如果卡机，或者在后台，会跳回调
    public readonly FixedLinkedSignal OnNetUpdate = new FixedLinkedSignal();
    //每1/10秒回调,如果卡机，或者在后台，会跳回调
    public readonly FixedLinkedSignal OnRealtimeTenCentSecond = new FixedLinkedSignal();
    //每1/4秒回调,如果卡机，或者在后台，会跳回调
    public readonly FixedLinkedSignal OnRealtimeQuadSecond = new FixedLinkedSignal();
    //每整秒回调,如果卡机，或者在后台，会跳回调
    public readonly FixedLinkedSignal OnRealtimeSecond = new FixedLinkedSignal();
    //每1/4分回调,如果卡机，或者在后台，会跳回调
    public readonly FixedLinkedSignal OnRealtimeQuadMinute = new FixedLinkedSignal();
    //每整分回调,如果卡机，或者在后台，会跳回调
    public readonly FixedLinkedSignal OnRealtimeMinute = new FixedLinkedSignal();
    //按下返回的回调
    public readonly FixedLinkedSignal OnPressEscapeKey = new FixedLinkedSignal();

    /// <summary>
    /// UI更新事件，帧率和普通Update可能不一样
    /// </summary>
    public readonly FixedLinkedSignal UIBaseUpdate = new FixedLinkedSignal();

    public static float DeltaTime;
    /// <summary>
    /// 不会被暂停的UI间隔时间
    /// </summary>
    public static float UIDeltaTime;

    protected void Awake()
    {
        BaseAwake.Dispatch(); 
    }

    //计时器缓存的判断值
    private int lastUpdateTenCentSecondTick;
    private int lastUpdateQuadSecondTick;
    private int lastUpdateRealtimeSecondTick;
    private int lastUpdateRealtimeQuadMinuteTick;
    private int lastUpdateRealtimeMinuteTick;

    private float _LastNetTime = -1;
    //Time.SinceLevelLoad 非缩放版本
    public float realtimeSinceLevelLoad { get; private set; }

    // Update is called once per frame
    void Update()
    {
        DeltaTime = Time.deltaTime;
        UIDeltaTime = Time.unscaledDeltaTime;
        realtimeSinceLevelLoad += Time.unscaledDeltaTime;

        Profiler.BeginSample("PreBaseUpdate.Dispatch");
        PreBaseUpdate.Dispatch();
        Profiler.EndSample();
        Profiler.BeginSample("BaseUpdate.Dispatch");
        BaseUpdate.Dispatch();
        Profiler.EndSample();
        Profiler.BeginSample("AfterBaseUpdate.Dispatch");
        AfterBaseUpdate.Dispatch();
        Profiler.EndSample();
        Profiler.BeginSample("UIBaseUpdate.Dispatch");
        UIBaseUpdate.Dispatch();
        Profiler.EndSample();

        //1000/1秒
        int realtimePercentSecond = (int)(Time.realtimeSinceStartup * 1000);

        //1/10秒回调 
        if (lastUpdateTenCentSecondTick <= 0 || (realtimePercentSecond - lastUpdateTenCentSecondTick) >= 100)
        {
            lastUpdateTenCentSecondTick = realtimePercentSecond;
            Profiler.BeginSample("OnRealTimeTenCentSecond.Dispatch");
            OnRealtimeTenCentSecond.Dispatch(); 
            Profiler.EndSample();
            _LastNetTime = Time.realtimeSinceStartup;
        }

        //1/4秒回调 
        if (lastUpdateQuadSecondTick <= 0 || (realtimePercentSecond - lastUpdateQuadSecondTick) >= 250)
        {
            lastUpdateQuadSecondTick = realtimePercentSecond;
            
            Profiler.BeginSample("OnRealTimeQuadSecond.Dispatch");
            OnRealtimeQuadSecond.Dispatch();
            Profiler.EndSample();
        }

        //整秒回调 
        if (lastUpdateRealtimeSecondTick <= 0 || (realtimePercentSecond - lastUpdateRealtimeSecondTick) >= 1000)
        {
            lastUpdateRealtimeSecondTick = realtimePercentSecond;
            Profiler.BeginSample("OnRealTimeSecond.Dispatch");
            OnRealtimeSecond.Dispatch();
            Profiler.EndSample();
        }

        //1/4分钟回调
        if (lastUpdateRealtimeQuadMinuteTick <= 0 || realtimePercentSecond - lastUpdateRealtimeQuadMinuteTick >= 15000)
        {
            lastUpdateRealtimeQuadMinuteTick = realtimePercentSecond;
            Profiler.BeginSample("OnRealTimeQuadMinute.Dispatch");
            OnRealtimeQuadMinute.Dispatch();
            Profiler.EndSample();
        }

        //整分回调  
        if (lastUpdateRealtimeMinuteTick <= 0 || realtimePercentSecond - lastUpdateRealtimeMinuteTick >= 60000)
        {
            lastUpdateRealtimeMinuteTick = realtimePercentSecond;
            Profiler.BeginSample("OnRealTimeMinute.Dispatch");
            OnRealtimeMinute.Dispatch();
            Profiler.EndSample();
        }

        //刷新脚本时间到shader
        //float refreshPeriod = 300f;
        //float time = realtimeSinceLevelLoad % refreshPeriod;
        //float sinTime = Mathf.Sin(realtimeSinceLevelLoad);
        //Shader.SetGlobalVector(ShaderProperty._GlobalPeriodTimer, new Vector4((realtimeSinceLevelLoad / 20.0f) % refreshPeriod, time, time * 2, time * 3));
        //Shader.SetGlobalVector(ShaderProperty._GlobalPeriodSinTimer, new Vector4(Mathf.Sin(realtimeSinceLevelLoad / 8), Mathf.Sin(realtimeSinceLevelLoad / 4), Mathf.Sin(realtimeSinceLevelLoad / 2), sinTime));
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnPressEscapeKey.Dispatch();
        }
    }

    void LateUpdate()
    {
        LaterBaseUpdate.Dispatch();
    }

    private int NetUpdateTicks = 0;
    void FixedUpdate()
    {
        NetUpdateTicks++;
        PreBaseFixedUpdate.Dispatch();
        BaseFixedUpdate.Dispatch();
        AfterBaseFixedUpdate.Dispatch();
        if(NetUpdateTicks%5==0) { OnNetUpdate.Dispatch();}
    }


    protected void OnApplicationQuit()
    {
        BaseOnApplicationQuit.Dispatch();
        //停止其他线程，避免出错，程序退出应该用不上了
        //AsyncRealTimerThreadSystem.Instance.Stop();
        Destroy(gameObject);
    }

    public void OnEnterNewScene()
    {
        realtimeSinceLevelLoad = 0;
    }


    protected void OnApplicationPause(bool paused)
    {
        BaseOnApplicationPause.Dispatch(paused);  
    }

    /// <summary>
    /// 根据普通对象的生命周期的跟随回调管理器
    /// </summary>
    /// <param name="obj"></param>
    public static MonoObjectLifeHandle CreateAFollowedObjectBaseMono(MonoBehaviour obj, bool ignoreActive = false)
    {
        return MonoObjectLifeHandle.Follow(obj, ignoreActive);
    }

    /// <summary>
    /// 一个跟随Mono对象生命周期的处理器
    /// </summary>
    public class MonoObjectLifeHandle
    {
        public readonly FixedLinkedSignal BaseUpdate = new FixedLinkedSignal();
        public readonly FixedLinkedSignal BaseFixedUpdate = new FixedLinkedSignal();

        public readonly FixedLinkedSignal OnDestoryObject = new FixedLinkedSignal();
        public readonly FixedLinkedSignal<bool> OnActiveChange = new FixedLinkedSignal<bool>();

        public MonoBehaviour FollowedObj { get; private set; }

        private bool destory;

        private bool active;

        private bool ignoreActive;

        private MonoObjectLifeHandle()
        {
        }

        //创建一个监听对象存在与否的监听器，如果对象不存在则不会受到回调事件
        public static MonoObjectLifeHandle Follow(MonoBehaviour obj, bool ignoreActive = false)
        {
            var handle = new MonoObjectLifeHandle();
            if (obj == null)
            {
                return null;
            }

            //初始化变量
            handle.ignoreActive = ignoreActive;
            handle.FollowedObj = obj;
            handle.destory = false;
            handle.active = obj.gameObject.activeSelf && obj.enabled;

            BaseMonoManagerForUnitySingleton.Instance.BaseUpdate.AddListener(handle.BaseUpdateFunc);
            BaseMonoManagerForUnitySingleton.Instance.BaseFixedUpdate.AddListener(handle.BaseFixedUpdateFunc);

            return handle;
        }

        //检查状态是否能调用更新
        private bool CanUpdate()
        {
            if (FollowedObj == null)
            {
                //如果之前没有销毁，回调事件
                if (!destory)
                {
                    OnDestoryObject.Dispatch();
                }
                BaseMonoManagerForUnitySingleton.Instance.BaseUpdate.RemoveListener(BaseUpdateFunc);
                BaseMonoManagerForUnitySingleton.Instance.BaseFixedUpdate.RemoveListener(BaseFixedUpdateFunc);
                return false;
            }

            bool oldActive = active;
            active = (FollowedObj.gameObject.activeSelf && FollowedObj.enabled);
            //如果激活发生变化，回调事件
            if (active != oldActive)
            {
                OnActiveChange.Dispatch(active);
            }

            if (!ignoreActive && !active)
            {
                return false;
            }

            return true;
        }

        // Update is called once per frame
        void BaseUpdateFunc()
        {
            if (!CanUpdate()) return;
            BaseUpdate.Dispatch();
        }
        void BaseFixedUpdateFunc()
        {
            if (!CanUpdate()) return;
            BaseFixedUpdate.Dispatch();
        }

    }
}
