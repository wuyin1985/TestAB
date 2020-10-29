using System;
using UnityEngine;
using System.Collections;
using System.Security.Cryptography;
using System.IO;
using System.Text;

public static class MD5Creater
{
    private static MD5 md5;
    public static MD5 Md5Instance
    {
        get
        {
            if (md5 == null)
                md5 = new MD5CryptoServiceProvider(); //MD5.Create()
            return md5;
        }
    }


    /// <summary>
    /// 文件的md5
    /// </summary>
    /// <param name="filePathName"></param>
    /// <returns></returns>
    public static string MD5File(string filePathName, bool isShort = false)
    {
        /*
        MD5CryptoServiceProvider md5Generator = new MD5CryptoServiceProvider();
        FileStream file = new FileStream(filePathName, FileMode.Open, FileAccess.Read, FileShare.Read);
        byte[] md5CodeBytes = md5Generator.ComputeHash(file);
        string strMD5Code = BitConverter.ToString(md5CodeBytes);
        file.Close();
        return strMD5Code;
        */
        using (FileStream file = new FileStream(filePathName, FileMode.Open))
        {
            using (MD5 md5 = new MD5CryptoServiceProvider())
            {
                byte[] retVal = md5.ComputeHash(file);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }

                if (isShort)
                {
                    return sb.ToString(8, 16);
                }
                return sb.ToString();
            }
        }
    }


    /// <summary>
    /// 文件的md5
    /// </summary>
    /// <param name="filePathName"></param>
    /// <returns></returns>
    public static long MD5FileShortLong(string filePathName)
    {
        /*
        MD5CryptoServiceProvider md5Generator = new MD5CryptoServiceProvider();
        FileStream file = new FileStream(filePathName, FileMode.Open, FileAccess.Read, FileShare.Read);
        byte[] md5CodeBytes = md5Generator.ComputeHash(file);
        string strMD5Code = BitConverter.ToString(md5CodeBytes);
        file.Close();
        return strMD5Code;
        */
        using (FileStream file = new FileStream(filePathName, FileMode.Open))
        {
            using (MD5 md5 = new MD5CryptoServiceProvider())
            {
                byte[] retVal = md5.ComputeHash(file);
                long num = 0;
                for (int ix = 4; ix < 12; ++ix)
                {
                    num <<= 8;
                    num |= (long)(retVal[ix] & 0xff);
                }
                return num;
            }
        }
    }

    public static MD5Struct GenerateMd5Code(byte[] bytes, int offset = 0, int len = -1)
    {
        if (len == -1)
        {
            len = bytes.Length - offset;
        }
        /*
        MD5CryptoServiceProvider md5Generator = new MD5CryptoServiceProvider();
        byte[] md5CodeBytes = md5Generator.ComputeHash(bytes);
        return BitConverter.ToString(md5CodeBytes);
        */
        if (len <= 0) return new MD5Struct();
        using (MD5 md5 = new MD5CryptoServiceProvider())
        {
            byte[] retVal = md5.ComputeHash(bytes, offset, len);
            return MD5Struct.CreateFromBytes(retVal);
        }
    }
    /// <summary>
    /// string md5加密
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static string Md5String(string source, bool isShort)
    {
        var md5Struct = Md5Struct(Encoding.UTF8.GetBytes(source));

        return md5Struct.GetMD5Str(isShort);
    }


    public static string MD5LongToHexStr(long md5)
    {

        byte[] num = new byte[8];
        for (int ix = 7; ix >= 0; --ix)
        {
            num[ix] = (byte)(md5 & 0xff);
            md5 >>= 8;
        }
        string md5str = string.Empty;
        md5str = System.BitConverter.ToString(num).Replace("-", string.Empty).ToLower();
        return md5str;
    }

    /// <summary>
    /// byte[] md5加密
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static MD5Struct Md5Struct(byte[] inputs)
    {
        byte[] result = Md5Instance.ComputeHash(inputs);

        return MD5Struct.CreateFromBytes(result);
    }

    /// <summary>
    /// byte[] md5加密
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static MD5Struct Md5Struct(string source)
    {
        byte[] result = Md5Instance.ComputeHash(Encoding.UTF8.GetBytes(source));

        return MD5Struct.CreateFromBytes(result);
    }


    /// <summary>
    /// byte[] md5加密
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static byte[] Md5Bytes(byte[] inputs)
    {
        byte[] result = Md5Instance.ComputeHash(inputs);
        return result;
    }


    public struct MD5Struct
    {
        public long MD51;
        public long MD52;

        public long GetShortMD5Long()
        {
            return MD51;
        }

        public string GetMD5Str(bool isShort)
        {
            if (isShort)
            {
                return MD5LongToHexStr(MD51);
            }
            var name = MD5LongToHexStr(MD51) + MD5LongToHexStr(MD52);

            return name;
        }
        public static MD5Struct CreateFromHexStr(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                CommonLog.Error("输入参数HEX字符串不正确");
                return new MD5Struct();
            }
            var bs = Convert.FromBase64String(hex);
            if (bs.Length == 16)
            {
                return CreateFromBytes(bs);
            }
            else
            {
                CommonLog.Error("输入参数HEX字符串不正确");
                return new MD5Struct();
            }
        }
        public static MD5Struct CreateFromLong(long md5short, long md5shortafter)
        {
            return new MD5Struct()
            {
                MD51 = md5short,
                MD52 = md5shortafter
            };
        }

        public static MD5Struct CreateFromBytes(byte[] md5s)
        {
            long num1 = 0;
            for (int ix = 0; ix < 8; ++ix)
            {
                num1 <<= 8;
                num1 |= (long)(md5s[ix] & 0xff);
            }

            long num2 = 0;
            for (int ix = 8; ix < 16; ++ix)
            {
                num2 <<= 8;
                num2 |= (long)(md5s[ix] & 0xff);
            }

            return new MD5Struct()
            {
                MD51 = num1,
                MD52 = num2
            };
        }
        public override bool Equals(object obj)
        {
            if (!(obj is MD5Struct))
                return false;

            var other = (MD5Struct)obj;

            return (this.MD51 == other.MD51) && (this.MD52 == other.MD52);
        }

        public override int GetHashCode()
        {
            var hashCode = 877286057;
            hashCode = hashCode * -1521134295 + MD51.GetHashCode();
            hashCode = hashCode * -1521134295 + MD52.GetHashCode();
            return hashCode;
        }
    }
}
