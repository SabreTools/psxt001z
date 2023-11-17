using System;
using System.IO;
using System.Text;

namespace psxt001z
{
    /// <see href="https://github.com/Dremora/psxt001z/blob/master/main.cpp"/>
    internal class filetools
    {
        private Stream f;

        private byte[] exename = new byte[20];

        private byte[] datevalue = new byte[11];

        public filetools(Stream file)
        {
            f = file;
        }

        public long size() => f.Length;

        public string exe()
        {
            f.Seek(51744, SeekOrigin.Begin);

            string filename = string.Empty;
            while (filename != "SYSTEM.CNF")
            {
                byte[] buf = new byte[10];
                f.Read(buf, 0, 10);
                filename = Encoding.ASCII.GetString(buf);
                f.Seek(-9, SeekOrigin.Current);
            }

            byte[] buffer = new byte[20];

            f.Seek(-32, SeekOrigin.Current);
            f.Read(buffer, 0, 4);
            uint lba = BitConverter.ToUInt32(buffer, 0);

            f.Seek((2352 * lba) + 29, SeekOrigin.Begin);
            f.Read(buffer, 0, 6);

            string iniLine = Encoding.ASCII.GetString(buffer);
            while (iniLine != "cdrom:")
            {
                f.Seek(-5, SeekOrigin.Current);
                f.Read(buffer, 0, 6);
                iniLine = Encoding.ASCII.GetString(buffer);
            }

            f.Read(buffer, 0, 1);
            if (buffer[0] != '\\')
                f.Seek(-1, SeekOrigin.Current);

            int i = -1;
            do
            {
                f.Read(buffer, ++i, 1);
            } while (buffer[i] != ';');

            for (long a = 0; a < i; a++)
            {
                exename[a] = (byte)char.ToUpper((char)buffer[a]);
            }

            return Encoding.ASCII.GetString(exename);
        }

        public string date()
        {
            byte[] buffer = new byte[12], datenofrmt = new byte[3];

            f.Seek(51744, SeekOrigin.Begin);

            do
            {
                f.Read(buffer, 0, 11);
                buffer[11] = 0;
                f.Seek(-10, SeekOrigin.Current);
            } while (Encoding.ASCII.GetString(exename) != Encoding.ASCII.GetString(buffer));

            f.Seek(-16, SeekOrigin.Current);
            f.Read(datenofrmt, 0, 3);

            if (datenofrmt[0] < 50)
            {
                byte[] year = Encoding.ASCII.GetBytes($"{2000 + datenofrmt[0]}");
                Array.Copy(year, 0, buffer, 0, 4);
            }
            else
            {
                byte[] year = Encoding.ASCII.GetBytes($"{1900 + datenofrmt[0]}");
                Array.Copy(year, 0, buffer, 0, 4);
            }

            datevalue[4] = (byte)'-';
            if (datenofrmt[1] < 10)
            {
                byte[] month = Encoding.ASCII.GetBytes($"0{datenofrmt[1]}");
                Array.Copy(month, 0, buffer, 5, 2);
            }
            else
            {
                byte[] month = Encoding.ASCII.GetBytes($"{datenofrmt[1]}");
                Array.Copy(month, 0, buffer, 5, 2);
            }

            datevalue[7] = (byte)'-';
            if (datenofrmt[2] < 10)
            {
                byte[] day = Encoding.ASCII.GetBytes($"0{datenofrmt[2]}");
                Array.Copy(day, 0, buffer, 8, 2);
            }
            else
            {
                byte[] day = Encoding.ASCII.GetBytes($"{datenofrmt[2]}");
                Array.Copy(day, 0, buffer, 8, 2);
            }

            return Encoding.ASCII.GetString(datevalue);
        }

        public int resize(long newsize)
        {
            long oldsize = size();
            if (oldsize < newsize)
            {
                f.SetLength(newsize);
                return 1;
            }
            else if (oldsize > newsize)
            {
                f.SetLength(newsize);
                return 2;
            }
            else
            {
                return 0;
            }
        }

        public int imagesize()
        {
            f.Seek(0x9368, SeekOrigin.Begin);
            byte[] sizebuf = new byte[4];
            f.Read(sizebuf, 0, 4);
            return BitConverter.ToInt32(sizebuf, 0);
        }
    }
}
