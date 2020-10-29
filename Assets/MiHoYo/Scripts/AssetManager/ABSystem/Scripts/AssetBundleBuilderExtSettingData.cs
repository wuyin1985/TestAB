using System.Collections.Generic;
using UnityEngine;


public class AssetBundleBuilderExtSettingData
{
    public static string SettingFilePath = @"/MiHoYo/Scripts/AssetManager/ABSystem/Editor/ASB打包文件夹设置.xml";

    [Header("按照Tag分离Bundle的文件夹，这些资源都是Sprite，必须按照Tag自动分好")]
    public List<string> ExtSpriteTagFolderAsset;
    [Header("额外的打包成Bundle的文件夹，这些文件夹因为不在AssetBundles目录下，无法加载，但是可以减少引用")]
    public List<string> ExtFolderAsset;
    [Header("打包成一个Bundle的文件夹")]
    public List<string> GroupABundlePathLists;
    [Header("图集打包成一个Bundle")]
    public List<string> AtlasABunldePathLists;
    [Header("资源Tag为InPackge的资源")]
    public List<string> GroupAssetInPackage;
}
