using Helper;
using PluginVideoSega;
using System;
using System.Drawing;
using System.IO;
using WinImploder;

namespace CodemastersBinImage
{
    public class BinImageData
    {
        byte[] data = null;

        public static BinImageData FromFile(string fileName)
        {
            if (!File.Exists(fileName)) return null;

            bool isBitmap;
            Bitmap image = null;
            try
            {
                image = (Bitmap.FromFile(fileName) as Bitmap);
                isBitmap = (image != null);
            }
            catch
            {
                isBitmap = false;
            }

            if (isBitmap)
            {
                return new BinImageData(image);
            }
            else
            {
                byte[] bytes = File.ReadAllBytes(fileName);

                return new BinImageData(bytes);
            }
        }

        public BinImageData(byte[] data)
        {
            if (ImploderWork.CheckImp(data))
            {
                this.data = ImploderWork.Explode(data);
            }
            else
            {
                this.data = data;
            }
        }

        public BinImageData(Bitmap image)
        {
            byte[] tiles;
            ushort[] mapping;
            Color[] palette;
            ushort width;
            ushort height;

            VideoSega.ImageToData(image, 8, 8, out tiles, out mapping, out palette, out width, out height);
            image.Dispose();

            MemoryStream dataStream = new MemoryStream();
            dataStream.WriteWordInc(0, (ushort)(tiles.Length / TileSize));
            dataStream.WriteWordInc(0, width);
            dataStream.WriteWordInc(0, height);

            byte[] palBytes = VideoSega.PaletteToByteArray(palette);
            dataStream.Write(palBytes, 0, palBytes.Length);

            byte[] mapBytes = Mapper.WordMapToByteMap(mapping);
            dataStream.Write(mapBytes, 0, mapBytes.Length);

            dataStream.Write(tiles, 0, tiles.Length);

            data = dataStream.ToArray();
        }

        public byte[] Data
        {
            get
            {
                if (data != null)
                {
                    return ImploderWork.ImplodeBest(data, (uint)data.Length);
                }
                return data;
            }
        }

        public int TilesCountOffset
        {
            get { return 0; }
        }

        public ushort TilesCount
        {
            get
            {
                if (data == null) return 0;
                return data.ReadWord(TilesCountOffset);
            }
            set
            {
                if (data == null) return;
                data.WriteWord(TilesCountOffset, value);
            }
        }

        public int WidthOffset
        {
            get { return TilesCountOffset + 2; }
        }

        public ushort Width
        {
            get
            {
                if (data == null) return 0;
                ushort width = data.ReadWord(WidthOffset);
                return (ushort)((width > 0xFF) ? 0 : width);
            }
            set
            {
                if (data == null) return;
                data.WriteWord(WidthOffset, (ushort)((value > 0xFF) ? 0 : value));
            }
        }

        public int HeightOffset
        {
            get { return WidthOffset + 2; }
        }

        public ushort Height
        {
            get
            {
                if (data == null) return 0;
                ushort height = data.ReadWord(HeightOffset);
                return (ushort)((height > 0xFF) ? 0 : height);
            }
            set
            {
                if (data == null) return;
                data.WriteWord(HeightOffset, (ushort)((value > 0xFF) ? 0 : value));
            }
        }

        public int PaletteOffset
        {
            get { return HeightOffset + 2; }
        }

        public int PaletteSize
        {
            get { return 0x20; }
        }

        public Color[] Palette
        {
            get
            {
                if (data == null) return null;
                if (PaletteOffset + PaletteSize > data.Length) return null;

                byte[] pal = new byte[PaletteSize];
                Array.Copy(data, PaletteOffset, pal, 0, pal.Length);

                return VideoSega.PaletteFromByteArray(pal);
            }

            set
            {
                if (data == null) return;
                byte[] pal = VideoSega.PaletteToByteArray(value);
                Array.Copy(pal, 0, data, PaletteOffset, PaletteSize);
            }
        }

        public int MappingOffset
        {
            get { return PaletteOffset + PaletteSize; }
        }

        public int MappingSize
        {
            get
            {
                ushort width = Width;
                ushort height = Height;

                if (width * height == 0) return 0;

                return width * height * 2;
            }
        }

        public ushort[] Mapping
        {
            get
            {
                if (data == null) return null;
                if (MappingOffset + MappingSize > data.Length) return null;

                ushort[] mapping = new ushort[MappingSize / 2];

                for (int i = 0; i < mapping.Length; ++i)
                {
                    mapping[i] = data.ReadWord(MappingOffset + i * 2);
                }

                return mapping;
            }

            set
            {
                if (data == null) return;

                for (int i = 0; i < value.Length; ++i)
                {
                    data.WriteWord(MappingOffset + i * 2, value[i]);
                }
            }
        }

        public int TilesOffset
        {
            get { return MappingOffset + MappingSize; }
        }

        public int TileSize
        {
            get { return 0x20; }
        }

        public int TilesSize
        {
            get { return TilesCount * TileSize; }
        }

        public byte[] Tiles
        {
            get
            {
                if (data == null) return null;
                if (TilesOffset + TilesSize > data.Length) return null;

                byte[] tiles = new byte[TilesSize];

                Array.Copy(data, TilesOffset, tiles, 0, TilesSize);

                return tiles;
            }

            set
            {
                ushort width = Width;
                ushort height = Height;

                if (width * height == 0) return;

                TilesCount = (ushort)(value.Length / TileSize);
                Array.Copy(value, 0, data, TilesOffset, TilesSize);
            }
        }

        public Bitmap Image
        {
            get
            {
                if (TilesCount == 0 || MappingSize == 0) return null;

                return VideoSega.ImageFromData(Tiles, Mapping, Palette, Width, Height);
            }
        }
    }
}
