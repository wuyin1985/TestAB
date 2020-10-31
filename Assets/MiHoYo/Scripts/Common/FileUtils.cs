using System.IO;
using System.Text;
using System.Collections.Generic;
using System;

public class FileUtils
{
    /// <summary>
    /// 基于UTF-8生成文件
    /// </summary>
    /// <param name="filePathName"></param>
    /// <param name="content"></param>
    /// <param name="createNewFile"></param>
    public static void AppendAllText(string filePathName, string content, bool createNewFile = false)
    {
        if (createNewFile && File.Exists(filePathName))
        {
            File.Delete(filePathName);
        }
        File.AppendAllText(filePathName, content, Encoding.UTF8);
    }

    public static void WriteAllText(string filePathName, string content, bool createNewFile = false)
    {
        if (createNewFile && File.Exists(filePathName))
        {
            File.Delete(filePathName);
        }
        File.WriteAllText(filePathName, content, Encoding.UTF8);
    }

    /// <summary>
    /// 写入文件字节
    /// </summary>
    /// <param name="filePathName"></param>
    /// <param name="content"></param>
    /// <param name="createNewFile"></param>
    public static void WriteAllBytes(string filePathName, byte[] content, bool createNewFile = false)
    {
        FileInfo info = new FileInfo(filePathName); 
        if (!info.Directory.Exists)
            info.Directory.Create(); 
        if (createNewFile && File.Exists(filePathName))
        {
            File.Delete(filePathName);
        }
        File.WriteAllBytes(filePathName, content);
    }

    /// <summary>
    /// 读取文件字节
    /// </summary>
    /// <param name="filePathName"></param>
    /// <param name="content"></param>
    /// <param name="createNewFile"></param>
    public static byte[] ReadAllBytes(string filePathName)
    {
        if (!File.Exists(filePathName))
        {
            return null;
        }
        return File.ReadAllBytes(filePathName);
    }
    /// <summary>
    /// 读取文件字节
    /// </summary>
    /// <param name="filePathName"></param>
    /// <param name="content"></param>
    /// <param name="createNewFile"></param>
    public static string ReadAllText(string filePathName)
    {
        if (!File.Exists(filePathName))
        {
            return null;
        }
        return File.ReadAllText(filePathName);
    }

    /// <summary>
    /// path：删除文件的路径 
    /// name：删除文件的名称
    /// </summary>
    /// <param name="path">我们的Path都约定带"/"</param>
    /// <param name="name"></param>
    public static void DeleteFile(string path, string name)
    {
        File.Delete(path + @"/" + name);
    }

    /// <summary>
    /// 改名字
    /// </summary>
    /// <param name="path">我们的Path都约定带"/"</param>
    /// <param name="name"></param>
    public static void RenameFile(string path,string oldName, string newName)
    {
        File.Move(path  + oldName,path+ newName);
    }

    public static void RenameFile(string sourceFilePath, string targetFilePath)
    {
        File.Move(sourceFilePath, targetFilePath);
    }

    public static void DeleteFile(string pathName)
    {
        var info = new FileInfo(pathName);
        if (info.Exists)
        {
            info.Attributes = FileAttributes.Normal;
            info.Delete();
        }
    }

    /// <summary>
    /// 删除目录下的所有指定文件格式的文件
    /// </summary>
    /// <param name="dirPath"></param>
    /// <param name="option"></param>
    /// <param name="searchPattern"></param>
    public static void DeleteFiles(string dirPath, SearchOption option = SearchOption.TopDirectoryOnly, string searchPattern = "")
    {
        if (!Directory.Exists(dirPath))
        {
            return;
        }
        string[] filesPath = GetFiles(dirPath, searchPattern, option);
        foreach (var path in filesPath)
        {
            File.Delete(path);
        }
    }

    public static bool IsFileExist(string path)
    {
        return File.Exists(path);
    }

    public static string GetFileName(string path, bool withoutExtension = false)
    {
        if (withoutExtension)
        {
            return Path.GetFileNameWithoutExtension(path);
        }
        return Path.GetFileName(path);
    }

    /// <summary>
    /// 计算文件的大小
    /// </summary>
    public static long FileSize(string path)
    {
        try
        {
            if (File.Exists(@path))
            {
                FileInfo fs = new FileInfo(@path);
                long size = fs.Length;
                return size;
            }
            else
            {
                return 0;
            }
        }
        catch (Exception ex)
        {
            throw new Exception("File Size fail, error:" + ex.Message);
        }
    }
    
    public static long DirectorySize(string path, string extension = "", SearchOption option = SearchOption.TopDirectoryOnly)
    {
        try
        {
            if (Directory.Exists(@path))
            {
                string[] files = GetFiles(@path, extension, option);
                long size = 0;
                for (int i = 0; i < files.Length; i++)
                {
                    size += FileSize(files[i]);
                }

                return size;
            }
            else
            {
                return 0;
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Directory Size fail, error:" + ex.Message);
        }
    }


    /// <summary>
    /// 获得指定目录下的特定后缀名文件【不区分大小写】
    /// </summary>
    /// <param name="path">指定目录</param>
    /// <param name="extension">后缀名</param>
    /// <param name="option">遍历目录</param>
    /// <param name="returnProjectPath">Assets/...</param>
    /// <returns>文件</returns>
    public static string[] GetFiles(string path, string extension = "", SearchOption option = SearchOption.AllDirectories)
    {
        if (Directory.Exists(path))
        {
            List<string> resultFileList = new List<string>();
            string[] filePathNames = Directory.GetFiles(path, "*.*", option);
            string lowerExtension = extension.ToLower();
            for (int i = 0; i < filePathNames.Length; ++i)
            {
                string filePathName = filePathNames[i];
                string lowerPathName = filePathName.ToLower();
                if (!string.IsNullOrEmpty(lowerExtension) && !lowerPathName.EndsWith(lowerExtension))
                {
                    continue;
                }
                resultFileList.Add(filePathName);
            }
            return resultFileList.ToArray();
        }
        return null;
    }

    /// <summary>
    /// 获得指定目录下的所有目录名
    /// </summary>
    /// <param name="path">指定目录</param>
    /// <param name="option">遍历</param>
    /// <returns>目录名</returns>
    public static string[] GetDirectories(string path,string pattern = "*" , SearchOption option = SearchOption.AllDirectories)
    {
        if (Directory.Exists(path))
        {
            return Directory.GetDirectories(path, "*", option);
        }
        return null;
    }

    /// <summary>
    /// 获取指定目录下的文件名（不包含.meta文件）
    /// </summary>
    /// <param name="assetPath">文件的直接父目录</param>
    /// <param name="filter">指定文件格式</param>
    /// <param name="option">是否遍历子目录</param>
    /// <returns></returns>
    public static List<string> GetFilesName(string assetPath, string extension = null, SearchOption option = SearchOption.TopDirectoryOnly, string searchPattern = "")
    {
        List<string> fileNameList = new List<string>();
        string[] filesPath = GetFiles(assetPath, searchPattern, option);
        if (filesPath == null) return null;
        for (int i = 0; i < filesPath.Length; i++)
        {
            string filePath = filesPath[i];
            string fileName = filePath.Substring(filePath.LastIndexOf("/", StringComparison.Ordinal) + 1);
            if (fileName.EndsWith(".meta"))
            {
                continue;
            }
            if (!string.IsNullOrEmpty(extension) && filePath.EndsWith(extension)) fileNameList.Add(fileName);
            else fileNameList.Add(fileName);
        }
        return fileNameList;
    }

    /// <summary>
    /// 检查目录是否存在, 不存在则创建
    /// </summary>
    /// <param name="path"></param>
    public static void CheckDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception)
            {

            }
        }
    }

    /// <summary>
    /// 拷贝指定文件到指定目录
    /// </summary>
    /// <param name="sourceFile"></param>
    /// <param name="destinationFolder"></param>
    /// <param name="filter"></param>
    public static void CopyFileToFolder(string sourceFile, string destinationFolder, string filter = "")
    {
        //Log.Info("==> CopyFileToFolder : sourceFile = " + sourceFile + ",destinationFolder = " + destinationFolder);
        if (!Directory.Exists(destinationFolder))
        {
            Directory.CreateDirectory(destinationFolder);
        }
        FileInfo t = new FileInfo(sourceFile);
        string destFile = destinationFolder + t.Name;
        if (filter == "" || sourceFile.Contains(filter))
        {
            File.Copy(sourceFile, destFile, true);
        }
    }

    /// <summary>
    /// 拷贝所有文件到指定目录
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="destinationPath"></param>
    /// <param name="filter"></param>
    public static void CopyFilesToFolder(string sourcePath, string destinationPath, string filter = "")
    {
        string[] filesPath = GetFiles(sourcePath, filter, SearchOption.TopDirectoryOnly);
        for (int i = 0; i < filesPath.Length; i++)
        {
            CopyFileToFolder(filesPath[i], destinationPath, filter);
        }
    }

    /// <summary>
    /// 获取子目录
    /// </summary>
    /// <param name="folderRootPath">子目录的直接父目录</param>
    /// <returns></returns>
    public static string[] GetFoldersName(string folderRootPath)
    {
        List<string> folderNameList = new List<string>();
        string[] foldersPath = Directory.GetDirectories(folderRootPath);
        for (int i = 0; i < foldersPath.Length; i++)
        {
            string folderName = foldersPath[i].Substring(foldersPath[i].LastIndexOf("/", StringComparison.Ordinal) + 1);
            if (string.IsNullOrEmpty(folderName)) continue;
            folderNameList.Add(folderName);
        }
        return folderNameList.ToArray();
    }

    /// <summary>
    /// 获取某目录下的所有文件路径 不包含meta文件
    /// </summary>
    /// <param name="rootPath">查找的根目录</param>
    /// <param name="contain">路径包含的字符串</param>
    /// <param name="hasExt">是否有扩展名</param>
    /// <returns></returns>
    public static List<string> GetFilesPath(string rootPath, string contain, bool hasExt)
    {
        List<string> pathList = new List<string>();

        string[] paths = FileUtils.GetFiles(rootPath);
        if (paths == null) return pathList;

        foreach (var path in paths)
        {
            if (path.EndsWith(".meta")) continue;

            string newPath = path.Replace("\\", "/");

            if (!string.IsNullOrEmpty(contain) && newPath.Contains(contain))
            {
                //替换路径的\
                //去除文件后缀名
                if (!hasExt) newPath = newPath.Substring(0, newPath.LastIndexOf(".", StringComparison.Ordinal));
                //截取路径
                int startIndex = newPath.LastIndexOf(contain, StringComparison.Ordinal) + contain.Length;
                newPath = newPath.Substring(startIndex);

                pathList.Add(newPath);
            }
        }
        return pathList;
    }

    /// <summary>
    /// 获取某目录下的所有文件路径的md5值 不包含meta文件
    /// </summary>
    /// <param name="rootPath">查找的根目录</param>
    /// <param name="contain">路径包含的字符串</param>
    /// <param name="hasExt">是否有扩展名</param>
    /// <returns></returns>
    public static List<string> GetMD5FilesPath(string rootPath, string contain, bool hasExt,bool isShort)
    {
        List<string> md5FilePath = new List<string>();

        List<string> filePathList = GetFilesPath(rootPath, contain, hasExt);
        foreach (var filePath in filePathList)
        {
            md5FilePath.Add(MD5Creater.Md5String(filePath, isShort));
        }
        return md5FilePath;
    }
    
}
