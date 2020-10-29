using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AssetBundleBuilderExtSetting
{
    [Header("按照Tag分离Bundle的文件夹，这些资源都是Sprite，必须按照Tag自动分好")]
    public List<string> ExtSpriteTagFolderAsset;
    [Header("额外的打包成Bundle的文件夹，这些文件夹因为不在AssetBundles目录下，无法加载，但是可以减少引用")]
    public List<string> ExtFolderAsset;
    [Header("打包成一个Bundle的文件夹")]
    public List<string> GroupABundlePathLists;
    [Header("资源Tag为InPackge的资源")]
    public List<string> GroupAssetInPackage;

    private static AssetBundleBuilderExtSetting _Instance;
    public static AssetBundleBuilderExtSetting Instance
    {
        get
        {
            if (_Instance != null) return _Instance;
            _Instance = new AssetBundleBuilderExtSetting();
            return _Instance;
        }
    }

    #region Editor
    public void ValidateAll()
    {
        ValidatePathList(ExtSpriteTagFolderAsset);
        ValidatePathList(ExtFolderAsset);
        ValidatePathList(GroupABundlePathLists);
    }

    private void ValidatePathList(List<string> paths)
    {
        var needRemoveListIndex = new List<int>();
        for (int i = paths.Count - 1; i >= 0; i--)
        {
            var p = paths[i];
            if (string.IsNullOrEmpty(p) || (!p.StartsWith("AssetBundles") && !p.StartsWith("Assets")))
            {
                p = "--";
            }
            p = p.Replace('\\', '/');
            if (!p.EndsWith("/"))
            {
                p = p + "/";
            }
            paths[i] = p;
        }
    }
    #endregion

    /// <summary>
    /// 检查资源是否是首包资源
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public bool CheckIsInPackageAsset(string path)
    {
        foreach (var gp in GroupAssetInPackage)
        {
            if (path.StartsWith(gp))
            { 
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 检查资源是否是在额外目录的
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public bool CheckIsInExtFolderAsset(string path)
    {
        foreach (var gp in ExtFolderAsset)
        {
            if (path.StartsWith(gp))
            {
                //TODO by zhouxiang
                ////不导场景文件，只有Assetbunlde目录导场景
                //if (path.EndsWith(".unity"))
                //{
                //    return false;
                //}
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 初始化配置
    /// </summary>
    /// <returns></returns>
    public bool InitProjectSettingFromFile()
    {
        var savePath = Application.dataPath + AssetBundleBuilderExtSettingData.SettingFilePath;
        var settingstr = FileUtils.ReadAllText(savePath);
        if (settingstr == null)
        {
            UnityEditor.EditorUtility.DisplayDialog("ASB打包设置", "路径下不存在当前打包设置:" + AssetBundleBuilderExtSettingData.SettingFilePath, "确定");
            return false;
        }
        var res = settingstr.FromXML<AssetBundleBuilderExtSettingData>();

        ExtSpriteTagFolderAsset = res.ExtSpriteTagFolderAsset;
        ExtFolderAsset = res.ExtFolderAsset;
        GroupABundlePathLists = GetABundlePathLists(res);
        GroupAssetInPackage = res.GroupAssetInPackage;
        return true;
    }

    private List<string> GetABundlePathLists(AssetBundleBuilderExtSettingData data)
    {
        List<string> tempList = new List<string>();

        //打成一个Bundle的文件夹
        foreach (var str in data.GroupABundlePathLists)
        {
            tempList.Add(str);
        }

        //打成一个Bundle的图集文件夹
        foreach (var str in data.AtlasABunldePathLists)
        {
            tempList.Add(str);
        }

        return tempList;
    }

    /// <summary>
    /// 获得被映射的Bundle文件夹名称
    /// </summary>
    /// <param name="pathWithoutExtension"></param>
    /// <returns></returns>
    public string GetGroupMappedBundlePath(string pathWithoutExtension, string extensionName)
    {
        //检查是否符合SpriteTag规则
        foreach (var gp in ExtSpriteTagFolderAsset)
        {
            if (pathWithoutExtension.StartsWith(gp))
            {
                var ass = AssetImporter.GetAtPath(pathWithoutExtension + extensionName);
                if (ass != null && ass is TextureImporter)
                {
                    var spass = (TextureImporter)ass;
                    if (!string.IsNullOrEmpty(spass.spritePackingTag))
                    {
                        return spass.spritePackingTag;
                    }
                }
            }
        }

        //检查合并Bundle单一文件夹规则
        foreach (var gp in GroupABundlePathLists)
        {
            if (pathWithoutExtension.StartsWith(gp))
            {
                return gp;
            }
        }
        return pathWithoutExtension;
    }

    public bool CheckIsSpriteInTagAsset(string path)
    {
        foreach (var gp in ExtSpriteTagFolderAsset)
        {
            if (path.StartsWith(gp))
            {
                var ass = AssetImporter.GetAtPath(path);
                if (ass != null && ass is TextureImporter)
                {
                    var spass = (TextureImporter)ass;
                    if (!string.IsNullOrEmpty(spass.spritePackingTag))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
}
