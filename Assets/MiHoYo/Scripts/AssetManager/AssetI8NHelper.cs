using UnityEngine;
#if UNITY_EDITOR
#endif


public enum eAssetLanguageVarType
{
    Default,//CN
    TW,//繁体
    KR,//韩文
    JP,//日文
    EN,//英文
    _Size_
}
public class AssetI8NHelper : MonoBehaviour
{
    /// <summary>
    /// 判断是否为其他语言的路径
    /// </summary>
    /// <returns></returns>
    public static eAssetLanguageVarType GetPathLanguage(string pathWithoutExtension)
    {
        for (int i = (int)eAssetLanguageVarType.Default + 1; i < (int)eAssetLanguageVarType._Size_; i++)
        {
            var t = (eAssetLanguageVarType)i;
            if (pathWithoutExtension.EndsWith(GetLangFileExtensionStr(t))
            || pathWithoutExtension.Contains(string.Intern(GetLangFileExtensionStr(t) + "/"))
            || pathWithoutExtension.Contains(string.Intern(GetLangFileExtensionStr(t) + "."))
            )
            {
                return t;
            }
        }
        return eAssetLanguageVarType.Default;
    }

    public static string GetDefaultPathLanguage(string pathWithoutExtension)
    {
        var lang = GetPathLanguage(pathWithoutExtension);
        if (lang == eAssetLanguageVarType.Default)
        {
            return pathWithoutExtension;
        }
        else
        {
            var ext = GetLangFileExtensionStr(lang);
            return pathWithoutExtension.Replace(ext, "");
        }
    }
    /// <summary>
    /// 获取对应语言文件后缀名
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public static string GetLangFileExtensionStr(eAssetLanguageVarType t)
    {
        return t == eAssetLanguageVarType.Default ? "" : "." + GetLangBundleVariantStr(t);
    }

    public static string GetLangBundleVariantStr(eAssetLanguageVarType t)
    {
        switch (t)
        {
            case eAssetLanguageVarType.TW:
                return "tw";
            case eAssetLanguageVarType.KR:
                return "kr";
            case eAssetLanguageVarType.JP:
                return "jp";
            case eAssetLanguageVarType.EN:
                return "en";
            default:
                return "default";
        }
    }

    public static string GetLangBundleStr(string bundleName, eAssetLanguageVarType t)
    {
        return bundleName + "." + GetLangBundleVariantStr(t);

    }

    public static string GetBundleNameWithOutLangStr(string bundleName)
    {
        if (bundleName.EndsWith(".tw"))
        {
            return bundleName.Replace(".tw", "");
        }
        if (bundleName.EndsWith(".kr"))
        {
            return bundleName.Replace(".kr", "");
        }
        if (bundleName.EndsWith(".jp"))
        {
            return bundleName.Replace(".jp", "");
        }
        if (bundleName.EndsWith(".en"))
        {
            return bundleName.Replace(".en", "");
        }
        if (bundleName.EndsWith(".default"))
        {
            return bundleName.Replace(".default", "");
        }

        return bundleName;
    }

}
