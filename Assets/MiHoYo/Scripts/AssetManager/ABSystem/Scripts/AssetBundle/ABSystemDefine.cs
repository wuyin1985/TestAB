namespace Res.ABSystem
{
    public class AssetBundleData
    {
        public string bundleName;
        public string bundleName_CurrentLang { get { return GetBundleNameWithLangExtension(GameAssetManager.AssetLanguageVar); } }
        public string bundleFileName;
        public string bundleFileName_CurrentLang { get { return GetBundleFileNameWithLangExtension(GameAssetManager.AssetLanguageVar); } }
        public long bundleHash0;
        public long bundleHash1;
        public AssetBundleAssetData[] assetsInfo;
        public string[] dependency;

        public static string BundleExtension { get { return AssetBundleTable.AssetBundleBundleData.BundleExtension; } }

        //所在地
        public MD5Creater.MD5Struct bundleHashName;
        /// <summary>
        /// 所拥有的语言
        /// </summary>
        public bool[] bundleLanguages = new bool[(int)eAssetLanguageVarType._Size_];
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
                return AssetI8NHelper.GetLangBundleStr(bundleName, lang);
            }
            else
            {
                return bundleName;
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

    public class AssetBundleAssetData
    {
        public string assetFullName;
        public string resPath;
    }

    public enum eBundlePathType
    {
        NoExist, TempAsset, StreamAsset, PersistenAsset,
    }

    public enum eLoadState
    {
        State_None = 0,
        State_Loading = 1,
        State_Error = 2,
        State_Complete = 3
    }

}
