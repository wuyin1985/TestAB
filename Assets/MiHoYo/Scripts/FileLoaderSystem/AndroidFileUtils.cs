#if UNITY_ANDROID 
using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

public class AndroidFileUtils
{
    private const string ACTIVITY_JAVA_CLASS = "com.unity3d.player.UnityPlayer";

    private static AndroidJavaObject assetManager;
    private static Dictionary<string, string[]> allAssetFileDict = new Dictionary<string, string[]>();

    private static HashSet<int> AttchedThreadIDS = new HashSet<int>();
    private static int MainThreadID = 0;

    protected static AndroidJavaObject AssetManager
    {
        get
        {
            int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            if (!AttchedThreadIDS.Contains(threadId))
            {
                AttchedThreadIDS.Add(threadId);
                AndroidJNI.AttachCurrentThread();
            }
            if (assetManager != null)
                return assetManager;

            using (AndroidJavaClass activityClass = new AndroidJavaClass(ACTIVITY_JAVA_CLASS))
            {
                using (var context = activityClass.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    assetManager = context.Call<AndroidJavaObject>("getAssets");
                }
            }
            return assetManager;
        }
    }

    public static void Init()
    {
        int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        MainThreadID = threadId;
        if (!AttchedThreadIDS.Contains(threadId))
        {
            AttchedThreadIDS.Add(threadId);
        }
        var ass = AssetManager;
    }

    protected static string GetAssetFilePath(string path)
    {
        int start = path.LastIndexOf("!/assets/");
        if (start < 0)
        {
            int start2 = path.LastIndexOf("!assets/");
            if (start2 < 0)
            {
                return path;
            }
            else
            {
                return path.Substring(start2 + 8);
            }
        }

        return path.Substring(start + 9);
    }

    public static Stream OpenFileStream(string filename, int offset = 0)
    {
        var stream = new InputStreamWrapper(AssetManager.Call<AndroidJavaObject>("open", GetAssetFilePath(filename)));
        if (offset > 0) stream.Seek(offset, SeekOrigin.Begin);
        return stream;
    }

    public static byte[] ReadFile(string path, int offset = 0, int len = -1)
    {
        //Debug.LogError("path:" + path + "offset:" + offset + "len:" + len);
        using (var stream = OpenFileStream(path, offset))
        {
            if (stream == null)
                throw new FileNotFoundException();
            if (len == -1) len = (int)(stream.Length - offset);
            byte[] buffer = new byte[len];
            stream.Read(buffer, 0, len);
            return buffer;
        }
    }

    public static string ReadTextFile(string path)
    {
        var strBytes = ReadFile(path);
        if (strBytes == null || strBytes.Length == 0)
        {
            return "";
        }
        else
        {
            return System.Text.Encoding.UTF8.GetString(strBytes).TrimEnd('\0');
        }
    }

    /// <summary>
    /// 数组类型的转换
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    private static AndroidJavaObject javaArrayFromCS(string[] values)
    {
        AndroidJavaClass arrayClass = new AndroidJavaClass("java.lang.reflect.Array");
        AndroidJavaObject arrayObject = arrayClass.CallStatic<AndroidJavaObject>("newInstance",
            new AndroidJavaClass("java.lang.String"),
            values.Length);
        for (int i = 0; i < values.Length; ++i)
        {
            arrayClass.CallStatic("set", arrayObject, i,
                new AndroidJavaObject("java.lang.String", values[i]));
        }

        return arrayObject;
    }


    /// <summary>
    /// 数组类型的转换
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    private static string[] javaArrayToCS(AndroidJavaObject arrayobject)
    {
        try
        {
            AndroidJavaClass arrayClass = new AndroidJavaClass("java.lang.reflect.Array");
            {
                int len = arrayClass.CallStatic<int>("getLength", arrayobject);

                string[] csarr = new string[len];
                for (int i = 0; i < len; ++i)
                {
                    var str = arrayClass.CallStatic<string>("get", arrayobject, i);
                    csarr[i] = str;
                }
                return csarr;
            }
        }
        catch (Exception e)
        {
            CommonLog.Error(e);
        }
        return new string[] { };
    }

    /// <summary>
    /// 获取Android Asset文件目录下所有的文件列表，文件路径应该为GetAssetFilePath处理过的
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public static string[] GetAndroidAssetFileList(string pathroot)
    {
        string[] filles = null;
        if (!allAssetFileDict.TryGetValue(pathroot, out filles))
        {
            try
            {
                using (AndroidJavaObject arrayobject = AssetManager.Call<AndroidJavaObject>("list", pathroot))
                {
                    filles = javaArrayToCS(arrayobject);
                }
                allAssetFileDict[pathroot] = filles;
            }
            catch (Exception e)
            {
                CommonLog.Error(e);
                return new string[] { };
            }
        }
        return filles;
    }

    public static bool CheckExistsInAndroidAsset(string path)
    {
        path = GetAssetFilePath(path);
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }
        try
        {
            var pathRoot = System.IO.Path.GetDirectoryName(path);
            var fileName = System.IO.Path.GetFileName(path);
            var allFile = GetAndroidAssetFileList(pathRoot);
            return allFile.ContainsIgnoreCase(fileName);
        }
        catch (Exception e) { CommonLog.Error(e); }

        return false;
    }

    public class InputStreamWrapper : Stream
    {
        private object _lock = new object();
        private long length = 0;
        private long position = 0;
        private AndroidJavaObject inputStream;
        public InputStreamWrapper(AndroidJavaObject inputStream)
        {
            this.inputStream = inputStream;
            this.length = inputStream.Call<int>("available");
        }

        public override bool CanRead { get { return this.position < this.length; } }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return this.length; }
        }

        public override long Position
        {
            get { return this.position; }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (_lock)
            {
                int ret = 0;
                IntPtr array = IntPtr.Zero;
                try
                {

                    array = AndroidJNI.NewSByteArray(count);
                    var method = AndroidJNIHelper.GetMethodID(inputStream.GetRawClass(), "read", "([B)I");
                    ret = AndroidJNI.CallIntMethod(inputStream.GetRawObject(), method, new[] { new jvalue() { l = array } });
                    var data = AndroidJNI.FromSByteArray(array);
                    if (ret >= 0)
                    {
                        //Array.Copy(data, 0, buffer, offset, ret);
                        Buffer.BlockCopy(data, 0, buffer, offset, ret);
                        position += ret;
                    }
                    else
                    {
                        return 0;
                    }
                }
                finally
                {
                    if (array != IntPtr.Zero)
                        AndroidJNI.DeleteLocalRef(array);
                }
                return ret;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var size = inputStream.Call<long>("skip", offset);
            position = offset;
            //Debug.LogError("Seek:" + offset + "return:" + size);
            return size;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (inputStream != null)
            {
                inputStream.Call("close");
                inputStream.Dispose();
                inputStream = null;
            }
            position = 0;
        }
    }
}
#endif