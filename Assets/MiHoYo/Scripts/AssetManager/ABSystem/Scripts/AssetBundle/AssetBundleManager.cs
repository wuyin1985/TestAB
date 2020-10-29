#if !AB_MODE && UNITY_EDITOR
#else
#define _AB_MODE_
#endif
using System;
using System.Collections.Generic;
using System.IO;

namespace Res.ABSystem
{
    public class AssetBundleManager : SimpleSingletonProvider<AssetBundleManager>
    {
        /// <summary>
        /// 已创建的所有Loader列表(包括加载完成和未完成的),AbBundleName与BundleInfo
        /// </summary>
        private Dictionary<string, AssetBundleInfo> _abCache = new Dictionary<string, AssetBundleInfo>();

        /// <summary>
        /// 路径名称和对应的导出数据表信息,AbBundleName与BundleData fromXML和本地检查信息
        /// </summary>
        private Dictionary<string, AssetBundleData> _abNameToData = new Dictionary<string, AssetBundleData>();

        /// <summary>
        /// 名称和对应的ABBundle数据信息
        /// </summary>
        private Dictionary<string, string> _resNameToBundleName = new Dictionary<string, string>();

        //全名缓存
        private Dictionary<string, string> _resPathTofullAssetPathData = new Dictionary<string, string>();

        public static FileMapSystem.FileMapSystem FileMapper =
            new FileMapSystem.FileMapSystem(AssetBundlePathResolver.BundleSaveDirName);

        private Dictionary<string, string[]> bundleDependencyDcit = new Dictionary<string, string[]>();

        private AssetBundleTable _table;

        public FileMapSystem.Version GetVersion()
        {
            return FileMapper.Version;
        }

        public AssetBundleTable GetTable()
        {
            return _table;
        }

        public FileMapSystem.FileMapSystem GetFileMapSystem()
        {
            return FileMapper;
        }

        //FileMapper系统
        public static bool FileMapperMode = true;

        void OnDestroy()
        {
            this.RemoveAllBundle();
        }

        //通过bundle的名称获取加载器
        public AssetBundleInfo GetInfoByBundleName(string bundleName)
        {
            if (!_abCache.TryGetValue(bundleName, out var abinfo))
            {
                //创建他的loader
                var loader = this.CreateLoader(bundleName);

                //读取他的data
                if (!_abNameToData.TryGetValue(bundleName, out var abd))
                {
                    CommonLog.Error("不存在bundleName为：{0}的配置信息", bundleName);
                    return null;
                }

                abinfo = new AssetBundleInfo();
                abinfo.data = abd;


                if (!bundleDependencyDcit.ContainsKey(bundleName))
                {
                    bundleDependencyDcit.Add(bundleName, abd.dependency);
                }

                //foreach (var data in bundleDependencyDcit)
                //{
                //    if (data.Value.Contains(bundleName))
                //    {
                //        if (bundleDependencyDcit[bundleName].Contains(data.Key))
                //        {
                //            CommonLog.Error("AssetBundle 出现相互依赖了");
                //        }
                //    }
                //}

                //缓存bundle
                _abCache[bundleName] = abinfo;

                foreach (var depname in abd.dependency)
                {
                    if (!abinfo.IsContains(GetInfoByBundleName(depname)))
                        abinfo.AddDependency(GetInfoByBundleName(depname));
                }

                loader.BundleData = abd;
                loader.DepLoaders = abinfo.GetAllDependency();
                abinfo.loader = loader;
            }

            return abinfo;
        }


        /// 判断是否某个资源在Bundle里
        public bool CheckIsInBundle(string resName, out string bundleName)
        {
            return _resNameToBundleName.TryGetValue(resName, out bundleName);
        }

        /// 根据ResName获取BundleInfo
        public AssetBundleInfo GetBundleInfoByResName(string resName)
        {
            string bundleName;
            if (_resNameToBundleName.TryGetValue(resName, out bundleName))
            {
                return GetInfoByBundleName(bundleName);
            }

            return null;
        }

        //获取加载全名
        public string GetAssetInBundleName(string name)
        {
            //if (_resPathTofullAssetPathData.ContainsKey(name))
            //{
            //    return _resPathTofullAssetPathData[name];
            //}
            return name;
        }

        //获取加载全名
        public string GetAssetRawFullName(string name)
        {
            if (_resPathTofullAssetPathData.ContainsKey(name))
            {
                return _resPathTofullAssetPathData[name];
            }

            return "";
        }

        /// <summary>
        /// 初始化
        /// </summary>
        protected override void InstanceInit()
        {
            base.InstanceInit();
            LoadAssetBundleConfig();
        }

        #region 解析配置

        /// <summary>
        /// 初始化加载配置
        /// </summary>
        public void LoadAssetBundleConfig()
        {
            try
            {
                if (GameAssetManager.USED_AB_MODE)
                {
                    if (FileMapperMode)
                    {
                        ////初始化FileMaper
                        FileMapper.InitFromLocalFile();
                        LoadFileMapperConfig();
                        CommonLog.Log("初始化AB系统配置结束:FileMode");
                    }
                    else
                    {
                        //默认先加载下载的bundle信息文件
                        LoadStreamingConfig();
                        CommonLog.Warning("初始化AB系统配置结束:ABMode");
                    }
                }
            }
            catch (Exception e)
            {
                CommonLog.Error(e);
            }

            //Log.W("当前配置文件版本为{0}", version);
        }

        //重新加载初始化ASB配置
        public void ReloadAssetBundleConfig(bool isUnloadAllBundle = true)
        {
            try
            {
                if (GameAssetManager.USED_AB_MODE)
                {
                    //释放所有之前资源
                    ClearAllABAndLoader(isUnloadAllBundle);
                    if (FileMapperMode)
                    {
                        LoadFileMapperConfig();
                    }
                    else
                    {
                        LoadStreamingConfig();
                    }
                }
            }
            catch (Exception e)
            {
                CommonLog.Error(e);
            }
        }

        //加载streamingAsset下的配表信息
        private bool LoadFileMapperConfig()
        {
            string cfgStr;
            //优先加载PersistentPath
            var patchXmlPath = AssetBundlePathResolver.GetBundleSourceFile(AssetBundlePathResolver.DependFileName);
            if (File.Exists(patchXmlPath))
            {
                cfgStr = File.ReadAllText(patchXmlPath);
            }
            else
            {
                //加载FileMap
                var strBytes = FileMapper.GetFileBytes(AssetBundlePathResolver.DependFileName);
                if (strBytes == null || strBytes.Length == 0)
                {
                    CommonLog.Error("初始化AB系统配置表失败");
                    return false;
                }
                cfgStr = System.Text.Encoding.UTF8.GetString(strBytes).TrimEnd('\0');
            }
        
            return ParseBundleConfig(cfgStr);
        }

        //加载streamingAsset下的配表信息
        private bool LoadStreamingConfig()
        {
            var path = AssetBundlePathResolver.GetStreamingBundlePath(AssetBundlePathResolver.DependFileName);
            if (!UnityStreamingFileHelper.CheckStreamExistsFile(path)) return false;
            var _xml = UnityStreamingFileHelper.GetStreamStringAllPlatform(path);
            return ParseBundleConfig(_xml);
        }

        //解析配表
        private bool ParseBundleConfig(string cfgStr)
        {
            try
            {
                var table = cfgStr.FromXML<AssetBundleTable>();
                if (table != null)
                {
                    _abNameToData.Clear();
                    _resPathTofullAssetPathData.Clear();
                    _resNameToBundleName.Clear();

                    //将XML配置转换到系统内置信息
                    foreach (var x in table.BundleInfos)
                    {
                        var xbundleMD5Name = x.bundleNameMD5Struct.GetMD5Str(!x.isComplexName);
                        if (!_abNameToData.TryGetValue(xbundleMD5Name, out var abd))
                        {
                            abd = new AssetBundleData
                            {
                                bundleName = xbundleMD5Name,
                                bundleFileName = x.bundleFileName,
                                bundleHashName = x.bundleNameMD5Struct,
                                dependency = x.dependenceBundleName ?? new string[0],
                                assetsInfo = new AssetBundleAssetData[x.assets.Length],
                                bundleLanguages = x.bundleLanguages
                            };


                            for (var i = 0; i < x.assets.Length; i++)
                            {
                                var ass = new AssetBundleAssetData
                                {
                                    resPath = x.assets[i].resPath,
                                    assetFullName = x.assets[i].assetFullName
                                };
                                abd.assetsInfo[i] = ass;

                                _resPathTofullAssetPathData[ass.resPath] = ass.assetFullName;
                                _resNameToBundleName[ass.resPath] = xbundleMD5Name;
                            }

                            _abNameToData[abd.bundleName] = abd;
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                CommonLog.Error(e);
                return false;
            }
        }

        #endregion

        /// <summary>
        /// 获取一个bundle的引用
        /// </summary>
        /// <param name="bundleName"></param>
        /// <returns></returns>
        public string[] GetDependencies(string bundleName)
        {
            if (_abNameToData != null && _abNameToData.ContainsKey(bundleName))
            {
                return _abNameToData[bundleName].dependency;
            }

            return new string[0];
        }

        //根据bundle名称创建加载器
        protected AssetBundleLoader CreateLoader(string bundleName)
        {
            AssetBundleLoader _loader;
            _loader = new FileMapperAssetBundleLoader();
            return _loader;
        }

        //删除所有的数据
        public void RemoveAllBundle()
        {
            foreach (AssetBundleInfo abi in _abCache.Values)
            {
                abi.Dispose();
            }

            _abCache.Clear();
        }


        public void SafeDispose(AssetBundleInfo bundle)
        {
            if (bundle != null)
            {
                var dised = bundle.SafeDispose();
                if (dised)
                {
                    RemoveBundleCache(bundle.data.bundleName);
                }
            }
        }

        public void Dispose(AssetBundleInfo bundle)
        {
            if (bundle != null)
            {
                bundle.Dispose();
                RemoveBundleCache(bundle.data.bundleName);
            }
        }

        /// <summary>
        /// 释放未使用的Bundle
        /// </summary>
        public void SafeDisposeAll()
        {
            _StrListHelper.Clear();
            CommonLog.Log(MAuthor.HSQ, $"共有bundle{_abCache.Count}个");
            //先统一清理依赖, 避免出现
            foreach (var abikv in _abCache)
            {
                abikv.Value.ClearDependentsIfUnused();
            }

            foreach (var abikv in _abCache)
            {
                //Log.E("销毁:{0},Asset:{1}",abikv.Key,abikv.Value.data.assetsInfo[0].assetFullName);
                var success = abikv.Value.SafeDispose();
                //被移除的加入
                if (success)
                {
                    _StrListHelper.Add(abikv.Key);
                }
            }

            foreach (var str in _StrListHelper)
            {
                RemoveBundleCache(str);
            }

            CommonLog.Log(MAuthor.HSQ, $"本次一共销毁了{_StrListHelper.Count}个,目前剩余{_abCache.Count}个bundle");
            // foreach (var abikv in _abCache)
            // {
            //     CommonLog.Log(MAuthor.HSQ, $"剩余:{abikv.Key},Asset:{abikv.Value.data.assetsInfo[0].assetFullName}");
            // }
        }

        public void ClearAllABAndLoader(bool isUnloadUsedBundle = true)
        {
            List<AssetBundleInfo> _list = new List<AssetBundleInfo>();
            foreach (var data in _abCache)
            {
                var ab = data.Value;
                if (ab != null && ab.IsUnused)
                {
                    //没办法，直接Dispose会导致修改集合报错
                    _list.Add(ab);
                }
            }

            foreach (var ab in _list)
            {
                ab.Dispose();
                _abCache.Remove(ab.data.bundleName);
            }
        }


        //删除相应的bundle Cache数据
        internal void RemoveBundleCache(string _name)
        {
            if (_abCache != null && _abCache.ContainsKey(_name))
            {
                _abCache[_name] = null;
                _abCache.Remove(_name);
            }
        }

        private List<string> _StrListHelper = new List<string>();

        /// <summary>
        /// 根据XMLtable表检查出需要下载的bundle
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static List<string> GetBundleNeedDownLoadList(AssetBundleTable table, eAssetLanguageVarType lanType)
        {
            var list = new List<string>();
            if (table != null)
            {
                foreach (var x in table.BundleInfos)
                {
                    //检查Persist
                    var has = AssetBundlePathResolver.CheckBundleExistInPersisten(
                        x.GetBundleFileNameWithLangExtension(lanType));
                    //然后检查Stream
                    if (!has)
                        has = AssetBundlePathResolver.CheckBundleExistInStreaming(
                            x.GetBundleFileNameWithLangExtension(lanType));
                    if (!has)
                    {
                        list.Add(x.GetBundleFileNameWithLangExtension(lanType));
                    }
                }
            }

            return list;
        }
    }
}