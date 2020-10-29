using System;
using System.Collections.Generic;

namespace Res.ABSystem
{
    [Serializable]
    public class AssetBundleTable
    {
        public AssetBundleBundleData[] BundleInfos;

        public void Merge(AssetBundleTable patch)
        {
            var originLength = BundleInfos.Length;
            var count = originLength + patch.BundleInfos.Length;
            Array.Resize(ref BundleInfos, count);
            Array.Copy(patch.BundleInfos, 0, BundleInfos,
                originLength, patch.BundleInfos.Length);
        }

        /// <summary>
        /// 从AssetList信息生成BundleList
        /// </summary>
        public void GenBundleInfoFromRaw(List<AssetBundleXMLRawData> AssetBundleRawInfoList)
        {
            if (AssetBundleRawInfoList != null)
            {
                var bundleDic = new Dictionary<string, AssetBundleBundleData>();
                //先处理默认语言eAssetLanguageVarType.Default
                foreach (var rawInfo in AssetBundleRawInfoList)
                {
                    //只以default作为Language设置来源
                    if (rawInfo.Language != eAssetLanguageVarType.Default)
                    {
                        continue;
                    }

                    AssetBundleBundleData bundleInfo;
                    if (!bundleDic.TryGetValue(rawInfo.bundleName, out bundleInfo))
                    {
                        bundleInfo = new AssetBundleBundleData();
                        bundleInfo.bundleName = rawInfo.bundleName;
                        bundleInfo.bundleNameMD5Struct = rawInfo.bundleMD5Struct;
                        bundleInfo.dependenceBundleName = rawInfo.dependenceBundleName;
                        bundleInfo.isComplexName = rawInfo.isComplexName;
                        bundleDic[bundleInfo.bundleName] = bundleInfo;
                    }

                    bundleInfo.bundleLanguages = rawInfo.OtherLanguages;

                    var bundleInfoAsset = new AssetBundleBundleAssetData()
                    {
                        resPath = rawInfo.resPath,
                        assetFullName = rawInfo.assetFullName
                    };

                    //扩容Asset记录
                    var oldAssets = bundleInfo.assets;
                    bundleInfo.assets = new AssetBundleBundleAssetData[oldAssets.Length + 1];
                    System.Array.Copy(oldAssets, bundleInfo.assets, oldAssets.Length);
                    bundleInfo.assets[oldAssets.Length] = bundleInfoAsset;
                }


                BundleInfos = new AssetBundleBundleData[bundleDic.Count];
                int i = 0;
                foreach (var v in bundleDic.Values)
                {
                    BundleInfos[i] = v;
                    i++;
                }
            }
        }

        /// <summary>
        /// 结构化的ASB数据
        /// </summary>
        [Serializable]
        public class AssetBundleBundleData
        {
            /**
             * 避免被压缩，使用如下后缀
             *".jpg", ".jpeg", ".png", ".gif", 
             *".wav", ".mp2", ".mp3", ".ogg", ".aac", 
             *".mpg", ".mpeg", ".mid", ".midi", ".smf", ".jet", 
             *".rtttl", ".imy", ".xmf", ".mp4", ".m4a", 
             *".m4v", ".3gp", ".3gpp", ".3g2", ".3gpp2", 
             *".amr", ".awb", ".wma", ".wmv" 
            **/
            //目前不用扩展名，会造与增量打包混乱
            public static string BundleExtension = ".ab";

            /// <summary>
            /// 上次打好的AB时候的Resource路径，CommonLog用，实际使用MD5后结果
            /// </summary>
            public string bundleName;

            /// <summary>
            /// bundle的MD5
            /// </summary>
            public string bundleMD5;

            /// <summary>
            /// Bundle值的MD5，完整版本的
            /// </summary>
            public MD5Creater.MD5Struct bundleNameMD5Struct;

            /// <summary>
            /// 默认false,如果出现MD5后冲突情况，为true,使用MD532位作为名字
            /// </summary>
            public bool isComplexName = false;

            /// <summary>
            /// 依赖的bundle名称
            /// </summary>
            public string[] dependenceBundleName;

            /// <summary>
            /// Bundle的文件名
            /// </summary>
            public string bundleFileName;

            /// <summary>
            /// 所有资源
            /// </summary>
            public AssetBundleBundleAssetData[] assets = new AssetBundleBundleAssetData[0];

            /// <summary>
            /// 所拥有的语言
            /// </summary>
            public bool[] bundleLanguages = new bool[(int) eAssetLanguageVarType._Size_];

            /// <summary>
            /// 是否有其他语言
            /// </summary>
            public bool hasOtherLanguage
            {
                get
                {
                    //非Default就行
                    for (int i = 1; i < bundleLanguages.Length; i++)
                    {
                        if (bundleLanguages[i]) return true;
                    }

                    return false;
                }
            }

            /// <summary>
            /// 获取BundleName带语言后缀名
            /// </summary>
            /// <param name="lang"></param>
            /// <returns></returns>
            public string GetBundleNameWithLangExtension(eAssetLanguageVarType lang)
            {
                if (hasOtherLanguage)
                {
                    return AssetI8NHelper.GetLangBundleStr(bundleNameMD5Struct.GetMD5Str(!isComplexName), lang);
                }
                else
                {
                    return bundleNameMD5Struct.GetMD5Str(!isComplexName);
                }
            }

            /// <summary>
            /// 获取BundleName带语言后缀名
            /// </summary>
            /// <param name="lang"></param>
            /// <returns></returns>
            public string GetBundleFileNameWithLangExtension(eAssetLanguageVarType lang)
            {
                if (hasOtherLanguage)
                {
                    return AssetI8NHelper.GetLangBundleStr(bundleFileName, lang) + BundleExtension;
                }
                else
                {
                    return bundleFileName + BundleExtension;
                }
            }
        }

        public class AssetBundleBundleAssetData
        {
            /// <summary>
            /// 从Asset开始的文件名称，带后缀名的，主要用于映射后，重设Bundle里面的资源名称
            /// </summary>
            public string assetFullName;

            /// <summary>
            /// 对应从AssetBundles开始的路径，MD5后就是BundleName和里面的AssetName
            /// </summary>
            public string resPath;
        }
    }


    [Serializable]
    public class AssetBundleXMLRawData
    {
        /// <summary>
        /// resShortName，resPath的文件名
        /// </summary>
        public string resShortName;

        /// <summary>
        /// 从Asset开始的文件名称，带后缀名的，主要用于映射后，重设Bundle里面的资源名称
        /// </summary>
        public string assetFullName;

        /// <summary>
        /// 对应从AssetBundles开始的路径，MD5后就是BundleName和里面的AssetName
        /// </summary>
        public string resPath;

        /// <summary>
        /// resPath值的MD5，完整版本的
        /// </summary>
        public MD5Creater.MD5Struct resPathMD5Struct;

        /// <summary>
        /// 上次打好的AB时候的Resource路径，使用时，使用下面的MD5
        /// </summary>
        public string bundleName;

        /// <summary>
        /// Bundle值的MD5，完整版本的
        /// </summary>
        public MD5Creater.MD5Struct bundleMD5Struct;

        /// <summary>
        /// 默认false,如果出现MD5后冲突情况，为true,使用MD532位作为名字
        /// </summary>
        public bool isComplexName = false;

        /// <summary>
        /// 依赖的bundle名称
        /// </summary>
        public string[] dependenceBundleName;

        /// <summary>
        /// Bundle的Hash128值
        /// </summary>
        public string fileName;

        /// <summary>
        /// Bundle多语言
        /// </summary>
        public eAssetLanguageVarType Language = eAssetLanguageVarType.Default;

        /// <summary>
        /// 其他语言资源
        /// </summary>
        public bool[] OtherLanguages = new bool[(int) eAssetLanguageVarType._Size_];

        public bool HasOtherLanguage
        {
            get
            {
                //非Default就行
                for (int i = 1; i < OtherLanguages.Length; i++)
                {
                    if (OtherLanguages[i]) return true;
                }

                return false;
            }
        }
    }

    [Serializable]
    public class AssetBundleUpdateInfo
    {
        public int Version_Build = 0;
        public string Version_Name = "0";

        public List<AssetBundleUpdateFileInfo> AssetBundleInfoList;

        [Serializable]
        public class AssetBundleUpdateFileInfo
        {
            /// <summary>
            /// 文件的MD5结果，一个Long够用了，不行别逼我上CRC
            /// </summary>
            public long FileMD5;

            /// <summary>
            /// Bundle文件名,文件可能是32位也可能是64位的，取决于IsComplexName
            /// </summary>
            public MD5Creater.MD5Struct FileName;

            //文件大小
            public int SizeKB;

            /// <summary>
            /// 默认false,如果出现MD5后冲突情况，为true,使用MD532位作为名字
            /// </summary>
            public bool IsComplexName = false;

            /// <summary>
            /// 在对应大文件的偏移量
            /// </summary>
            public int Offset;

            public string FileNameStr
            {
                get { return FileName.GetMD5Str(IsComplexName); }
            }

            public string FileMD5Str
            {
                get { return MD5Creater.MD5LongToHexStr(FileMD5); }
            }
        }
    }
}