using Helper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace PluginVideoSega
{
    public static class VideoSega
    {
        public static Color[] PaletteFromByteArray(byte[] pal)
        {
            Color[] retn = new Color[16];

            for (int x = 0, offset = 0; x < 16; x++, offset += 2)
                retn[x] = ColorFromWord(pal.ReadWord(offset));

            return retn;
        }

        public static byte[] PaletteToByteArray(Color[] pal)
        {
            byte[] retn = new byte[32];

            int offset = 0;
            for (int x = 0; x < 16; x++, offset += 2)
            {
                ushort W = ColorToWord(pal[x]);

                retn[offset + 0] = (byte)((W >> 8) & 0xFF);
                retn[offset + 1] = (byte)((W >> 0) & 0xFF);
            }

            return retn;
        }

        public static ushort ColorToWord(Color color)
        {
            ushort W = 0;

            W |= (ushort)(((color.R >> 4) & 0xE) << 0);
            W |= (ushort)(((color.G >> 4) & 0xE) << 4);
            W |= (ushort)(((color.B >> 4) & 0xE) << 8);

            return W;
        }

        public static Color ColorFromWord(ushort Word)
        {
            byte r = (byte)(((Word >> 0) & 0xE) << 4);
            byte g = (byte)(((Word >> 4) & 0xE) << 4);
            byte b = (byte)(((Word >> 8) & 0xE) << 4);
            return Color.FromArgb(r, g, b);
        }

        public static Color[] PaletteApplySega(Color[] pal)
        {
            byte[] tmp = PaletteToByteArray(pal);
            return PaletteFromByteArray(tmp);
        }

        public static Color ColorApplySega(Color color)
        {
            ushort tmp = ColorToWord(color);
            return ColorFromWord(tmp);
        }

        public static void TileToData(Bitmap image, out byte[] tile, Color[] palette)
        {
            tile = new byte[0x20];

            for (int h = 0, i = 0; h < 8; h++)
                for (int w = 0; w < 4; w++, i++)
                {
                    int pixIndex1 = Array.FindIndex(palette, c => (c.ToArgb() == image.GetPixel(w * 2 + 0, h).ToArgb()));
                    int pixIndex2 = Array.FindIndex(palette, c => (c.ToArgb() == image.GetPixel(w * 2 + 1, h).ToArgb()));

                    byte b = 0x00;

                    if (pixIndex1 != -1) b |= (byte)((pixIndex1 << 4) & 0xF0);
                    if (pixIndex2 != -1) b |= (byte)((pixIndex2 << 0) & 0x0F);

                    tile[i] = b;
                }
        }

        public static bool TileFromData(byte[] bytes, BitmapData data, int x, int y, byte[] tiles, ushort word, Color[] palette)
        {
            ushort tilePos = Mapper.TilePos(word);
            for (int h = 0; h < 8; h++)
                for (int w = 0; w < 4; w++)
                {
                    if (tilePos >= tiles.Length) return false;

                    byte B = tiles[tilePos++];

                    int newW1 = x + (Mapper.HF(word) ? (7 - w * 2 - 0) : (w * 2 + 0));
                    int newW2 = x + (Mapper.HF(word) ? (7 - w * 2 - 1) : (w * 2 + 1));
                    int newH = (Mapper.VF(word) ? (y + (8 - 1) - h) : (y + h));

                    bytes[newH * data.Stride + newW1] = (byte)((B & 0xF0) >> 4);
                    bytes[newH * data.Stride + newW2] = (byte)((B & 0x0F) >> 0);
                }

            return true;
        }

        public static Bitmap ImageMaskFromData(ushort[] mapping, ushort width, ushort height)
        {
            if (mapping == null) return null;
            if (width == 0) return null;
            if (height == 0) return null;

            Bitmap mask = new Bitmap(width * 8, height * 8, PixelFormat.Format8bppIndexed);

            ColorPalette maskPal = mask.Palette;

            for (int i = 0; i < maskPal.Entries.Length; ++i)
            {
                maskPal.Entries[i] = VideoSega.ColorApplySega(Color.White); // Clear palette
            }

            BitmapData data = mask.LockBits(new Rectangle(0, 0, mask.Width, mask.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

            byte[] bytes = new byte[data.Height * data.Stride];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

            byte pal = 0;
            for (ushort y = 0; y < height; y += 8)
                for (ushort x = 0; x < width; x += 8)
                {
                    ushort word = mapping[y * width + x];
                    bool prior = Mapper.P(word);
                    pal = Mapper.PalIdx(word);

                    for (int h = 0; h < 8; h++)
                        for (int w = 0; w < 8; w++)
                        {
                            bytes[(y + h) * data.Stride + (x + w)] = (byte)(prior ? 1 : 0);
                        }
                }

            maskPal.Entries[1] = Color.FromArgb((pal << 4), 0, 0); // First color for non prior
            mask.Palette = maskPal;

            Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);

            mask.UnlockBits(data);

            return mask;
        }

        public static Bitmap ImageFromData(byte[] tiles, ushort[] mapping, Color[] palette, ushort width, ushort height)
        {
            if (tiles == null) return null;
            if (mapping == null) return null;
            if (palette == null) return null;
            if (width == 0) return null;
            if (height == 0) return null;

            Bitmap image = new Bitmap(width * 8, height * 8, PixelFormat.Format8bppIndexed);

            ColorPalette imagePal = image.Palette;

            for (int i = 0; i < imagePal.Entries.Length; ++i)
            {
                imagePal.Entries[i] = VideoSega.ColorApplySega(Color.Black); // Clear palette
            }
            for (int i = 0; i < palette.Length; ++i)
            {
                imagePal.Entries[i] = palette[i]; // Fill image palette
            }
            image.Palette = imagePal;

            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

            byte[] bytes = new byte[data.Height * data.Stride];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

            for (ushort y = 0; y < height; ++y)
                for (ushort x = 0; x < width; ++x)
                {
                    if (!TileFromData(bytes, data, x * 8, y * 8, tiles, mapping[y * width + x], palette)) return null;
                }

            Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);

            image.UnlockBits(data);

            return image;
        }

        public static void ImageToData(Bitmap image, Bitmap mask, out byte[] tiles, out ushort[] mapping, out Color[] palette, out ushort width, out ushort height)
        {
            width = (ushort)image.Width;
            height = (ushort)image.Height;

            List<Tuple<byte[], int>> tilesAllHF = new List<Tuple<byte[], int>>();
            List<Tuple<byte[][], int>> tilesAll = new List<Tuple<byte[][], int>>();

            for (int y = 0; y < height; y += 8)
                for (int x = 0; x < width; x += 8)
                {
                    int idx = (y * width) / 8 + (x % 8);

                    Rectangle rect = new Rectangle(new Point(x, y), new Size(8, 8));
                    Bitmap tile = Helpers.CropImage(image, rect);

                    byte[] hash00 = Helpers.BitmapToArray(tile); // !HF, !VF
                    tile.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    byte[] hash10 = Helpers.BitmapToArray(tile); //  HF, !VF
                    tile.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    byte[] hash11 = Helpers.BitmapToArray(tile); //  HF,  VF
                    tile.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    byte[] hash01 = Helpers.BitmapToArray(tile); // !HF,  VF

                    tilesAll.Add(new Tuple<byte[][], int>(new byte[][] { hash00, hash10, hash11, hash01 }, idx));

                    tilesAllHF.Add(new Tuple<byte[], int>(hash00, idx));
                    tilesAllHF.Add(new Tuple<byte[], int>(hash10, idx));
                    tilesAllHF.Add(new Tuple<byte[], int>(hash11, idx));
                    tilesAllHF.Add(new Tuple<byte[], int>(hash01, idx));
                }

            tilesAllHF = tilesAllHF.Distinct(new Helpers.CompareKey()).ToList(); //remove tile duplicates
            tilesAllHF = tilesAllHF.Distinct(new Helpers.CompareValue()).ToList(); //remove tile duplicates

            BitmapData maskData = mask.LockBits(new Rectangle(0, 0, mask.Width, mask.Height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);

            byte[] maskBytes = new byte[maskData.Height * maskData.Stride];
            Marshal.Copy(maskData.Scan0, maskBytes, 0, maskBytes.Length);

            MemoryStream stream = new MemoryStream();
            for (int i = 0; i < tilesAll.Count; ++i)
            {
                int x = (i % (width / 8));
                int y = (i / (width / 8));

                int idx = 0;
                for (int l = 0; l < tilesAll[i].Item1.Length; ++l)
                    if ((idx = tilesAllHF.FindIndex(a => Helpers.CompareByteArray.Compare(a.Item1, tilesAll[i].Item1[l]))) >= 0)
                    {
                        ushort word = 0;
                        bool HF = false, VF = false; // !HF, !VF
                        switch (l)
                        {
                            case 1: // HF, !VF
                                HF = true; VF = false;
                                break;
                            case 2: // HF, VF
                                HF = true; VF = true;
                                break;
                            case 3: // !HF, VF
                                HF = false; VF = true;
                                break;
                        }

                        bool prior = false;
                        for (int h = 0; h < 8; h++)
                            for (int w = 0; w < 8; w++)
                            {
                                 if (maskBytes[(y + h) * maskData.Stride + (x + w)] == 1)
                                {
                                    prior = true;
                                    break;
                                }
                            }

                        word = Mapper.EncodeTileInfo((ushort)idx, HF, VF, (byte)(mask.Palette.Entries[1].R >> 4), prior);
                        stream.WriteWordInc(0, word);
                        break;
                    }
            }

            mask.UnlockBits(maskData);

            width /= 8;
            height /= 8;

            mapping = Mapper.ByteMapToWordMap(stream.ToArray());
            palette = PaletteFromImage(image);

            MemoryStream tilesStream = new MemoryStream();

            int tilesCount = tilesAllHF.Count;
            for (int x = 0; x < tilesCount; x++)
            {
                Bitmap tile = Helpers.BitmapFromArray(tilesAllHF[x].Item1);

                byte[] tileBytes;
                TileToData(tile, out tileBytes, palette);
                tilesStream.Write(tileBytes, 0, tileBytes.Length);
            }
            tiles = tilesStream.ToArray();
        }

        public static Color[] PaletteFromImage(Bitmap image)
        {
            List<Color> colors = new List<Color>();

            for (int i = 0; i < image.Palette.Entries.Length; ++i)
            {
                colors.Add(image.Palette.Entries[i]);
            }

            colors = colors.Distinct().ToList();

            Color[] retn = new Color[16];
            for (int i = 0; i < Math.Min(colors.Count, retn.Length); ++i)
                retn[i] = ColorApplySega(colors[i]);

            return retn;
        }

        public static ushort GetVramWriteAddr(uint value)
        {
            return (ushort)(((value & Helpers.mask(0, 2)) << 14) | ((value & Helpers.mask(16, 14)) >> 16));
        }

        public static uint SetVramWriteAddr(ushort value)
        {
            return (uint)(((value >> 14) & Helpers.mask(0, 2)) | ((value << 16) & Helpers.mask(16, 14)));
        }
    }

    public static class Mapper
    {
        public static ushort TileIdx(ushort Word)
        {
            return (ushort)(Word & 0x7FF);
        }

        public static byte PalIdx(ushort Word)
        {
            return (byte)((Word & 0x6000) >> 13);
        }

        public static bool HF(ushort Word)
        {
            return ((Word & 0x800) >> 11) == 1;
        }

        public static bool VF(ushort Word)
        {
            return ((Word & 0x1000) >> 12) == 1;
        }

        public static bool P(ushort Word)
        {
            return ((Word & 0x8000) >> 15) == 1;
        }

        public static ushort ApplyTileIdx(ushort Word, ushort tileIdx)
        {
            return (ushort)((Word & ~0x07FF) | tileIdx);
        }

        public static ushort ApplyPalIdx(ushort Word, byte palIdx)
        {
            return (ushort)((Word & ~0x6000) | (palIdx << 13));
        }

        public static ushort ApplyHF(ushort Word, int hf)
        {
            return (ushort)((Word & ~0x0800) | (hf << 11));
        }

        public static ushort ApplyVF(ushort Word, int vf)
        {
            return (ushort)((Word & ~0x1000) | (vf << 12));
        }

        public static ushort ApplyP(ushort Word, int p)
        {
            return (ushort)((Word & ~0x8000) | (p << 15));
        }

        public static ushort TilePos(ushort Word)
        {
            ushort idx = TileIdx(Word);
            ushort tilesPos = (ushort)(idx * 0x20);
            return tilesPos;
        }

        public static ushort EncodeTileInfo(ushort idx, bool hf, bool vf, byte palIndex, bool p)
        {
            int retn = ((p ? 1 : 0) << 15) | ((palIndex & 3) << 13) | ((vf ? 1 : 0) << 12) | ((hf ? 1 : 0) << 11) | (idx & 0x7FF);
            return (ushort)retn;
        }

        public static byte[] WordMapToByteMap(ushort[] wordMap, ushort StartMask = 0)
        {
            int len = wordMap.Length;

            byte[] retn = new byte[len * 2];
            for (int i = 0, j = 0; i < len; i++, j += 2)
            {
                retn[j] = (byte)(((wordMap[i] & 0xFF00) >> 8) - StartMask);
                retn[j + 1] = (byte)((wordMap[i] & 0xFF) - StartMask);
            }

            return retn;
        }

        public static ushort[] ByteMapToWordMap(byte[] byteMap, ushort StartMask = 0)
        {
            int len = byteMap.Length / 2;
            ushort[] retn = new ushort[len];
            for (int i = 0, j = 0; i < len; i++, j += 2)
                retn[i] = (ushort)((((byteMap[j] << 8) | byteMap[j + 1]) & 0xFFFF) + StartMask);
            return retn;
        }
    }
}