using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class UnityPersistFileHelper
{


    public static AssetBundle ReadPersistAssetBundle(string dir, string fileName, ulong offset, uint crc = 0U)
    {
        var path = GetPersistAssetFilePath(dir, fileName);
        if (File.Exists(path))
        {
            return AssetBundle.LoadFromFile(path, crc, offset);
        }
        return null;
    }

    public static AssetBundleCreateRequest ReadPersistAssetBundleAsync(string dir, string fileName, ulong offset, uint crc = 0U)
    {
        var path = GetPersistAssetFilePath(dir, fileName);
        if (File.Exists(path))
        {
            return AssetBundle.LoadFromFileAsync(path, crc, offset);
        }
        return null;
    }

    public static bool IsPersistAssetFileExist(string dir, string fileName)
    {
        return File.Exists(GetPersistAssetFilePath(dir, fileName));
    }

 
    public static byte[] ReadPersistAssetFileAllBytes(string dir, string fileName)
    {
        var path = GetPersistAssetFilePath(dir, fileName);
        if (File.Exists(path))
        {
            var result = File.ReadAllBytes(path);
            return result;
        }
        return null;
    }

    public static Stream ReadPersistAssetFileByStream(string dir, string fileName, int offset = 0)
    {
        var path = GetPersistAssetFilePath(dir, fileName);
        if (File.Exists(path))
        {
            var result = File.OpenRead(path);
            //Log.E("File:" + fileName+ "offset:" + offset + "len:" + len);
            if (offset > 0) result.Seek(offset, SeekOrigin.Begin);
            return result;
        }
        return null;
    }


    public static byte[] ReadPersistAssetFileAllBytes(string dir, string fileName, int offset, int len)
    {
        var path = GetPersistAssetFilePath(dir, fileName);
        if (File.Exists(path))
        {
            var bs = new byte[len];
            var result = File.OpenRead(path);
            //Log.E("File:" + fileName+ "offset:" + offset + "len:" + len);
            result.Seek(offset, SeekOrigin.Begin);
            result.Read(bs, 0, len);
            return bs;
        }
        return null;
    }
    
    public static string ReadPersistAssetFileAllText(string dir, string fileName)
    {
        var path = GetPersistAssetFilePath(dir, fileName);
        if (File.Exists(path))
        {
            var result = File.ReadAllText(path, encoding: System.Text.Encoding.UTF8);
            return result;
        }
        return null;
    }


    public static string[] GetPersistAssetFileList(string dir, string pattern = "*")
    {
        var path = GetPersistAssetFilePath(dir, "");
        var filePath = Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly);
        var fileNames = new string[filePath.Length];
        for (int i = 0; i < filePath.Length; i++)
        {
            fileNames[i] = System.IO.Path.GetFileName(filePath[i]);
        }
        return fileNames;
    }

    public static void DeletePersistAssetFileList(string dir, List<string> files)
    {
        foreach (var f in files)
        {
            var path = GetPersistAssetFilePath(dir, f);
            FileUtils.DeleteFile(path);
        }
    }
    
    public static string GetPersistAssetFilePath(string dir, string fileName)
    {
        string filePath = null;
        if (string.IsNullOrEmpty(dir))
        {
            filePath = string.Format("{0}/{1}", UnityFileLoaderHelper.PersistenPath, fileName);
        }
        else
        {
            if (dir.EndsWith("/"))
            {
                filePath = string.Format("{0}/{1}{2}", UnityFileLoaderHelper.PersistenPath, dir, fileName);
            }
            else
            {
                filePath = string.Format("{0}/{1}/{2}", UnityFileLoaderHelper.PersistenPath, dir, fileName);
            }
        }
        return filePath;
    }


    public static void SaveToPersistAssetFile(string SavedDir, string fileName, byte[] rawData)
    {
        if (rawData == null) return;
        //保存结果为文件 
        try
        {
            {
                string tempName = new Guid().ToString() + ".download";
                var pathRoot = GetPersistAssetFilePath(SavedDir, "");
                //删了旧文件
                FileUtils.DeleteFile(pathRoot + fileName);
                //写到临时文件
                FileUtils.WriteAllBytes(pathRoot + tempName, rawData);
                //写完改名字
                FileUtils.RenameFile(pathRoot, tempName, fileName);
            }
        }
        catch (Exception e)
        {
            CommonLog.Error("Save File Dir:{0} Name:{1} failure .Reason:{2}", SavedDir, fileName, e);
        }
    }
}
