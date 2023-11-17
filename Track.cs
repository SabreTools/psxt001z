using System;
using System.Globalization;
using System.IO;

namespace psxt001z
{
    /// <see href="https://github.com/Dremora/psxt001z/blob/master/main.cpp"/>
    internal class track
    {
        private Stream f;

        private string? path;

        private uint start;

        private uint crc;

        private uint size;

        private byte[] riff = new byte[44];

        private bool isriff;

        private byte mode;

        private bool smallfile;

        private byte[]? fcontents;

        private bool savetrack;

        //private long[] fastoffset = new long[77];

        private long offset;

        private long current;

        public track(string[] args)
        {
            f = File.OpenRead(args[1]);
            start = uint.Parse(args[2]);
            size = uint.Parse(args[3]);
            crc = uint.Parse(args[4], NumberStyles.HexNumber);
            offset = 0;
            isriff = false;
            current = 0;
            smallfile = false;
            mode = 1;
            savetrack = false;

            Console.WriteLine($"File: {args[1]}");
            Console.WriteLine($"Start: {start}");
            Console.WriteLine($"Size: {size}");
            Console.WriteLine($"CRC-32: {crc}");

            for (byte i = 6; i < args.Length; i++)
            {
                if (args.Length >= 7)
                {
                    if (args[i] == "r")
                    {
                        riff[0] = 0x52;
                        riff[1] = 0x49;
                        riff[2] = 0x46;
                        riff[3] = 0x46;
                        riff[8] = 0x57;
                        riff[9] = 0x41;
                        riff[10] = 0x56;
                        riff[11] = 0x45;
                        riff[12] = 0x66;
                        riff[13] = 0x6D;
                        riff[14] = 0x74;
                        riff[15] = 0x20;
                        riff[16] = 0x10;
                        riff[17] = 0x00;
                        riff[18] = 0x00;
                        riff[19] = 0x00;
                        riff[20] = 0x01;
                        riff[21] = 0x00;
                        riff[22] = 0x02;
                        riff[23] = 0x00;
                        riff[24] = 0x44;
                        riff[25] = 0xAC;
                        riff[26] = 0x00;
                        riff[27] = 0x00;
                        riff[28] = 0x10;
                        riff[29] = 0xB1;
                        riff[30] = 0x02;
                        riff[31] = 0x00;
                        riff[32] = 0x04;
                        riff[33] = 0x00;
                        riff[34] = 0x10;
                        riff[35] = 0x00;
                        riff[36] = 0x64;
                        riff[37] = 0x61;
                        riff[38] = 0x74;
                        riff[39] = 0x61;
                        isriff = true;
                        Array.Copy(BitConverter.GetBytes(size - 8), 0, riff, 4, 4);
                        Array.Copy(BitConverter.GetBytes(size - 44), 0, riff, 40, 4);
                        size -= 44;
                    }
                    else if (args[i][0] == '+')
                    {
                        mode = (byte)'p';
                    }
                    else if (args[i][0] == '-')
                    {
                        mode = (byte)'n';
                    }
                    else if (args[i] == "s")
                    {
                        savetrack = true;
                        i++;
                        path = args[i];
                    }
                }
            }

            if (size <= 100000000)
            {
                fcontents = new byte[size + 40000];
                f.Seek(start - 20000, SeekOrigin.Begin);
                //f.Read(fcontents, 0, (int)size);
                for (uint i = 0; i < size + 40000; i++)
                {
                    if (start + i <= 20000)
                        f.Seek(start + i - 20000, SeekOrigin.Begin);
                    if (f.Read(fcontents, (int)i, 1) == 0)
                        fcontents[i] = 0x00;
                }
                smallfile = true;
            }
        }

        public bool trackmain()
        {
            if (mode == 'p')
            {
                if (current > 20000)
                {
                    mode = 0;
                    return true;
                }

                offset = current;
                current += 4;
                return calculate();
            }
            else if (mode == 'n')
            {
                if (current > 20000)
                {
                    mode = 0;
                    return true;
                }

                offset = -current;
                current += 4;
                return calculate();
            }
            else
            {
                if (current > 20000)
                {
                    mode = 0;
                    return true;
                }

                offset = current;
                if (calculate())
                {
                    return true;
                }
                else
                {
                    offset = -current;
                    current += 4;
                    return calculate();
                }
            }
        }

        public bool calculate()
        {
            crc32 calc = new crc32();
            if (smallfile && fcontents != null)
            {
                if (isriff)
                    calc.ProcessCRC(riff, 0, 44);

                calc.ProcessCRC(fcontents, (int)(20000 + offset), (int)size);
            }
            else
            {
                var buffer = new byte[1];
                uint i;
                f.Seek(start + offset, SeekOrigin.Begin);
                if (isriff)
                    calc.ProcessCRC(riff, 0, 44);

                for (i = 0; i < size; i++)
                {
                    if (f.Read(buffer, 0, 1) == 0)
                    {
                        buffer[0] = 0x00;
                        f.Seek(start + offset + i + 1, SeekOrigin.Begin);
                    }
                    calc.ProcessCRC(buffer, 0, 1);
                }
            }

            Console.WriteLine($"Offset correction {offset} bytes, {offset / 4} samples, CRC-32 {calc.m_crc32:08x}");
            return (calc.m_crc32 == crc);
        }

        public void done()
        {
            //if (smallfile)
            //    fcontents = null;

            if (mode == 0x00)
            {
                Console.WriteLine("\nCan't find offset!");
            }
            else
            {
                if (savetrack && !string.IsNullOrWhiteSpace(path))
                {
                    byte[] buffer = new byte[1];
                    Stream f2 = File.OpenWrite(path);
                    if (isriff)
                        f2.Write(riff, 0, 44);

                    f.Seek(start + offset, SeekOrigin.Begin);
                    for (uint i = 0; i < size; i++)
                    {
                        if (f.Read(buffer, 0, 1) == 0)
                        {
                            buffer[0] = 0x00;
                            f.Seek(start + offset + i + 1, SeekOrigin.Begin);
                        }

                        f2.Write(buffer, 0, 1);
                    }
                }

                Console.WriteLine($"\nDONE!\n\nOffset correction: {offset} bytes / {offset / 4} samples");
            }
        }
    }
}
