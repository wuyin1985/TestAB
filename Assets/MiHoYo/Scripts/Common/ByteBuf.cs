/**
		 *  缓冲区
		 **/
using System;
using System.Text;
//using ICSharpCode.SharpZipLib.Checksums;

namespace BytesTools
{
    /// <summary>
    /// 根据高位在前编码
    /// </summary>
	public class ByteBuf
    {
        private int len;
        // TODO: 改为从内存分配池中分配数据
        private byte[] data;
        private int readerIndex;
        private int writerIndex;
        private int markReader;
        private int markWriter;
        public bool IsReadable { get; private set; }
        public bool IsWriteable { get; private set; }


        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="capacity"></param>
        public ByteBuf(int capacity)
        {
            this.len = capacity;
            //this.data = new byte[len];
            this.data = new byte[capacity];
            readerIndex = 0;
            writerIndex = 0;
            markReader = 0;
            markWriter = 0;
            IsReadable = true;
            IsWriteable = true;
        }


        public ByteBuf(byte[] buf)
        {
            if (buf == null) buf = new byte[0];
            this.len = buf.Length;
            this.data = buf;
            readerIndex = 0;
            writerIndex = 0;
            markReader = 0;
            markWriter = 0;
            IsReadable = true;
            IsWriteable = true;
        }

        public static ByteBuf CreateFromBytes(byte[] bs)
        {
            var bb = new ByteBuf(bs);
            bb.writerIndex = bs.Length;
            return bb;
        }

        /**
		 *  容量
		 **/
        public int Capacity()
        {
            return len;
        }

        /**
		 * 扩容
		 */
        public ByteBuf Capacity(int nc)
        {
            if (nc > len)
            {
                byte[] old = data;
                //data = new byte[nc];
                data = new byte[nc];
                Array.Copy(old, data, len);
                len = nc;
            }
            return this;
        }

        /**
	     * 清除掉所有标记
	     * @return 
	    **/
        public ByteBuf Clear()
        {

            readerIndex = 0;
            writerIndex = 0;
            markReader = 0;
            markWriter = 0;
            return this;
        }

        /**
		 * 拷贝
		 **/
        public ByteBuf Copy()
        {
            ByteBuf item = new ByteBuf(len);
            Array.Copy(this.data, item.data, len);
            item.readerIndex = readerIndex;
            item.writerIndex = writerIndex;
            item.markReader = markReader;
            item.markWriter = markWriter;

            return item;
        }

        /**
        * 翻转字节数组，如果本地字节序列为低字节序列，则进行翻转以转换为高字节序列
        */
        private byte[] Flip(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        /**
        * 翻转字节数组，如果本地字节序列为低字节序列，则进行翻转以转换为高字节序列
        */
        private byte[] Flip(byte[] bytes, int index, int len)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes, index, len);
            }
            return bytes;
        }

        /// <summary>
        /// 返回所有写入内容形成的数组
        /// </summary>
        /// <returns></returns>
        public byte[] GetAllBytesCopy()
        {
            var bs = new System.Byte[writerIndex];
            Array.Copy(data, bs, writerIndex);
            return bs;
        }
        /**
		 * 获取一个字节
		 **/
        public byte GetByte(int index)
        {
            if (index < len)
            {
                return data[index];
            }
            return (byte)0;
        }
        /**
		 * 读取四字节整形F
		 **/
        public int GetInt(int index)
        {
            if (index + 3 < len)
            {
                int ret = ((int)data[index]) << 24;
                ret |= ((int)data[index + 1]) << 16;
                ret |= ((int)data[index + 2]) << 8;
                ret |= ((int)data[index + 3]);
                return ret;
            }
            return 0;
        }
        /**
		 * 读取两字节整形
		 **/
        public short GetShort(int index)
        {
            if (index + 1 < len)
            {
                short r1 = (short)(data[index] << 8);
                short r2 = (short)(data[index + 1]);
                short ret = (short)(r1 | r2);
                return ret;
            }
            return 0;
        }
        /**
		 * 标记读
		 **/
        public int MarkReaderIndex()
        {
            markReader = readerIndex;
            return readerIndex;
        }
        /**
		 * 标记写
		 **/
        public int MarkWriterIndex()
        {
            markWriter = writerIndex;
            return writerIndex;
        }
        /**
		 * 可写长度
		 **/
        public int MaxWritableBytes()
        {
            return len - writerIndex;
        }


        public bool HasInts(int num)
        {
            return (readerIndex + 4 * num - 1 < writerIndex);
        }

        public bool HasBytes(int num)
        {
            return (readerIndex + num - 1 < writerIndex);
        }


        /**
		 * 读取一个Bool
		 **/
        public bool ReadBool()
        {
            byte by = ReadByte();
            bool b = by != 0;
            return b;
        }
        /**
		 * 读取一个字节
		 **/
        public byte ReadByte()
        {
            if (!IsReadable)
            {
#if UNITY_EDITOR
                CommonLog.Error("这个是不可读的对象");
#endif
            }
            else
            {
                if (readerIndex < writerIndex)
                {
                    byte ret = data[readerIndex++];
                    return ret;
                }
                else
                {
#if UNITY_EDITOR
                    CommonLog.Error("这个是不合法的数据");
#endif
                }
            }
            return (byte)0;
        }

        /**
        * 读取N个字节
        **/
        public byte[] ReadBytes(int len)
        {
            if (!IsReadable)
            {
#if UNITY_EDITOR
                CommonLog.Error("这个是不可读的对象");
#endif
            }
            else
            {
                if (len > 0 && readerIndex + len - 1 < writerIndex)
                {
                    byte[] data = new byte[len];
                    for (int i = 0; i < len; i++)
                    {
                        data[i] = ReadByte();
                    }
                    return data;
                }
                else
                {
#if UNITY_EDITOR
                    CommonLog.Error("这个是不合法的数据");
#endif
                }
            }
            return new byte[0];
        }

        /**
        * 读取N个字节
        **/
        public byte[] ReadBytes()
        {
            var len = ReadShort();
            return ReadBytes(len);
        }

        /**
        * 读取N个字节
        **/
        public ByteBuf ReadByteBuf()
        {
            var len = ReadShort();
            if (len > 0 && readerIndex + len - 1 < writerIndex)
            {
                var nbyte = new ByteBuf(data);
                nbyte.readerIndex = readerIndex;
                nbyte.writerIndex = writerIndex;
                nbyte.markReader = markReader;
                nbyte.markWriter = markWriter;
                nbyte.IsWriteable = false;
                nbyte.IsReadable = true;
                return nbyte;
            }
            else
            {
#if UNITY_EDITOR
                CommonLog.Error("这个是不合法的数据");
#endif
            }
            return null;
        }

        /**
		 * 读取四字节整形
		 **/
        public int ReadInt()
        {
            if (!IsReadable)
            {
#if UNITY_EDITOR
                CommonLog.Error("这个是不可读的对象");
#endif
            }
            else
            {
                if (readerIndex + 3 < writerIndex)
                {
                    unchecked
                    {
                        int ret = (int)(((data[readerIndex++]) << 24) & 0xff000000);
                        ret |= (((data[readerIndex++]) << 16) & 0x00ff0000);
                        ret |= (((data[readerIndex++]) << 8) & 0x0000ff00);
                        ret |= (((data[readerIndex++])) & 0x000000ff);
                        return ret;
                    }
                }
                else
                {
#if UNITY_EDITOR
                    CommonLog.Error("这个是不合法的数据");
#endif
                }
            }
            return 0;
        }
        /**
 * 读取四字节整形
 **/
        public uint ReadUInt()
        {
            if (!IsReadable)
            {
#if UNITY_EDITOR
                CommonLog.Error("这个是不可读的对象");
#endif
            }
            else
            {
                if (readerIndex + 3 < writerIndex)
                {
                    unchecked
                    {
                        uint ret = (uint)(((data[readerIndex++]) << 24) & 0xff000000);
                        ret |= (uint)(((data[readerIndex++]) << 16) & 0x00ff0000);
                        ret |= (uint)(((data[readerIndex++]) << 8) & 0x0000ff00);
                        ret |= (uint)(((data[readerIndex++])) & 0x000000ff);
                        return ret;
                    }
                }
                else
                {
#if UNITY_EDITOR
                    CommonLog.Error("这个是不合法的数据");
#endif
                }
            }
            return 0u;
        }


        /**
        * 读取八字节整形
        **/
        public long ReadLong()
        {
            if (!IsReadable)
            {
#if UNITY_EDITOR
                CommonLog.Error("这个是不可读的对象");
#endif
            }
            else
            {
                if (readerIndex + 7 < writerIndex)
                {
                    unchecked
                    {
                        ulong ret = ((((ulong)data[readerIndex++]) << 56) & 0xff00000000000000);
                        ret |= (((ulong)(data[readerIndex++]) << 48) & 0x00ff000000000000);
                        ret |= (((ulong)(data[readerIndex++]) << 40) & 0x0000ff0000000000);
                        ret |= (((ulong)(data[readerIndex++]) << 32) & 0x000000ff00000000);
                        ret |= (((ulong)(data[readerIndex++]) << 24) & 0x00000000ff000000);
                        ret |= (((ulong)(data[readerIndex++]) << 16) & 0x0000000000ff0000);
                        ret |= (((ulong)(data[readerIndex++]) << 8) & 0x000000000000ff00);
                        ret |= (((ulong)(data[readerIndex++])) & 0x00000000000000ff);
                        return (long)ret;
                    }
                }
                else
                {
#if UNITY_EDITOR
                    CommonLog.Error("这个是不合法的数据");
#endif
                }
            }
            return 0;
        }

        /**
		 * 读取两个字节的整形
		 **/
        public short ReadShort()
        {
            if (!IsReadable)
            {
#if UNITY_EDITOR
                CommonLog.Error("这个是不可读的对象");
#endif
            }
            else
            {
                if (readerIndex + 1 < writerIndex)
                {
                    int h = data[readerIndex++];
                    int l = data[readerIndex++] & 0x000000ff;
                    int len = ((h << 8) & 0x0000ff00) | (l);
                    return (short)len;
                }
                else
                {
#if UNITY_EDITOR
                    CommonLog.Error("这个是不合法的数据");
#endif
                }
            }
            return 0;
        }

        public static unsafe int SingleToInt32Bits(float value)
        {
            return *(int*)(&value);
        }
        public static unsafe float Int32BitsToSingle(int value)
        {
            return *(float*)(&value);
        }

        /**
        *  读取4个字节的float
        **/
        public float ReadFloat()
        {
            if (!IsReadable)
            {
#if UNITY_EDITOR
                CommonLog.Error("这个是不可读的对象");
#endif
            }
            else
            {
                if (readerIndex + 3 < writerIndex)
                {
                    int intbs = ReadInt();
                    var res = Int32BitsToSingle(intbs);
                    return res;
                }
                else
                {
#if UNITY_EDITOR
                    CommonLog.Error("这个是不合法的数据");
#endif
                }
            }
            return 0f;
        }

        /**
        * 读取3个float的V3
        **/
        public UnityEngine.Vector2 ReadVector2()
        {
            var v2 = new UnityEngine.Vector2();
            for (int i = 0; i < 2; i++)
            {
                v2[i] = ReadFloat();
            }
            return v2;
        }

        /**
        * 读取3个float的V3
        **/
        public UnityEngine.Vector3 ReadVector3()
        {
            var v3 = new UnityEngine.Vector3();
            for (int i = 0; i < 3; i++)
            {
                v3[i] = ReadFloat();
            }
            return v3;
        }

        /**
        * 读取4个float的V4
        **/
        public UnityEngine.Vector4 ReadVector4()
        {
            var v4 = new UnityEngine.Vector4();
            for (int i = 0; i < 4; i++)
            {
                v4[i] = ReadFloat();
            }
            return v4;
        }

        /**
        * 读取16个字节的Q4
        **/
        public UnityEngine.Quaternion ReadQuaternion()
        {
            var q4 = new UnityEngine.Quaternion();
            for (int i = 0; i < 4; i++)
            {
                q4[i] = ReadFloat();
            }
            return q4;
        }

        /**
		 * 可读字节数
		 **/
        public int ReadableBytes()
        {
            return writerIndex - readerIndex;
        }
        /**
		 * 读指针
		 **/
        public int ReaderIndex
        {
            get
            {
                return readerIndex;
            }
        }
        /**
		 * 移动读指针
		 **/
        public ByteBuf SetReaderIndex(int readerIndex)
        {
            if (readerIndex <= writerIndex)
            {
                this.readerIndex = readerIndex;
            }
            return this;
        }
        /**
		 * 重置读指针
		 **/
        public ByteBuf ResetReaderIndex()
        {
            if (markReader <= writerIndex)
            {
                this.readerIndex = markReader;
            }
            return this;
        }
        /**
		 * 重置写指针
		 **/
        public ByteBuf ResetWriterIndex()
        {
            if (markWriter >= readerIndex)
            {
                writerIndex = markWriter;
            }
            return this;
        }
        /**
		 * 设置字节
		 **/
        public ByteBuf SetByte(int index, byte value)
        {
            if (index < len)
            {
                data[index] = value;
            }
            return this;
        }


        /**
		 * 设置字节
		 **/
        public ByteBuf SetBytes(int index, byte[] src, int from, int len)
        {
            if (index + len <= len)
            {
                Array.Copy(src, from, data, index, len);
            }
            return this;
        }
        /**
		 * 设置读写指针
		 **/
        public ByteBuf SetIndex(int readerIndex, int writerIndex)
        {
            if (readerIndex >= 0 && readerIndex <= writerIndex && writerIndex <= len)
            {
                this.readerIndex = readerIndex;
                this.writerIndex = writerIndex;
            }
            return this;
        }
        /**
		 * 设置四字节整形
		 **/
        public ByteBuf SetInt(int index, int value)
        {
            if (index + 4 <= len)
            {
                data[index++] = (byte)((value >> 24) & 0xff);
                data[index++] = (byte)((value >> 16) & 0xff);
                data[index++] = (byte)((value >> 8) & 0xff);
                data[index++] = (byte)(value & 0xff);
            }
            return this;
        }

        /**
        * 设置八字节整形
         **/
        public ByteBuf SetLong(int index, long value)
        {
            if (index + 8 <= len)
            {
                data[index++] = (byte)((value >> 56) & 0xff);
                data[index++] = (byte)((value >> 48) & 0xff);
                data[index++] = (byte)((value >> 40) & 0xff);
                data[index++] = (byte)((value >> 32) & 0xff);
                data[index++] = (byte)((value >> 24) & 0xff);
                data[index++] = (byte)((value >> 16) & 0xff);
                data[index++] = (byte)((value >> 8) & 0xff);
                data[index++] = (byte)(value & 0xff);
            }
            return this;
        }

        /**
		 * 设置两字节整形
		 **/
        public ByteBuf SetShort(int index, short value)
        {
            if (index + 2 <= len)
            {
                data[index++] = (byte)((value >> 8) & 0xff);
                data[index++] = (byte)(value & 0xff);
            }
            return this;
        }
        /**
		 * 略过一些字节
		 **/
        public ByteBuf SkipBytes(int length)
        {
            if (readerIndex + length <= writerIndex)
            {
                readerIndex += length;
            }
            return this;
        }
        /**
		 * 剩余的可写字节数
		 **/
        public int WritableBytes()
        {
            return len - writerIndex;
        }
        /**
		 * 写入一个字节
		 * 
		 **/
        public ByteBuf WriteByte(byte value)
        {
            if (!IsWriteable)
            {
#if UNITY_EDITOR
                CommonLog.Error("这个是不可写的对象");
#endif
                return this;
            }

            this.Capacity(writerIndex + 1);
            this.data[writerIndex++] = value;
            return this;
        }

        /**
        * 写入一个字节
        **/
        public ByteBuf WriteBool(bool b)
        {
            if (!IsWriteable)
            {
#if UNITY_EDITOR
                CommonLog.Error("这个是不可写的对象");
#endif
                return this;
            }
            this.Capacity(writerIndex + 1);
            this.data[writerIndex++] = b ? (byte)1 : (byte)0;
            return this;
        }


        /**
		 * 写入四字节整形
		 **/
        public ByteBuf WriteInt(int value)
        {
            if (!IsWriteable)
            {
#if UNITY_EDITOR
                CommonLog.Error("这个是不可写的对象");
#endif
                return this;
            }
            Capacity(writerIndex + 4);
            data[writerIndex++] = (byte)((value >> 24) & 0xff);
            data[writerIndex++] = (byte)((value >> 16) & 0xff);
            data[writerIndex++] = (byte)((value >> 8) & 0xff);
            data[writerIndex++] = (byte)(value & 0xff);
            return this;
        }


        /**
		 * 写入四字节整形
		 **/
        public ByteBuf WriteUInt(uint value)
        {
            if (!IsWriteable)
            {
#if UNITY_EDITOR
                CommonLog.Error("这个是不可写的对象");
#endif
                return this;
            }
            Capacity(writerIndex + 4);
            data[writerIndex++] = (byte)((value >> 24) & 0xff);
            data[writerIndex++] = (byte)((value >> 16) & 0xff);
            data[writerIndex++] = (byte)((value >> 8) & 0xff);
            data[writerIndex++] = (byte)(value & 0xff);
            return this;
        }

        /**
        * 写入八字节整形
        **/
        public ByteBuf WriteLong(long value)
        {
            if (!IsWriteable)
            {
#if UNITY_EDITOR
                CommonLog.Error("这个是不可写的对象");
#endif
                return this;
            }
            Capacity(writerIndex + 8);
            data[writerIndex++] = (byte)((value >> 56) & 0xff);
            data[writerIndex++] = (byte)((value >> 48) & 0xff);
            data[writerIndex++] = (byte)((value >> 40) & 0xff);
            data[writerIndex++] = (byte)((value >> 32) & 0xff);
            data[writerIndex++] = (byte)((value >> 24) & 0xff);
            data[writerIndex++] = (byte)((value >> 16) & 0xff);
            data[writerIndex++] = (byte)((value >> 8) & 0xff);
            data[writerIndex++] = (byte)(value & 0xff);
            return this;
        }

        /**
		 * 写入两字节整形
		 **/
        public ByteBuf WriteShort(short value)
        {
            if (!IsWriteable)
            {
#if UNITY_EDITOR
                CommonLog.Error("这个是不可写的对象");
#endif
                return this;
            }
            Capacity(writerIndex + 2);
            data[writerIndex++] = (byte)((value >> 8) & 0xff);
            data[writerIndex++] = (byte)(value & 0xff);
            return this;
        }

        /**
        * 写入两字节整形
        **/

        public ByteBuf WriteFloat(float value)
        {
            if (!IsWriteable)
            {
#if UNITY_EDITOR
                CommonLog.Error("这个是不可写的对象");
#endif
                return this;
            }
            var idata = SingleToInt32Bits(value);
            WriteInt(idata);
            return this;
        }


        /**
        * 读取2个float的V2
        **/
        public ByteBuf WriteVector2(UnityEngine.Vector2 v2)
        {
            for (int i = 0; i < 2; i++)
            {
                WriteFloat(v2[i]);
            }
            return this;
        }

        /**
        * 读取3个float的V3
        **/
        public ByteBuf WriteVector3(UnityEngine.Vector3 v3)
        {
            for (int i = 0; i < 3; i++)
            {
                WriteFloat(v3[i]);
            }
            return this;
        }

        /**
        * 读取4个float的V4
        **/
        public ByteBuf WriteVector4(UnityEngine.Vector4 v4)
        {
            for (int i = 0; i < 4; i++)
            {
                WriteFloat(v4[i]);
            }
            return this;
        }

        /**
        * 读取16个字节的Q4
        **/
        public ByteBuf WriteQuaternion(UnityEngine.Quaternion q4)
        {
            for (int i = 0; i < 4; i++)
            {
                WriteFloat(q4[i]);
            }
            return this;
        }

        /**
		 * 写入一部分字节
		 **/
        public ByteBuf WriteBytes(ByteBuf src)
        {
            if (!IsWriteable)
            {
#if UNITY_EDITOR
                CommonLog.Error("这个是不可写的对象");
#endif
                return this;
            }
            short sum = (short)(src.writerIndex - src.readerIndex);
            if (sum >= 0)
            {
                Capacity(writerIndex + sum + 2);

                WriteShort(sum);
                if (sum > 0)
                {
                    Array.Copy(src.data, src.readerIndex, data, writerIndex, sum);
                    writerIndex += sum;
                    src.readerIndex += sum;
                }
            }
            else
            {
#if UNITY_EDITOR
                CommonLog.Error("写入对象过长");
#endif
            }
            return this;
        }
        /**
		 * 写入一部分字节
		 **/
        public ByteBuf WriteBytes(ByteBuf src, int len)
        {
            if (!IsWriteable)
            {
#if UNITY_EDITOR
                CommonLog.Error("这个是不可写的对象");
#endif
                return this;
            }
            if (len > 0)
            {
                Capacity(writerIndex + len);
                Array.Copy(src.data, src.readerIndex, data, writerIndex, len);
                writerIndex += len;
                src.readerIndex += len;
            }
            return this;
        }
        /**
		 * 写入一部分字节
		 **/
        public ByteBuf WriteBytes(byte[] src)
        {
            if (!IsWriteable)
            {
#if UNITY_EDITOR
                CommonLog.Error("这个是不可写的对象");
#endif
                return this;
            }
            if (src == null)
            {
                WriteShort(0);
                return this;
            }
            short sum = (short)src.Length;
            WriteShort(sum);
            Capacity(writerIndex + sum);
            if (sum > 0)
            {
                Array.Copy(src, 0, data, writerIndex, sum);
                writerIndex += sum;
            }
            return this;
        }
        /**
		 * 写入一部分字节
		 **/
        public ByteBuf WriteBytes(byte[] src, int off, short len)
        {
            if (!IsWriteable)
            {
#if UNITY_EDITOR
                CommonLog.Error("这个是不可写的对象");
#endif
                return this;
            }
            if (src == null)
            {
                WriteShort(0);
                return this;
            }
            short sum = len;
            WriteShort(sum);
            if (sum > 0)
            {
                Capacity(writerIndex + sum);
                Array.Copy(src, off, data, writerIndex, sum);
                writerIndex += sum;
            }
            return this;
        }
        /**
		 * 读取utf字符串
		 **/
        public string ReadUTF8()
        {
            if (!IsReadable)
            {
#if UNITY_EDITOR
                CommonLog.Error("这个是不可读的对象");
#endif
            }
            short len = ReadShort(); // 字节数
            byte[] charBuff = new byte[len]; //
            Array.Copy(data, readerIndex, charBuff, 0, len);
            readerIndex += len;
            return Encoding.UTF8.GetString(charBuff);
        }

        /**
		 * 写入utf字符串
		 * 
		 **/
        public ByteBuf WriteUTF8(string value)
        {
            if (!IsWriteable)
            {
#if UNITY_EDITOR
                CommonLog.Error("这个是不可写的对象");
#endif
                return this;
            }
            byte[] content = Encoding.UTF8.GetBytes(value.ToCharArray());
            int len = content.Length;
            Capacity(writerIndex + len + 2);
            WriteShort((short)len);
            Array.Copy(content, 0, data, writerIndex, len);
            writerIndex += len;
            return this;
        }


        /**
		 * 写指针
		 **/
        public int WriterIndex
        {
            get { return writerIndex; }
        }
        /**
		 * 移动写指针
		 **/
        public ByteBuf SetWriterIndex(int writerIndex)
        {
            if (writerIndex >= readerIndex && writerIndex <= len)
            {
                this.writerIndex = writerIndex;
            }
            return this;
        }
        /**
		 * 原始字节数组
		 **/
        public byte[] GetRaw()
        {
            return data;
        }
        //private static Crc32 crc32 = new Crc32();

        //public uint CalculateCheckSum(int startIndex, int len)
        //{
        //    if (data == null || data.Length == 0) return 0;
        //    crc32.Reset();
        //    crc32.Update(data, startIndex, len);
        //    //uint Crc32StartValue = 0xFFFFFFFF;
        //    //int bytes = data.Length;
        //    //uint newCrc = Crc32StartValue;
        //    //
        //    //for (int n = 0; n < bytes; n += 1)
        //    //{ 
        //    //    newCrc = crc32Table[(newCrc ^ data[n]) & 0xFF] ^ (newCrc >> 8);
        //    //}
        //    return (uint)crc32.Value;
        //}


    }
}

public class ByteBufHelper
{

    /**
     * 读取四字节整形
     **/
    public static int ReadInt(byte[] bs, int startIndex)
    {
        if (startIndex + 3 < bs.Length)
        {
            unchecked
            {
                int ret = (int)(((bs[startIndex]) << 24) & 0xff000000);
                ret |= (((bs[startIndex + 1]) << 16) & 0x00ff0000);
                ret |= (((bs[startIndex + 2]) << 8) & 0x0000ff00);
                ret |= (((bs[startIndex + 3])) & 0x000000ff);
                return ret;
            }
        }
        return 0;
    }
    public static uint GetUIntFromBytes(byte b1, byte b2, byte b3, byte b4)
    {
        unchecked
        {
            uint ret = (uint)(((b1) << 24) & 0xff000000);
            ret |= (uint)(((b2) << 16) & 0x00ff0000);
            ret |= (uint)(((b3) << 8) & 0x0000ff00);
            ret |= (uint)(((b4)) & 0x000000ff);
            return ret;
        }
    }
    public static byte[] GetByteFromUInt(uint val)
    {
        var data = new byte[4];
        data[0] = (byte)((val >> 24) & 0xff);
        data[1] = (byte)((val >> 16) & 0xff);
        data[2] = (byte)((val >> 8) & 0xff);
        data[3] = (byte)(val & 0xff);
        return data;
    }
    public static byte GetByteFromUInt(uint val, int index)
    {
        byte data = 0;
        if (index == 0) { data = (byte)((val >> 24) & 0xff); }
        if (index == 1) { data = (byte)((val >> 16) & 0xff); }
        if (index == 2) { data = (byte)((val >> 8) & 0xff); }
        if (index == 3) { data = (byte)((val) & 0xff); }
        return data;
    }
}
