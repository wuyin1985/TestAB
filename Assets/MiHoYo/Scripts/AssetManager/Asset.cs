using Res.ABSystem;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;



[Serializable]
public class Asset : CustomYieldInstruction
{

    #region Yield处理 
    public override bool keepWaiting
    {
        get { return !IsDone; }
    }
    #endregion

    private UnityEngine.Object _assetValue;

    private Type _valueType;


    public List<WeakReference> _refList;

    public EventT<Asset> OnAssetLoaded_CallbackOnce;

    public List<EventT<Asset>> OnAssetLoaded_CallBacks = new List<EventT<Asset>>();


    //资源名称
    public string AssetName;

    //默认情况下，BundlePathKey=AssetName,如果资源在Bundle目录里，那么这个值取决于导出的配置文件
    public string BundlePathKey;

    public bool IsFromBundle;

    public AssetBundleInfo Bundle;

    // 资源类型
    public eAssetType AssetType;

    // 异步加载的时候判断是否加载完成
    public bool IsDone;

    public void SetAssetValue(UnityEngine.Object val)
    {
        this._assetValue = val;
        CommonLog.Log(MAuthor.HSQ, $"资源{this.AssetName}加载完成");
        IsDone = true;
        if (OnAssetLoaded_CallbackOnce != null)
        {
            this.OnAssetLoaded_CallbackOnce.Invoke(this);
            OnAssetLoaded_CallbackOnce = null;
        }

        if (OnAssetLoaded_CallBacks != null)
        {
            for (int i = 0; i < OnAssetLoaded_CallBacks.Count; i++)
            {
                OnAssetLoaded_CallBacks[i].Invoke(this);
            }
            OnAssetLoaded_CallBacks.Clear();
        }
    }

    /// <summary>
    /// 获得引用对象，但是Prefab不允许直接调用，如果需要请读取_RawAssetValue
    /// </summary>
    internal UnityEngine.Object _RawAssetValue
    {
        get
        {
            return this._assetValue;
        }
    }

    public Type ValueType
    {
        get
        {
            if (this._valueType == null)
            {
                this._valueType = GetValueType(AssetType);
            }
            return this._valueType;
        }
        set
        {
            this._valueType = value;
        }
    }

    //场景计数器
    public int UsedSceneCounter;

    public Asset(string assetName, eAssetType assetType, string bundlePath, bool isFromBundle)
    {
        this._assetValue = null;
        this.IsDone = false;
        this.AssetName = assetName;
        this.AssetType = assetType;
        this.ValueType = GetValueType(assetType);
        this.BundlePathKey = bundlePath;
        this.IsFromBundle = isFromBundle;
    }

    public Asset(string assetName, Type type, string bundlePath, bool isFromBundle)
    {
        this._assetValue = null;
        this.IsDone = false;
        this.AssetName = assetName;
        this.AssetType = eAssetType.None;
        this.ValueType = type;
        this.BundlePathKey = bundlePath;
        this.IsFromBundle = isFromBundle;
    }

    public Asset(string assetName, eAssetType assetType, string bundlePath, bool isFromBundle, EventT<Asset> callBack)
    {
        this._assetValue = null;
        this.IsDone = false;
        this.AssetName = assetName;
        this.AssetType = assetType;
        this.ValueType = GetValueType(assetType);
        this.BundlePathKey = bundlePath;
        this.OnAssetLoaded_CallbackOnce = callBack;
        this.IsFromBundle = isFromBundle;
    }

    public Asset(string assetName, Type type, string bundlePath, bool isFromBundle, EventT<Asset> callBack)
    {
        this._assetValue = null;
        this.IsDone = false;
        this.AssetName = assetName;
        this.AssetType = eAssetType.None;
        this.ValueType = type;
        this.BundlePathKey = bundlePath;
        this.OnAssetLoaded_CallbackOnce = callBack;
        this.IsFromBundle = isFromBundle;
    }


    public static Type GetValueType(eAssetType _type)
    {
        Type _typeData;
        switch (_type)
        {
            case eAssetType.TextAsset:
                _typeData = typeof(TextAsset);
                return _typeData;
            case eAssetType.Texture:
                _typeData = typeof(Texture);
                return _typeData;
            case eAssetType.GameObject:
                _typeData = typeof(GameObject);
                return _typeData;
            case eAssetType.Sprite:
                _typeData = typeof(Sprite);
                return _typeData;
            case eAssetType.SpriteAtlas:
                _typeData = typeof(SpriteAtlas);
                return _typeData;
            case eAssetType.AudioClip:
                _typeData = typeof(AudioClip);
                return _typeData;
            case eAssetType.AnimClip:
                _typeData = typeof(AnimationClip);
                return _typeData;
            case eAssetType.Material:
                _typeData = typeof(Material);
                return _typeData;
            case eAssetType.Font:
                _typeData = typeof(Font);
                return _typeData;
                //case eAssetType.SkeletonDataAsset:
                //    _typeData = typeof(SkeletonDataAsset);
                //    return _typeData;
        }
        return typeof(UnityEngine.Object);
    }

    /// <summary>
    /// 获取对象实例值，如果不Retain，切换场景的时候资源会被释放
    /// </summary>
    /// <returns></returns>
    public T GetAndRetainValue<T>(UnityEngine.Object keepRetainObj) where T : UnityEngine.Object
    {
        Retain(keepRetainObj);
        if (AssetType == eAssetType.GameObject)
        {
            CommonLog.Error("资源类型{0},不允许直接读取,请使用Readinstance 或者调用实例化", AssetType);
            return (T)this._assetValue;
        }
        else
        {
            return (T)this._assetValue;
        }
    }
    /// <summary>
    /// 创建对象的实例
    /// </summary>
    /// <returns></returns>
    public GameObject Instantiate()
    {
        return Instantiate(Vector3.zero, Quaternion.identity);
    }

    public GameObject Instantiate(Vector3 pos, Quaternion rot, Transform parent)
    {
        if (this._assetValue == null)
        {
            CommonLog.Error("AssetName={0},对象不存在！！！！", AssetName);
            return null;
        }

        GameObject obj = _assetValue as GameObject;
        obj.transform.SetPositionAndRotation(pos, rot);
        var @object = UnityEngine.Object.Instantiate(_assetValue, parent, false);
        Retain(@object);        
        return (GameObject)@object;
    }

    public void SetBundle(AssetBundleInfo info)
    {
        Bundle = info;
        Bundle.AssetRefRetain();
    }

    /// <summary>
    /// 创建对象的实例
    /// </summary>
    /// <returns></returns>
    public GameObject Instantiate(Vector3 pos, Quaternion rot, bool isOnlyInThisScene = false)
    {
        if (this._assetValue == null)
        {
            CommonLog.Error("AssetName={0},对象不存在！！！！", AssetName);
            return null;
        }
        var @object = UnityEngine.Object.Instantiate(this._assetValue, pos, rot);
        //if (!isOnlyInThisScene)
        //{
        Retain(@object);
        //}
        return (GameObject)@object;
    }

    public T Instantiate<T>(Transform parent = null) where T : class
    {
        if (this._assetValue == null)
        {
            CommonLog.Error("AssetName={0},对象不存在！！！！", AssetName);
            return default;
        }

        if (this._assetValue is T raw)
        {
            var @object = UnityEngine.Object.Instantiate(this._assetValue, parent);
            Retain(@object);
            return @object as T;
        }
        else
        {
            CommonLog.Error($"AssetName={AssetName},期待类型{typeof(T)}不匹配资源类型{AssetType}！！！！");
            return default;
        }
    }

    public void Retain(UnityEngine.Object refobj)
    {
        if (this._refList == null)
        {
            this._refList = new List<WeakReference>();
        }
        _refList.Add(new WeakReference(refobj, false));
    }
    //是否是Instantiate对象
    public bool IsRefObj(object refobj)
    {
        if (_refList != null && refobj != null)
        {
            foreach (var _data in _refList)
            {
                if (_data != null && _data.Target != null && _data.Target == refobj && (_data.Target as UnityEngine.Object) != null)
                {
                    return true;
                }
            }
        }
        if (_assetValue != null && this._assetValue == (UnityEngine.Object)refobj)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 单纯的释放引用，以便于释放资源
    /// </summary>
    /// <param name="refobj"></param>
    public void ReleaseRef(object refobj)
    {
        if (this._refList == null)
        {
            return;
        }
        for (int i = 0; i < _refList.Count; i++)
        {
            var wrf = _refList[i];
            if (wrf != null && wrf.Target == refobj)
            {
                _refList.RemoveAt(i);
                break;
            }
        }
    }

    /// <summary>
    /// 获得实际引用对象数量
    /// </summary>
    /// <returns></returns>
    public int GetRefObjectCount()
    {
        if (this._refList == null)
        {
            return 0;
        }
        int count = 0;
        for (int i = this._refList.Count - 1; i >= 0; i--)
        {
            if (this._refList[i] != null && this._refList[i].Target != null && (this._refList[i].Target as UnityEngine.Object) != null)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 得到 引用对象空间长度
    /// </summary>
    /// <returns></returns>
    public int GetObjectListLen()
    {
        if (this._refList == null)
        {
            return 0;
        }
        else
        {
            return this._refList.Count;
        }
    }

    /// <summary>
    /// 清理List，移除空对象
    /// </summary>
    public void CleanObjectList()
    {
        if (this._refList == null)
        {
            return;
        }
        for (int i = this._refList.Count - 1; i >= 0; i--)
        {
            if (this._refList[i] == null || this._refList[i].Target == null || (this._refList[i].Target as UnityEngine.Object) == null)
            {
                _refList.RemoveAt(i);
            }
        }
    }
    /// <summary>
    /// 重新标记场景，如果没有引用就无需刷新标记
    /// </summary>
    /// <param name="current"></param>
    public void RefreshSceneCounter(int current)
    {
        if (GetRefObjectCount() > 0)
        {
            UsedSceneCounter = current;
        }
    }

    /// <summary>
    /// 释放对象的引用
    /// </summary>
    /// <param name="instance"></param>
    public void ReleaseInstance(UnityEngine.Object instance)
    {
        ReleaseRef(instance);
        UnityEngine.Object.Destroy(instance);
    }

    public void SafeDispose()
    {
        Dispose(false);
    }

    /// <summary>
    /// 释放资源和磁盘空间
    /// </summary> 
    public void Dispose(bool isforce = false, bool disposeBundleImmediate = false)
    {
        if (!IsDone) return;
        //资源类型由Unity来管理清理
        if (this._assetValue != null)
        {
            try
            {
                if (IsFromBundle)
                {
                    Bundle.AssetRefRelease();
                    if (isforce)
                    {
                        if (_refList != null)
                        {
                            for (int i = 0; i < this._refList.Count; i++)
                            {
                                var _obj = this._refList[i].Target as UnityEngine.Object;
                                UnityEngine.Object.DestroyImmediate(_obj, true);
                                if (this._refList.Count > i)
                                {
                                    this._refList[i].Target = null;
                                }
                            }
                            _refList.Clear();
                        }
                        if (disposeBundleImmediate) AssetBundleManager.Instance.SafeDispose(Bundle);
                        Bundle = null;
                    }
                    else
                    {
                        if (disposeBundleImmediate) AssetBundleManager.Instance.SafeDispose(Bundle);
                        Bundle = null;
                    }
                }
                else
                {
                    if (isforce)
                    {
                        if (AssetType != eAssetType.GameObject)
                        {
                            Resources.UnloadAsset(this._assetValue);
                        }
                        else
                        {
                            if (_refList != null)
                            {
                                for (int i = 0; i < this._refList.Count; i++)
                                {
                                    if (this._refList[i] != null && this._refList[i].Target != null)
                                    {
                                        var _obj = this._refList[i].Target as UnityEngine.Object;
                                        if (_obj != null)
                                        {
                                            UnityEngine.Object.DestroyImmediate(_obj, true);
                                        }
                                        else
                                        {
                                            UnityEngine.Object.Destroy(_obj);
                                        }
                                        if (this._refList.Count > i)
                                        {
                                            this._refList[i].Target = null;
                                        }
                                    }
                                    if (this._refList.Count > i)
                                    {
                                        this._refList[i] = null;
                                    }
                                }
                                this._refList.Clear();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                CommonLog.Error(e);
                throw;
            }
        }
        this._assetValue = null;
        this.IsDone = false;
        if (_refList != null) this._refList.Clear();
    }
}
