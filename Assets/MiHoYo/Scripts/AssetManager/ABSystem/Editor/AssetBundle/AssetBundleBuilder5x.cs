
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Res.ABSystem
{
    public abstract class ABBuilder
    {
        public AssetBundleBuild[] BuildMap { get; private set; }

        public AssetBundleManifest Manifest { get; protected set; }

        public AssetBundleTable XMLTable;

        public List<AssetBundleXMLRawData> AssetBundleRawInfoList;

        public string BundleSavePath = "";


            /// <summary>
            /// 是否打开进度条UI
            /// </summary>
        public static bool IsOpenProgressUI = true;

        public ABBuilder()
        {

        }

        void InitDirs()
        {
            new DirectoryInfo(BundleSavePath).Create();
        }

        /// <summary>
        /// 通过XML生成BuildMap
        /// </summary>
        private void CollectABBuildMap()
        {
            //收集所有AssetBundle文件夹
            var abInfo = new AssetBundleBuild[AssetBundleRawInfoList.Count];
            Dictionary<string, List<string>> assetBundleDict = new Dictionary<string, List<string>>();

            for (int i = 0; i < abInfo.Length; i++)
            {
                var xmlInfo = AssetBundleRawInfoList[i];

                if(IsOpenProgressUI) EditorUtility.DisplayProgressBar("Loading", xmlInfo.assetFullName, (float)i/abInfo.Length);

                ////先刷新Import 
                ////修改bundle名   
                //AssetImporter ai = AssetImporter.GetAtPath(xmlInfo.assetFullName);
                //
                //ai.assetBundleName = xmlInfo.bundleName;
                //
                //if (xmlInfo.HasOtherLanguage) { ai.assetBundleVariant = AssetI8NHelper.GetLangBundleVariantStr(xmlInfo.Language); }
                //else
                //{
                //    ai.assetBundleVariant = null;
                //}
                var info = new AssetBundleBuild();
                info.assetBundleName = xmlInfo.bundleMD5Struct.GetMD5Str(!xmlInfo.isComplexName);
                if (xmlInfo.HasOtherLanguage) 
                {info.assetBundleVariant = AssetI8NHelper.GetLangBundleVariantStr(xmlInfo.Language);}
                else
                {
                    info.assetBundleVariant = null;
                }
                info.addressableNames = new string[] {  xmlInfo.resPath.ToLower() };
                info.assetNames = new string[] {xmlInfo.assetFullName };

                abInfo[i] = info;
            }

            this.BuildMap = abInfo;
        }
        //根据打包后的Manifest刷新XML
        public void RefreshXMLTableByManifest(AssetBundleManifest manifest)
        {
            HashSet<string> bundleHash = new HashSet<string>();
            foreach (var t in XMLTable.BundleInfos)
            {
                var defaultBundleName = t.GetBundleNameWithLangExtension(eAssetLanguageVarType.Default);
                var dependences = manifest.GetAllDependencies(defaultBundleName);
                t.dependenceBundleName = new string[dependences.Length];
                for (int i = 0; i < dependences.Length; i++)
                {
                    t.dependenceBundleName[i] = AssetI8NHelper.GetBundleNameWithOutLangStr(dependences[i]);
                }
                var hash = manifest.GetAssetBundleHash(defaultBundleName);
                if (bundleHash.Contains(hash.ToString()))
                {
                    //fileHash重名的，打印出来，并且使用bundleName来合并，避免冲突
                    t.bundleFileName = defaultBundleName+ "_" + hash.ToString()  ;
                    CommonLog.Error(string.Format("卧槽，有bundleFileHash冲突，bundle信息为,bundleName:{0},bundleHash:{1},asset:{2}"
                        , defaultBundleName, hash.ToString(), t.assets[0].resPath));
                }
                else
                {
                    bundleHash.Add(defaultBundleName);
                    t.bundleFileName =defaultBundleName;
                }
            }

        }

        public virtual void Begin()
        {
            InitDirs();
            if(IsOpenProgressUI)EditorUtility.DisplayProgressBar("Loading", "Loading...", 0.1f);
            Caching.ClearCache();

            CollectABBuildMap();
        }

        private void ComputeBundleMd5()
        {
            foreach (var info in XMLTable.BundleInfos)
            {
                if (FileUtils.IsFileExist(BundleSavePath + info.GetBundleNameWithLangExtension(eAssetLanguageVarType.Default)))
                {
                    //FileUtils.ReadAllBytes();
                }
            }
        }

        public virtual void End(AssetBundleManifest manifest, bool addPacker = false)
        {
            //生成TableBundle信息
            XMLTable = new AssetBundleTable();

            XMLTable.GenBundleInfoFromRaw(AssetBundleRawInfoList);

            //刷新XML数据
            RefreshXMLTableByManifest(manifest);

            var abMainifestName =  System.IO.Path.GetFileName(BundleSavePath.Substring(0,BundleSavePath.Length-1));
            //删了主AB包
            FileUtils.DeleteFile(BundleSavePath + abMainifestName);
            //删了无用的manifest
            //FileUtils.DeleteFiles(BundleSavePath(), SearchOption.TopDirectoryOnly, ".manifest");

            //重命名所有Bundle
            //foreach (var info in XMLTable.BundleInfos)

            for(int index = 0; index < XMLTable.BundleInfos.Length; index++)
            {
                var bunldeInfo = XMLTable.BundleInfos[index];
                for (int i = 0; i < bunldeInfo.bundleLanguages.Length; i++)
                {
                    if (bunldeInfo.bundleLanguages[i])
                    {
                        if (!FileUtils.IsFileExist(BundleSavePath + bunldeInfo.GetBundleNameWithLangExtension((eAssetLanguageVarType)i)))
                        {
                            CommonLog.Error("找不到打包后的资源：" + bunldeInfo.assets[0].assetFullName);
                            continue;
                        }

                        //是否增量打Bundle
                        if (addPacker)
                        {
                            string sourceFilePath = BundleSavePath + bunldeInfo.GetBundleNameWithLangExtension((eAssetLanguageVarType)i);
                            string targetFilePath = BundleSavePath + bunldeInfo.GetBundleFileNameWithLangExtension((eAssetLanguageVarType)i);
                            if (!sourceFilePath.Equals(targetFilePath))
                            {
                                if (File.Exists(targetFilePath))
                                    File.Delete(targetFilePath);

                                File.Copy(BundleSavePath + bunldeInfo.GetBundleNameWithLangExtension((eAssetLanguageVarType)i),
                                    BundleSavePath + bunldeInfo.GetBundleFileNameWithLangExtension((eAssetLanguageVarType)i), true);
                            }
                        }
                        else
                        {
                            FileUtils.RenameFile(BundleSavePath, bunldeInfo.GetBundleNameWithLangExtension((eAssetLanguageVarType)i),
                                bunldeInfo.GetBundleFileNameWithLangExtension((eAssetLanguageVarType)i));
                        }
                    }
                }

                //计算Bundle的MD5
                var assetBundleBytes = FileUtils.ReadAllBytes($"{BundleSavePath}/{bunldeInfo.bundleFileName}");
                if (assetBundleBytes != null && assetBundleBytes.Length > 0)
                {
                    var md5Str = MD5Creater.Md5Struct(assetBundleBytes).GetMD5Str(false);
                    bunldeInfo.bundleMD5 = md5Str;
                }
            }

            //TODO 写上版本号

            string xml = XMLTable.ToXML();

            File.WriteAllText(BundleSavePath + AssetBundlePathResolver.DependFileName, xml);

            EditorUtility.ClearProgressBar();

            CommonLog.Warning(string.Format("热更新XML生成完成"));

        }


        public abstract AssetBundleManifest Export(bool forceRebuild = false);

        /// <summary>
        /// 删除无关manifest资源
        /// PS！这个改为交给Jenkins和打包的代码自行处理，不在打包流程干这个了。
        /// </summary>
        /// <param name="all"></param>
        protected void RemoveUnused()
        {

            //DirectoryInfo di = new DirectoryInfo(BundleSavePath);
            //FileInfo[] abFiles = di.GetFiles("*.ab");
            //for (int i = 0; i < abFiles.Length; i++)
            //{
            //    FileInfo fi = abFiles[i];
            //    Log.Info("Remove unused AB : " + fi.Name);
            //
            //    fi.Delete();
            //    //for U5X
            //    File.Delete(fi.FullName + ".manifest");
            //}
        }
    }

    public class AssetBundleBuilder5x_PC : ABBuilder
    {

        public override AssetBundleManifest Export(bool forceRebuild = false)
        {
            return ExportPC(forceRebuild);
        }


        private AssetBundleManifest ExportPC(bool forceRebuild = false)
        {
            BuildAssetBundleOptions options = BuildAssetBundleOptions.DeterministicAssetBundle //保证增量HASH不变
                                            | BuildAssetBundleOptions.ChunkBasedCompression //使用LZ4
                                            | BuildAssetBundleOptions.DisableLoadAssetByFileName //去除FileName，节约内存
                                            | BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension //去除FileName加后缀名，节约内存
                                            | BuildAssetBundleOptions.StrictMode; //最严格模式
                                            //| BuildAssetBundleOptions.DisableWriteTypeTree  //不做Unity版本兼容=。=有点可怕，所以，不能选

            if (forceRebuild)
                options |= BuildAssetBundleOptions.ForceRebuildAssetBundle; //全部AB都重建

            //开始打包
            Manifest = BuildPipeline.BuildAssetBundles(BundleSavePath, BuildMap, options, BuildTarget.StandaloneWindows64);

            this.RemoveUnused();

            AssetDatabase.Refresh();

            return Manifest;

        }
    }

    public class AssetBundleBuilder5x_Android : ABBuilder
    {

        public override AssetBundleManifest Export(bool forceRebuild = false)
        {
            return ExportAndroid(forceRebuild);
        }

        private AssetBundleManifest ExportAndroid(bool forceRebuild = false)
        {
            BuildAssetBundleOptions options = BuildAssetBundleOptions.DeterministicAssetBundle //保证增量HASH不变
                                            | BuildAssetBundleOptions.ChunkBasedCompression //使用LZ4
                                            | BuildAssetBundleOptions.DisableLoadAssetByFileName //去除FileName，节约内存
                                            | BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension; //去除FileName加后缀名，节约内存
                                            //| BuildAssetBundleOptions.StrictMode; //最严格模式
                                            //| BuildAssetBundleOptions.DisableWriteTypeTree  //不做Unity版本兼容=。=有点可怕，所以，不能选

            if (forceRebuild)
                options |= BuildAssetBundleOptions.ForceRebuildAssetBundle; //全部AB都重建

            //开始打包
            Manifest = BuildPipeline.BuildAssetBundles(BundleSavePath, BuildMap, options, BuildTarget.Android);

            this.RemoveUnused();


            AssetDatabase.Refresh();
            return Manifest;

        }
    }

    public class AssetBundleBuilder5x_IOS : ABBuilder
    {

        public override AssetBundleManifest Export(bool forceRebuild = false)
        {
            return ExportIOS(forceRebuild);
        }


        private AssetBundleManifest ExportIOS(bool forceRebuild = false)
        {
            BuildAssetBundleOptions options = BuildAssetBundleOptions.DeterministicAssetBundle //保证增量HASH不变
                                            | BuildAssetBundleOptions.ChunkBasedCompression //使用LZ4
                                            | BuildAssetBundleOptions.DisableLoadAssetByFileName //去除FileName，节约内存
                                            | BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension; //去除FileName加后缀名，节约内存
                                            //| BuildAssetBundleOptions.StrictMode; //最严格模式
                                            //| BuildAssetBundleOptions.DisableWriteTypeTree  //不做Unity版本兼容=。=有点可怕，所以，不能选

            if (forceRebuild)
                options |= BuildAssetBundleOptions.ForceRebuildAssetBundle; //全部AB都重建

            //开始打包
            Manifest = BuildPipeline.BuildAssetBundles(BundleSavePath, BuildMap,options, BuildTarget.iOS);

            this.RemoveUnused();

            AssetDatabase.Refresh();
            return Manifest;
        }

    }


}
