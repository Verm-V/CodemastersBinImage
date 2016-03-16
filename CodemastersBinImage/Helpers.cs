using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace Helper
{
    public static class Helpers
    {
        public static UInt16 ReadWord(this byte[] array, int index)
        {
            return (ushort)((array[index] << 8) | array[index + 1]);
        }

        public static byte ReadByteInc(this MemoryStream stream, int index = 0)
        {
            stream.Seek(index, SeekOrigin.Current);

            byte[] array = new byte[1];
            stream.Read(array, 0, array.Length);
            return array[0];
        }

        public static UInt16 ReadWord(this MemoryStream stream, int index = 0)
        {
            long pos = stream.Position;
            stream.Seek(index, SeekOrigin.Current);

            byte[] array = new byte[2];
            stream.Read(array, 0, array.Length);

            stream.Position = pos;
            return (ushort)((array[0] << 8) | array[1]);
        }

        public static UInt16 ReadWordInc(this MemoryStream stream, int index = 0)
        {
            stream.Seek(index, SeekOrigin.Current);

            byte[] array = new byte[2];
            stream.Read(array, 0, array.Length);
            return (ushort)((array[0] << 8) | array[1]);
        }

        public static UInt32 ReadLong(this byte[] array, int index)
        {
            return (uint)((array[index] << 24) | (array[index + 1] << 16) | (array[index + 2] << 8) | array[index + 3]);
        }

        public static UInt32 ReadLong(this MemoryStream stream, int index = 0)
        {
            long pos = stream.Position;
            stream.Seek(index, SeekOrigin.Current);

            byte[] array = new byte[4];
            stream.Read(array, 0, array.Length);

            stream.Position = pos;
            return (uint)((array[0] << 24) | (array[1] << 16) | (array[2] << 8) | array[3]);
        }

        public static UInt32 ReadLongInc(this MemoryStream stream, int index = 0)
        {
            stream.Seek(index, SeekOrigin.Current);

            byte[] array = new byte[4];
            stream.Read(array, 0, array.Length);
            return (uint)((array[0] << 24) | (array[1] << 16) | (array[2] << 8) | array[3]);
        }

        public static void WriteWord(this byte[] array, int index, ushort value)
        {
            array[index++] = (byte)((value >> 8) & 0xFF);
            array[index] = (byte)(value & 0xFF);
        }

        public static void WriteWordInc(this MemoryStream stream, int index, ushort value)
        {
            stream.Seek(index, SeekOrigin.Current);

            byte[] array = new byte[2];
            array[0] = (byte)((value >> 8) & 0xFF);
            array[1] = (byte)(value & 0xFF);
            stream.Write(array, 0, array.Length);
        }

        public static void WriteLong(this byte[] array, int index, uint value)
        {
            array[index++] = (byte)((value >> 24) & 0xFF);
            array[index++] = (byte)((value >> 16) & 0xFF);
            array[index++] = (byte)((value >> 8) & 0xFF);
            array[index] = (byte)(value & 0xFF);
        }

        public static void WriteLongInc(this MemoryStream stream, int index, uint value)
        {
            stream.Seek(index, SeekOrigin.Current);

            byte[] array = new byte[4];
            array[0] = (byte)((value >> 24) & 0xFF);
            array[1] = (byte)((value >> 16) & 0xFF);
            array[2] = (byte)((value >> 8) & 0xFF);
            array[3] = (byte)(value & 0xFF);
            stream.Write(array, 0, array.Length);
        }

        public static void IncPos(this MemoryStream stream, int value)
        {
            stream.Seek(value, SeekOrigin.Current);
        }

        public static void SubPos(this MemoryStream stream, int value)
        {
            stream.Seek(-value, SeekOrigin.Current);
        }

        public static Bitmap CropImage(Bitmap source, Rectangle cropArea)
        {
            return source.Clone(cropArea, source.PixelFormat);
        }

        public static byte[] BitmapToArray(Bitmap image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
        }

        public static Bitmap BitmapFromArray(byte[] array)
        {
            using (var ms = new MemoryStream(array))
            {
                return new Bitmap(ms);
            }
        }

        public static uint mask(byte bit_idx, byte bits_cnt = 1)
        {
            return (uint)(((1 << bits_cnt) - 1) << bit_idx);
        }

        public static class CompareByteArray
        {
            [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
            static extern int memcmp(byte[] b1, byte[] b2, long count);

            static public bool Compare(byte[] b1, byte[] b2)
            {
                return b1.Length == b2.Length && memcmp(b1, b2, b1.Length) == 0;
            }
        }

        public class CompareByteArrays : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[] x, byte[] y)
            {
                return Helpers.CompareByteArray.Compare(x, y);
            }
            public int GetHashCode(byte[] obj)
            {
                var str = Convert.ToBase64String(obj);
                return str.GetHashCode();
            }
        }

        public class CompareKey : IEqualityComparer<Tuple<byte[], int>>
        {
            public bool Equals(Tuple<byte[], int> x, Tuple<byte[], int> y)
            {
                return Helpers.CompareByteArray.Compare(x.Item1, y.Item1);
            }
            public int GetHashCode(Tuple<byte[], int> obj)
            {
                var str = Convert.ToBase64String(obj.Item1);
                return str.GetHashCode();
            }
        }

        public class CompareValue : IEqualityComparer<Tuple<byte[], int>>
        {
            public bool Equals(Tuple<byte[], int> x, Tuple<byte[], int> y)
            {
                return (x.Item2 == y.Item2);
            }
            public int GetHashCode(Tuple<byte[], int> obj)
            {
                return obj.Item2.GetHashCode();
            }
        }

        public static int SearchBytes(byte[] array, byte[] value, int from)
        {
            int found = 0;
            for (int i = from; i < array.Length; i++)
            {
                if (array[i] == value[found])
                {
                    if (++found == value.Length)
                    {
                        return i - found + 1;
                    }
                }
                else
                {
                    found = 0;
                }
            }
            return -1;
        }
    }
}
