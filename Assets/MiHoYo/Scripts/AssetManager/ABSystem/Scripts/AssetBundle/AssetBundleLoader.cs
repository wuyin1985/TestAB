using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

//using UnityEngine.Profiling;
namespace Res.ABSystem
{
    public class AssetBundleLoaderManager : UnitySingleton<AssetBundleLoaderManager>
    {
        private LinkedList<AssetBundleLoader> AsyncLoaders = new LinkedList<AssetBundleLoader>();
        private HashSet<AssetBundleLoader> AsyncLoadersHash = new HashSet<AssetBundleLoader>();

        public static int MaxLoadThreadNum = 20;
        private static int CurrentLoadingNum = 0;
        /// <summary>
        /// ab修改前后对比测试开关
        /// </summary>
        private void Update()
        {
            //Profiler.BeginSample("Update");
            Profiler.BeginSample("CurrentLoading");
            //剩下可以加载的数量 
            foreach (var loader in AsyncLoaders)
            {
                if (MaxLoadThreadNum - CurrentLoadingNum <= 0)
                {
                    break;
                }
                Profiler.BeginSample("CheckWhichLoad");
                if (loader.State == eLoadState.State_None && loader.CheckAllDependenceComplete())
                {
                    loader.LoadBundleAsync();
                    CurrentLoadingNum++;
                }
                Profiler.EndSample();
            }
            Profiler.EndSample();

            //Profiler.EndSample();
        }

        public AsbLoaderAsyncResult AddLoaderAsyncTask(EventVoid onfinish = null, params AssetBundleLoader[] ls)
        {
            foreach (var l in ls)
            {
                if (!AsyncLoadersHash.Contains(l))
                {
                    AsyncLoadersHash.Add(l);
                    AsyncLoaders.AddLast(l);
                }
            }
            return new AsbLoaderAsyncResult().InitByLoaders(onfinish, ls);
        }

        public void RemoveAsyncLoader(AssetBundleLoader loader)
        {
            CurrentLoadingNum = Math.Max(0, CurrentLoadingNum -1) ;
            AsyncLoadersHash.Remove(loader);
            AsyncLoaders.Remove(loader);
        }
        
        public AssetBundle CheckAndBreakAsyncLoad(AssetBundleLoader loader)
        {
            //abCreateReq 如果存在，  代表已经向unity请求异步加载
            if (loader.abCreateReq != null)
            {
                CommonLog.Log(MAuthor.HSQ, $"{loader.BundleData.assetsInfo[0].assetFullName} 在异步加载时触发强制同步加载");
                //强制同步加载
                return loader.abCreateReq.assetBundle;
            }

            return null;
        }
    }

    public class AsbLoaderAsyncResult : CustomYieldInstruction
    {
        public AssetBundleLoader[] Loaders;

        private EventVoid OnFinishCallback;

        private bool _IsFinish = false;
            
        public AsbLoaderAsyncResult InitByLoaders(EventVoid OnFinish = null, params AssetBundleLoader[] ls)
        {
            Loaders = ls;
            for (int i = 0; i < ls.Length; i++)
            {
                var l = Loaders[i];
                l.parent = this;
            }
            OnFinishCallback = OnFinish;
            RefreshIsFinish();
            return this;
        }

        public AsbLoaderAsyncResult InitByLoaders(List<AssetBundleLoader> ls, EventVoid OnFinish = null)
        {
            if (ls != null)
            {
                Loaders = new AssetBundleLoader[ls.Count];
                int i = 0;
                foreach (var l in ls)
                {
                    Loaders[i] = l;
                    i++;
                    l.parent = this;
                }
            }
            OnFinishCallback = OnFinish;
            RefreshIsFinish();
            return this;
        }

        public void RefreshIsFinish()
        {
            if (_IsFinish) return;
            if (Loaders == null)
            {
                _IsFinish = true;
                OnFinishCallback?.Invoke();
            }
            bool isFin = true;
            foreach (var l in Loaders)
            {
                if (!l.IsComplete)
                {
                    isFin = false;
                    break;
                }
            }

            if (isFin)
            {
                OnFinishCallback?.Invoke();
                _IsFinish = true;
            }
        }

        public bool IsError()
        {
            if (Loaders == null) return false;
            bool isError = false;
            foreach (var l in Loaders)
            {
                if (l.State == eLoadState.State_Error)
                {
                    isError = true;
                    break;
                }
            }
            return isError;
        }


        internal void OnChildrenFinish()
        {
            RefreshIsFinish();
        }

        public override bool keepWaiting
        {
            get { return !_IsFinish; }
        }
    }
    /// <summary>
    /// Loader 父类
    /// </summary>
    public abstract class AssetBundleLoader
    {
        internal AsbLoaderAsyncResult parent = null;

        public AssetBundle Bundle;

        public AssetBundleData BundleData;

        public eLoadState State = eLoadState.State_None;
        internal AssetBundleCreateRequest abCreateReq;
        public HashSet<AssetBundleInfo> DepLoaders;

        protected abstract void LoadSelf();

        protected abstract AssetBundleCreateRequest LoadSelfAsync();
        /// <summary>
        /// 其它都准备好了，加载AssetBundle
        /// 注意：这个方法只能被 AssetBundleManager 调用
        /// 由 Manager 统一分配加载时机，防止加载过卡
        /// </summary>
        public virtual AssetBundle LoadBundle(System.Action<AssetBundle> callback = null)
        {
            if (State == eLoadState.State_None)
            {
                State = eLoadState.State_Loading;
                ////先加载依赖包
                //var depres = this.LoadDepends();
                var depres = eLoadState.State_Complete;//目前Loader的依赖加载挪到AbInfo中处理
                //加载自身
                LoadSelf();
                if (Bundle != null && depres == eLoadState.State_Complete)
                {
                    this.Complete();
                }
                else
                {
                    this.Error();
                }
            }
            else if (State == eLoadState.State_Error)
            {
                this.Error();
            }
            else if (State == eLoadState.State_Complete)
            {
                this.Complete();
            }
            if (callback != null)
            {
                callback.Invoke(Bundle);
            }
            return Bundle;
        }

        /// <summary>
        /// 其它都准备好了，加载AssetBundle
        /// 注意：这个方法只能被 AssetBundleManager 调用
        /// 由 Manager 统一分配加载时机，防止加载过卡
        /// </summary>
        public virtual void LoadBundleAsync(EventVoid callback = null)
        {
            if (State == eLoadState.State_None)
            {
                State = eLoadState.State_Loading;
                //var e1 = this.ILoadDependsAsync();
                ////先加载依赖包
                //while (e1 != null && e1.MoveNext())
                //{
                //    yield return e1.Current;
                //}//目前Loader的依赖加载挪到AbInfo中处理
                //加载自身
                abCreateReq = LoadSelfAsync();
                LoadBundleAsync_CallBack = callback;
                abCreateReq.completed += LoadBundleAsync_Finish;
            }
            else if (State == eLoadState.State_Error)
            {
                this.Error();
            }
            else if (State == eLoadState.State_Complete)
            {
                this.Complete();
            }
        }


        private EventVoid LoadBundleAsync_CallBack = null;
        private void LoadBundleAsync_Finish(AsyncOperation op)
        {
            if (op is AssetBundleCreateRequest abcr)
            {
                Bundle = abcr.assetBundle;
                if (Bundle != null && State != eLoadState.State_Error)
                {
                    this.Complete();
                }
                else
                {
                    this.Error();
                }

                if (LoadBundleAsync_CallBack != null)
                {
                    LoadBundleAsync_CallBack.Invoke();
                    LoadBundleAsync_CallBack = null;
                }

                abCreateReq = null;
            }
        }

        public virtual bool IsComplete
        {
            get
            {
                return State == eLoadState.State_Error || State == eLoadState.State_Complete;
            }
        }

        public virtual bool CheckAllDependenceComplete()
        {
            if (DepLoaders == null)
            {
                return true;
            }
            bool isComplete = true;
            foreach (var dl in DepLoaders)
            {
                if (!dl.loader.IsComplete)
                {
                    isComplete = false;
                    break;
                }
            }
            return isComplete;
        }

        protected virtual void Complete()
        {
            //None状态说明加载未完成，Loader被干掉了
            Profiler.BeginSample("OnAsyncLoadComplete");
            if (State == eLoadState.State_None && Bundle != null)
            {
                CommonLog.Error($"Bundle：{BundleData.bundleFileName} Assets:{BundleData.assetsInfo[0].assetFullName}加载完成，但Loader被干掉了" );
                Bundle.Unload(true);
                Bundle = null;
            }
            else
            {
                // CommonLog.Log(MAuthor.HSQ,$"Bundle：{BundleData.bundleFileName} Assets:{BundleData.assetsInfo[0].assetFullName}加载完成" );
                State = eLoadState.State_Complete;
                if (parent != null) parent.OnChildrenFinish();
            }
            AssetBundleLoaderManager.Instance.RemoveAsyncLoader(this);
            Profiler.EndSample();
        }
        //清理AB的加载器
        public virtual void Dispose(bool isUnloadAsset = false)
        {
            this.State = eLoadState.State_None;
            if (Bundle != null)
            {
                Bundle.Unload(true);
                Bundle = null;
            }
            AssetBundleLoaderManager.Instance.RemoveAsyncLoader(this);
        }
        protected virtual void Error()
        {
            State = eLoadState.State_Error;
            if (Bundle != null)
            {
                Bundle.Unload(true);
                Bundle = null;
            }
            AssetBundleLoaderManager.Instance.RemoveAsyncLoader(this);
            CommonLog.Error($"Bundle：{BundleData.bundleFileName} Assets:{BundleData.assetsInfo[0].assetFullName}加载失败" );
            if (parent != null) parent.OnChildrenFinish();
        }
    }


    /// <summary>
    /// 在手机运行时加载
    /// </summary>
    public class FileMapperAssetBundleLoader : AssetBundleLoader
    {
        /// <summary>
        /// 开始加载
        /// </summary>
        override protected void LoadSelf()
        {
            try
            {
                var mapper = AssetBundleManager.FileMapper;
                var bundleName = BundleData.bundleFileName_CurrentLang;
                var ab = mapper.LoadBundleFromFile(bundleName);
                Bundle = ab;
                if (Bundle == null)
                {
                    CommonLog.Error("Load Bundle Err:bundleName:{0},fileName:{1}.{2}",
                        BundleData.bundleName, BundleData.bundleFileName
                        , AssetBundleTable.AssetBundleBundleData.BundleExtension);
                }
            }
            catch (Exception e)
            {
                CommonLog.Error("Load Bundle Err:{0}bundleName:{1},path:{2}.{3}", e.Message
                    , BundleData.bundleName, BundleData.bundleFileName
                    , AssetBundleTable.AssetBundleBundleData.BundleExtension);
            }
        }

        /// <summary>
        /// 开始加载
        /// </summary>
        protected override AssetBundleCreateRequest LoadSelfAsync()
        {
            Profiler.BeginSample(nameof(LoadSelfAsync));
            var mapper = AssetBundleManager.FileMapper;
            var bundleName = BundleData.bundleFileName_CurrentLang;
            var ab = mapper.LoadBundleFromFileAsync(bundleName);
            // Bundle = ab.assetBundle;
            //   if (Bundle == null)
            //   {
            //       CommonLog.Error("Load Bundle Err:" + bundleName);
            //   }
            Profiler.EndSample();
            return ab;
        }
    }

}