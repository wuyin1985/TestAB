using System;
using Unity.Burst;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

public enum MAuthor
{
    LZ,     // 龙斫
    ST,     // 孙通
    TY,     // 唐耀
    WY,     // 吴垠
    XJP,    // 徐金平
    ZT,     // 周腾
    ZX,     // 周祥
    ZYD,    // 祝亚东
    HSQ,    // 宦思谦    
}

public enum MHandler
{
    HJR,    // 胡津睿
    LX,     // 梁轩
    TEY,    // 陶恩宇
    TJ,     // 陶金
    WDS,    // 王东升
    WK,     // 王可
    XJ,     // 胥君
    YJB,    // 尹建波
    YZF,    // 杨振飞
    YZL,    // 禹子良
    ZM,     // 张盟
    ZW,     // 章文
    ZGY,    // 赵光宇
    ZZH,    // 朱增辉
}

/// <summary>
/// 通用的log输出类
/// </summary>
public class CommonLog
{
    public static string[] author =
    {
        "龙斫",
        "孙通",
        "唐耀",
        "吴垠",
        "徐金平",
        "周腾",
        "周祥",
        "祝亚东",
        "宦思谦",
    };

    public static string[] handler =
    {
        "胡津睿",
        "梁轩",
        "陶恩宇",
        "陶金",
        "王东升",
        "王可",
        "胥君",
        "尹建波",
        "杨振飞",
        "禹子良",
        "张盟",
        "章文",
        "赵光宇",
        "朱增辉",
    };
    static int mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
 
    public static bool IsMainThread =>
        System.Threading.Thread.CurrentThread.ManagedThreadId == mainThreadId && 
        !JobsUtility.IsExecutingJob;
    
#if MHY_DEBUG
    private static int commonLogWhiteList = -1;
#endif

    public static void RefreshWhitList()
    {
#if MHY_DEBUG
        commonLogWhiteList = PlayerPrefs.GetInt("CommonLogWhiteList");
#endif
    }
    public static bool GetWhiteList(MAuthor author)
    {
#if MHY_DEBUG
        if (IsMainThread)
        {
            if (commonLogWhiteList == -1)
            {
                if (PlayerPrefs.HasKey("CommonLogWhiteList"))
                {
                    commonLogWhiteList = PlayerPrefs.GetInt("CommonLogWhiteList");
                }
                else
                {
                    return true;
                }
            }
            return ((commonLogWhiteList >> (int)author) & 1) == 1;
        }
        else
        {
            if (commonLogWhiteList == -1)
            {
                return true;
            }
            else
            {
                return ((commonLogWhiteList >> (int)author) & 1) == 1;
            }
        }
#else
            return true;
#endif
    }

    public static void Log(string message)
    {
        Debug.Log(message);
    }

    public static void Log(string message, params object[] args)
    {
        Debug.LogFormat(message, args);
    }

    public static void Log(object message)
    {
        Debug.Log(message);
    }

    /// <param name="_author">作者</param>
    /// <param name="_message">信息</param>
    [BurstDiscard]
    public static void Log(MAuthor _author, object _message)
    {
        if (GetWhiteList(_author))
        {
            Log($"#{author[(int)_author]}# {_message}");
        }
    }

    /// <param name="_author">作者</param>
    /// <param name="_message">信息</param>
    /// <param name="_handle">处理者</param>
    [System.Diagnostics.Conditional("MHY_DEBUG")]
    public static void Log(MAuthor _author, object _message, MHandler _handler)
    {
        if (GetWhiteList(_author))
        {
            Log($"#{author[(int)_author]}# {_message}, 请找<color=#00ff00>{handler[(int)_handler]}</color>解决");
        }
    }

    public static void Error(object message)
    {
        Debug.LogError(message);
    }

    public static void Error(string message)
    {
        Debug.LogError(message);
    }

    public static void Error(Exception e)
    {
        Debug.LogException(e);
    }

    public static void Error(Exception e, string message)
    {
        Debug.LogException(e);
        Debug.LogError(message);
    }

    public static void Error(UnityEngine.Object context, string message)
    {
        Debug.LogError(message, context);
    }

    public static void Error(UnityEngine.Object context, string message, params object[] args)
    {
        Debug.LogErrorFormat(message, context, args);
    }


    public static void Error(string message, params object[] args)
    {
        Debug.LogErrorFormat(message, args);
    }

    /// <param name="_author">作者</param>
    /// <param name="_message">消息</param>
    public static void Error(MAuthor _author, object _message)
    {
        if (GetWhiteList(_author))
        {
            Debug.LogError($"[{author[(int)_author]}] {_message}");
        }
    }

    /// <param name="_author">作者</param>
    /// <param name="_message">消息</param>
    /// <param name="_handler">处理者</param>
    public static void Error(MAuthor _author, object _message, MHandler _handler)
    {
        if (GetWhiteList(_author))
        {
            Debug.LogError($"[{author[(int)_author]}] {_message}, 请找<color=#00ff00>{handler[(int)_handler]}</color>解决");
        }
    }


    [System.Diagnostics.Conditional("MHY_DEBUG")]
    public static void Warning(object message)
    {
        Debug.LogWarning(message);
    }

    [System.Diagnostics.Conditional("MHY_DEBUG")]
    public static void Warning(string message)
    {
        Debug.LogWarning(message);
    }

    [System.Diagnostics.Conditional("MHY_DEBUG")]
    public static void Warning(UnityEngine.Object context, string format, params object[] args)
    {
        Debug.LogWarningFormat(context, format, args);
    }

    [System.Diagnostics.Conditional("MHY_DEBUG")]
    public static void Warning(string message, params object[] args)
    {
        Debug.LogWarningFormat(message, args);
    }

    /// <param name="_author">作者</param>
    /// <param name="_message">消息</param>
    [System.Diagnostics.Conditional("MHY_DEBUG")]
    public static void LogWarning(MAuthor _author, object _message)
    {
        if (GetWhiteList(_author))
        {
            Debug.LogWarning($"[{author[(int)_author]}] {_message}");
        }
    }

    /// <param name="_author">作者</param>
    /// <param name="_message">消息</param>
    [System.Diagnostics.Conditional("MHY_DEBUG")]
    public static void LogWarning(MAuthor _author, object _message, MHandler _handler)
    {
        if (GetWhiteList(_author))
        {
            Debug.LogWarning($"[{author[(int)_author]}] {_message}, 请找<color=#00ff00>{handler[(int)_handler]}</color>解决");
        }
    }    
}

#if UNITY_EDITOR
/// <summary>
/// 编辑器用日志输出，不受MHY_DEBUG约束
/// </summary>
public class CommonEditorLog
{
    /// <summary>
    /// Editor Log
    /// </summary>
    /// <param name="message"></param>
    public static void Log(string message)
    {
        Debug.Log(string.Concat("[编辑器日志] ", message));
    }
    /// <summary>
    /// Editor Warning
    /// </summary>
    /// <param name="message"></param>
    public static void Warning(string message)
    {
        Debug.LogWarning(string.Concat("[编辑器日志] ", message));
    }
    /// <summary>
    /// Editor Error
    /// </summary>
    /// <param name="message"></param>
    public static void Error(string message)
    {
        Debug.LogError(string.Concat("[编辑器日志] ", message));
    }
}
#endif

/// <summary>
/// 项目部分内容转移用，勿删
/// </summary>
namespace Utils.Common
{
    public class Log
    {
        //public static void Log(string message, params object[] args)
        //{
        //    Debug.LogFormat(message, args);
        //}

        //public static void Log(object message)
        //{
        //    Debug.Log(message);
        //}


        public static void Error(object message)
        {
            CommonLog.Error(message);
        }


        public static void Error(string message, params object[] args)
        {
            CommonLog.Error(message, args);
        }

        public static void Warning(object message)
        {
            CommonLog.Warning(message);
        }

        public static void Warning(string message, params object[] args)
        {
            CommonLog.Warning(message, args);
        }
    }
}

//带模块key 的log输出
public class CommonLogger
{
    public string moduleName;

    public CommonLogger(string moduleName)
    {
        this.moduleName = moduleName;
    }

    public void Log(object message)
    {
        Debug.Log(message);
    }

    public void Log(string message)
    {
        Debug.Log($"[<color=white>{moduleName}</color>] {message}");
    }

    public void Log(string message, params object[] args)
    {
        Debug.LogFormat($"[<color=white>{moduleName}</color>] {message}", args);
    }

    public void Error(object message)
    {
        Debug.LogError(message);
    }

    public void Error(string message)
    {
        Debug.LogError($"[<color=white>{moduleName}</color>] {message}");
    }

    public void Error(UnityEngine.Object context, string message)
    {
        Debug.LogError($"[<color=white>{moduleName}</color>] {message}", context);
    }

    public void Error(UnityEngine.Object context, string message, params object[] args)
    {
        Debug.LogErrorFormat($"[<color=white>{moduleName}</color>] {message}", context, args);
    }

    public void Error(string message, params object[] args)
    {
        Debug.LogErrorFormat($"[<color=white>{moduleName}</color>] {message}", args);
    }

    public void Error(Exception e, string message)
    {
        Debug.LogException(e);
        Debug.LogError($"[<color=white>{moduleName}</color>] {message}");
    }

    public void Error(Exception e, string message, params object[] args)
    {
        Debug.LogException(e);
        Debug.LogErrorFormat($"[<color=white>{moduleName}</color>] {message}", args);
    }


    public void Warning(object message)
    {
        Debug.LogWarning(message);
    }

    public void Warning(string message)
    {
        Debug.LogWarning($"[<color=white>{moduleName}</color>] {message}");
    }

    public void Warning(string message, params object[] args)
    {
        Debug.LogWarningFormat($"[<color=white>{moduleName}</color>] {message}", args);
    }
}