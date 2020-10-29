using UnityEngine;

/// <summary>
/// 在所有场景中保持单例状态
/// </summary>
public class UnitySingleton<T> : MonoBehaviour where T : Component
{
    private static T mInstance;

    public static T Instance
    {
        get
        {
            if (mInstance == null)
            {
                GameObject go = new GameObject();
                mInstance = (T)go.AddComponent(typeof(T));
                go.name = "---<Singleton>---" + typeof(T).Name;
                go.hideFlags = HideFlags.DontSave;
                DontDestroyOnLoad(go);

                var st = mInstance as UnitySingleton<T>;
                st.InstanceInit();
            }
            return mInstance;
        }
    }

    /// <summary>
    /// 第一次创建实例的调用代码
    /// </summary>
    protected virtual void InstanceInit()
    {

    }
}

/// <summary>
/// 仅在当前场景保持单例状态
/// </summary>
/// <typeparam name="T"></typeparam>
public class UnitySingletonForCurrentScene<T> : MonoBehaviour where T : Component
{
    static private T mInstance;
    static public T Instance
    {
        get
        {
            if (mInstance == null)
            {
                GameObject go = new GameObject();
                mInstance = (T)go.AddComponent(typeof(T));
                go.name = "<DontSave>" + typeof(T).Name;
                go.hideFlags = HideFlags.DontSave;
                var ins = mInstance as UnitySingletonForCurrentScene<T>;
                ins.InstanceInit();
            }

            return mInstance;
        }                
    }

    protected virtual void InstanceInit()
    {

    }    
}

public class SimpleSingletonProvider<T> where T : class, new()
{
    private static T _instance;

    private static object _lockHelper = new object();

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lockHelper)
                {
                    if (_instance == null)
                    {
                        _instance = new T();
                        var st = _instance as SimpleSingletonProvider<T>;
                        st.InstanceInit();
                    }
                }
            }

            return _instance;
        }
    }

    /// <summary>
    /// 第一次创建实例的调用代码
    /// </summary>
    protected virtual void InstanceInit()
    {

    }
}

public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        Instance = GetComponent<T>();
    }   

    protected virtual void OnDestroy()
    {
        Instance = null;
    }
}


public abstract class SimpleServerProxySingletonProvider<T> : SimpleSingletonProvider<T> where T : class, new()
{
    //第一次链接上会调用,主要也只有登陆，ping这种级别的Proxy，如果有，会用
    public virtual void OnConnected(){}
    //断线调用
    public virtual void OnDisConnected(){}
    //断线重连成功后调用
    public virtual void OnReConnected(){}
}