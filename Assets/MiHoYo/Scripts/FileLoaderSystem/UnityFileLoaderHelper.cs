using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine; 

/// <summary>
/// 文件加载系统，统一了全平台的StreamAsset、PersistAsset的文件读取到同一个接口。
/// </summary>
public static class UnityFileLoaderHelper
{
    public enum eFileLoaderPosType
    {
        None,
        PersistAsset,
        StreamAsset,
    }
    //缓存路径
    private static string _DataPath = "";
    //获取缓存目录
    public static string DataPath
    {
        get
        {
            if (_DataPath == "")
            {
#if UNITY_ANDROID && !UNITY_EDITOR && OBB
                _DataPath = FileMapSystem.Obb.AndroidFileUtils_ObbMode.ObbPath();
#else
                _DataPath = Application.dataPath;
#endif
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
#if UNITY_ANDROID && !UNITY_EDITOR && OBB
                _DataPath = FileMapSystem.Obb.AndroidFileUtils_ObbMode.ObbPath();
#else
                _StreamingAssetsPath = Application.streamingAssetsPath;
#endif
            }
            return _StreamingAssetsPath;
        }
    }

    //缓存路径
    private static string _PersistenPath = "";
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

    public static bool USED_AB_MODE { get; set; }

    public static void InitForMainThread()
    {
        //初始化Application路径
        var d = DataPath;
        var s = StreamingAssetsPath;
        var p = PersistenPath;
#if UNITY_ANDROID && !UNITY_EDITOR
#if OBB
        FileMapSystem.Obb.AndroidFileUtils_ObbMode.Init();
#else
        AndroidFileUtils.Init();
#endif
#endif
    }

    public static byte[] ReadFileAllBytes(string filePath)
    {
        if (USED_AB_MODE)
        {
            var fileName = Path.GetFileName(filePath);
            var fileDir = filePath.Replace(fileName, string.Empty).Replace("\\", "/");
            return ReadFileAllBytes(fileDir, fileName);
        }
        else
        {
            return File.ReadAllBytes(filePath);
        }
    }

    public static byte[] ReadFileAllBytes(string dir, string fileName)
    {
        eFileLoaderPosType pos;
        return ReadFileAllBytes(dir, fileName, out pos);
    }

    public static byte[] ReadFileAllBytes(string dir, string fileName, out eFileLoaderPosType pos)
    {
        var hasFile = IsFileExist(dir, fileName, out pos);
        if (hasFile)
        {
            if (pos == eFileLoaderPosType.PersistAsset)
            {
                return UnityPersistFileHelper.ReadPersistAssetFileAllBytes(dir, fileName);
            }
            if (pos == eFileLoaderPosType.StreamAsset)
            {
                return UnityStreamingFileHelper.ReadStreamAssetFileAllBytes(dir, fileName);
            }
        }
        return null;
    }

    public static Stream ReadFileByStream(string filePath, int offset = 0)
    {
        if (USED_AB_MODE)
        {
            var fileName = Path.GetFileName(filePath);
            var fileDir = filePath.Replace(fileName, string.Empty).Replace("\\", "/");
            return ReadFileByStream(fileDir, fileName, offset);
        }
        else
        {
            var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return fs;
        }
    }

    public static Stream ReadFileByStream(string dir, string fileName, int offset = 0)
    {
        eFileLoaderPosType pos;
        return ReadFileByStream(dir, fileName, out pos, offset);
    }
    public static Stream ReadFileByStream(string dir, string fileName, out eFileLoaderPosType pos, int offset = 0)
    {
        var hasFile = IsFileExist(dir, fileName, out pos);
        if (hasFile)
        {
            if (pos == eFileLoaderPosType.PersistAsset)
            {
                return UnityPersistFileHelper.ReadPersistAssetFileByStream(dir, fileName, offset);
            }
            if (pos == eFileLoaderPosType.StreamAsset)
            {
                return UnityStreamingFileHelper.ReadStreamAssetFileByStream(dir, fileName, offset);
            }
        }
        return null;
    }


    public static byte[] ReadFileAllBytes(string dir, string fileName, int offset, int len)
    {
        eFileLoaderPosType pos;
        return ReadFileAllBytes(dir, fileName, offset, len, out pos);
    }

    public static byte[] ReadFileAllBytes(string dir, string fileName, int offset, int len, out eFileLoaderPosType pos)
    {
        var hasFile = IsFileExist(dir, fileName, out pos);
        if (hasFile)
        {
            if (pos == eFileLoaderPosType.PersistAsset)
            {
                return UnityPersistFileHelper.ReadPersistAssetFileAllBytes(dir, fileName, offset, len);
            }
            if (pos == eFileLoaderPosType.StreamAsset)
            {
                return UnityStreamingFileHelper.ReadStreamAssetFileAllBytes(dir, fileName, offset, len);
            }
        }
        return null;
    }

    public static AssetBundle ReadAssetBundle(string dir, string fileName, ulong offset, out eFileLoaderPosType pos, uint crc = 0U)
    {
        var hasFile = IsFileExist(dir, fileName, out pos);
        if (hasFile)
        {
            if (pos == eFileLoaderPosType.PersistAsset)
            {
                return UnityPersistFileHelper.ReadPersistAssetBundle(dir, fileName, offset, crc);
            }
            if (pos == eFileLoaderPosType.StreamAsset)
            {
                return UnityStreamingFileHelper.ReadStreamAssetBundle(dir, fileName, offset, crc);
            }
        }
        return null;
    }
     

    public static AssetBundleCreateRequest ReadAssetBundleAsync(string dir, string fileName, ulong offset, out eFileLoaderPosType pos, uint crc = 0U)
    {
        var hasFile = IsFileExist(dir, fileName, out pos);
        if (hasFile)
        {
            if (pos == eFileLoaderPosType.PersistAsset)
            {
                return UnityPersistFileHelper.ReadPersistAssetBundleAsync(dir, fileName, offset, crc);
            }
            if (pos == eFileLoaderPosType.StreamAsset)
            {
                return UnityStreamingFileHelper.ReadStreamAssetBundleAsync(dir, fileName, offset, crc);
            }
        }
        return null;
    }
     
    public static string ReadFileAllText(string filePath)
    {
        if (USED_AB_MODE)
        {
            var fileName = Path.GetFileName(filePath);
            var fileDir = filePath.Replace(fileName, string.Empty).Replace("\\", "/");
            return ReadFileAllText(fileDir, fileName);
        }
        else
        {
            return File.ReadAllText(filePath);
        }
    }
    public static string ReadFileAllText(string dir, string fileName)
    {
        eFileLoaderPosType pos;
        var hasFile = IsFileExist(dir, fileName, out pos);
        if (hasFile)
        {
            if (pos == eFileLoaderPosType.PersistAsset)
            {
                return UnityPersistFileHelper.ReadPersistAssetFileAllText(dir, fileName);
            }
            if (pos == eFileLoaderPosType.StreamAsset)
            {
                return UnityStreamingFileHelper.ReadStreamAssetFileAllText(dir, fileName);
            }
        }
        return null;
    }
     
    /// <summary>
    /// 检查文件是否存在，检查顺序为Persist然后Stream
    /// </summary>
    /// <param name="dir"></param>
    /// <param name="path"></param>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static bool IsFileExist(string dir, string fileName, out eFileLoaderPosType pos)
    {
        if (UnityPersistFileHelper.IsPersistAssetFileExist(dir, fileName))
        {
            pos = eFileLoaderPosType.PersistAsset;
            return true;
        }
        if (UnityStreamingFileHelper.IsStreamAssetFileExist(dir, fileName))
        {
            pos = eFileLoaderPosType.StreamAsset;
            return true;
        }
        pos = eFileLoaderPosType.None;
        return false;
    }
       
}
