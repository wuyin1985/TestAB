using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Res.ABSystem;
using UnityObject = UnityEngine.Object;
using UnityEngine.SceneManagement;
using System.Text;
using System.Xml.Serialization;
using TheWar.Module;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Profiling;

#endif

/// <summary>
/// 资源管理，对外屏蔽Bundle和Resources的区别
/// Author:Eric 
/// </summary>
public class GameAssetManager : SimpleSingletonProvider<GameAssetManager>
{
    /// <summary>
    /// 是否使用Bundle
    /// </summary>
    public static bool USED_AB_MODE { get; private set; }

    private System.Text.StringBuilder _textBuilder = new System.Text.StringBuilder();
    private Dictionary<string, Asset> _assetMap = new Dictionary<string, Asset>();
    private Asset _tmpAsset;

    private const string ABAssetStart = "AssetBundles/";
    //多语言的类型
    public static eAssetLanguageVarType AssetLanguageVar = eAssetLanguageVarType.TW;
    //过去N个场景无人引用后，才允许释放
    public static int ReleaseWaitSceneNum = 0;

    private bool IsInit = false;

    protected override void InstanceInit()
    {
        base.InstanceInit();
#if !AB_MODE && !UNITY_EDITOR
            CommonLog.Log($"目前处于非AB模式");
#endif
        if (!IsInit)
        {
/*#if UNITY_EDITOR
            USED_AB_MODE = UnityEngine.PlayerPrefs.GetInt("AB_MODE", 0) == 1;
#else*/
            USED_AB_MODE = true;
//#endif
            //GameObjectPool.InitPool();
            UnityFileLoaderHelper.InitForMainThread();
            UnityFileLoaderHelper.USED_AB_MODE = USED_AB_MODE;
            IsInit = true;
        }
    }

    public void LoadVersionCfg()
    {
        string versionPath = UnityFileLoaderHelper.StreamingAssetsPath + "/version.json";
    }

    /// <summary>
    /// 生成版本配置文件
    /// </summary>
    public static void GenerateVersionCfg()
    {
        StringBuilder sb = new StringBuilder();

        VersionData data = new VersionData
        {
            appVersion = "1.0.0",
            resVersion = "1.0.0",
            cfgVersion = "1.0.0",
            appUpdateUrl = "http://10.0.35.49:8080/job/wargame_p4_android_mac_branch/ws/deploy/android/",
            resUpdateUrl = "http://10.0.35.49:8080/job/wargame_p4_android_mac_branch/ws/deploy/android/"
        };

        var xmlStr = data.ToXML();
        string versionPath = Application.streamingAssetsPath + "/version.xml";
        FileUtils.WriteAllText(versionPath, xmlStr, true);
        CommonLog.Log("Generate Version Config Success!");
    }


    public static AssetID GetAssetID(AssetRef asset)
    {
        return new AssetID("bundleName", asset.AssetPath);
    }

    public static AssetID GetAssetID(string assetPath)
    {
        return new AssetID("bundleName", assetPath);
    }

#if UNITY_EDITOR
    private List<string> AllEditorAssetsPath = new List<string>();
    private Dictionary<string, string[]> EditorKeyPathWithRealPath = null;
    protected virtual string FindAssetPath(string name)
    {
        /* search assets */
        if (EditorKeyPathWithRealPath == null)
        {
            EditorKeyPathWithRealPath = new Dictionary<string, string[]>();

            var assetGuids = AssetDatabase.FindAssets("", new string[] { "Assets/Game", "Assets/AssetBundles", "Assets/Configs" });

            AllEditorAssetsPath.Clear();
            foreach (var data in assetGuids)
            {
                AllEditorAssetsPath.Add(AssetDatabase.GUIDToAssetPath(data));
            }

            foreach (var path in AllEditorAssetsPath)
            {
                if (path.Contains(".meta")) continue;

                if (path.Contains(".psd") || path.Contains(".tga")) continue;

                if (path.Contains(".mp3") || path.Contains(".wav")) continue;

                //必须是AB目录下的文件
                //if (p.Contains(ABAssetStart) && p.Contains(".") && !AssetDatabase.IsValidFolder(p))
                if (path.Contains(".") && !AssetDatabase.IsValidFolder(path))
                {
                    //排除其他语言格式  
                    bool otherLan = false;
                    foreach (var lan in System.Enum.GetNames(typeof(eAssetLanguageVarType)))
                    {
                        if (lan == "Default") { continue; }
                        if (path.Contains("." + lan)) { otherLan = true; }
                    }
                    if (otherLan) continue;

                    var bundlePath = GetAssetBundlePath(path);
                    if (string.IsNullOrEmpty(bundlePath))
                        continue;

                    EditorKeyPathWithRealPath[bundlePath] = new string[(int)eAssetLanguageVarType._Size_] { path, null, null, null, null };

                    var hashSet = new HashSet<eAssetLanguageVarType>
                    {
                        eAssetLanguageVarType.Default
                    };
                }
            }
            //加入存在的语言
            foreach (var path in AllEditorAssetsPath)
            {
                if (path.Contains(".meta")) continue;

                if (path.Contains(".psd") || path.Contains(".tga")) continue;

                if (path.Contains(".mp3") || path.Contains(".wav")) continue;

                //必须是AB目录下的文件
                //if (p.Contains(ABAssetStart) && p.Split('.').Length > 2 && !AssetDatabase.IsValidFolder(p))
                if (path.Split('.').Length > 2 && !AssetDatabase.IsValidFolder(path))
                {
                    //排除其他语言格式  
                    var lanType = AssetI8NHelper.GetPathLanguage(path.Replace(Path.GetExtension(path), ""));
                    if (lanType == eAssetLanguageVarType.Default) continue;

                    var bundlePath = GetAssetBundlePath(path);
                    if (string.IsNullOrEmpty(bundlePath))
                        continue;

                    var assetPath = bundlePath.Replace(AssetI8NHelper.GetLangFileExtensionStr(lanType), "");
                    if (EditorKeyPathWithRealPath.ContainsKey(assetPath))
                    {
                        EditorKeyPathWithRealPath[assetPath][(int)lanType] = path;
                    }
                    else
                    {

                        CommonLog.Error("未找到多语言资源，资源:{0} ,不存在对应的原始资源:{1}", path, assetPath);
                    }
                }
            }
        }

        string[] assetpath = null;
        var hasRes = EditorKeyPathWithRealPath.TryGetValue(name, out assetpath);
        if (hasRes)
        {
            //检查是否有其他语言
            if (AssetLanguageVar != eAssetLanguageVarType.Default
                && EditorKeyPathWithRealPath[name][(int)AssetLanguageVar] != null)
            {
                return assetpath[(int)AssetLanguageVar];
            }
            return assetpath[0];
        }
        else
        {
            return null;
        }
    }

    private string GetAssetBundlePath(string assetPath)
    {
        if (!assetPath.Contains(".") || AssetDatabase.IsValidFolder(assetPath))
            return null;

        //assetPath = assetPath.Replace("Assets/", string.Empty);
        var startIndex = assetPath.IndexOf("/") + 1;
        var endIndex = assetPath.IndexOf(Path.GetExtension(assetPath));
        if (endIndex - startIndex < 0)
        {
            CommonLog.Error(assetPath);
            return null;
        }
        return assetPath.Substring(startIndex, endIndex - startIndex);
    }

    private string GetAssetPathFromAssetBundleAndAssetName(string bundleName, string assetName)
    {
        assetName = Path.GetFileNameWithoutExtension(assetName);
        var assetPaths = AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(bundleName, assetName);
        if (assetPaths.Length == 0)
            return assetName;
        return assetPaths[0];
    }

#endif

    //public T LoadAsset<T>(AssetRef assetRef, bool warning = true)
    //{
    //    return LoadAsset<T>(assetRef.AssetPath, warning);
    //}

    public T LoadRawAsset<T>(string pathAndassetName, bool warning = true)
        where T : UnityObject
    {
        return LoadAsset<T>(pathAndassetName,warning)._RawAssetValue as T;
    }

    public Asset LoadAsset<T>(string pathAndassetName, bool warning = true)
        where T : UnityObject
    {
        pathAndassetName = CheckAssetPath(pathAndassetName);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            CommonLog.Error("非运行模式下，请勿调用AssetManager");
            return null;
        }

        if (pathAndassetName.Contains(@"\"))
        {
            CommonLog.Error("路径{0}格式错误，请使用/替换\\", pathAndassetName);
            return null;
        }

#endif

        string bundlePath = this.GetBundlePath(pathAndassetName, eAssetType.None);
        Asset asset = this.CheckAssetInDicCache(typeof(T), bundlePath);
        if (asset != null)
        {
            if (asset.IsDone)
            {
                return asset;
            }
            this.LoadAssetFromResources<T>(asset, warning);
            return asset;
        }
        else
        {
            bool isFromBundle = AssetBundleManager.Instance.CheckIsInBundle(pathAndassetName, out var bundlename);
            asset = new Asset(pathAndassetName, typeof(T), bundlePath, isFromBundle);
            this.LoadAssetFromResources<T>(asset, warning);
            this.AddAssetData(bundlePath, typeof(T), asset);
            return asset;
        }
    }

    public Asset LoadAsset(AssetRef assetRef, eAssetType assetType, bool warning = true)
    {
        return LoadAsset(assetRef.AssetPath, assetType, warning);
    }

    /// <summary>
    /// PS,即便不存在，也能返回Asset，只是内容为空
    /// </summary>
    /// <param name="pathAndassetName">路径大小写敏感</param>
    /// <param name="assetType"></param>
    /// <param name="warning">如果不存在，是否Log</param>
    /// <returns></returns>
    public Asset LoadAsset(string pathAndassetName, eAssetType assetType, bool warning = true)
    {
        pathAndassetName = CheckAssetPath(pathAndassetName);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            CommonLog.Error("非运行模式下，请勿调用AssetManager");
            return null;
        }

        if (pathAndassetName.Contains(@"\"))
        {
            CommonLog.Error("路径{0}格式错误，请使用/替换\\", pathAndassetName);
            return null;
        }

#endif

        string bundlePath = this.GetBundlePath(pathAndassetName, assetType);
        Asset asset = this.CheckAssetInDicCache(assetType, bundlePath);
        if (asset != null)
        {
            if (asset.IsDone)
            {
                //asset.OnAssetLoaded_CallbackOnce = null;
                //if (asset.IsFromBundle)
                //{
                //    //if (asset.checkAssetIn(pathAndassetName) == false)
                //    {
                //        var loader = AssetBundleManager.Instance.GetBundleInfoByResName(asset.AssetName);
                //        var val = loader.LoadByName(AssetBundleManager.Instance.GetAssetInBundleName(asset.AssetName),
                //            asset.getValueType(assetType));
                //        this.SetLoadAssetValue(asset, val, warning);
                //        //asset.addAssetIn(pathAndassetName, val);
                //    }
                //else
                //{
                //    var val1 = asset.allAssets[pathAndassetName];
                //    this.SetLoadAssetValue(asset, val1, warning);
                //}
                //}
                return asset;
            }
            this.LoadAssetFromResources(asset, warning, assetType);
            return asset;
        }
        else
        {
            string bundlename = null;
            bool isFromBundle = AssetBundleManager.Instance.CheckIsInBundle(pathAndassetName, out bundlename);
            asset = new Asset(pathAndassetName, assetType, bundlePath, isFromBundle);
            this.LoadAssetFromResources(asset, warning, assetType);
            this.AddAssetData(bundlePath, assetType, asset);
            return asset;

        }
    }

    public void LoadScene(string pathAndassetName, Action<string, Scene> completed, LoadSceneMode mode = LoadSceneMode.Single)
    {
        pathAndassetName = CheckAssetPath(pathAndassetName);

        if (USED_AB_MODE)
        {
            Asset asset = LoadAsset(pathAndassetName, eAssetType.SceneAsset);
            var scene2 = UnityEngine.SceneManagement.SceneManager.LoadScene(pathAndassetName, new LoadSceneParameters(mode));
            completed?.Invoke(null, scene2);
        }
        else
        {
#if UNITY_EDITOR
            var scenePath = FindAssetPath(pathAndassetName);
            var scene = EditorSceneManager.LoadScene(scenePath, new LoadSceneParameters(mode));
            completed?.Invoke(null, scene);
#endif
        }
    }

    public void LoadSceneAsync(string pathAndassetName, Action<string, Scene,Asset> completed, LoadSceneMode mode = LoadSceneMode.Single)
    {
        pathAndassetName = CheckAssetPath(pathAndassetName);

        if (USED_AB_MODE)
        {
            Asset asset = LoadAssetAsync(pathAndassetName, eAssetType.SceneAsset, (data) =>
            {
                AsyncOperation asyncOp = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(pathAndassetName, mode);
                asyncOp.completed += (operation) =>
                {
                    var sceneName = Path.GetFileName(pathAndassetName);
                    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
                    completed?.Invoke(null, scene, data);
                };
            });
            
        }
        else
        {
#if UNITY_EDITOR
            var scenePath = FindAssetPath(pathAndassetName);
            AsyncOperation asyncOp = EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(mode));
            asyncOp.completed += (data) =>
            {
                var sceneName = Path.GetFileName(pathAndassetName);
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
                completed?.Invoke(null, scene, null);
            };
#endif
        }
    }

    /*public Asset LoadAssetAsync<T>(string pathAndassetName, EventT<Asset> loadedCallBack = null, bool warning = true)
        where T : UnityObject
    {
        pathAndassetName = CheckAssetPath(pathAndassetName);
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            CommonLog.Error("非运行模式下，请勿调用AssetManager");
            return new Asset(pathAndassetName, eAssetType.None, null, false);
        }

        if (pathAndassetName.Contains(@"\"))
        {
            CommonLog.Error("路径{0}格式错误，请使用/替换\\", pathAndassetName);
            return null;
        }
#endif

        string bundlePath = this.GetBundlePath(pathAndassetName, eAssetType.None);
        Asset asset = this.CheckAssetInDicCache(typeof(T), bundlePath);
        if (asset != null)
        {
            if (loadedCallBack != null)
            {
                if (asset.IsDone)
                {
                    loadedCallBack(asset);
                }
                else
                {
                    //asset.OnAssetLoaded_CallbackOnce += loadedCallBack;
                    asset.OnAssetLoaded_CallBacks.Add(loadedCallBack);
                }
            }
            return asset;
        }
        bool isFromBundle = AssetBundleManager.Instance.CheckIsInBundle(pathAndassetName, out var bundlename);
        asset = new Asset(pathAndassetName, typeof(T), bundlePath, isFromBundle, loadedCallBack);
        
        this.AddAssetData(bundlePath, typeof(T), asset);
        var task = GameTaskManager.Instance.CreateTask(this.LoadAssetFromResourcesAsync<T>(asset, warning));
        
        return asset;
    }*/

    public Asset LoadAssetAsync(string pathAndassetName, Type assetType, EventT<Asset> loadedCallBack = null, bool warning = true)
    {
        pathAndassetName = CheckAssetPath(pathAndassetName);
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            CommonLog.Error("非运行模式下，请勿调用AssetManager");
            return new Asset(pathAndassetName, eAssetType.None, null, false);
        }

        if (pathAndassetName.Contains(@"\"))
        {
            CommonLog.Error("路径{0}格式错误，请使用/替换\\", pathAndassetName);
            return null;
        }
#endif

        string bundlePath = this.GetBundlePath(pathAndassetName, eAssetType.None);
        Asset asset = this.CheckAssetInDicCache(assetType, bundlePath);
        if (asset != null)
        {
            if (loadedCallBack != null)
            {
                if (asset.IsDone)
                {
                    loadedCallBack(asset);
                }
                else
                {
                    //asset.OnAssetLoaded_CallbackOnce += loadedCallBack;
                    asset.OnAssetLoaded_CallBacks.Add(loadedCallBack);
                }
            }
            return asset;
        }
        bool isFromBundle = AssetBundleManager.Instance.CheckIsInBundle(pathAndassetName, out var bundlename);
        asset = new Asset(pathAndassetName, assetType, bundlePath, isFromBundle, loadedCallBack);
        this.AddAssetData(bundlePath, assetType, asset);
        if (USED_AB_MODE)
        {
           // var task = GameTaskManager.Instance.CreateTask(this.LoadAssetFromResourcesAsync(asset, assetType, warning));
        }
        else
        {
            LoadAssetFromDatabase(asset, assetType, warning);
        }

        return asset;
    }

    public Asset LoadAssetAsync(AssetRef assetRef, eAssetType assetType, EventT<Asset> loadedCallBack = null, bool warning = true)
    {
        return LoadAssetAsync(assetRef.AssetPath, assetType, loadedCallBack, warning);
    }

    /// <summary>
    /// PS,即便不存在，也能返回Asset，只是内容为空
    /// </summary>
    /// <param name="pathAndassetName">路径大小写敏感</param>
    /// <param name="assetType"></param>
    /// <param name="warning">如果不存在，是否Log</param>
    /// <returns></returns>
    public Asset LoadAssetAsync(string pathAndassetName, eAssetType assetType, EventT<Asset> loadedCallBack = null, bool warning = true)
    {
        pathAndassetName = CheckAssetPath(pathAndassetName);
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            CommonLog.Error("非运行模式下，请勿调用AssetManager");
            return new Asset(pathAndassetName, assetType, null, false);
        }

        if (pathAndassetName.Contains(@"\"))
        {
            CommonLog.Error("路径{0}格式错误，请使用/替换\\", pathAndassetName);
            return null;
        }
#endif
        //CommonLog.Log($ "加载类型为：{0}的资源：{1}", assetType, assetName);
        string bundlePath = this.GetBundlePath(pathAndassetName, assetType);
        Asset asset = this.CheckAssetInDicCache(assetType, bundlePath);
        if (asset != null)
        {
            if (loadedCallBack != null)
            {
                if (asset.IsDone)
                {
                    loadedCallBack(asset);
                }
                else
                {
                    //asset.OnAssetLoaded_CallbackOnce += loadedCallBack;
                    asset.OnAssetLoaded_CallBacks.Add(loadedCallBack);
                }
            }
            return asset;
        }
        string bundlename;
        bool isFromBundle = AssetBundleManager.Instance.CheckIsInBundle(pathAndassetName, out bundlename);
        asset = new Asset(pathAndassetName, assetType, bundlePath, isFromBundle, loadedCallBack);
        this.AddAssetData(bundlePath, assetType, asset);
        if (USED_AB_MODE)
        {
            //var task = GameTaskManager.Instance.CreateTask(this.LoadAssetFromResourcesAsync(asset, warning, assetType));
        }
        else
        {
            LoadAssetFromDatabase(asset, warning, assetType);
        }
        
        return asset;
    }


    /// <summary>
    /// 检测资源路径
    /// </summary>
    /// <param name="assetPath"></param>
    /// <returns></returns>
    public static string CheckAssetPath(string assetPath, bool withoutExtension = false, bool startFromAsset = true)
    {
        if (null == assetPath)
            return null;

        if (withoutExtension && assetPath.Contains("."))
        {
            var extension = Path.GetExtension(assetPath);
            assetPath = assetPath.Replace(extension, string.Empty);
        }

        if (!startFromAsset && assetPath.StartsWith("Assets/"))
        {
            int startIndex = assetPath.IndexOf("/", StringComparison.Ordinal) + 1;
            assetPath = assetPath.Substring(startIndex);
        }
        return assetPath;
    }

    public Asset GetAsset(string assetName, eAssetType assetType)
    {
        string bundlePath = this.GetBundlePath(assetName, assetType);
        Asset asset = this.CheckAssetInDicCache(assetType, bundlePath);
        if (asset != null)
        {
            return asset;
        }
        return null;
    }

    //根据对象销毁资源
    public void DisposeAssetByObj(object _obj)
    {
        string key = "";
        foreach (var _data in this._assetMap)
        {
            if (_data.Value.IsRefObj(_obj) && _data.Value.IsFromBundle == false)
            {
                _data.Value.Dispose(true);
                key = _data.Value.BundlePathKey;
                break;
            }
        }
        if (key != "")
        {
            this._assetMap.Remove(key);
        }
    }

    private void LoadAssetFromResources<T>(Asset asset, bool warning)
        where T : UnityObject
    {
        if (USED_AB_MODE)
        {
            if (asset.IsFromBundle)
            {
                var abInfo = AssetBundleManager.Instance.GetBundleInfoByResName(asset.AssetName);
                if (abInfo == null)
                {
                    if (warning)
                    {
                        CommonLog.Error("Bundle资源加载失败，缺少资源：{0}，类型：{1}，BundleName:{2}"
                            , asset.AssetName, asset.AssetType.ToString(), abInfo.bundle);
                    }
                    asset.IsDone = true;

                    return;
                }
                else
                {
                    asset.SetBundle(abInfo);
                    var val = abInfo.LoadByName(AssetBundleManager.Instance.GetAssetInBundleName(asset.AssetName), asset.ValueType);
                    this.SetLoadAssetValue(asset, val, warning);
                }
            }
            else
            {
                this.SetLoadAssetValue(asset, Resources.Load(asset.AssetName, asset.ValueType), warning);
            }
        }
        else
        {
#if UNITY_EDITOR
            //编辑器模式下，直接用AssetDataBase或者Resource
            if (Application.isPlaying)
            {
                //if (asset.AssetName.StartsWith(ABAssetStart))
                {
                    var path = FindAssetPath(asset.AssetName);
                    if (path != null)
                    {
                        var obj = AssetDatabase.LoadAssetAtPath(path, asset.ValueType);
                        this.SetLoadAssetValue(asset, obj, warning);
                    }
                    else
                    {
                        this.SetLoadAssetValue(asset, null, warning);
                    }
                    return;
                }
            }
#endif
        }
    }


    private void LoadAssetFromResources(Asset asset, bool warning, eAssetType assetType)
    {
        if (USED_AB_MODE)
        {
            if (asset.IsFromBundle)
            {
                var abInfo = AssetBundleManager.Instance.GetBundleInfoByResName(asset.AssetName);
                if (abInfo == null)
                {
                    if (warning)
                    {
                        CommonLog.Error("Bundle资源加载失败，缺少资源：{0}，类型：{1}，BundleName:{2}"
                            , asset.AssetName, asset.AssetType.ToString(), abInfo.bundle);
                    }
                    asset.IsDone = true;
                    return;
                }
                else
                {
                    asset.SetBundle(abInfo);
                    UnityEngine.Object val = null;
                    if (assetType == eAssetType.SceneAsset)
                    {
                        abInfo.LoadBundle();
                    }
                    else
                    {
                        val = abInfo.LoadByName(AssetBundleManager.Instance.GetAssetInBundleName(asset.AssetName), asset.ValueType);
                    }
                    this.SetLoadAssetValue(asset, val, warning);
                }
            }
            else
            {
                this.SetLoadAssetValue(asset, Resources.Load(asset.AssetName, asset.ValueType), warning);
            }
        }
        else
        {
#if UNITY_EDITOR
            //编辑器模式下，直接用AssetDataBase或者Resource
            if (Application.isPlaying)
            {
                //if (asset.AssetName.StartsWith(ABAssetStart))
                {
                    var path = FindAssetPath(asset.AssetName);
                    if (path != null)
                    {
                        UnityEngine.Object obj = null;
                        if (assetType == eAssetType.SceneAsset)
                        {

                        }
                        else
                        {
                            obj = UnityEditor.AssetDatabase.LoadAssetAtPath(path, asset.ValueType);
                        }
                        this.SetLoadAssetValue(asset, obj, warning);
                    }
                    else
                    {
                        this.SetLoadAssetValue(asset, null, warning);
                    }
                    return;
                }
            }
#endif
        }
    }

    private IEnumerator LoadAssetFromResourcesAsync<T>(Asset asset, bool warning)
        where T : UnityObject
    {
        if (USED_AB_MODE)
        {
            if (asset.IsFromBundle)
            {
                var abinfo = AssetBundleManager.Instance.GetBundleInfoByResName(asset.AssetName);
                if (abinfo == null)
                {
                    if (warning)
                    {
                        CommonLog.Error("Bundle资源加载失败，缺少资源：{0}，类型：{1}，BundleName:{2}"
                            , asset.AssetName, asset.AssetType.ToString(), abinfo.bundle);
                    }
                    asset.IsDone = true;

                }
                else
                {
                    asset.SetBundle(abinfo);
                    //异步加载Bundle
                    if (!abinfo.isReady)
                    {
                        var e1 = abinfo.LoadBundleAsync();
                        yield return e1;
                        if (e1.IsError())
                        {
                            CommonLog.Error("加载资源{0}的bundle加载异常", asset.AssetName);
                        }
                    }

                    var req = abinfo.GetLoadAsyncByName(AssetBundleManager.Instance.GetAssetInBundleName(asset.AssetName), asset.ValueType);
                    yield return req;
                    this.SetLoadAssetValue(asset, req.asset, warning);
                }
            }
            else
            {
                var res = Resources.LoadAsync(asset.AssetName, asset.ValueType);
                yield return res;
                this.SetLoadAssetValue(asset, res.asset);
            }
        }
        else
        {
#if UNITY_EDITOR
            //编辑器模式下，直接用AssetDataBase或者Resource
            //if (Application.isPlaying && asset.AssetName.StartsWith("AssetBundles"))
            if (Application.isPlaying)
            {
                var path = FindAssetPath(asset.AssetName);
                var obj = AssetDatabase.LoadAssetAtPath(path, typeof(T));
                this.SetLoadAssetValue(asset, obj, warning);
                yield return asset;
            }
            yield return null;
#endif
        }
    }

    private IEnumerator LoadAssetFromResourcesAsync(Asset asset, Type assetType, bool warning)
    {
        if (USED_AB_MODE)
        {
            if (asset.IsFromBundle)
            {
                var abinfo = AssetBundleManager.Instance.GetBundleInfoByResName(asset.AssetName);
                if (abinfo == null)
                {
                    if (warning)
                    {
                        CommonLog.Error("Bundle资源加载失败，缺少资源：{0}，类型：{1}，BundleName:{2}"
                            , asset.AssetName, asset.AssetType.ToString(), abinfo.bundle);
                    }
                    asset.IsDone = true;
                }
                else
                {
                    asset.SetBundle(abinfo);
                    //异步加载Bundle
                    if (!abinfo.isReady)
                    {
                        var e1 = abinfo.LoadBundleAsync();
                        yield return e1;
                        if (e1.IsError())
                        {
                            CommonLog.Error("加载资源{0}的bundle加载异常", asset.AssetName);
                        }
                    }

                    var req = abinfo.GetLoadAsyncByName(AssetBundleManager.Instance.GetAssetInBundleName(asset.AssetName), asset.ValueType);
                    yield return req;
                    this.SetLoadAssetValue(asset, req.asset, warning);
                }
            }
            else
            {
                var res = Resources.LoadAsync(asset.AssetName, asset.ValueType);
                yield return res;
                this.SetLoadAssetValue(asset, res.asset);
            }
        }
//         else
//         {
// #if UNITY_EDITOR
//             //编辑器模式下，直接用AssetDataBase或者Resource
//             //if (Application.isPlaying && asset.AssetName.StartsWith("AssetBundles"))
//             if (Application.isPlaying)
//             {
//                 // Profiler.BeginSample("loadAssetsSync");
//                 var path = FindAssetPath(asset.AssetName);
//                 var obj = AssetDatabase.LoadAssetAtPath(path, assetType);
//                 this.SetLoadAssetValue(asset, obj, warning);
//                 // Profiler.EndSample();
//                 yield return asset;
//             }
//             else
//             {
//                 yield return null;
//             }
// #endif
//         }
    }

    //编辑器模式下，直接用AssetDataBase或者Resource
    private void LoadAssetFromDatabase(Asset asset, Type assetType, bool warning)
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            Profiler.BeginSample("loadAssetsSync");
            var path = FindAssetPath(asset.AssetName);
            var obj = AssetDatabase.LoadAssetAtPath(path, assetType);
            this.SetLoadAssetValue(asset, obj, warning);
            Profiler.EndSample();
        }
#endif
    }
    private IEnumerator LoadAssetFromResourcesAsync(Asset asset, bool warning, eAssetType assetType)
    {
        if (USED_AB_MODE)
        {
            if (asset.IsFromBundle)
            {
                var abinfo = AssetBundleManager.Instance.GetBundleInfoByResName(asset.AssetName);
                if (abinfo == null)
                {
                    if (warning)
                    {
                        CommonLog.Error("Bundle资源加载失败，缺少资源：{0}，类型：{1}，BundleName:{2}"
                            , asset.AssetName, asset.AssetType.ToString(), abinfo.bundle);
                    }
                    asset.IsDone = true;
                }
                else
                {
                    asset.SetBundle(abinfo);
                    //异步加载Bundle
                    if (!abinfo.isReady)
                    {
                        var e1 = abinfo.LoadBundleAsync();
                        yield return e1;
                        if (e1.IsError())
                        {
                            CommonLog.Error("加载资源{0}的bundle加载异常", asset.AssetName);
                        }
                    }

                    UnityEngine.Object reqValue = null;
                    if (assetType != eAssetType.SceneAsset)
                    {
                        var req = abinfo.GetLoadAsyncByName(AssetBundleManager.Instance.GetAssetInBundleName(asset.AssetName), asset.ValueType);
                        yield return req;
                        reqValue = req.asset;
                    }
                    else
                    {
                        // abinfo.LoadBundle();
                        //var req = abinfo.LoadBundleAsync();
                        //yield return req;
                    }
                    this.SetLoadAssetValue(asset, reqValue, warning);
                }
            }
            else
            {
                var res = Resources.LoadAsync(asset.AssetName, asset.ValueType);
                yield return res;
                this.SetLoadAssetValue(asset, res.asset);
            }
        }
//         else
//         {
// #if UNITY_EDITOR
//             //编辑器模式下，直接用AssetDataBase或者Resource
//             //if (Application.isPlaying && asset.AssetName.StartsWith("AssetBundles"))
//             if (Application.isPlaying)
//             {
//                 var path = FindAssetPath(asset.AssetName);
//                 UnityEngine.Object obj = null;
//                 if (assetType == eAssetType.SceneAsset)
//                 {
//
//                 }
//                 else
//                 {
//                     obj = UnityEditor.AssetDatabase.LoadAssetAtPath(path, asset.ValueType);
//                     if (GameUtility.HasMissingScript(obj as GameObject))
//                         CommonLog.Log($"{path} prefab missing Script!");
//                 }
//                 this.SetLoadAssetValue(asset, obj, warning);
//                 yield return asset;
//             }
//             yield return null;
// #endif
//         }
    }
    //编辑器模式下，直接用AssetDataBase或者Resource
    private void LoadAssetFromDatabase(Asset asset, bool warning, eAssetType assetType)
    {
#if UNITY_EDITOR
        //编辑器模式下，直接用AssetDataBase或者Resource
        //if (Application.isPlaying && asset.AssetName.StartsWith("AssetBundles"))
        Profiler.BeginSample(nameof(LoadAssetFromDatabase));
        if (Application.isPlaying)
        {
            var path = FindAssetPath(asset.AssetName);
            UnityEngine.Object obj = null;
            if (assetType == eAssetType.SceneAsset)
            {

            }
            else
            {
                obj = UnityEditor.AssetDatabase.LoadAssetAtPath(path, asset.ValueType);
                /*if (GameUtility.HasMissingScript(obj as GameObject))
                    CommonLog.Log($"{path} prefab missing Script!");*/
            }
            this.SetLoadAssetValue(asset, obj, warning);
        }
        Profiler.EndSample();
#endif
    }

    private void SetLoadAssetValue(Asset asset, UnityEngine.Object value, bool warning = true)
    {
        asset.SetAssetValue(value);
        if (asset._RawAssetValue == null && asset.AssetType != eAssetType.SceneAsset)
        {
            if (warning)
            {
                CommonLog.Error("资源加载失败，缺少资源：{0}，类型：{1}", asset.AssetName,
                    asset.AssetType.ToString());
            }
        }
    }

    private void AddAssetData(string bundlePath, Type type, Asset asset)
    {
        var key = $"{bundlePath}_{type.Name}";
        this._assetMap.Add(key, asset);
    }

    private void AddAssetData(string bundlePath, eAssetType assetType, Asset asset)
    {
        var type = Asset.GetValueType(assetType);
        var key = $"{bundlePath}_{type.Name}";
        this._assetMap.Add(key, asset);
        
    }

    private Asset CheckAssetInDicCache(eAssetType assetType, string bundlePath)
    {
        //增加Type.Name为了解决Texture和Sprite使用相同的缓存无法互相转换
        var type = Asset.GetValueType(assetType);
        if (this._assetMap.TryGetValue($"{bundlePath}_{type.Name}", out _tmpAsset))
        {
            return _tmpAsset;
        }
        return null;
    }

    private Asset CheckAssetInDicCache(Type type, string bundlePath)
    {
        //增加Type.Name为了解决Texture和Sprite使用相同的缓存无法互相转换
        if (this._assetMap.TryGetValue($"{bundlePath}_{type.Name}", out _tmpAsset))
        {
            return _tmpAsset;
        }
        return null;
    }

    public string GetBundlePath(string assetName, eAssetType assetType)
    {
        if (AssetBundleManager.Instance.CheckIsInBundle(assetName, out var bundleName))
        {
            return bundleName;
        }
        else
        {
            return assetName;
        }
    }

    public int GetAssetCount()
    {
        return _assetMap.Count;
    }

    public void ReleaseAssetInstance(Asset asset, UnityEngine.Object instance)
    {
        if (this._assetMap.TryGetValue(asset.BundlePathKey, out _tmpAsset))
        {
            _tmpAsset.ReleaseInstance(instance);
        }
    }

    public void ReleaseInstance(string path, GameObject obj)
    {
        if (this._assetMap.TryGetValue(path, out _tmpAsset))
        {
            _tmpAsset.ReleaseInstance(obj);
        }
        else
        {
            GameObject.Destroy(obj);
        }
    }

    /// <summary>
    /// PS这个性能最差，可以酌情使用上面2个
    /// </summary>
    /// <param name="obj"></param>
    public void ReleaseInstance(GameObject obj)
    {
        Asset ass = null;
        foreach (var _data in this._assetMap)
        {
            if (_data.Value.IsRefObj(obj))
            {
                ass = _data.Value;
                break;
            }
        }

        if (ass != null)
        {
            ass.ReleaseInstance(obj);
        }
        else
        {
            GameObject.Destroy(obj);
        }
    }

    //销毁Instantiate对应出来的对象
    public void ReleaseAsset(string Path, eAssetType assetType, UnityEngine.Object obj)
    {
        string bundlePath = this.GetBundlePath(Path, assetType);
        Asset asset = this.CheckAssetInDicCache(assetType, bundlePath);
        if (asset != null)
        {
            asset.ReleaseInstance(obj);
        }
    }

    public void DisposeAsset(string assetName, eAssetType assetType, bool isClear = false)
    {
        string bundlePath = this.GetBundlePath(assetName, assetType);
        Asset asset = this.CheckAssetInDicCache(assetType, bundlePath);
        if (asset != null && asset.IsFromBundle == false)
        {
            asset.Dispose(isClear);
            this._assetMap.Remove(bundlePath);
        }
    }

    public void DisposeAsset(Asset asset, bool isClear = false)
    {
        if (this._assetMap.TryGetValue(asset.BundlePathKey, out _tmpAsset))
        {
            _tmpAsset.Dispose(isClear);
            this._assetMap.Remove(asset.BundlePathKey);
        }
    }

    public void SafeDisposeAllAsset()
    {
        List<string> needRemovelist2 = new List<string>();
        foreach (KeyValuePair<string, Asset> current in this._assetMap)
        {
            var asset = current.Value;
            if (asset != null)
            {
                var objectListLen = asset.GetObjectListLen();
                var objectRefCount = asset.GetRefObjectCount();
                var canRemove = CheckIsDisposeForSceneCounter(asset);
                if (canRemove && (objectRefCount == 0))
                {
                    needRemovelist2.Add(current.Key);
                    // CommonLog.Log(MAuthor.HSQ, $"释放了{asset.AssetName}");
                    asset.SafeDispose();
                }
                else
                {
                    CommonLog.Log(MAuthor.HSQ,$"残留了{asset.AssetName}, 引用数量:{objectRefCount}");
                    //不删除的话就清理清理资源
                    if (objectRefCount != objectListLen)
                    {
                        asset.CleanObjectList();
                    }
                }
            }
        }
        CommonLog.Log(MAuthor.HSQ,$"共释放了{needRemovelist2.Count}份Assets");
        int count = needRemovelist2.Count;
        for (int i = 0; i < count; i++)
        {
            this._assetMap.Remove(needRemovelist2[i]);
        }
    }

    /// <summary>
    /// 检查所有资源是否加载完毕
    /// </summary>
    /// <returns></returns>
    public bool CheckAllAssetsDone()
    {
        var rtn = true;

        foreach (var asset in _assetMap)
        {
            if (!asset.Value.IsDone)
            {
                CommonLog.LogWarning(MAuthor.HSQ, $"wait asset {asset.Value.AssetName} load finish");
                rtn = false;
                break;
            }
        }
        return rtn;
    }

    /// <summary>
    /// 重新标记所有的Asset当前SceneCounter
    /// </summary>
    public void RemarkAllSceneCounter()
    {
        foreach (KeyValuePair<string, Asset> current in this._assetMap)
        {
            var asset = current.Value;
            if (asset != null)
            {
                //同时重新标记当前的场景计数器
                asset.RefreshSceneCounter(0);
            }
        }
    }

    //检测是否需要删除资源,根据当前counter和asset的counter来判断，一般要比SceneMgr的小3才卸载
    private bool CheckIsDisposeForSceneCounter(Asset asset)
    {
        //直接先测试最激进，不预留，直接放
        if (asset != null)
        {
            // if (SceneMgr.CurrentSceneCounter - asset.UsedSceneCounter >= ReleaseWaitSceneNum)
            {
                return true;
            }
            //return false;
        }
        return false;
    }
}


/// <summary>
/// 资源类型
/// </summary>
public enum eAssetType
{
    None,

    GameObject,
    Texture,
    AudioClip,
    TextAsset,
    SpriteAtlas,
    Sprite,
    AnimClip,
    Material,
    SceneAsset,
    Font,
    SkeletonDataAsset,
}

/// <summary>
/// 释放内存类型
/// </summary>
public enum eFreeMemoryType
{
    /// <summary>
    /// 立即释放
    /// </summary>
    Immediately,
    /// <summary>
    /// 延迟释放
    /// </summary>
    Delay,
    /// <summary>
    /// 从不
    /// </summary>
    Never,
}