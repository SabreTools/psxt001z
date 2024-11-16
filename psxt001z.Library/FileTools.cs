using System;
using System.IO;
using System.Text;
using SabreTools.IO.Extensions;

namespace psxt001z
{
    /// <see href="https://github.com/Dremora/psxt001z/blob/master/main.cpp"/>
    public class FileTools(Stream file)
    {
        private readonly Stream _file = file;

        private readonly byte[] _executableName = new byte[20];

        private readonly byte[] _dateValue = new byte[11];

        public long Size() => _file.Length;

        public string GetExecutableName()
        {
            _file.Seek(51744, SeekOrigin.Begin);

            string filename = string.Empty;
            while (filename != "SYSTEM.CNF")
            {
                byte[] buf = _file.ReadBytes(10);
                filename = Encoding.ASCII.GetString(buf);
                _file.Seek(-9, SeekOrigin.Current);
            }

            _file.Seek(-32, SeekOrigin.Current);
            uint lba = _file.ReadUInt32();

            _file.Seek((2352 * lba) + 29, SeekOrigin.Begin);
            byte[] buffer = _file.ReadBytes(6);

            string iniLine = Encoding.ASCII.GetString(buffer);
            while (iniLine != "cdrom:")
            {
                _file.Seek(-5, SeekOrigin.Current);
                buffer = _file.ReadBytes(6);
                iniLine = Encoding.ASCII.GetString(buffer);
            }

            buffer = _file.ReadBytes(1);
            if (buffer[0] != '\\')
                _file.Seek(-1, SeekOrigin.Current);

            int i = -1;
            do
            {
                _ = _file.Read(buffer, ++i, 1);
            } while (buffer[i] != ';');

            for (long a = 0; a < i; a++)
            {
                _executableName[a] = (byte)char.ToUpper((char)buffer[a]);
            }

            return Encoding.ASCII.GetString(_executableName);
        }

        public string GetDate()
        {
            _file.Seek(51744, SeekOrigin.Begin);

            byte[] buffer = new byte[12];
            do
            {
                _ = _file.Read(buffer, 0, 11);
                buffer[11] = 0;
                _file.Seek(-10, SeekOrigin.Current);
            } while (Encoding.ASCII.GetString(_executableName) != Encoding.ASCII.GetString(buffer));

            _file.Seek(-16, SeekOrigin.Current);

            byte[] datenofrmt = _file.ReadBytes(3);
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
            _ = _file.Read(sizebuf, 0, 4);
            return BitConverter.ToInt32(sizebuf, 0);
        }
    }
}
