using System.Collections;
using System.Collections.Generic;
using System;
using BytesTools;
using UnityEditor;

namespace FileMapSystem
{
    public struct Version
    {
        //第一个不同，代表序列化的协议不一样了，每个客户端允许使用的Major只有一种，懒得管就0
        public int Version_Major;

        //这个不同代表之前的数据好清空了,是来自于不同分支的数据，建议根据版本分支来，一个客户端分支新打包版本，加1
        public int Version_Minor;

        //这个不同，代表日常数据修改,这个值可以一辈子都向上增长,默认至少1
        public int Version_Build;

        /// <summary>
        /// 返回大小，-1，0，1
        /// </summary>
        /// <returns></returns>
        public int CompareVersion(Version other)
        {
            if (this.Version_Major == other.Version_Major)
            {
                if (this.Version_Minor == other.Version_Minor)
                {
                    if (this.Version_Build == other.Version_Build)
                    {
                        return 0;
                    }
                    else
                    {
                        return this.Version_Build > other.Version_Build ? 1 : -1;
                    }
                }
                else
                {
                    return this.Version_Minor > other.Version_Minor ? 1 : -1;
                }
            }
            else
            {
                return this.Version_Major > other.Version_Major ? 1 : -1;
            }
        }


        public static bool operator ==(Version a, Version b)
        {
            // Return true if the fields match:
            return a.Version_Major == b.Version_Major && a.Version_Minor == b.Version_Minor &&
                   a.Version_Build == b.Version_Build;
        }

        public static bool operator !=(Version a, Version b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}", Version_Major, Version_Minor, Version_Build);
        }

        public override int GetHashCode()
        {
            var hashCode = 773040390;
            hashCode = hashCode * -1521134295 + Version_Major.GetHashCode();
            hashCode = hashCode * -1521134295 + Version_Minor.GetHashCode();
            hashCode = hashCode * -1521134295 + Version_Build.GetHashCode();
            return hashCode;
        }
    }

    public enum eFileMapperLoaderPosType
    {
        None,
        PersistAsset,
        StreamAsset,
        MapperAsset_PersistAsset,
        MapperAsset_StreamAsset,
    }

    public class FileMapGroupInfo
    {
        public long MD51;
        public long MD52;
        public Version Ver;
        public FileMapInfo[] AllFileMapInfo = new FileMapInfo[0];
        public static string FileExtension = ".xmf";

        /// <summary>
        /// 无论啥错误都会返回一个默认的
        /// </summary>
        /// <param name="cfgBytes"></param>
        /// <returns></returns>
        public static FileMapGroupInfo ReadFromByteBuf(byte[] cfgBytes)
        {
            var group = new FileMapGroupInfo();
            var byteBuf = ByteBuf.CreateFromBytes(cfgBytes);
            group.MD51 = byteBuf.ReadLong();
            group.MD52 = byteBuf.ReadLong();
            group.Ver = new Version()
            {
                Version_Major = byteBuf.ReadInt(),
                Version_Minor = byteBuf.ReadInt(),
                Version_Build = byteBuf.ReadInt(),
            };
            var readedMd5 = MD5Creater.MD5Struct.CreateFromLong(group.MD51, group.MD52);
            var fileMD5 = MD5Creater.GenerateMd5Code(cfgBytes, 16);
            if (!readedMd5.Equals(fileMD5))
            {
                CommonLog.Error("MD5校验不通过！");
                return null;
            }

            int FileNum = byteBuf.ReadInt();
            group.AllFileMapInfo = new FileMapInfo[FileNum];
            for (int i = 0; i < group.AllFileMapInfo.Length; i++)
            {
                group.AllFileMapInfo[i] = FileMapInfo.ReadFromByteBuf(byteBuf);
            }

            return group;
        }

        public MD5Creater.MD5Struct WriteToByteBuf(ByteBuf buf)
        {
            //标记MD5
            int firstIndex = buf.WriterIndex;
            buf.WriteLong(0);
            buf.WriteLong(0);
            //写入Ver
            buf.WriteInt(Ver.Version_Major);
            buf.WriteInt(Ver.Version_Minor);
            buf.WriteInt(Ver.Version_Build);
            //data
            buf.WriteInt(AllFileMapInfo.Length);
            for (int i = 0; i < AllFileMapInfo.Length; i++)
            {
                var info = AllFileMapInfo[i];
                info.WriteToByteBuf(buf);
            }

            int finalIndex = buf.WriterIndex;
            var bytelen = finalIndex - firstIndex;
            //计算MD5
            var fileMD5 = MD5Creater.GenerateMd5Code(buf.GetRaw(), firstIndex + 16, bytelen - 16);
            buf.SetWriterIndex(firstIndex);
            buf.WriteLong(fileMD5.MD51);
            buf.WriteLong(fileMD5.MD52);
            buf.SetWriterIndex(finalIndex);

            return fileMD5;
        }
    }

    public class FileMapInfo
    {
        public string FileName;
        public long FileData_MD51;
        public long FileData_MD52;
        public int FileTag;
        public int Offset;

        public int Len;

        //被映射的Block名称
        public long MapedFileName_MD51;

        public long MapedFileName_MD52;

        //public long MappedFileLong ;
        public int FileVersion = 0; //0代表默认版本
        public static string FileExtension = ".wmv";

        public static FileMapInfo ReadFromByteBuf(ByteBuf cfg)
        {
            var info = new FileMapInfo();
            info.FileName = cfg.ReadUTF8();
            info.FileData_MD51 = cfg.ReadLong();
            info.FileData_MD52 = cfg.ReadLong();
            info.FileTag = cfg.ReadInt();
            info.Offset = cfg.ReadInt();
            info.Len = cfg.ReadInt();
            info.MapedFileName_MD51 = cfg.ReadLong();
            info.MapedFileName_MD52 = cfg.ReadLong();
            //info.MappedFileLong = cfg.ReadLong();
            return info;
        }

        public void WriteToByteBuf(ByteBuf cfg)
        {
            cfg.WriteUTF8(FileName);
            cfg.WriteLong(FileData_MD51);
            cfg.WriteLong(FileData_MD52);
            cfg.WriteInt(FileTag);
            cfg.WriteInt(Offset);
            cfg.WriteInt(Len);
            cfg.WriteLong(MapedFileName_MD51);
            cfg.WriteLong(MapedFileName_MD52);
            //cfg.WriteLong(MappedFileLong);
        }

        public string GetMappedFileName()
        {
            var name = string.Format("{0}{1}{2}", MD5Creater.MD5LongToHexStr(MapedFileName_MD51)
                , MD5Creater.MD5LongToHexStr(MapedFileName_MD52), FileExtension);
            return name;
        }

        public bool IsSame(FileMapInfo info)
        {
            if (info == null) return false;
            return this.FileName == info.FileName && this.FileData_MD51 == info.FileData_MD51 &&
                   this.FileData_MD52 == info.FileData_MD52;
        }

        public FileMapInfo Clone()
        {
            var newF = new FileMapInfo();
            newF.FileName = FileName;
            newF.FileData_MD51 = FileData_MD51;
            newF.FileData_MD52 = FileData_MD52;
            newF.FileTag = FileTag;
            newF.Offset = Offset;
            newF.Len = Len;
            newF.MapedFileName_MD51 = MapedFileName_MD51;
            newF.MapedFileName_MD52 = MapedFileName_MD52;
            //newF.MappedFileLong = MappedFileLong;
            return newF;
        }
    }

    public class FileMapSystemTagFindResultHelper
    {
        public int[] SearchTags;

        public List<FileMapInfo> AllInfos = new List<FileMapInfo>();

        public List<WWWFileDownloader.DownloadFileInfo> Downloaded = new List<WWWFileDownloader.DownloadFileInfo>();

        public List<WWWFileDownloader.DownloadFileInfo> Missed = new List<WWWFileDownloader.DownloadFileInfo>();

        public long AllSize
        {
            get { return DownloadedSize + MissedSize; }
        }

        public long DownloadedSize;

        public long MissedSize;

        public bool ContainsTag(int tag)
        {
            foreach (var t in SearchTags)
            {
                if (t == tag)
                {
                    return true;
                }
            }

            return false;
        }

        public static FileMapSystemTagFindResultHelper FindInfos(FileMapSystem sys, int[] tags)
        {
            var res = new FileMapSystemTagFindResultHelper();
            res.SearchTags = tags;
            var dictDowned = new Dictionary<string, int>();
            var dictMiss = new Dictionary<string, int>();
            foreach (var info in sys.FileInfo.AllFileMapInfo)
            {
                if (res.ContainsTag(info.FileTag))
                {
                    res.AllInfos.Add(info);
                    var mappedName = info.GetMappedFileName();
                    UnityFileLoaderHelper.eFileLoaderPosType pos;
                    if (UnityFileLoaderHelper.IsFileExist(sys.Dir, mappedName, out pos))
                    {
                        if (!dictDowned.ContainsKey(mappedName))
                        {
                            dictDowned[mappedName] = info.Len;
                        }
                        else
                        {
                            dictDowned[mappedName] = dictDowned[mappedName] + info.Len;
                        }
                    }
                    else
                    {
                        if (!dictMiss.ContainsKey(mappedName))
                        {
                            dictMiss[mappedName] = info.Len;
                        }
                        else
                        {
                            dictMiss[mappedName] = dictMiss[mappedName] + info.Len;
                        }
                    }
                }
            }

            foreach (var info in dictDowned)
            {
                var wd = new WWWFileDownloader.DownloadFileInfo()
                {
                    FileName = info.Key,
                    FileSize = info.Value
                };
                res.Downloaded.Add(wd);
                res.DownloadedSize += info.Value;
            }

            foreach (var info in dictMiss)
            {
                var wd = new WWWFileDownloader.DownloadFileInfo()
                {
                    FileName = info.Key,
                    FileSize = info.Value
                };
                res.Missed.Add(wd);
                res.MissedSize += info.Value;
            }

            return res;
        }
    }
}