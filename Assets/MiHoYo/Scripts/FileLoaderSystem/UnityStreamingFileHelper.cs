using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

//获取streamingAsset下面的资源
public static class UnityStreamingFileHelper
{
     
    public static bool IsStreamAssetFileExist(string dir, string fileName)
    {
        return CheckStreamExistsFile(GetStreamAssetFilePath(dir, fileName));
    }

    public static string[] GetStreamAssetFileList(string dir)
    {
        var path = GetStreamAssetFilePath(dir, "");
#if UNITY_EDITOR || UNITY_STANDALONE_WIN 
        var filePath =  Directory.GetFiles(path);
        var fileNames = new string[filePath.Length];
        for (int i = 0; i < filePath.Length; i++)
        {
            fileNames[i] = System.IO.Path.GetFileName(filePath[i]);
        } 
        return fileNames;
#elif UNITY_ANDROID
#if OBB
        return FileMapSystem.Obb.AndroidFileUtils_ObbMode.GetAllFileNames(path);
#else
        return AndroidFileUtils.GetAndroidAssetFileList(path);
#endif
#else
        var filePath =  Directory.GetFiles(path);
        var fileNames = new string[filePath.Length];
        for (int i = 0; i < filePath.Length; i++)
        {
            fileNames[i] = System.IO.Path.GetFileName(filePath[i]);
        } 
        return fileNames;
#endif
    }

    //根据不同平台读取字符串数据
    public static string GetStreamStringAllPlatform(string path)
    {
        if (!CheckStreamExistsFile(path))
        {
            return "";
        }
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        return File.ReadAllText(path);
#elif UNITY_ANDROID
            return AndroidFileUtils.ReadTextFile(path);
#else
            return File.ReadAllText(path); 
#endif
    }

    public static bool CheckStreamExistsFile(string path)
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        return File.Exists(path);
#elif UNITY_ANDROID
#if OBB
        return FileMapSystem.Obb.AndroidFileUtils_ObbMode.CheckStreamExistsFile(path);
#else
        return AndroidFileUtils.CheckExistsInAndroidAsset(path);
#endif
#else
       return File.Exists(path);
#endif

    }

    //根据不同平台获取相应的字节数据
    public static byte[] ReadStreamAssetFileAllBytes(string dir, string fileName)
    {
        var path = GetStreamAssetFilePath(dir, fileName);
        if (!CheckStreamExistsFile(path))
        {
            return null;
        }
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        return File.ReadAllBytes(path);
#elif UNITY_ANDROID
#if OBB
            return FileMapSystem.Obb.AndroidFileUtils_ObbMode.ReadAllBytes(path);
#else
            return AndroidFileUtils.ReadFile(path);
#endif
#else
            return File.ReadAllBytes(path); 
#endif
    }
    

    //根据不同平台获取相应的Stream
    public static Stream ReadStreamAssetFileByStream(string dir, string fileName,int offset=0)
    {
        var path = GetStreamAssetFilePath(dir, fileName);
        if (!CheckStreamExistsFile(path))
        {
            return null;
        }        
#if UNITY_EDITOR || UNITY_STANDALONE_WIN 
            var result = File.OpenRead(path) ;
            if(offset!=0) result.Seek(offset, SeekOrigin.Begin);
            return result; 
#elif UNITY_ANDROID
#if OBB
            var result = FileMapSystem.Obb.AndroidFileUtils_ObbMode.OpenRead(path);
            if(offset!=0) result.Seek(offset, SeekOrigin.Begin);
            return result; 
#else
            return AndroidFileUtils.OpenFileStream(path,offset);
#endif
#else 
            var result = File.OpenRead(path) ; 
            if(offset!=0) result.Seek(offset, SeekOrigin.Begin);
            return result; 
#endif
    }

    //根据不同平台获取相应的字节数据
    public static byte[] ReadStreamAssetFileAllBytes(string dir, string fileName, int offset, int len)
    {
        var path = GetStreamAssetFilePath(dir, fileName);
        if (!CheckStreamExistsFile(path))
        {
            return null;
        }        

        //return AndroidFileUtils.ReadFile(path);
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        using (var result = File.OpenRead(path))
        {
            var bs = new byte[len];
            if(offset!=0) result.Seek(offset, SeekOrigin.Begin);
            result.Read(bs, 0, len);
            return bs;
        }
#elif UNITY_ANDROID
#if OBB
            var result = FileMapSystem.Obb.AndroidFileUtils_ObbMode.OpenRead(path);
            if(offset!=0) result.Seek(offset, SeekOrigin.Begin);
            if (len == -1) len = (int)(result.Length - offset);
            byte[] buffer = new byte[len];
            result.Read(buffer, 0, len);
            return buffer; 
#else
            return AndroidFileUtils.ReadFile(path,offset,len);
#endif
#else 
        using (var result = File.OpenRead(path))
        {
            var bs = new byte[len];
            if(offset!=0) result.Seek(offset, SeekOrigin.Begin);
            result.Read(bs, 0, len);
            return bs;
        }
#endif
    }

    //根据不同平台读取字符串数据
    public static string ReadStreamAssetFileAllText(string dir, string fileName)
    {
        var path = GetStreamAssetFilePath(dir, fileName);
        if (!CheckStreamExistsFile(path))
        {
            return "";
        }
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            return File.ReadAllText(path);
#elif UNITY_ANDROID 
            var bs = ReadStreamAssetFileAllBytes(dir, fileName);
            return ReadTextFile(bs);
#else
            return File.ReadAllText(path); 
#endif
    }

    private static string ReadTextFile(byte[] strBytes)
    { 
        if (strBytes == null || strBytes.Length == 0)
        {
            return "";
        }
        else
        {
            return System.Text.Encoding.UTF8.GetString(strBytes).TrimEnd('\0');
        }
    }

    public static AssetBundle ReadStreamAssetBundle(string dir, string fileName,ulong offset,uint crc=0U)
    {   
        var path = GetStreamAssetFilePath(dir, fileName);
        if (CheckStreamExistsFile(path))
        {  
#if  UNITY_ANDROID && !UNITY_EDITOR && OBB
            FileMapSystem.Obb.AndroidFileUtils_ObbMode.ReadInfo info;
            if (FileMapSystem.Obb.AndroidFileUtils_ObbMode.TryGetInfo(path, out info))
            {
                path = info.readPath;
                return  AssetBundle.LoadFromFile(path, crc, offset + (ulong)info.offset);
            } else{
                return null;
            }
#endif  
            return AssetBundle.LoadFromFile(path, crc, offset); 
        }
        return null;
    }

    public static AssetBundleCreateRequest ReadStreamAssetBundleAsync(string dir, string fileName,ulong offset,uint crc=0U)
    {
        var path = GetStreamAssetFilePath(dir, fileName);
        if (CheckStreamExistsFile(path))
        {  
#if  UNITY_ANDROID && !UNITY_EDITOR && OBB
            FileMapSystem.Obb.AndroidFileUtils_ObbMode.ReadInfo info;
            if (FileMapSystem.Obb.AndroidFileUtils_ObbMode.TryGetInfo(path, out info))
            {
                path = info.readPath;
                return  AssetBundle.LoadFromFileAsync(path, crc, offset + (ulong)info.offset);
            } else{
                return null;
            }
#endif  
            return AssetBundle.LoadFromFileAsync(path, crc, offset); 
             
        }
        return null;
    }
     
    public static string GetStreamAssetFilePath(string dir, string fileName)
    {
        string filePath = null;
        if (string.IsNullOrEmpty(dir))
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
            filePath = string.Format("{0}/{1}", UnityFileLoaderHelper.StreamingAssetsPath, fileName);
#elif UNITY_ANDROID
            //Android路径太特殊了，没有SteamingAsset,需要自行Jar拼接
            filePath = string.Format("{0}!assets/{1}",UnityFileLoaderHelper.DataPath, fileName); 
#else
            filePath = string.Format("{0}/{1}", UnityFileLoaderHelper.StreamingAssetsPath,  fileName); 
#endif
        }
        else
        {
            if (dir.EndsWith("/"))
            {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
                filePath = string.Format("{0}/{1}{2}", UnityFileLoaderHelper.StreamingAssetsPath, dir, fileName);
#elif UNITY_ANDROID
            //Android路径太特殊了，没有SteamingAsset,需要自行Jar拼接
            filePath = string.Format("{0}!assets/{1}{2}",  UnityFileLoaderHelper.DataPath,dir,  fileName);
#else
            filePath = string.Format("{0}/{1}{2}", UnityFileLoaderHelper.StreamingAssetsPath, dir,  fileName);
#endif
            }
            else
            {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
                filePath = string.Format("{0}/{1}/{2}", UnityFileLoaderHelper.StreamingAssetsPath, dir, fileName);
#elif UNITY_ANDROID
                //Android路径太特殊了，没有SteamingAsset,需要自行Jar拼接
                filePath = string.Format("{0}!assets/{1}/{2}",  UnityFileLoaderHelper.DataPath,dir,  fileName);
#else
                filePath = string.Format("{0}/{1}/{2}", UnityFileLoaderHelper.StreamingAssetsPath, dir,  fileName);
#endif
            }
        }
        return filePath;
    }

}
