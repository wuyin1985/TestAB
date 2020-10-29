using System.IO;
using UnityEngine;

namespace Res.ABSystem
{
    /// <summary>
    /// AB 打包及运行时路径解决器
    /// </summary>
    public class AssetBundlePathResolver
    {
        //bundle保存的文件夹名称
        public static string BundleSaveDirName = "AssetbundlesCache";
        /// AB 依赖信息文件名
        public static string DependFileName = "AssetBundleXMLData.xml";
        //压缩文件的名字
        public static string CompressZipName = "AssetbundlesZip.7z";
        //文件夹信息
        static DirectoryInfo cacheDir;
        //是否走新的bundle模式
        public static bool IsNewBundleModel = true;
        //缓存路径
        private static string _PersistenPath = "";
        //服务器上初始完整bundle地址
        public static string ServiceBundlePath = "";
        //资源版本号
        public static string ResourceVersion = "";
        //缓存路径
        private static string _DataPath = "";
        //获取缓存目录
        public static string DataPath
        {
            get
            {
                if (_DataPath == "")
                {
                    _DataPath = Application.dataPath;
                }
                return _DataPath;
            }
        }

        //缓存路径
        private static string _StreamingAssetsPath = "";
        //获取缓存目录
        public static string StreamingAssetsPath
        {
            get
            {
                if (_StreamingAssetsPath == "")
                {
                    _StreamingAssetsPath = Application.streamingAssetsPath;
                }
                return _StreamingAssetsPath;
            }
        }
        //获取缓存目录
        public static string PersistenPath
        {
            get
            {
                if (_PersistenPath == "")
                {
                    _PersistenPath = Application.persistentDataPath;
                }
                return _PersistenPath;
            }
        }
#if UNITY_EDITOR
        //===============================================打包相关路径==========================================================
        private static string _BundlesGeneratePathName = "assetbundles";
        public static string BundlesGeneratePathName
        {
            get {
                return _BundlesGeneratePathName;
            }
            set {
                _BundlesGeneratePathName = value;
            }
        }

        //pc端打包bundle保存路径
        private static string _BundlePCSavedPath = "";

        public static string BundlePCSavedPath
        {
            get
            {
                if (string.IsNullOrEmpty(_BundlePCSavedPath))
                {
                    var dir = System.IO.Directory.GetParent(DataPath);
                    _BundlePCSavedPath = System.IO.Path.Combine(dir.Parent.ToString(), $"{_BundlesGeneratePathName}\\pc\\ab\\");
                }
                return _BundlePCSavedPath;
            }
            set
            {
                _BundlePCSavedPath = value;
            }
        }
        private static string _BundleAndroidSavedPath = null;
        public static string BundleAndroidSavedPath
        {
            get
            {
                if (string.IsNullOrEmpty(_BundleAndroidSavedPath))
                {
                    var dir = System.IO.Directory.GetParent(DataPath);
                    _BundleAndroidSavedPath = System.IO.Path.Combine(dir.Parent.ToString(), $"{_BundlesGeneratePathName}/android/ab/");
                }
                return _BundleAndroidSavedPath;
            }
            set
            {
                _BundleAndroidSavedPath = value;
            }
        }
        private static string _BundleIOSSavedPath = null;
        public static string BundleIOSSavedPath
        {
            get
            {
                if (string.IsNullOrEmpty(_BundleIOSSavedPath))
                {
                    var dir = System.IO.Directory.GetParent(DataPath);
                    _BundleIOSSavedPath = System.IO.Path.Combine(dir.Parent.ToString(), $"{_BundlesGeneratePathName}/ios/ab/");
                }
                return _BundleIOSSavedPath;
            }
            set
            {
                _BundleIOSSavedPath = value;
            }
        }
        /// <summary>
        /// 在编辑器模型下将 abName 转为 Assets/... 路径
        /// 这样就可以不用打包直接用了
        /// </summary>
        /// <param name="abName"></param>
        /// <returns></returns>
        public static string GetEditorModePath(string abName)
        {
            //将 Assets.AA.BB.prefab 转为 Assets/AA/BB.prefab
            abName = abName.Replace(".", "/");
            int last = abName.LastIndexOf("/");

            if (last == -1)
                return abName;

            string path = string.Format("{0}.{1}", abName.Substring(0, last), abName.Substring(last + 1));
            return path;
        }
        //===============================================打包相关路径==========================================================
#endif

        /// <summary>
        /// 获取 AB 源文件路径（打包进安装包的）
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetBundleSourceFile(string path)
        {
            string filePath = null;
            filePath = string.Format("{0}/{1}/{2}", PersistenPath, BundleSaveDirName, path);
            return filePath;
        }
        //根据jar包读取streamingAsset文件的特殊路径（去掉streamingAsset前缀）
        public static string GetStreamingBundlePath(string fileName)
        {
            string filePath = null;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN ||UNITY_STANDALONE_OSX||UNITY_STANDALONE_LINUX 
            filePath = string.Format("{0}/{1}/{2}", Application.streamingAssetsPath, BundleSaveDirName, fileName);
#elif UNITY_ANDROID
//Android路径太特殊了，没有SteamingAsset,需要自行Jar拼接
            filePath = string.Format("{0}!assets/{1}/{2}", DataPath,BundleSaveDirName,  fileName); 
#else
            filePath = string.Format("{0}/{1}/{2}", Application.streamingAssetsPath, BundleSaveDirName,  fileName); 
#endif
            return filePath;
        }

        //获取streaming根目录地址
        public static string GetStreamingPath(string fileName)
        {
            string filePath = null;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN ||UNITY_STANDALONE_OSX||UNITY_STANDALONE_LINUX 
            filePath = string.Format("{0}/{1}", Application.streamingAssetsPath, fileName);
#elif UNITY_ANDROID
            //Android路径太特殊了，没有SteamingAsset,需要自行Jar拼接
            filePath = string.Format("{0}!assets/{1}",DataPath,  fileName); 
#else
            filePath = string.Format("{0}/{1}", Application.streamingAssetsPath,fileName); 
#endif
            return filePath;
        }

        public static bool CheckBundleExistInStreaming(string fileName)
        {
            var filePath = GetStreamingBundlePath(fileName);
            return false;
        }

        public static bool CheckBundleExistInPersisten(string fileName)
        {
            var filePath = GetBundleSourceFile(fileName);
            return false;
        }


        /// <summary>
        /// 用于缓存AB的目录，要求可写
        /// </summary>
        public static string GetBundleCacheDir()
        {
            string dir = "";
#if UNITY_EDITOR || UNITY_STANDALONE_WIN ||UNITY_STANDALONE_OSX||UNITY_STANDALONE_LINUX 
            dir = string.Format("{0}/{1}/", PersistenPath, BundleSaveDirName);
#elif UNITY_ANDROID || UNITY_IPHONE
             dir = string.Format("{0}/{1}/", PersistenPath, BundleSaveDirName);
#else
             dir = string.Format("{0}/{1}/", PersistenPath , BundleSaveDirName);
#endif
            return dir;
        }


        //创建bundle缓存目录
        public static void CreateBundleCacheDir()
        {
            string cachePath = GetBundleCacheDir();
            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }
            CreatDependDir();
        }

        //创建缓存目录
        public static void CreatDependDir()
        {
            string cachePath = GetBundleSourceFile("streaming/");
            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }
        }

    }
}

