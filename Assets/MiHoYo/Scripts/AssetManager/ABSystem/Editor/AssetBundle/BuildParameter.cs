using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildParameter
{
    public bool Development = true;
    /// <summary>
    /// 开启连接Profiler
    /// </summary>
    public bool ConnectWithProfiler = false;
    /// <summary>
    /// 开启Deep Profiler
    /// </summary>
    public bool EnableDeepProfilingSupport = false;
    /// <summary>
    /// 是否允许脚本Debug
    /// </summary>
    public bool AllowDebugging = false;
    /// <summary>
    /// 脚本编译方式
    /// </summary>
    public bool IL2CPP = true;
    /// <summary>
    /// 是否重新导入UI资源
    /// </summary>
    public bool ReImportUIRes = false;
    /// <summary>
    /// 是否清理旧的AB资源
    /// </summary>
    public bool ClearOldAB = false;
    /// <summary>
    /// 打包宏设置
    /// </summary>
    public string DefineSymbols = "MHY_DEBUG";
    /// <summary>
    /// App显示名
    /// </summary>
    public string ProductName = "TheWar2061";
    /// <summary>
    /// App包名
    /// </summary>
    public string BundleIdentifier = "com.thewar.wargame";
    /// <summary>
    /// App版本号
    /// </summary>
    public string AppVersion = "1.0.0";
    /// <summary>
    /// version配置文件
    /// </summary>
    public string VersionCfg = "version.xml";


    public void SetExtraArg(string buildParam)
    {
        CommonLog.Log($"ExtraArg = {buildParam}");

        //是否为development
        var devBuildStr = GetBuildSettingArgStr("Development", buildParam);
        Development = devBuildStr == "false" ? false : true;

        //是否开启Profiler
        var ConnectWithProfilerStr = GetBuildSettingArgStr("ConnectWithProfiler", buildParam);
        ConnectWithProfiler = ConnectWithProfilerStr == "false" ? false : true;

        //是否开启Deep Profiler
        var EnableDeepProfilingSupportStr = GetBuildSettingArgStr("EnableDeepProfilingSupport", buildParam);
        EnableDeepProfilingSupport = EnableDeepProfilingSupportStr == "false" ? false : true;

        //是否开启debug
        var AllowDebuggingStr = GetBuildSettingArgStr("AllowDebugging", buildParam);
        AllowDebugging = AllowDebuggingStr == "false" ? false : true;

        var IL2CPPStr = GetBuildSettingArgStr("IL2CPP", buildParam);
        IL2CPP = IL2CPPStr == "false" ? false : true;

        //是否ReImport UI资源
        var ReImportUIResStr = GetBuildSettingArgStr("ReImportUIRes", buildParam);
        ReImportUIRes = ReImportUIResStr == "false" ? false : true;

        //是否清理Old AB
        var ClearOldABStr = GetBuildSettingArgStr("ClearOldAB", buildParam);
        ClearOldAB = ClearOldABStr == "false" ? false : true;

        //修改宏参数
        DefineSymbols = GetBuildSettingArgStr("DefineSymbols", buildParam);

        //获取App显示名
        ProductName = GetBuildSettingArgStr("ProductName", buildParam);

        //获取App包名
        BundleIdentifier = GetBuildSettingArgStr("BundleIdentifier", buildParam);

        //获取App版本号
        AppVersion = GetBuildSettingArgStr("AppVersion", buildParam);
        
        //版本配置文件
        VersionCfg = GetBuildSettingArgStr("VersionCfg", buildParam);

        CommonLog.Log($"Development = {Development}\n" +
            $"ConnectWithProfiler = {ConnectWithProfiler}\n" +
            $"EnableDeepProfilingSupport = {EnableDeepProfilingSupport}\n" +
            $"AllowDebugging = {AllowDebugging}\n" +
            $"IL2CPP = {IL2CPP}\n" +
            $"ReImportUIRes = {ReImportUIRes}\n" +
            $"ClearOldAB = {ClearOldAB}\n" +
            $"ProductName = {ProductName}\n" +
            $"BundleIdentifier = {BundleIdentifier}\n" +
            $"AppVersion = {AppVersion}\n" +
            $"VersionCfg = {VersionCfg}\n" +
            $"DefineSymbols = {DefineSymbols}");
    }

    private string GetBuildSettingArgStr(string argName, string searchSource)
    {
        var strArray = searchSource.Split(',');
        for (int i = 0; i < strArray.Length; i++)
        {
            if (strArray[i].Contains(argName))
            {
                int shBgn = strArray[i].IndexOf("=") + 1;
                int shEnd = strArray[i].IndexOf("]");
                if (shBgn > -1 && shEnd > -1 && shEnd > shBgn)
                {
                    var rtn = strArray[i].Substring(shBgn, shEnd - shBgn);
                    CommonLog.Log($"get build setting argument {argName} = {rtn}");
                    return rtn;
                }
                else
                {
                    CommonLog.Error($"get build setting argument {argName} failed!");
                }
            }
        }
        return string.Empty;
    }
}
