using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FileMapSystem
{
    public class FileMapGroupDesc
    {
        public long Md51;
        public long Md52;
        public string FileName;
        public long Len;
        public List<FileMapInfo> FileMapInfos = new List<FileMapInfo>();
    }

    /// <summary>
    /// 统一的FilePack系统，把文件重新映射为一堆大文件加一个配置文件,大文件以MD5 short命名
    /// </summary>
    public class FileMapSystem
    {
        public FileMapGroupInfo FileInfo = new FileMapGroupInfo();

        private Dictionary<string, FileMapInfo> FileNameToMD5Name = new Dictionary<string, FileMapInfo>();

        public FileMapSystem(string dir)
        {
            Dir = dir;
        }

        public Version Version => FileInfo.Ver;

        //对应目录
        public string Dir { get; private set; }

        private string _FileMapInfoFileName;

        public string FileMapInfoFileName
        {
            get
            {
                if (_FileMapInfoFileName == null)
                {
                    if (Dir.EndsWith("/"))
                    {
                        _FileMapInfoFileName = Path.GetFileName(Dir.Substring(0, Dir.Length - 1))
                                               + FileMapGroupInfo.FileExtension;
                    }
                    else
                    {
                        _FileMapInfoFileName = Path.GetFileName(Dir) + FileMapGroupInfo.FileExtension;
                    }
                }

                return _FileMapInfoFileName;
            }
        }

        /// <summary>
        /// 返回大小，-1，0，1
        /// </summary>
        /// <returns></returns>
        public int CompareVersion(FileMapSystem sys)
        {
            return FileInfo.Ver.CompareVersion(sys.FileInfo.Ver);
        }

        /// <summary>
        /// 比较MD5确定是否一样
        /// </summary>
        /// <returns></returns>
        public bool CheckSameContent(FileMapSystem sys)
        {
            return FileInfo.MD51 == sys.FileInfo.MD51 && FileInfo.MD52 == sys.FileInfo.MD52;
        }

        /// <summary>
        /// 从本地StreamAsset初始化FileMap信息,会尝试合并Persist的补丁
        /// </summary>
        public void InitFromLocalFile()
        {
            var isFromPersist = true;
            FileMapSystem PFM = null;
            try
            {
                PFM = CreateFromPersistAssetFile(Dir);
                //检查是否读取到了坏文件
                if (PFM?.FileInfo == null || PFM.FileInfo.Ver.Version_Build == 0)
                {
                    isFromPersist = false;
                }
            }
            catch (Exception e)
            {
                isFromPersist = false;
                CommonLog.Error(e);
            }

            if (isFromPersist)
            {
                InitFileMapInfo(PFM);
            }
            else
            {
                //不行就读取本地
                var SFM = CreateFromStreamAssetFile(Dir);
                if (SFM == null)
                {
                    CommonLog.Error("初始化AB系统配置表失败");
                }
                else
                {
                    InitFileMapInfo(SFM);
                }
            }
        }

        public static FileMapSystem CreateFromPersistAssetFile(string Dir)
        {
            var FileMap = new FileMapSystem(Dir);
            var bs = UnityPersistFileHelper.ReadPersistAssetFileAllBytes(Dir, FileMap.FileMapInfoFileName);
            if (bs == null || bs.Length == 0)
            {
                return null;
            }

            FileMap.InitFileMapInfo(bs);
            return FileMap;
        }

        public static FileMapSystem CreateFromStreamAssetFile(string Dir)
        {
            var FileMap = new FileMapSystem(Dir);
            var bs = UnityStreamingFileHelper.ReadStreamAssetFileAllBytes(Dir, FileMap.FileMapInfoFileName);
            if (bs == null || bs.Length == 0)
            {
                //Debug.LogError("本地读取到数据长度为0,Dir:"+Dir);
                return null;
            }

            FileMap.InitFileMapInfo(bs);
            //Debug.LogError("初始化Map成功,len:"+bs.Length+"Dir:"+Dir);
            return FileMap;
        }

        public bool InitFileMapInfo(byte[] infoBytes)
        {
            try
            {
                //MD5校验
                var fi = FileMapGroupInfo.ReadFromByteBuf(infoBytes);
                if (fi == null) return false;
                FileInfo = fi;
                RecalFileInfo2NameMap();
                return true;
            }
            catch (Exception e)
            {
                CommonLog.Error(e);
            }

            return false;
        }

        public void RecalFileInfo2NameMap()
        {
            FileNameToMD5Name.Clear();
            foreach (var f in FileInfo.AllFileMapInfo)
            {
                FileNameToMD5Name[f.FileName] = f;
            }
        }

        public void InitFileMapInfo(FileMapSystem sys)
        {
            if (sys == null) return;
            Dir = sys.Dir;
            _FileMapInfoFileName = sys.FileMapInfoFileName;
            FileInfo = sys.FileInfo;
            RecalFileInfo2NameMap();
        }

        /// <summary>
        /// 根据文件夹和文件名称，自动判断Mapper或者在Stream任意位置，总之读取到你想要的Bundle
        /// </summary>
        public AssetBundle LoadBundleFromFile(string fileName)
        {
            eFileMapperLoaderPosType pos;
            return LoadBundleFromFile(fileName, out pos);
        }

        /// <summary>
        /// 根据文件夹和文件名称，自动判断Mapper或者在Stream任意位置，总之读取到你想要的Bundle
        /// </summary>
        public AssetBundle LoadBundleFromFile(string fileName, out eFileMapperLoaderPosType pos, uint crc = 0U)
        {
            pos = eFileMapperLoaderPosType.None;
            var info = GetFileInfo(fileName);
            if (info != null)
            {
                UnityFileLoaderHelper.eFileLoaderPosType filePos;
                var asb = UnityFileLoaderHelper.ReadAssetBundle(Dir, info.GetMappedFileName(), (ulong) info.Offset,
                    out filePos, crc);
                if (filePos == UnityFileLoaderHelper.eFileLoaderPosType.PersistAsset)
                {
                    pos = eFileMapperLoaderPosType.MapperAsset_PersistAsset;
                }

                if (filePos == UnityFileLoaderHelper.eFileLoaderPosType.StreamAsset)
                {
                    pos = eFileMapperLoaderPosType.MapperAsset_StreamAsset;
                }

                return asb;
            }
            else
            {
                UnityFileLoaderHelper.eFileLoaderPosType filePos;
                var asb = UnityFileLoaderHelper.ReadAssetBundle(Dir, fileName, 0, out filePos, crc);
                if (filePos == UnityFileLoaderHelper.eFileLoaderPosType.PersistAsset)
                {
                    pos = eFileMapperLoaderPosType.PersistAsset;
                }

                if (filePos == UnityFileLoaderHelper.eFileLoaderPosType.StreamAsset)
                {
                    pos = eFileMapperLoaderPosType.StreamAsset;
                }

                return asb;
            }
        }

        /// <summary>
        /// 根据文件夹和文件名称，自动判断Mapper或者在Stream任意位置，总之读取到你想要的Bundle
        /// </summary>
        public AssetBundleCreateRequest LoadBundleFromFileAsync(string fileName)
        {
            eFileMapperLoaderPosType pos;
            return LoadBundleFromFileAsync(fileName, out pos);
        }

        /// <summary>
        /// 根据文件夹和文件名称，自动判断Mapper或者在Stream任意位置，总之读取到你想要的Bundle
        /// </summary>
        public AssetBundleCreateRequest LoadBundleFromFileAsync(string fileName, out eFileMapperLoaderPosType pos,
            uint crc = 0U)
        {
            pos = eFileMapperLoaderPosType.None;
            var info = GetFileInfo(fileName);
            if (info != null)
            {
                UnityFileLoaderHelper.eFileLoaderPosType filePos;
                var asb = UnityFileLoaderHelper.ReadAssetBundleAsync(Dir, info.GetMappedFileName(), (ulong) info.Offset,
                    out filePos, crc);
                if (filePos == UnityFileLoaderHelper.eFileLoaderPosType.PersistAsset)
                {
                    pos = eFileMapperLoaderPosType.MapperAsset_PersistAsset;
                }

                if (filePos == UnityFileLoaderHelper.eFileLoaderPosType.StreamAsset)
                {
                    pos = eFileMapperLoaderPosType.MapperAsset_StreamAsset;
                }

                return asb;
            }
            else
            {
                UnityFileLoaderHelper.eFileLoaderPosType filePos;
                var asb = UnityFileLoaderHelper.ReadAssetBundleAsync(Dir, fileName, 0, out filePos, crc);
                if (filePos == UnityFileLoaderHelper.eFileLoaderPosType.PersistAsset)
                {
                    pos = eFileMapperLoaderPosType.PersistAsset;
                }

                if (filePos == UnityFileLoaderHelper.eFileLoaderPosType.StreamAsset)
                {
                    pos = eFileMapperLoaderPosType.StreamAsset;
                }

                return asb;
            }
        }


        #region File读取

        /// <summary>
        /// 根据文件夹和文件名称，自动判断Mapper或者在Stream任意位置，总之读取到你想要的文件
        /// 文件读取顺序，Mapper Persist,Mapper Stream,源文件 Persist,源文件 Stream
        /// PS!本API没有做单文件边界判断，读取的时候需要通过FileInfo自行判断是否读多了，避免读出超过单文件的边界到下个文件，产生解析错误。
        /// </summary>
        public Stream GetFileStream(string fileName)
        {
            eFileMapperLoaderPosType pos;
            return GetFileStream(fileName, out pos);
        }

        /// <summary>
        /// 根据文件夹和文件名称，自动判断Mapper或者在Stream任意位置，总之读取到你想要的文件
        /// 文件读取顺序，Mapper Persist,Mapper Stream,源文件 Persist,源文件 Stream
        /// PS!本API没有做单文件边界判断，读取的时候需要通过FileInfo自行判断是否读多了，避免读出超过单文件的边界到下个文件，产生解析错误。
        /// </summary>
        public Stream GetFileStream(string fileName, out eFileMapperLoaderPosType pos)
        {
            pos = eFileMapperLoaderPosType.None;
            var info = GetFileInfo(fileName);
            if (info != null)
            {
                UnityFileLoaderHelper.eFileLoaderPosType filePos;
                var bs = UnityFileLoaderHelper.ReadFileByStream(Dir, info.GetMappedFileName(), out filePos,
                    info.Offset);
                if (filePos == UnityFileLoaderHelper.eFileLoaderPosType.PersistAsset)
                {
                    pos = eFileMapperLoaderPosType.MapperAsset_PersistAsset;
                }

                if (filePos == UnityFileLoaderHelper.eFileLoaderPosType.StreamAsset)
                {
                    pos = eFileMapperLoaderPosType.MapperAsset_StreamAsset;
                }

                return bs;
            }
            else
            {
                UnityFileLoaderHelper.eFileLoaderPosType filePos;
                var bs = UnityFileLoaderHelper.ReadFileByStream(Dir, fileName, out filePos);
                if (filePos == UnityFileLoaderHelper.eFileLoaderPosType.PersistAsset)
                {
                    pos = eFileMapperLoaderPosType.PersistAsset;
                }

                if (filePos == UnityFileLoaderHelper.eFileLoaderPosType.StreamAsset)
                {
                    pos = eFileMapperLoaderPosType.StreamAsset;
                }

                return bs;
            }
        }

        /// <summary>
        /// 根据文件夹和文件名称，自动判断Mapper或者在Stream任意位置，总之读取到你想要的文件
        /// </summary>
        public byte[] GetFileBytes(string fileName)
        {
            eFileMapperLoaderPosType pos;
            return GetFileBytes(fileName, out pos);
        }

        /// <summary>
        /// 根据文件夹和文件名称，自动判断Mapper或者在Stream任意位置，总之读取到你想要的文件
        /// 文件读取顺序，Mapper Persist,Mapper Stream,源文件Persist,源文件Stream
        /// </summary>
        public byte[] GetFileBytes(string fileName, out eFileMapperLoaderPosType pos)
        {
            pos = eFileMapperLoaderPosType.None;
            var info = GetFileInfo(fileName);
            if (info != null)
            {
                UnityFileLoaderHelper.eFileLoaderPosType filePos;
                var bs = UnityFileLoaderHelper.ReadFileAllBytes(Dir, info.GetMappedFileName(), info.Offset, info.Len,
                    out filePos);
                if (filePos == UnityFileLoaderHelper.eFileLoaderPosType.PersistAsset)
                {
                    pos = eFileMapperLoaderPosType.MapperAsset_PersistAsset;
                }

                if (filePos == UnityFileLoaderHelper.eFileLoaderPosType.StreamAsset)
                {
                    pos = eFileMapperLoaderPosType.MapperAsset_StreamAsset;
                }

                return bs;
            }
            else
            {
                UnityFileLoaderHelper.eFileLoaderPosType filePos;
                var bs = UnityFileLoaderHelper.ReadFileAllBytes(Dir, fileName, out filePos);
                if (filePos == UnityFileLoaderHelper.eFileLoaderPosType.PersistAsset)
                {
                    pos = eFileMapperLoaderPosType.PersistAsset;
                }

                if (filePos == UnityFileLoaderHelper.eFileLoaderPosType.StreamAsset)
                {
                    pos = eFileMapperLoaderPosType.StreamAsset;
                }

                return bs;
            }
        }

        /// <summary>
        /// 得到被映射后的文件名称,文件不存在返回空
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public string GetMappedFilePath(string fileName, out eFileMapperLoaderPosType pos)
        {
            pos = eFileMapperLoaderPosType.None;
            var info = GetFileInfo(fileName);
            if (info != null)
            {
                UnityFileLoaderHelper.eFileLoaderPosType filePos;
                var has = UnityFileLoaderHelper.IsFileExist(Dir, info.GetMappedFileName(), out filePos);
                if (filePos == UnityFileLoaderHelper.eFileLoaderPosType.PersistAsset)
                {
                    pos = eFileMapperLoaderPosType.MapperAsset_PersistAsset;
                    return UnityPersistFileHelper.GetPersistAssetFilePath(Dir, info.GetMappedFileName());
                }

                if (filePos == UnityFileLoaderHelper.eFileLoaderPosType.StreamAsset)
                {
                    pos = eFileMapperLoaderPosType.MapperAsset_StreamAsset;
                    return UnityStreamingFileHelper.GetStreamAssetFilePath(Dir, info.GetMappedFileName());
                }

                return string.Empty;
            }
            else
            {
                UnityFileLoaderHelper.eFileLoaderPosType filePos;
                var has = UnityFileLoaderHelper.IsFileExist(Dir, fileName, out filePos);
                if (filePos == UnityFileLoaderHelper.eFileLoaderPosType.PersistAsset)
                {
                    pos = eFileMapperLoaderPosType.PersistAsset;
                    return UnityPersistFileHelper.GetPersistAssetFilePath(Dir, fileName);
                }

                if (filePos == UnityFileLoaderHelper.eFileLoaderPosType.StreamAsset)
                {
                    pos = eFileMapperLoaderPosType.StreamAsset;
                    return UnityStreamingFileHelper.GetStreamAssetFilePath(Dir, fileName);
                }

                return string.Empty;
            }
        }

        #endregion

        public Dictionary<string, FileMapGroupDesc> GetMissFileMaps(FileMapSystem other)
        {
            var dic = new Dictionary<string, FileMapGroupDesc>();

            void Add2Dic(FileMapInfo mapInfo)
            {
                var key = mapInfo.GetMappedFileName();
                //判断本地成功下载了没有
                
                if (!dic.TryGetValue(key, out var desc))
                {
                    desc = new FileMapGroupDesc {Md51 = mapInfo.MapedFileName_MD51, Md52 = mapInfo.MapedFileName_MD52};
                    dic.Add(key, desc);
                }

                desc.FileMapInfos.Add(mapInfo);
            }

            foreach (var fileMapInfoIter in other.FileNameToMD5Name)
            {
                var fileName = fileMapInfoIter.Key;
                var newInfo = fileMapInfoIter.Value;
                if (!FileNameToMD5Name.TryGetValue(fileName, out var oldInfo))
                {
                    Add2Dic(newInfo);
                }
                else
                {
                    if (oldInfo.FileData_MD51 != newInfo.FileData_MD51 ||
                        oldInfo.FileData_MD52 != newInfo.FileData_MD52)
                    {
                        Add2Dic(newInfo);
                    }
                }
            }

            //更新大小
            foreach (var fileMapInfoIter in other.FileNameToMD5Name)
            {
                var newInfo = fileMapInfoIter.Value;
                var key = newInfo.GetMappedFileName();
                if (dic.TryGetValue(key, out var desc))
                {
                    desc.Len += newInfo.Len;
                }
            }

            return dic;
        }


        /// <summary>
        /// 一般0为Default
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public FileMapSystemTagFindResultHelper FindFileByTags(params int[] tags)
        {
            var helper = FileMapSystemTagFindResultHelper.FindInfos(this, tags);
            return helper;
        }


        /// <summary>
        /// 删除非当前版本使用的资源
        /// </summary>
        public void DeleteAllOldFile()
        {
            try
            {
                var allDirFiles = UnityPersistFileHelper.GetPersistAssetFileList(Dir, "*" + FileMapInfo.FileExtension);
                var needFiles = new List<string>(FileInfo.AllFileMapInfo.Length);
                for (int i = 0; i < FileInfo.AllFileMapInfo.Length; i++)
                {
                    needFiles.Add(FileInfo.AllFileMapInfo[i].GetMappedFileName());
                }

                var allNeedDelFiles = new List<string>();
                foreach (var file in allDirFiles)
                {
                    if (!needFiles.Contains(file))
                    {
                        allNeedDelFiles.Add(file);
                    }
                }

                if (allNeedDelFiles.Count > 0) UnityPersistFileHelper.DeletePersistAssetFileList(Dir, allNeedDelFiles);
            }
            catch (Exception e)
            {
                CommonLog.Error(e);
            }
        }

        public void DeletePersistFileByInfos(List<FileMapInfo> infos)
        {
            if (infos == null || infos.Count == 0) return;
            try
            {
                var needFiles = new List<string>(infos.Count);
                foreach (var info in infos)
                {
                    if (FileNameToMD5Name.ContainsKey(info.FileName))
                    {
                        needFiles.Add(info.FileName);
                    }
                }

                UnityPersistFileHelper.DeletePersistAssetFileList(Dir, needFiles);
            }
            catch (Exception e)
            {
                CommonLog.Error(e);
            }
        }

        public void ClearAllPersistDownloaded()
        {
            try
            {
                var allDirFiles = UnityPersistFileHelper.GetPersistAssetFileList(Dir, "*" + FileMapInfo.FileExtension);
                if (allDirFiles.Length > 0)
                    UnityPersistFileHelper.DeletePersistAssetFileList(Dir, new List<string>(allDirFiles));
            }
            catch (Exception e)
            {
                CommonLog.Error(e);
            }
        }

        public FileMapInfo GetFileInfo(string fileName)
        {
            FileMapInfo file;
            if (FileNameToMD5Name.TryGetValue(fileName, out file))
            {
                return file;
            }

            return null;
        }
    }
}