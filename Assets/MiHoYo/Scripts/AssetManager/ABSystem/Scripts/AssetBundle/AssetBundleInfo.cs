using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Res.ABSystem
{
    public class AssetBundleInfo
    {
        public delegate void OnUnloadedHandler(AssetBundleInfo abi);
        public OnUnloadedHandler onUnloaded;


        internal AssetBundle bundle;

        public AssetBundleData data;

        internal AssetBundleLoader loader;

        /// <summary>
        /// 标记当前是否准备完毕
        /// </summary>
        private bool _isReady;

        public bool isReady
        {
            get { return _isReady; }

            private set { _isReady = value; }
        }

        /// <summary>
        /// 强制的引用计数
        /// </summary> 

        public int bundleRefCount { get; private set; }
        public int assetRefCount { get; private set; }


        private HashSet<AssetBundleInfo> deps = new HashSet<AssetBundleInfo>();
        private List<WeakReference> references = new List<WeakReference>();

        public AssetBundleInfo()
        {
        }

        public void AddDependency(AssetBundleInfo target)
        {
            try
            {
                if (!deps.Contains(target))
                {
                    if (deps.Add(target))
                    {
                        target.BundleDepRetain();
                    }
                }
                else
                {
                    CommonLog.Error(target);
                }
            }
            catch (Exception e)
            {
                CommonLog.Error(e);
            }
        }

        public bool IsContains(AssetBundleInfo target)
        {
            if (deps.Contains(target))
                return true;
            return false;
        }

        public HashSet<AssetBundleInfo> GetAllDependency()
        {
            return deps;
        }

        /// <summary>
        /// 引用计数增一
        /// </summary>
        public void BundleDepRetain()
        {
            bundleRefCount++;
        }
        /// <summary>
        /// 引用计数增一
        /// </summary>
        public void AssetRefRetain()
        {
            assetRefCount++;
        }
        /// <summary>
        /// 引用计数减一
        /// </summary>
        public void BundleRefRelease()
        {
            bundleRefCount--;
        }

        /// <summary>
        /// 释放引用
        /// </summary>
        /// <param name="owner"></param>
        public void AssetRefRelease()
        {
            assetRefCount--;
        }

        /// <summary>
        /// 这个资源是否不用了
        /// </summary>
        /// <returns></returns>
        public bool IsUnused
        {
            get { return _isReady && bundleRefCount <= 0 && assetRefCount <= 0; }
        }

        //统一清理bundle时，需要先清理bundle的依赖计数，否则会有残留的依赖资源，只有在scene切换时才能使用
        public void ClearDependentsIfUnused()
        {
            if (IsUnused && deps.Count > 0)
            {
                foreach (var d in deps)
                {
                    d.BundleRefRelease();
                    d.ClearDependentsIfUnused();
                }
                deps.Clear();
            } 
        }
        /// <summary>
        /// 如果还有引用则不卸载
        /// </summary>
        /// <returns></returns>
        public bool SafeDispose()
        {
            if (!_isReady)
            {
                if (loader != null )//&& loader.Bundle != null
                {
                    DiposeLoader();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            /*if (references.Count > 0)
            {
                return false;
            }*/
            if (IsUnused)
            {
                foreach (var d in deps)
                {
                    d.BundleRefRelease();
                    //d.SafeDispose();//依赖切场景再一并释放吧
                }
                UnloadBundle();
                deps.Clear();
                references.Clear();
                if (onUnloaded != null) { onUnloaded(this); }
                return true;
            }
            else
            {
            
                CommonLog.Log(MAuthor.HSQ,$"残留 bundle:{data.bundleName} asset:{data.assetsInfo[0].assetFullName} _isReady:{_isReady} bundleRefCount:{bundleRefCount}  assetRefCount:{assetRefCount} ");
            }
            return false;
        }

        public virtual void Dispose()
        {
            if (!_isReady)
            {
                DiposeLoader();
                return;
            }

            foreach (var d in deps)
            {
                d.BundleRefRelease();
            }

            UnloadBundle();

            deps.Clear();
            references.Clear();
            if (onUnloaded != null) { onUnloaded(this); }

        }

        private void DiposeLoader()
        {
            if (loader != null)
            {
                loader.Dispose(true);
            }
            loader = null;
        }

        //通过名字和类型进行加载相应的资源
        public Object LoadByName(string name, Type type)
        {
            if (!isReady)
            {
                LoadBundle();
            }
            if (bundle != null)
            {
                Object _data = bundle.LoadAsset(name, type);
                return _data;
            }
            return null;
        }

        public void LoadBundle(int deep = 0)
        {
            if (isReady || loader == null) return;
            if (deep >= 50)
            {
                throw new Exception("recycle ref happend in LoadBundle");
            }
            
            foreach (var depinfo in deps)
            {
                if (!depinfo.isReady)
                {
                    depinfo.LoadBundle(deep + 1);
                }
            }
            bundle = AssetBundleLoaderManager.Instance.CheckAndBreakAsyncLoad(loader);
            if (bundle == null)
            {
                bundle = loader.LoadBundle();
            }
            isReady = true;

        }

        //通过名称异步加载
        public AssetBundleRequest GetLoadAsyncByName(string name, Type type)
        {
            if (!isReady || bundle == null)
            {
                CommonLog.LogWarning(MAuthor.HSQ,$"load bundle sync should not be happened when load asset async!");
                LoadBundle();
            }
            if (bundle == null)
            {
                return null;
            }
            return bundle.LoadAssetAsync(name, type);
        }

        /// <summary>
        /// 异步从内存加载Bundle
        /// </summary>
        /// <returns></returns>
        public AsbLoaderAsyncResult LoadBundleAsync(int deep = 0)
        {
            if (deep >= 50)
            {
                throw new Exception("recycle ref happend in LoadBundle");
            }
            
            if (!isReady && loader != null)
            {
                foreach (var depinfo in deps)
                {
                    if (!depinfo.isReady && depinfo.loader.State == eLoadState.State_None)
                    {
                        depinfo.LoadBundleAsync(deep + 1);
                    }
                }
                var res = AssetBundleLoaderManager.Instance.AddLoaderAsyncTask(LoadBundleAsync_TaskFinish, loader);
                return res;
            }
            return null;
        }

        private void LoadBundleAsync_TaskFinish()
        {
            bundle = loader.Bundle;
            isReady = true;
        }


        void UnloadBundle()
        {
            if (bundle != null)
            {
                CommonLog.Log(MAuthor.HSQ,$"释放Bundle: {data.bundleFileName} Asset: {data.assetsInfo[0].assetFullName}");
                bundle.Unload(true);
                DiposeLoader();
            }
            bundle = null;
            _isReady = false;
        }
    }
}