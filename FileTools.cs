using System;
using System.IO;
using System.Text;

namespace psxt001z
{
    /// <see href="https://github.com/Dremora/psxt001z/blob/master/main.cpp"/>
    internal class FileTools
    {
        private Stream _file;

        private byte[] _executableName = new byte[20];

        private byte[] _dateValue = new byte[11];

        public FileTools(Stream file)
        {
            _file = file;
        }

        public long Size() => _file.Length;

        public string GetExecutableName()
        {
            _file.Seek(51744, SeekOrigin.Begin);

            string filename = string.Empty;
            while (filename != "SYSTEM.CNF")
            {
                byte[] buf = new byte[10];
                _file.Read(buf, 0, 10);
                filename = Encoding.ASCII.GetString(buf);
                _file.Seek(-9, SeekOrigin.Current);
            }

            byte[] buffer = new byte[20];

            _file.Seek(-32, SeekOrigin.Current);
            _file.Read(buffer, 0, 4);
            uint lba = BitConverter.ToUInt32(buffer, 0);

            _file.Seek((2352 * lba) + 29, SeekOrigin.Begin);
            _file.Read(buffer, 0, 6);

            string iniLine = Encoding.ASCII.GetString(buffer);
            while (iniLine != "cdrom:")
            {
                _file.Seek(-5, SeekOrigin.Current);
                _file.Read(buffer, 0, 6);
                iniLine = Encoding.ASCII.GetString(buffer);
            }

            _file.Read(buffer, 0, 1);
            if (buffer[0] != '\\')
                _file.Seek(-1, SeekOrigin.Current);

            int i = -1;
            do
            {
                _file.Read(buffer, ++i, 1);
            } while (buffer[i] != ';');

            for (long a = 0; a < i; a++)
            {
                _executableName[a] = (byte)char.ToUpper((char)buffer[a]);
            }

            return Encoding.ASCII.GetString(_executableName);
        }

        public string GetDate()
        {
            byte[] buffer = new byte[12], datenofrmt = new byte[3];

            _file.Seek(51744, SeekOrigin.Begin);

            do
            {
                _file.Read(buffer, 0, 11);
                buffer[11] = 0;
                _file.Seek(-10, SeekOrigin.Current);
            } while (Encoding.ASCII.GetString(_executableName) != Encoding.ASCII.GetString(buffer));

            _file.Seek(-16, SeekOrigin.Current);
            _file.Read(datenofrmt, 0, 3);

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

            _dateValue[4] = (byte)'-';
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

            _dateValue[7] = (byte)'-';
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

            return Encoding.ASCII.GetString(_dateValue);
        }

        public int Resize(long newsize)
        {
            long oldsize = Size();
            if (oldsize < newsize)
            {
                _file.SetLength(newsize);
                return 1;
            }
            else if (oldsize > newsize)
            {
                _file.SetLength(newsize);
                return 2;
            }
            else
            {
                return 0;
            }
        }

        public int GetImageSize()
        {
            _file.Seek(0x9368, SeekOrigin.Begin);
            byte[] sizebuf = new byte[4];
            _file.Read(sizebuf, 0, 4);
            return BitConverter.ToInt32(sizebuf, 0);
        }
    }
}
