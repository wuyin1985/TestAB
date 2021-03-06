using Res.ABSystem;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


/// <summary>
/// AssetBundle编译
/// </summary>
public static class YSEditorBuildBundles
{
    private static List<string> DontPackFiles = new List<string>()
    {
        ".meta", ".js", ".cs", ".asmdef", ".iml", ".xlsx", ".zip", ".rar",
        ".7z", ".xsxproj", ".dll", ".so", ".mp3", ".wav", ".lua", ".py"
    }; // ".FBX", ".fbx" //".psd", ".tga"

    private const string OPEN_XLUA = "OPEN_XLUA";

    /// <summary>
    /// 重命名asset的bundle名称，导出映射文件
    /// </summary>
    public static void Execute()
    {
    }

    public static void SwitchToIOS()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
    }

    public static void SwitchToAndroid()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
    }

    /// <summary>
    /// 检查是否有短MD5重复
    /// </summary>
    public static void CheckBundleTableAboutShortMD5Repeat(List<AssetBundleXMLRawData> tableDatas)
    {
        HashSet<string> MD5Sets = new HashSet<string>();
        List<string> RPMD5S = new List<string>();
        foreach (var t in tableDatas)
        {
            if (t.Language != eAssetLanguageVarType.Default) continue;
            var assMD5Name = t.resPathMD5Struct.GetMD5Str(true);
            if (!MD5Sets.Contains(assMD5Name))
            {
                MD5Sets.Add(assMD5Name);
            }
            else
            {
                RPMD5S.Add(assMD5Name);
                CommonLog.Log($"出现了重复的MD5路径对象,关注看下是否是创建了同名对象！{t.assetFullName},路径：{t.resPathMD5Struct}");
            }
        }

        //重新映射这批对象，看看是否是路径相同的对象
        Dictionary<string, List<AssetBundleXMLRawData>> MD5KeyToDatas
            = new Dictionary<string, List<AssetBundleXMLRawData>>();
        foreach (var t in tableDatas)
        {
            if (t.Language != eAssetLanguageVarType.Default) continue;
            var assMD5Name = t.resPathMD5Struct.GetMD5Str(true);
            if (RPMD5S.Contains(assMD5Name))
            {
                List<AssetBundleXMLRawData> list;
                MD5KeyToDatas.TryGetValue(assMD5Name, out list);
                if (list == null)
                {
                    list = new List<AssetBundleXMLRawData>();
                    MD5KeyToDatas[assMD5Name] = list;
                }

                list.Add(t);
            }
        }

        List<string> repeatedSameMD5 = new List<string>();
        foreach (var kv in MD5KeyToDatas)
        {
            var list = kv.Value;
            var md5Value = kv.Key;
            string path = null;
            foreach (var l in list)
            {
                if (path == null)
                {
                    path = l.assetFullName;
                }

                //判断是否有路径不同，但是MD5相同的内容
                if (l.assetFullName != path)
                {
                    repeatedSameMD5.Add(md5Value);
                }
            }
        }
    }


    public static List<AssetBundleXMLRawData> CleanAndGenXMLRawInfo(string path)
    {
        //生成AssetsConfig.Asset文件
        //Game.CreatResList.CreateNewAssetsConfig();

        //全删除打包时间太久
        //删除原先所有导出文件
        //if (Directory.Exists(path))
        //{
        //    Directory.Delete(path, true);
        //}

        //判断是否需要创建路径
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        EditorUtility.DisplayProgressBar("Loading", "Loading...", 0.1f);
        Caching.ClearCache();

        //清空数据
        //CleanOldName(); 

        CommonLog.Log($"检索需要打包的资源");
        var AssetBundleRawInfoList = RefreshNewNameAndGetAssetBundleList();
        CommonLog.Log($"检查资源是否有重名");
        CheckBundleTableAboutShortMD5Repeat(AssetBundleRawInfoList);

        AssetDatabase.Refresh();

        EditorUtility.ClearProgressBar();

        return AssetBundleRawInfoList;
    }


    private static void CleanAndGenMD5UpdateFileFileHelper(
        List<AssetBundleUpdateInfo.AssetBundleUpdateFileInfo> upfileList, string path, MD5Creater.MD5Struct md5,
        bool isComplex)
    {
        var fi = new AssetBundleUpdateInfo.AssetBundleUpdateFileInfo();
        fi.FileName = md5;
        fi.IsComplexName = isComplex;
        fi.FileMD5 = MD5Creater.MD5FileShortLong(path + fi.FileName.GetMD5Str(!fi.IsComplexName));
        fi.SizeKB = Mathf.Clamp((int) (FileUtils.FileSize(path + fi.FileName.GetMD5Str(!fi.IsComplexName)) / 1024), 1,
            int.MaxValue);
        upfileList.Add(fi);
    }

    [MenuItem("Assets/CleanBundleNames", false, 30)]
    public static void CleanSelectionsBundleName()
    {
        var assets = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);
        foreach (var asset in assets)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            var importer = AssetImporter.GetAtPath(assetPath);
            importer.assetBundleName = null;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }


    ///// <summary>
    ///// 编译assetbundle
    ///// </summary>
    //[MenuItem("Packer/打包工具/Clean AB Setting", false, 30)]
    //public static void BuildAssetBundles_Clean()
    //{ 
    //    //清空数据
    //    CleanOldName();
    //    CommonLog.Log("清除旧的AB设置成功");
    //    EditorUtility.ClearProgressBar();
    //}

    //[MenuItem("Packer/打包工具/生成AssetBundleNames", false, 30)]
    public static void GenerateAssetBundleNames()
    {
        var assets = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);
        //foreach (var data in objects)
        for (int i = 0; i < assets.Length; i++)
        {
            var assetPath = AssetDatabase.GetAssetPath(assets[i]);

            if (!assetPath.Contains(".")) continue;

            if (assetPath.EndsWith(".meta")) continue;

            //特殊图片文件格式不打包
            if (assetPath.EndsWith(".psd") || assetPath.EndsWith(".tga")) continue;

            //音频使用插件打包
            if (assetPath.EndsWith(".wav") || assetPath.EndsWith(".mp3")) continue;

            if (assetPath.EndsWith(".cs") || assetPath.EndsWith(".js")) continue;

            if (!assetPath.Contains("/AssetBundles/")) continue;

            var bundleName = Path.GetDirectoryName(assetPath).Replace("\\", "/");
            bundleName = bundleName.Replace("Assets/AssetBundles/", "").ToLower();

            EditorUtility.DisplayProgressBar("Generate BundleNames", $"{bundleName}", (float) i / assets.Length);

            ChangeAssetBundleNotImport(assetPath, bundleName);
        }

        EditorUtility.ClearProgressBar();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    internal static void ChangeAssetBundleNotImport(
        string assetPath, string assetBundle)
    {
        var importer = AssetImporter.GetAtPath(assetPath);
        if (!string.Equals(
            importer.assetBundleName,
            assetBundle,
            StringComparison.Ordinal))
        {
            importer.assetBundleName = assetBundle;
        }
    }

    /// <summary>
    /// 编译assetbundle
    /// </summary>
    [MenuItem("Packer/打包工具/打包PC AB", false, 30)]
    public static void BuildAssetBundles_PC()
    {
#if UNITY_STANDALONE_WIN ||UNITY_STANDALONE_OSX||UNITY_STANDALONE_LINUX

        var tab = CleanAndGenXMLRawInfo(AssetBundlePathResolver.BundlePCSavedPath);
        if (tab == null) return;

        ABBuilder builder = new AssetBundleBuilder5x_PC();
        builder.BundleSavePath = AssetBundlePathResolver.BundlePCSavedPath;
        builder.AssetBundleRawInfoList = tab;

        AssetBundleManifest manifest = null;

        if (tab.Count > 0)
        {
            builder.Begin();
            try
            {
                manifest = builder.Export();
            }
            catch (System.Exception e)
            {
                CommonLog.Error(e);
            }

            if (manifest != null)
            {
                builder.End(manifest, true);
            }
            else
            {
                CommonLog.Error("manifest 为空");
            }
        }
        else
        {
            CommonLog.Warning(string.Format("本次导出没有生成文件"));
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        CommonLog.Log("资源打包完成！");
        return;
#else
        CommonLog.Error("请先切换到对应PC平台");
#endif
    }

    /// <summary>
    /// 编译assetbundle
    /// </summary>
    [MenuItem("Packer/打包工具/打包Android AB", false, 30)]
    public static void BuildAssetBundles_Android()
    {
#if UNITY_ANDROID
        var tab = CleanAndGenXMLRawInfo(AssetBundlePathResolver.BundleAndroidSavedPath);
        if (tab == null) return;

        ABBuilder builder = new AssetBundleBuilder5x_Android();
        builder.BundleSavePath = AssetBundlePathResolver.BundleAndroidSavedPath;
        builder.AssetBundleRawInfoList = tab;

        AssetBundleManifest manifest = null;

        if (tab.Count > 0)
        {
            builder.Begin();
            try
            {
                manifest = builder.Export();
            }
            catch (System.Exception e)
            {
                CommonLog.Error(e);
            }
            builder.End(manifest, true);
        }
        else
        {
            CommonLog.Warning(string.Format("本次导出没有生成文件"));
        }
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
        CommonLog.Log("资源打包完成！");
        return;
#else
        CommonLog.Error("请先切换到对应Android平台");
#endif
    }

    /// <summary>
    /// 编译assetbundle
    /// </summary>
    [MenuItem("Packer/打包工具/打包IOS", false, 30)]
    public static void BuildAssetBundles_IOS()
    {
#if UNITY_IOS
        var tab = CleanAndGenXMLRawInfo(AssetBundlePathResolver.BundleIOSSavedPath);
        if (tab == null) return;

        ABBuilder builder = new AssetBundleBuilder5x_IOS();
        builder.BundleSavePath = AssetBundlePathResolver.BundleIOSSavedPath;
        builder.AssetBundleRawInfoList = tab;

        AssetBundleManifest manifest = null;

        if (tab.Count > 0)
        {
            builder.Begin();
            try
            {
                manifest = builder.Export();
            }
            catch (System.Exception e)
            {
                CommonLog.Error(e);
            }
            builder.End(manifest, true);
        }
        else
        {
            CommonLog.Warning(string.Format("本次导出没有生成文件"));
        }
         
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
        CommonLog.Log("资源打包完成！");
        return;
#else
        CommonLog.Error("请先切换到对应IOS平台");
#endif
    }


    #region AudioPacker

    public static void AutoBuildAssetBundles_PC(bool forceRebuild = false)
    {
        //判断是不是PC平台
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows ||
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64 ||
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneOSX ||
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneLinux64)
        {
            var tab = RefreshNewNameAndGetAssetBundleList();
            CheckBundleTableAboutShortMD5Repeat(tab);
            AssetDatabase.Refresh();

            ABBuilder builder = new AssetBundleBuilder5x_PC();
            builder.BundleSavePath = AssetBundlePathResolver.BundlePCSavedPath;
            builder.AssetBundleRawInfoList = tab;

            AssetBundleManifest manifest = null;

            if (tab.Count > 0)
            {
                builder.Begin();
                try
                {
                    manifest = builder.Export(forceRebuild);
                }
                catch (System.Exception e)
                {
                    CommonLog.Error(e);
                }

                builder.End(manifest, true);
            }
            else
            {
                CommonLog.Warning(string.Format("本次导出没有生成文件"));
            }

            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
            CommonLog.Log("资源打包完成！");
            return;
        }

        CommonLog.Error("请先切换到对应PC平台");
    }

    public static void AutoBuildAssetBundles_Android(bool forceRebuild = false)
    {
        //判断是不是Android平台
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
        {
            var tab = RefreshNewNameAndGetAssetBundleList();
            CheckBundleTableAboutShortMD5Repeat(tab);

            ABBuilder builder = new AssetBundleBuilder5x_Android();
            builder.BundleSavePath = AssetBundlePathResolver.BundleAndroidSavedPath;
            builder.AssetBundleRawInfoList = tab;

            AssetBundleManifest manifest = null;

            if (tab.Count > 0)
            {
                builder.Begin();
                try
                {
                    manifest = builder.Export(forceRebuild);
                }
                catch (System.Exception e)
                {
                    CommonLog.Error(e);
                }

                builder.End(manifest, true);
            }
            else
            {
                CommonLog.Warning(string.Format("本次导出没有生成文件"));
            }

            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
            CommonLog.Log("资源打包完成！");
            return;
        }

        CommonLog.Error("请先切换到对应Android平台");
    }


    public static void AutoBuildAssetBundles_IOS(bool forceRebuild = false)
    {
        //判断是不是iOS平台
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
        {
            var tab = RefreshNewNameAndGetAssetBundleList();
            CheckBundleTableAboutShortMD5Repeat(tab);
            AssetDatabase.Refresh();

            ABBuilder builder = new AssetBundleBuilder5x_IOS();
            builder.BundleSavePath = AssetBundlePathResolver.BundleIOSSavedPath;
            builder.AssetBundleRawInfoList = tab;

            AssetBundleManifest manifest = null;

            if (tab.Count > 0)
            {
                builder.Begin();
                try
                {
                    manifest = builder.Export(forceRebuild);
                }
                catch (System.Exception e)
                {
                    CommonLog.Error(e);
                }

                builder.End(manifest, true);
            }
            else
            {
                CommonLog.Warning(string.Format("本次导出没有生成文件"));
            }

            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
            CommonLog.Log("资源打包完成！");
            return;
        }

        CommonLog.Error("请先切换到对应IOS平台");
    }

    #endregion


    /// <summary>
    /// 清除原本的旧名称
    /// </summary>
    public static void CleanOldName()
    {
        var names = AssetDatabase.GetAllAssetBundleNames();
        for (int i = 0; i < names.Length; i++)
        {
            var name = names[i];
            EditorUtility.DisplayProgressBar("Cleaning", name, (float) i / names.Length);
            AssetDatabase.RemoveAssetBundleName(name, true);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static bool CheckNeedPackFile(string assetPath)
    {
        var extension = Path.GetExtension(assetPath);
        if (DontPackFiles.Contains(extension))
            return false;

        if (assetPath.Contains("LightingData.asset") || assetPath.Contains("New Shader Graph.shadergraph"))
            return false;

        return true;
    }

    private static List<AssetBundleXMLRawData> RefreshNewNameAndGetAssetBundleList()
    {
        List<AssetBundleXMLRawData> list = new List<AssetBundleXMLRawData>();
        //非Default的资源
        List<AssetBundleXMLRawData> foreignlist = new List<AssetBundleXMLRawData>();

        bool checkConfig = AssetBundleBuilderExtSetting.Instance.InitProjectSettingFromFile();
        if (!checkConfig)
            return null;

        var paths = AssetDatabase.GetAllAssetPaths();

        bool isBreak = false;
        for (int i = 0; i < paths.Length; i++)
        {
            var path = paths[i];

            if (!path.Contains(".")) continue;

            //不需要打包的文件类型
            if (!CheckNeedPackFile(path)) continue;

            if (!path.Contains("/AssetBundles/")
                && !AssetBundleBuilderExtSetting.Instance.CheckIsInExtFolderAsset(path)
                && !AssetBundleBuilderExtSetting.Instance.CheckIsSpriteInTagAsset(path))
                continue;

            //if (!path.Contains("/AssetBundles/")) continue;

            //不导文件夹
            if (AssetDatabase.IsValidFolder(path)) continue;

            //是否取消打包
            isBreak = EditorUtility.DisplayCancelableProgressBar("Loading", path, (float) i / paths.Length);
            if (isBreak)
            {
                CommonLog.Log("终止打包");
                return null;
            }

            AssetBundleXMLRawData xmlRawData = new AssetBundleXMLRawData();

            var extensionName =
                Path.GetExtension(path); //path.Remove(0,path.LastIndexOf(".", StringComparison.Ordinal));
            var withoutExtensionPath =
                path.Replace(extensionName,
                    string.Empty); //path.Substring(0, path.LastIndexOf(".", StringComparison.Ordinal));


            int abResourceStartIndex = withoutExtensionPath.LastIndexOf("AssetBundles", StringComparison.Ordinal);
            var resPath = AssetI8NHelper.GetDefaultPathLanguage(abResourceStartIndex >= 0
                ? withoutExtensionPath.Substring(abResourceStartIndex)
                : withoutExtensionPath);

            //剔除 Assets/
            if (resPath.StartsWith("Assets/"))
            {
                int startIndex = resPath.IndexOf("/", StringComparison.Ordinal) + 1;
                resPath = resPath.Substring(startIndex);
            }

            var langType = AssetI8NHelper.GetPathLanguage(path);

            xmlRawData.resPath = resPath;

            xmlRawData.resPathMD5Struct = MD5Creater.Md5Struct(xmlRawData.resPath);

            xmlRawData.Language = langType;

            string fileName = Path.GetFileNameWithoutExtension(resPath);
            string bundleName =
                Path.GetDirectoryName(resPath.Substring(0, resPath.LastIndexOf("/", StringComparison.Ordinal)));

            //设置bundleName,使用时，使用下面的MD5
            xmlRawData.bundleName =
                AssetBundleBuilderExtSetting.Instance.GetGroupMappedBundlePath(xmlRawData.resPath, extensionName);

            xmlRawData.bundleMD5Struct =
                MD5Creater.Md5Struct(
                    AssetBundleBuilderExtSetting.Instance.GetGroupMappedBundlePath(xmlRawData.resPath, extensionName));

            //fullName带后缀名
            //int assetStartIndex = path.IndexOf("Assets", StringComparison.Ordinal);
            xmlRawData.assetFullName = path; //path.Substring(assetStartIndex);

            //int resStartIndex = resPath.LastIndexOf("/", StringComparison.Ordinal) + 1;
            xmlRawData.resShortName = Path.GetFileName(resPath); //resPath.Substring(resStartIndex);

            xmlRawData.OtherLanguages[(int) langType] = true;
            list.Add(xmlRawData);

            foreignlist.Add(xmlRawData);
        }

        //第二遍开始计算asset的多语言信息
        foreach (var xmlRaw in list)
        {
            if (xmlRaw.Language == eAssetLanguageVarType.Default)
            {
                foreach (var xmlForeign in foreignlist)
                {
                    if (xmlForeign.bundleName == xmlRaw.bundleName)
                    {
                        xmlRaw.OtherLanguages[(int) xmlForeign.Language] = true;
                    }
                }
            }
        }


        return list;
    }

    public static void RefreshAssetImporterInfo(List<AssetBundleXMLRawData> data)
    {
    }
}