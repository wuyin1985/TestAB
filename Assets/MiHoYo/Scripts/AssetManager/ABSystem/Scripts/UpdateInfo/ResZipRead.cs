using System;
using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

//快速读取streamingAsset目录下文件的工具类
public class ResZipRead
{
#if UNITY_ANDROID && !UNITY_EDITOR
    const string LIB = "zipfile";

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    static extern IntPtr minizip_open(string pkgpath, string prepath);
    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    static extern void minizip_close(IntPtr package);
    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    static extern int minizip_exist(IntPtr package, uint id);
    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    static extern uint minizip_length(IntPtr package, uint id);
    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    static extern uint minizip_read(IntPtr package, uint id, IntPtr output, uint len);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
    IntPtr apkfile;
#else
    string prepath;
#endif

    private static ResZipRead _instance = null;

    public static ResZipRead Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new ResZipRead();
            }
            return _instance;
        }
    }

    public ResZipRead()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        apkfile = minizip_open(Application.dataPath, "assets/");
#else
        prepath = Application.streamingAssetsPath + "/";
#endif
    }

    ~ResZipRead()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        minizip_close(apkfile);
#endif
    }
    //获取指定路径文件的id
    private uint FNV(string path)
    {
        uint num = 2166136261U;
        byte[] bytes = Encoding.UTF8.GetBytes(path);
        for (int i = 0, j = bytes.Length; i < j; ++i)
        {
            byte c = bytes[i];
            if (c == 0)
                break;
            if (c == (byte)'\\')
                c = (byte)'/';
            num ^= c;
            num *= 16777619U;
        }
        return num;
    }
    //获取streamingAsset目录下文件的长度
    public uint Length(string path)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        uint id = FNV(path);
        if (minizip_exist(apkfile, id) != 0)
        {
            return minizip_length(apkfile, id);
        }
        return 0;
#else
        try
        {
            FileStream stream = File.Open(prepath + path, FileMode.Open);
            return (uint)stream.Length;
        }
        catch (Exception)
        {
            return 0;
        }
#endif
    }
    //在streamAsset目录下是否存在
    public bool Exist(string path)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return minizip_exist(apkfile, FNV(path)) != 0;
#else
        return File.Exists(prepath + path);
#endif
    }
    //通过读取zip的方式获取streamingAsset下文件的字节数据
    public byte[] Read(string path)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        uint id = FNV(path);
        if (minizip_exist(apkfile, id) != 0)
        {
            uint len = minizip_length(apkfile, id);
            byte[] bytes = new byte[len];
            GCHandle gch = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            IntPtr output = Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0);
            minizip_read(apkfile, id, output, len);
            gch.Free();
            return bytes;
        }
        return File.ReadAllBytes(Application.dataPath + "/" + path);
#else
        return File.ReadAllBytes(prepath + path);
#endif
    }
}
