using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Zepheus.FiestaLib.Data
{
    public sealed class BlockInfo
    {
        public ushort MapID { get; private set; }
        public string ShortName { get; private set; }
        private Stream stream;
        private BinaryReader reader;

        private int width;
        private int height;

        public int Width { get { return width * 50; } }
        public int Height { get { return (int)(height * 6.25); } }

        public BlockInfo(string path, ushort mapid, bool cache = false)
        {
            MapID = mapid;
            ShortName = Path.GetFileNameWithoutExtension(path);
            if (cache)
            {
                stream = new MemoryStream();
                using (var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    file.CopyTo(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                }
            }
            else
            {
                stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            reader = new BinaryReader(stream);
            LoadBasics();
        }

        private void LoadBasics()
        {
            width = reader.ReadInt32();
            height  = reader.ReadInt32();
        }


        private static readonly byte[] powers = new byte[] { 1, 2, 4, 8, 16, 32, 64, 128, 255 };
        public bool CanWalk(int x, int y)
        {
            if (x <= 0 || y <= 0 || x >= Width || y >= Height) return false;
            int mapx = (int)(x / 50f);
            int mapy = (int)(y / 6.25f);

            int skipy = ((mapy - 1) * width); //gets the current Y line we're reading
            int bitoff = mapx % 8; //seeks for the bit we have to read
            long offset = 8 + skipy + mapx;
            if(offset >= stream.Length) return false;
            stream.Seek(offset, SeekOrigin.Begin);
            byte read = reader.ReadByte();
            return (read & powers[bitoff]) == 0;
        }
    }
}
