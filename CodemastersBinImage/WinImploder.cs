using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WinImploder
{
    public static class ImploderWork
    {
        [DllImport(@"win_imploder.dll", CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern int implode(byte* input, uint size, byte mode);
        [DllImport(@"win_imploder.dll", CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern int imploded_size(byte* input);

        [DllImport(@"win_imploder.dll", CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern int check_imp(byte* input);

        [DllImport(@"win_imploder.dll", CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern int explode(byte* input);
        [DllImport(@"win_imploder.dll", CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern int exploded_size(byte* input);

        public unsafe static byte[] Implode(byte[] input, uint size, byte mode)
        {
            byte[] output = new byte[0x10000];
            Array.Copy(input, output, input.Length);

            fixed (byte* pOutput = output)
            {
                int out_size = implode(pOutput, size, mode);
                if (out_size != 0)
                {
                    byte[] result = new byte[out_size];
                    Array.Copy(output, result, out_size);

                    return result;
                }

                return null;
            }
        }

        public static byte[] ImplodeBest(byte[] input, uint size)
        {
            byte bestMode = 0;
            int bestLen = Implode(input, size, 0).Length;

            if (bestLen == 0) return null;

            for (byte i = 1; i < 0x0C; ++i)
            {
                int len = Implode(input, size, i).Length;

                if (len == 0) continue;

                if (bestLen > len)
                {
                    bestMode = i;
                    bestLen = len;
                }
            }

            if (bestLen == 0) return null;
            return Implode(input, size, bestMode);
        }

        public unsafe static int ImplodedSize(byte[] input, int pos = 0)
        {
            fixed (byte* pInput = input)
            {
                return imploded_size(&pInput[pos]);
            }
        }

        public unsafe static bool CheckImp(byte[] input, int pos = 0)
        {
            fixed (byte* pInput = input)
            {
                return (check_imp(&pInput[pos]) != 0);
            }
        }

        public unsafe static byte[] Explode(byte[] input, int pos = 0)
        {
            byte[] output = new byte[ExplodedSize(input, pos)];
            Array.Copy(input, output, input.Length);

            fixed (byte* pOutput = output)
            {
                int out_size = explode(&pOutput[0]);

                if (out_size != 0)
                {
                    byte[] result = new byte[out_size];
                    Array.Copy(output, result, out_size);

                    return result;
                }

                return null;
            }
        }

        public unsafe static int ExplodedSize(byte[] input, int pos)
        {
            fixed (byte* pInput = input)
            {
                return exploded_size(&pInput[pos]);
            }
        }

        public static List<Tuple<int, int>> FindImploded(byte[] input)
        {
            List<Tuple<int, int>> list = new List<Tuple<int, int>>();

            int i = 0;
            while (i < input.Length)
            {
                if (CheckImp(input, i))
                {
                    int size = ImplodedSize(input, i);

                    if (i + size > input.Length)
                    {
                        i++;
                        continue;
                    }

                    list.Add(new Tuple<int, int>(i, size));

                    i += size;
                }
                else
                {
                    i++;
                }
            }

            return ((list.Count > 0) ? list : null);
        }
    }
}
