using System;
using System.Globalization;
using System.IO;

namespace psxt001z
{
    /// <see href="https://github.com/Dremora/psxt001z/blob/master/main.cpp"/>
    internal class Track
    {
        private readonly Stream _file;

        private readonly string? _path;

        private readonly uint _start;

        private readonly uint _crc;

        private readonly uint _size;

        private readonly byte[] _riff = new byte[44];

        private readonly bool _isRiff;

        private byte _mode;

        private readonly bool _smallFile;

        private readonly byte[]? _fileContents;

        private readonly bool _saveTrack;

        //private readonly long[] _fastOffset = new long[77];

        private long _offset;

        private long _current;

        public Track(string[] args)
        {
            _file = File.OpenRead(args[1]);
            _start = uint.Parse(args[2]);
            _size = uint.Parse(args[3]);
            _crc = uint.Parse(args[4], NumberStyles.HexNumber);
            _offset = 0;
            _isRiff = false;
            _current = 0;
            _smallFile = false;
            _mode = 1;
            _saveTrack = false;

            Console.WriteLine($"File: {args[1]}");
            Console.WriteLine($"Start: {_start}");
            Console.WriteLine($"Size: {_size}");
            Console.WriteLine($"CRC-32: {_crc}");

            for (byte i = 6; i < args.Length; i++)
            {
                if (args.Length >= 7)
                {
                    if (args[i] == "r")
                    {
                        _riff[0] = 0x52;
                        _riff[1] = 0x49;
                        _riff[2] = 0x46;
                        _riff[3] = 0x46;
                        _riff[8] = 0x57;
                        _riff[9] = 0x41;
                        _riff[10] = 0x56;
                        _riff[11] = 0x45;
                        _riff[12] = 0x66;
                        _riff[13] = 0x6D;
                        _riff[14] = 0x74;
                        _riff[15] = 0x20;
                        _riff[16] = 0x10;
                        _riff[17] = 0x00;
                        _riff[18] = 0x00;
                        _riff[19] = 0x00;
                        _riff[20] = 0x01;
                        _riff[21] = 0x00;
                        _riff[22] = 0x02;
                        _riff[23] = 0x00;
                        _riff[24] = 0x44;
                        _riff[25] = 0xAC;
                        _riff[26] = 0x00;
                        _riff[27] = 0x00;
                        _riff[28] = 0x10;
                        _riff[29] = 0xB1;
                        _riff[30] = 0x02;
                        _riff[31] = 0x00;
                        _riff[32] = 0x04;
                        _riff[33] = 0x00;
                        _riff[34] = 0x10;
                        _riff[35] = 0x00;
                        _riff[36] = 0x64;
                        _riff[37] = 0x61;
                        _riff[38] = 0x74;
                        _riff[39] = 0x61;
                        _isRiff = true;
                        Array.Copy(BitConverter.GetBytes(_size - 8), 0, _riff, 4, 4);
                        Array.Copy(BitConverter.GetBytes(_size - 44), 0, _riff, 40, 4);
                        _size -= 44;
                    }
                    else if (args[i][0] == '+')
                    {
                        _mode = (byte)'p';
                    }
                    else if (args[i][0] == '-')
                    {
                        _mode = (byte)'n';
                    }
                    else if (args[i] == "s")
                    {
                        _saveTrack = true;
                        i++;
                        _path = args[i];
                    }
                }
            }

            if (_size <= 100000000)
            {
                _fileContents = new byte[_size + 40000];
                _file.Seek(_start - 20000, SeekOrigin.Begin);
                //f.Read(fcontents, 0, (int)size);
                for (uint i = 0; i < _size + 40000; i++)
                {
                    if (_start + i <= 20000)
                        _file.Seek(_start + i - 20000, SeekOrigin.Begin);
                    if (_file.Read(_fileContents, (int)i, 1) == 0)
                        _fileContents[i] = 0x00;
                }
                _smallFile = true;
            }
        }

        public bool GuessOffsetCorrection()
        {
            if (_mode == 'p')
            {
                if (_current > 20000)
                {
                    _mode = 0;
                    return true;
                }

                _offset = _current;
                _current += 4;
                return Calculate();
            }
            else if (_mode == 'n')
            {
                if (_current > 20000)
                {
                    _mode = 0;
                    return true;
                }

                _offset = -_current;
                _current += 4;
                return Calculate();
            }
            else
            {
                if (_current > 20000)
                {
                    _mode = 0;
                    return true;
                }

                _offset = _current;
                if (Calculate())
                {
                    return true;
                }
                else
                {
                    _offset = -_current;
                    _current += 4;
                    return Calculate();
                }
            }
        }

        private bool Calculate()
        {
            var calc = new CRC32();
            if (_smallFile && _fileContents != null)
            {
                if (_isRiff)
                    calc.Calculate(_riff, 0, 44);

                calc.Calculate(_fileContents, (int)(20000 + _offset), (int)_size);
            }
            else
            {
                var buffer = new byte[1];
                uint i;
                _file.Seek(_start + _offset, SeekOrigin.Begin);
                if (_isRiff)
                    calc.Calculate(_riff, 0, 44);

                for (i = 0; i < _size; i++)
                {
                    if (_file.Read(buffer, 0, 1) == 0)
                    {
                        buffer[0] = 0x00;
                        _file.Seek(_start + _offset + i + 1, SeekOrigin.Begin);
                    }
                    calc.Calculate(buffer, 0, 1);
                }
            }

            Console.WriteLine($"Offset correction {_offset} bytes, {_offset / 4} samples, CRC-32 {calc.Hash:08x}");
            return (calc.Hash == _crc);
        }

        public void Done()
        {
            //if (smallfile)
            //    fcontents = null;

            if (_mode == 0x00)
            {
                Console.WriteLine("\nCan't find offset!");
            }
            else
            {
                if (_saveTrack && !string.IsNullOrWhiteSpace(_path))
                {
                    byte[] buffer = new byte[1];
                    Stream f2 = File.OpenWrite(_path);
                    if (_isRiff)
                        f2.Write(_riff, 0, 44);

                    _file.Seek(_start + _offset, SeekOrigin.Begin);
                    for (uint i = 0; i < _size; i++)
                    {
                        if (_file.Read(buffer, 0, 1) == 0)
                        {
                            buffer[0] = 0x00;
                            _file.Seek(_start + _offset + i + 1, SeekOrigin.Begin);
                        }

                        f2.Write(buffer, 0, 1);
                    }
                }

                Console.WriteLine($"\nDONE!\n\nOffset correction: {_offset} bytes / {_offset / 4} samples");
            }
        }
    }
}
