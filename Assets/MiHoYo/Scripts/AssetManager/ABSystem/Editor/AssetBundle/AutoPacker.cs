using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using System.IO;
using System;
using System.Linq;
using System.Text;
using UnityEngine.Profiling;
using Res.ABSystem;

public enum AutoPackerState
{
    None,

    /// <summary>
    /// 打包AB资源
    /// </summary>
    Pack_AB,

    /// <summary>
    /// 构建App
    /// </summary>
    Build_App,
}

public class AutoPacker : IActiveBuildTargetChanged
{
    private static string ExportPath = "";

    private static string ExportName = "";

    #region Build Params

    public static BuildParameter buildParameter;

    #endregion


    public int callbackOrder => 0;

    public static AutoPackerState curState;


    #region Set Build Parameters

    /// <summary>
    /// 设置打包参数
    /// </summary>
    public static void SetBuildParam()
    {
        var buildString = GetBuildString();
        if (buildString != null)
        {
            buildParameter.SetExtraArg(buildString);
        }
    }

    private static string GetBuildSettingArgStr(string argName, string searchSource)
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

    #endregion

    #region Packer AssetBundle

    private static string GetBuildString()
    {
        var args = Environment.GetCommandLineArgs();
        return args.FirstOrDefault(s => s.Contains("[Development="));
    }

    public static void PackAB_PC()
    {
        //修改assetbundles生成路径
        var buildString = GetBuildString();
        if (buildString != null)
        {
            buildParameter = new BuildParameter();
            buildParameter.SetExtraArg(buildString);
        }

        //判断是不是PC平台
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows ||
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64 ||
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneOSX ||
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneLinux64)
        {
            PackAB(BuildTarget.StandaloneWindows64);
        }
        else
        {
            curState = AutoPackerState.Pack_AB;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone,
                BuildTarget.StandaloneWindows64);
        }
    }

    public static void PackAB_Android()
    {
        //修改assetbundles生成路径
        var buildString = GetBuildString();
        if (buildString != null)
        {
            buildParameter = new BuildParameter();
            buildParameter.SetExtraArg(buildString);
        }

        //判断是不是Android平台
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
        {
            PackAB(BuildTarget.Android);
        }
        else
        {
            curState = AutoPackerState.Pack_AB;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }
    }

    public static void PackAB_IOS()
    {
        //修改assetbundles生成路径
        var buildString = GetBuildString();
        if (buildString != null)
        {
            buildParameter = new BuildParameter();
            buildParameter.SetExtraArg(buildString);
        }

        //判断是不是iOS平台
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
        {
            PackAB(BuildTarget.iOS);
        }
        else
        {
            curState = AutoPackerState.Pack_AB;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
        }
    }

    #endregion

    #region Build App

    public static void BuildPC()
    {
        //修改导出路径
        string[] args = System.Environment.GetCommandLineArgs();
        if (args != null)
        {
            if (args.Length >= 6)
                ExportPath = args[5];
            if (args.Length >= 7)
                ExportName = args[6];

            var buildString = GetBuildString();
            if (buildString != null)
            {
                buildParameter = new BuildParameter();
                buildParameter.SetExtraArg(buildString);
            }
        }

        //判断是不是PC平台
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows ||
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64 ||
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneOSX ||
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneLinux64)
        {
            BuildApp(BuildTarget.StandaloneWindows64);
        }
        else
        {
            curState = AutoPackerState.Build_App;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone,
                BuildTarget.StandaloneWindows64);
        }
    }

    public static void BuildAndroid()
    {
        //修改导出路径
        string[] args = System.Environment.GetCommandLineArgs();
        if (args != null)
        {
            if (args.Length >= 6)
                ExportPath = args[5];
            if (args.Length >= 7)
                ExportName = args[6];
            
            var buildParam = GetBuildString();
            if (buildParam != null)
            {
                buildParameter = new BuildParameter();
                buildParameter.SetExtraArg(args[7]);
            }
        }

        //判断是不是Android平台
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
        {
            BuildApp(BuildTarget.Android);
        }
        else
        {
            curState = AutoPackerState.Build_App;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }
    }

    public static void BuildIOS()
    {
        //修改导出路径
        string[] args = System.Environment.GetCommandLineArgs();
        if (args != null && args.Length >= 6)
            ExportPath = args[5];
        if (args != null && args.Length >= 7)
            ExportName = args[6];

        var buildString = GetBuildString();
        if (buildString != null)
        {
            buildParameter = new BuildParameter();
            buildParameter.SetExtraArg(buildString);
        }

        //判断是不是iOS平台
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
        {
            BuildApp(BuildTarget.iOS);
        }
        else
        {
            curState = AutoPackerState.Build_App;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
        }
    }

    #endregion

    /// <summary>
    /// 切换平台回调
    /// </summary>
    /// <param name="previousTarget"></param>
    /// <param name="newTarget"></param>
    public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
    {
        AssetDatabase.Refresh();

        CommonLog.Log($"Switch {newTarget.ToString()}");

        if (curState == AutoPackerState.None)
        {
        }
        else if (curState == AutoPackerState.Pack_AB)
        {
            PackAB(newTarget);
        }
        else if (curState == AutoPackerState.Build_App)
        {
            BuildApp(newTarget);
        }
    }

    public static void PackAB(BuildTarget newTarget)
    {
        //重新导入UI资源
        /*if (buildParameter.ReImportUIRes)
        {
            ReImportUIRes();
        }*/

        switch (newTarget)
        {
            case BuildTarget.iOS:
            {
                //设置宏
                DefineSymbolsTool.SetDefineSymbols(BuildTargetGroup.iOS, buildParameter.DefineSymbols);
                ClearOldAssetBundles(AssetBundlePathResolver.BundleIOSSavedPath);
                YSEditorBuildBundles.AutoBuildAssetBundles_IOS(buildParameter.ClearOldAB);
            }
                break;
            case BuildTarget.Android:
            {
                //设置宏
                DefineSymbolsTool.SetDefineSymbols(BuildTargetGroup.Android, buildParameter.DefineSymbols);
                ClearOldAssetBundles(AssetBundlePathResolver.BundleAndroidSavedPath);
                YSEditorBuildBundles.AutoBuildAssetBundles_Android(buildParameter.ClearOldAB);
            }
                break;
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
            case BuildTarget.StandaloneOSX:
            case BuildTarget.StandaloneLinux64:
            {
                //设置宏
                DefineSymbolsTool.SetDefineSymbols(BuildTargetGroup.Standalone, buildParameter.DefineSymbols);
                ClearOldAssetBundles(AssetBundlePathResolver.BundlePCSavedPath);
                YSEditorBuildBundles.AutoBuildAssetBundles_PC(buildParameter.ClearOldAB);
            }
                break;
        }

        CompileTest();
    }

    public static void BuildApp(BuildTarget newTarget)
    {
        switch (newTarget)
        {
            case BuildTarget.iOS:
            {
                ExportXcode();
            }
                break;
            case BuildTarget.Android:
            {
                EditorUserBuildSettings.androidCreateSymbolsZip = true;
                ExportAndroid();
            }
                break;
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
            case BuildTarget.StandaloneOSX:
            case BuildTarget.StandaloneLinux64:
            {
                ExportPC();
            }
                break;
        }

        CompileTest();
    }

    private static BuildOptions GenBuildOptions()
    {
        BuildOptions buildOptions = BuildOptions.Development | BuildOptions.StrictMode;
        if (!buildParameter.Development)
        {
            buildOptions = BuildOptions.None | BuildOptions.StrictMode;
        }
        else
        {
            //勾选ConnectWithProfiler 手机启动非常慢
            if (buildParameter.ConnectWithProfiler)
                buildOptions |= BuildOptions.ConnectWithProfiler;
            if (buildParameter.EnableDeepProfilingSupport)
                buildOptions |= BuildOptions.EnableDeepProfilingSupport;
            if (buildParameter.AllowDebugging)
                buildOptions |= BuildOptions.AllowDebugging;
        }

        return buildOptions;
    }

    //[MenuItem("Packer/Export PC", false , 20)]
    public static void ExportPC()
    {
        //拷贝资源
        //-EditorToolUtils.InstallGameAssets();
        //生成版本配置文件
        //-GameAssetManager.GenerateVersionCfg(buildParameter.AppVersion, buildParameter.VersionCfg);

        SetPCSettings();

        List<string> levels = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled) continue;
            levels.Add(scene.path);
        }

        //string subDirName = string.Format($"{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day}-{DateTime.Now.Hour}-{DateTime.Now.Minute}");
        var exportPath = string.Format($"{Application.dataPath}{ExportPath}");

        if (!Directory.Exists(exportPath))
            Directory.CreateDirectory(exportPath);

        var exportFile = exportPath + ExportName;
        var buildOptions = GenBuildOptions();
        CommonLog.Log($"ExportPath={exportFile}");
        BuildPipeline.BuildPlayer(levels.ToArray(), exportFile, BuildTarget.StandaloneWindows64, buildOptions);
    }

    public static void ExportXcode()
    {
        //拷贝资源
        //-EditorToolUtils.InstallGameAssets();
        //生成版本配置文件
        //-GameAssetManager.GenerateVersionCfg(buildParameter.AppVersion, buildParameter.VersionCfg);

        SetIOSSettings();

        List<string> levels = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled) continue;
            levels.Add(scene.path);
        }

        //string subDirName = string.Format($"{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day}-{DateTime.Now.Hour}-{DateTime.Now.Minute}");
        var exportPath = string.Format($"{Application.dataPath}{ExportPath}");

        if (!Directory.Exists(exportPath))
            Directory.CreateDirectory(exportPath);

        var exportFile = exportPath + ExportName;
        var buildOptions = GenBuildOptions();

        CommonLog.Log($"ExportPath={exportFile}");
        BuildPipeline.BuildPlayer(levels.ToArray(), exportFile, BuildTarget.iOS, buildOptions);
    }

    public static void ExportAndroid()
    {
        //拷贝资源
        //-EditorToolUtils.InstallGameAssets();
        //生成版本配置文件
        //-GameAssetManager.GenerateVersionCfg(buildParameter.AppVersion, buildParameter.VersionCfg);

        SetAndroidSettings();

        List<string> levels = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled) continue;
            levels.Add(scene.path);
        }
        //var exportPath = string.Format($"{Application.dataPath}{ExportPath}");

        //if (!Directory.Exists(exportPath))
        //    Directory.CreateDirectory(exportPath);
        var exportApk = ExportPath + ExportName;

        var buildOptions = GenBuildOptions();
        CommonLog.Log($"ExportPath={exportApk}");
        BuildPipeline.BuildPlayer(levels.ToArray(), exportApk, BuildTarget.Android, buildOptions);
    }

    public static void SetIOSSettings()
    {
        //之后这块逻辑写到jenkins里
        if (buildParameter.Development)
        {
            //-AkPluginActivator.ActivateProfile();
            EditorUserBuildSettings.development = true;
        }
        else
        {
            //-AkPluginActivator.ActivateRelease();
            EditorUserBuildSettings.development = false;
        }

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS,
            buildParameter.IL2CPP ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x);
        PlayerSettings.companyName = "thewar";
        PlayerSettings.productName = !string.IsNullOrEmpty(buildParameter.ProductName)
            ? buildParameter.ProductName
            : "TheWar2061";
        PlayerSettings.bundleVersion = GetBuildVersion();
        PlayerSettings.colorSpace = ColorSpace.Linear;

        PlayerSettings.applicationIdentifier = !string.IsNullOrEmpty(buildParameter.BundleIdentifier)
            ? buildParameter.BundleIdentifier
            : "com.thewar.wargame";
        PlayerSettings.iOS.appleDeveloperTeamID = "U24AL62UE9";
        PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Distribution;
        PlayerSettings.iOS.iOSManualProvisioningProfileID = "97ef5508-8974-4d18-9b6c-cab7154304ea";

        //设置宏
        DefineSymbolsTool.SetDefineSymbols(BuildTargetGroup.iOS, buildParameter.DefineSymbols);
    }

    public static void SetAndroidSettings()
    {
        if (buildParameter.Development)
        {
            //-AkPluginActivator.ActivateProfile();
            EditorUserBuildSettings.development = true;
        }
        else
        {
            //-AkPluginActivator.ActivateRelease();
            EditorUserBuildSettings.development = false;
        }

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,
            buildParameter.IL2CPP ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x);
        if (buildParameter.IL2CPP)
        {
            PlayerSettings.SetArchitecture(BuildTargetGroup.Android, (int) AndroidArchitecture.ARM64);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        }
        else
        {
            PlayerSettings.SetArchitecture(BuildTargetGroup.Android, (int) AndroidArchitecture.ARMv7);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
        }

        PlayerSettings.companyName = "thewar";
        PlayerSettings.productName = !string.IsNullOrEmpty(buildParameter.ProductName)
            ? buildParameter.ProductName
            : "TheWar2061";
        PlayerSettings.bundleVersion = GetBuildVersion();
        PlayerSettings.colorSpace = ColorSpace.Linear;

        PlayerSettings.applicationIdentifier = !string.IsNullOrEmpty(buildParameter.BundleIdentifier)
            ? buildParameter.BundleIdentifier
            : "com.thewar.wargame";

        //设置宏
        DefineSymbolsTool.SetDefineSymbols(BuildTargetGroup.Android, buildParameter.DefineSymbols);
    }

    public static void SetPCSettings()
    {
        if (buildParameter.Development)
        {
            //-AkPluginActivator.ActivateProfile();
            EditorUserBuildSettings.development = true;
        }
        else
        {
            //-AkPluginActivator.ActivateRelease();
            EditorUserBuildSettings.development = false;
        }

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone,
            buildParameter.IL2CPP ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x);
        PlayerSettings.companyName = "thewar";
        PlayerSettings.productName = !string.IsNullOrEmpty(buildParameter.ProductName)
            ? buildParameter.ProductName
            : "TheWar2061";
        PlayerSettings.bundleVersion = GetBuildVersion();
        PlayerSettings.colorSpace = ColorSpace.Linear;

        PlayerSettings.applicationIdentifier = !string.IsNullOrEmpty(buildParameter.BundleIdentifier)
            ? buildParameter.BundleIdentifier
            : "com.thewar.wargame";

        //设置宏
        DefineSymbolsTool.SetDefineSymbols(BuildTargetGroup.Standalone, buildParameter.DefineSymbols);
    }

    public static string GetBuildVersion()
    {
        string version = "1.0.0";

        //填充时间
        var now = DateTime.Now;
        version += string.Format(".{0}{1:D2}{2:D2}{3:D2}{4:D2}",
            now.Year, now.Month, now.Day,
            now.Hour, now.Minute);
        return version;
    }

    #region ReImportUI Res

    //[MenuItem("Tools/ReImport UI Res", false, 10)]
    public static void ReImportUIRes()
    {
        var filePaths =
            Directory.GetFiles($"{Application.dataPath}/AssetBundles/UI", "*.*", SearchOption.AllDirectories);

        foreach (var filePath in filePaths)
        {
            if (!filePath.EndsWith(".prefab"))
                continue;

            var assetPath = filePath.Substring(filePath.IndexOf("/Assets/") + 1).Replace("\\", "/");
            AssetDatabase.ImportAsset(assetPath);
            CommonLog.Log($"Import Asset {assetPath}");
        }

        //AssetDatabase.ForceReserializeAssets(tempList, ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    //[MenuItem("Tools/Clear Old AB")]
    public static void ClearABTest()
    {
        ClearOldAssetBundles(AssetBundlePathResolver.BundlePCSavedPath);
    }

    public static void ClearOldAssetBundles(string bundlePath)
    {
        if (!buildParameter.ClearOldAB)
            return;

        if (Directory.Exists(bundlePath))
            Directory.Delete(bundlePath, true);

        Directory.CreateDirectory(bundlePath);
        CommonLog.Log($"Clear Old AssetBundles!");
    }

    #endregion

    #region Auto Compile

    [MenuItem("Packer/CompileTest", false)]
    /// <summary>
    /// 编译测试
    /// </summary>
    public static void CompileTest()
    {
        string filePath = Application.dataPath + "/../Build/compile.log";
        if (File.Exists(filePath))
            File.Delete(filePath);

        //创建新的文件
        File.WriteAllText(filePath, "Compile Success!");
        CommonLog.Log($"Compile Success!");
    }

    #endregion
}