using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using SabreTools.IO.Extensions;
using static psxt001z.LibCrypt;

namespace psxt001z
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine($"psxt001z by Dremora (ported by Matt Nadareski), {VERSION}");
            Console.WriteLine();

            if (args.Length == 0)
            {
                Help();
                return 1;
            }

            switch (args[0])
            {
                case "--checksums" or "-c":
                    Checksums(args);
                    break;

                case "--libcrypt" or "-l":
                    DetectLibCrypt(args.Skip(2).ToArray());
                    break;

                case "--libcryptdrv":
                    DetectLibCryptDrive(args.Skip(2).ToArray());
                    break;

                case "--libcryptdrvfast":
                    DetectLibCryptDriveFast(args.Skip(2).ToArray());
                    break;

                case "--xorlibcrypt":
                    XorLibCrypt();
                    break;

                case "--zektor" when args.Length == 2:
                    Zektor(args[1]);
                    break;

                case "--antizektor" when args.Length == 2:
                    AntiZektor(args[1]);
                    break;

                case "--patch" when args.Length == 4:
                    Patch(args);
                    break;

                case "--resize" when args.Length == 3:
                    Resize(args);
                    break;

                case "--track" when args.Length >= 5 && args.Length <= 9:
                    var trackfix = new Track(args);
                    while (!trackfix.GuessOffsetCorrection()) { }
                    trackfix.Done();
                    break;

                case "--str" when args.Length == 4:
                    Str(args);
                    break;

                case "--str2bs" when args.Length == 2:
                    Str2Bs(args);
                    break;

                case "--gen" when args.Length == 3 || args.Length == 4:
                    Generate(args);
                    break;

                case "--scan" when args.Length == 2:
                    LibCrypt.Info(args[1], false);
                    break;

                case "--fix" when args.Length == 2:
                    LibCrypt.Info(args[1], true);
                    break;

                case "--sub" when args.Length == 3:
                    Sub(args[1], args[2]);
                    break;

                case "--m3s" when args.Length == 2:
                    M3S(args[1]);
                    break;

                case "--matrix" when args.Length == 5:
                    Matrix(args);
                    break;

                default:
                    if (args.Length == 1)
                        Info(args[1]);
                    else
                        Help();
                    break;
            }

            return 1;
        }

        private static void Info(string filename)
        {
            Stream image;
            try
            {
                image = File.OpenRead(filename);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening file \"{filename}\"! {ex}");
                return;
            }

            var file = new FileTools(image);
            long imagesize = image.Length;

            Console.WriteLine($"File: {filename}");
            if (imagesize % 2352 != 0)
            {
                Console.WriteLine($"File \"{filename}\" is not Mode2/2352 image!");
                image.Close();
                return;
            }

            long realsectors = file.GetImageSize();
            long imagesectors = imagesize / 2352;
            long realsize = realsectors * 2352;
            if (imagesize == realsize)
            {
                Console.WriteLine($"Size (bytes):   {imagesize} (OK)");
                Console.WriteLine($"Size (sectors): {imagesectors} (OK)");
            }
            else
            {
                Console.WriteLine($"Size (bytes):   {imagesize}");
                Console.WriteLine($"From image:     {realsize}");
                Console.WriteLine($"Size (sectors): {imagesectors}");
                Console.WriteLine($"From image:     {realsectors}");
            }

            Console.WriteLine($"EDC in Form 2 sectors: {(GetEDC(image) ? "YES" : "NO")}");

            string exe = file.GetExecutableName();
            Console.WriteLine($"ID: {exe.Substring(0, 4)}-{exe.Substring(5)}");
            Console.WriteLine($"Date: {file.GetDate()}");

            Console.Write("System area: ");
            image.Seek(0, SeekOrigin.Begin);

            byte[] buffer = new byte[2352];
            var crc = new CRC32();
            for (uint i = 0; i < 16; i++)
            {
                _ = image.Read(buffer, 0, 2352);
                crc.Calculate(buffer, 0, 2352);
            }

            uint imagecrc = crc.Hash;
            switch (imagecrc)
            {
                case 0x11e3052d: Console.WriteLine("Eu EDC"); break;
                case 0x808c19f6: Console.WriteLine("Eu NoEDC"); break;
                case 0x70ffa73e: Console.WriteLine("Eu Alt NoEDC"); break;
                case 0x7f9a25b1: Console.WriteLine("Eu Alt 2 EDC"); break;
                case 0x783aca30: Console.WriteLine("Jap EDC"); break;
                case 0xe955d6eb: Console.WriteLine("Jap NoEDC"); break;
                case 0x9b519a2e: Console.WriteLine("US EDC"); break;
                case 0x0a3e86f5: Console.WriteLine("US NoEDC"); break;
                case 0x6773d4db: Console.WriteLine("US Alt NoEDC"); break;
                default: Console.WriteLine($"Unknown, crc {imagecrc:x}"); break;
            }
            Console.WriteLine();

            image.Close();
            return;
        }

        private static void Help()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("======");
            Console.WriteLine("psxt001z.exe image.bin");
            Console.WriteLine("  Display image's info.");
            Console.WriteLine();
            Console.WriteLine("psxt001z.exe --scan image.bin");
            Console.WriteLine("  Scan image.bin postgap for errors.");
            Console.WriteLine();
            Console.WriteLine("psxt001z.exe --fix image.bin");
            Console.WriteLine("  Scan image.bin postgap for errors and fix them.");
            Console.WriteLine();
            Console.WriteLine("psxt001z.exe --libcryptdrvfast <drive letter>");
            Console.WriteLine("  Check subchannels for LibCrypt protection using new detection\n  method (disc).");
            Console.WriteLine();
            Console.WriteLine("psxt001z.exe --checksums file [start [end]]");
            Console.WriteLine("  Calculate file's checksums (CRC-32, MD5, SHA-1).");
            Console.WriteLine("  [in] file   Specifies the file, which checksums will be calculated.");
            Console.WriteLine("       start  Specifies start position for checksums calculation.");
            Console.WriteLine("       size   Specifies size of block for checksums calculation.");
            Console.WriteLine();
            Console.WriteLine("psxt001z.exe --zektor image.bin");
            Console.WriteLine("  Zektor. Replace EDC in Form 2 Mode 2 sectors with zeroes.");
            Console.WriteLine();
            Console.WriteLine("psxt001z.exe --antizektor image.bin");
            Console.WriteLine("  Antizektor. Restore EDC in Form 2 Mode 2 sectors.");
            Console.WriteLine();
            Console.WriteLine("psxt001z.exe --resize image.bin size");
            Console.WriteLine("  Resize file to requested size.");
            Console.WriteLine();
            Console.WriteLine("psxt001z.exe --patch image.bin patch.bin offset");
            Console.WriteLine("  Insert patch.bin into image.bin, skipping given number of bytes from the\n  offset.");
            Console.WriteLine();
            Console.WriteLine("psxt001z.exe --track image.bin bytes_to_skip size crc-32 [r] [+/-/f] [s filename]");
            Console.WriteLine("  Try to guess an offset correction of the image dump by searching a track with\n  given size and CRC-32.\n  r - Calculate crc with RIFF header.\n  +/- - Search only for positive or negative offset correction.\n  s - Save track with given filename.");
            Console.WriteLine();
            Console.WriteLine("psxt001z.exe --gen file.bin filesize [-r]");
            Console.WriteLine("  Generate a file of the requested size.\n  -r - add RIFF header.");
            Console.WriteLine();
            Console.WriteLine("psxt001z.exe --str file.str video.str audio.xa");
            Console.WriteLine("  Deinterleave file.str to video.str and audio.xa.");
            Console.WriteLine();
            Console.WriteLine("psxt001z.exe --str2bs file.str");
            Console.WriteLine("  Convert file.str to .bs-files.");
            Console.WriteLine();
            Console.WriteLine("psxt001z.exe --sub subchannel.sub size");
            Console.WriteLine("  Generate RAW subchannel with given size (in sectors).");
            Console.WriteLine();
            Console.WriteLine("psxt001z.exe --m3s subchannel.m3s");
            Console.WriteLine("  Generate M3S subchannel.");
            Console.WriteLine();
            Console.WriteLine("psxt001z.exe --libcrypt <sub> [<sbi>]");
            Console.WriteLine("Usage: psxt001z.exe --libcrypt <sub> [<sbi>]");
            Console.WriteLine("  Check subchannels for LibCrypt protection. (file)");
            Console.WriteLine("  [in]  <sub>   Specifies the subchannel file to be scanned.");
            Console.WriteLine("  [out] <sbi>   Specifies the subchannel file in SBI format where protected\n  sectors will be written.");
            Console.WriteLine();
            Console.WriteLine("psxt001z.exe --libcryptdrv <drive letter>");
            Console.WriteLine("  Check subchannels for LibCrypt protection (disc).");
            Console.WriteLine();
            Console.Write("Press any key to continue...");
            Console.ReadKey();
        }

        private static bool Patch(string[] args)
        {
            Stream f1, f2;
            try
            {
                f1 = File.Open(args[1], FileMode.Open, FileAccess.ReadWrite);
                f2 = File.Open(args[2], FileMode.Open, FileAccess.Read);

                f1.Seek(long.Parse(args[3]), SeekOrigin.Begin);

                Console.WriteLine($"Patching \"{args[1]}\" with \"{args[2]}\", skipping {args[3]} bytes...");

                int i = 0;
                while (f1.Position < f1.Length && f2.Position < f2.Length)
                {
                    byte[] buffer = new byte[1];
                    _ = f2.Read(buffer, 0, 1);
                    f1.Write(buffer, 0, 1);
                    i++;
                }

                Console.WriteLine("Done!");
                Console.WriteLine();
                Console.WriteLine($"{i} bytes were replaced");
                Console.WriteLine("File was successully patched!");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        private static bool Resize(string[] args)
        {
            Stream f;
            try
            {
                f = File.Open(args[1], FileMode.Open, FileAccess.ReadWrite);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }

            var image = new FileTools(f);
            uint newsize = uint.Parse(args[2]);
            switch (image.Resize(newsize))
            {
                case 0:
                    Console.Write($"File's \"{args[1]}\" size is already {newsize} bytes!");
                    break;
                case 1:
                    Console.Write($"File \"{args[1]}\" was successfully resized to {newsize} bytes!");
                    break;
                case 2:
                    Console.Write($"File \"{args[1]}\" was successfully truncated to {newsize} bytes!");
                    break;
            }

            return true;
        }

        private static bool Copy(string[] args)
        {
            //args[1] - infile
            //args[2] - outfile
            //args[3] - startbyte
            //args[4] - length

            Stream infile;
            try
            {
                infile = File.Open(args[1], FileMode.Open, FileAccess.Read);
            }
            catch
            {
                Console.WriteLine($"File {args[1]} can't be found.");
                return true;
            }

            return true;
            //HANDLE hfile = CreateFileW(args[1], GENERIC_WRITE, FILE_SHARE_READ, 0, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, 0);
            //SetFilePointer(hfile, newsize, 0, FILE_BEGIN);
            //SetEndOfFile(hfile);
        }

        private static void AntiZektor(string filename)
        {
            Stream image = File.Open(filename, FileMode.Open, FileAccess.ReadWrite);

            byte[] ecc_f_lut = new byte[256];
            byte[] ecc_b_lut = new byte[256];
            int[] edc_lut = new int[256];

            for (int a = 0; a < 256; a++)
            {
                int b = ((a << 1) ^ ((a & 0x80) != 0 ? 0x11D : 0));

                ecc_f_lut[a] = (byte)b;
                ecc_b_lut[a ^ b] = (byte)a;

                int edc_init = a;
                for (b = 0; b < 8; b++)
                {
                    edc_init = (int)((edc_init >> 1) ^ ((edc_init & 1) != 0 ? 0xD8018001 : 0));
                }

                edc_lut[a] = edc_init;
            }

            long filesize = image.Length;
            if (filesize % 2352 != 0)
            {
                Console.Write($"File '{filename}' is not Mode2/2352 image!");
                image.Close();
                return;
            }

            long sectors = filesize / 2352;
            Console.WriteLine("Converting image...");
            for (long sector = 0; sector < sectors; sector++)
            {
                image.Seek(sector * 2352 + 18, SeekOrigin.Begin);

                byte[] z = new byte[1];
                _ = image.Read(z, 0, 1);
                if ((z[0] >> 5 & 0x1) != 0)
                {
                    image.Seek(-3, SeekOrigin.Current);

                    byte[] buffer = new byte[2332];
                    _ = image.Read(buffer, 0, 2332);
                    image.Seek(0, SeekOrigin.Current);

                    buffer = BitConverter.GetBytes(CalculateEDC(buffer, 0, 2332, edc_lut));
                    image.Write(buffer, 0, 4);
                }
            }

            image.Close();
            Console.WriteLine("Done!");
            return;
        }

        private static byte Checksums(string[] args)
        {
            if (args.Length < 3 || args.Length > 5)
            {
                Console.WriteLine("psxt001z.exe --checksums file [start [end]]");
                Console.WriteLine("  Calculate file's checksums (CRC-32, MD5, SHA-1).");
                Console.WriteLine("  [in] file   Specifies the file, which checksums will be calculated.");
                Console.WriteLine("       start  Specifies start position for checksums calculation.");
                Console.WriteLine("       size   Specifies size of block for checksums calculation.");
                Console.WriteLine();
                return 0;
            }

            // Opening file
            Stream file = File.OpenRead(args[2]);
            Console.WriteLine($"File:   {args[2]}");

            double percents = 0;

            long filesize = file.Length;

            long start;
            if (args.Length > 3)
            {
                start = long.Parse(args[3]);
                if (start >= filesize)
                {
                    Console.WriteLine("Error:  start position can't be larger than filesize!");
                    return 0;
                }

                Console.WriteLine($"Start:  {start}");
            }
            else
            {
                start = 0;
            }

            long block;
            if (args.Length > 4)
            {
                block = long.Parse(args[4]);
                if (block > filesize)
                {
                    Console.WriteLine("Error:  block size can't be larger than filesize!");
                    return 0;
                }

                if (block == 0)
                {
                    Console.WriteLine("Error:  block size can't equal with zero!");
                    return 0;
                }
            }
            else
            {
                block = filesize - start;
            }

            if (block + start > filesize)
            {
                Console.WriteLine("Error:  block size and start position can't be larger than file size!");
                return 0;
            }

            Console.Write($"Size:   {block}");
            long total = (long)Math.Ceiling((double)block / 1024);

            // checksums
            byte[] Message_Digest = new byte[20];
            byte[] buffer = new byte[1024], digest = new byte[16];
            int len;

            var crc = new CRC32();
            MD5 md5 = MD5.Create();
            md5.Initialize();
            SHA1 sha1 = SHA1.Create();

            file.Seek(start, SeekOrigin.Begin);

            for (uint i = 0; i < total; i++)
            {
                if (i * 100 / total > percents)
                {
                    percents = i * 100 / total;
                    Console.Write($"\rCalculating checksums: {percents}%");
                }

                len = file.Read(buffer, 0, 1024);

                if (block <= len)
                {
                    len = (int)block;
                    block = 0;
                }
                else
                {
                    block -= 1024;
                }

                md5.TransformBlock(buffer, 0, len, null, 0);
                sha1.TransformBlock(buffer, 0, len, null, 0);
                crc.Calculate(buffer, 0, len);
            }

            md5.TransformFinalBlock(digest, 0, digest.Length);
            sha1.TransformFinalBlock(Message_Digest, 0, Message_Digest.Length);

            Console.WriteLine($"\rCRC-32: {crc.Hash:8x}                      \n");
            Console.WriteLine($"MD5:    {md5.Hash.ToHexString()}");
            Console.WriteLine($"SHA-1:  {sha1.Hash.ToHexString()}");
            Console.WriteLine();
            return 1;
        }

        private static void Generate(string[] args)
        {
            Stream f = File.OpenWrite(args[2]);
            byte[] riff = new byte[44];
            long size = long.Parse(args[3]);

            if (args.Length == 5)
            {
                if (args[4] == "-r")
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
                    Array.Copy(BitConverter.GetBytes(size - 8), 0, riff, 4, 4);
                    Array.Copy(BitConverter.GetBytes(size - 44), 0, riff, 40, 4);
                    f.Write(riff, 0, 44);
                }
            }

            f.Seek(size - 1, SeekOrigin.Begin);
            f.WriteByte(0x00);
            f.Close();

            Console.WriteLine($"File '{args[2]}' with size {size} bytes was successfully generated!");
            return;
        }

        private static void M3S(string filename)
        {
            byte[] buffer = [0x41, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

            Stream subchannel = File.Open(filename, FileMode.Create, FileAccess.Write);
            Console.Write($"File: {filename}");

            for (long sector = 13350, sector2 = sector + 150; sector < 17850; sector++, sector2++)
            {
                double mindbl = sector / 60 / 75;
                byte min = (byte)Math.Floor(mindbl);

                double secdbl = (sector - (min * 60 * 75)) / 75;
                byte sec = (byte)Math.Floor(secdbl);

                byte frame = (byte)(sector - (min * 60 * 75) - (sec * 75));

                buffer[3] = IntegerToBinary(min);
                buffer[4] = IntegerToBinary(sec);
                buffer[5] = IntegerToBinary(frame);

                mindbl = sector2 / 60 / 75;
                min = (byte)Math.Floor(mindbl);

                secdbl = (sector2 - (min * 60 * 75)) / 75;
                sec = (byte)Math.Floor(secdbl);

                frame = (byte)(sector2 - (min * 60 * 75) - (sec * 75));

                buffer[7] = IntegerToBinary(min);
                buffer[8] = IntegerToBinary(sec);
                buffer[9] = IntegerToBinary(frame);

                ushort crc = CRC16.Calculate(buffer, 0, 10);
                subchannel.Write(buffer, 0, 10);
                subchannel.WriteByte((byte)(crc >> 8));
                subchannel.WriteByte((byte)(crc & 0xFF));

                for (int i = 0; i < 4; i++)
                {
                    subchannel.WriteByte(0x00);
                }

                Console.WriteLine($"Creating M3S: {100 * sector}%\r");
            }

            Console.WriteLine("Creating M3S: 100%");
            subchannel.Close();
            Console.WriteLine("Done!");
            return;
        }

        private static void Matrix(string[] args)
        {
            Stream f1 = File.Open(args[2], FileMode.Open, FileAccess.ReadWrite);
            Stream f2 = File.Open(args[3], FileMode.Open, FileAccess.ReadWrite);
            Stream f3 = File.Open(args[4], FileMode.Open, FileAccess.ReadWrite);
            Stream f4 = File.Open(args[5], FileMode.Create, FileAccess.Write);

            long subsize = f1.Length;
            for (long i = 0; i < subsize; i++)
            {
                byte[] r1 = new byte[1];
                _ = f1.Read(r1, 0, 1);

                byte[] r2 = new byte[1];
                _ = f2.Read(r2, 0, 1);

                byte[] r3 = new byte[1];
                _ = f3.Read(r3, 0, 1);

                if (r1 == r2)
                {
                    f4.Write(r1, 0, 1);
                }
                else if (r1 == r3)
                {
                    f4.Write(r1, 0, 1);
                }
                else if (r2 == r3)
                {
                    f4.Write(r2, 0, 1);
                }
                else
                {
                    Console.WriteLine($"Byte 0x{i:x} ({i}) is different!");
                    Console.WriteLine($"{args[2]}: {r1[0]:2x}");
                    Console.WriteLine($"{args[3]}: {r2[0]:2x}");
                    Console.WriteLine($"{args[4]}: {r3[0]:2x}");
                    Console.WriteLine();
                    return;
                }
            }

            Console.WriteLine("Done!");
            return;
        }

        private static void Str(string[] args)
        {
            Stream str = File.OpenRead(args[2]);
            long filesize = str.Length;
            if (filesize % 2336 != 0)
            {
                Console.Write($"File '{args[2]}' is not in STR format!");
                str.Close();
                return;
            }

            long sectors = filesize / 2336;
            Stream video = File.OpenWrite(args[3]);

            sectors = filesize / 2336;
            Stream audio = File.OpenWrite(args[4]);

            for (long i = 0; i < sectors; i++)
            {
                str.Seek(2, SeekOrigin.Current);

                byte[] ctrlbyte = new byte[1];
                _ = str.Read(ctrlbyte, 0, 1);

                byte[] buffer = new byte[2336];
                if ((ctrlbyte[0] >> 5 & 0x1) != 0)
                {
                    str.Seek(-3, SeekOrigin.Current);
                    _ = str.Read(buffer, 0, 2336);
                    audio.Write(buffer, 0, 2336);
                }
                else
                {
                    str.Seek(5, SeekOrigin.Current);
                    _ = str.Read(buffer, 0, 2048);
                    video.Write(buffer, 0, 2048);
                    str.Seek(280, SeekOrigin.Current);
                }
            }

            str.Close();
            audio.Close();
            video.Close();

            Console.WriteLine("Done!");

            return;
        }

        private static void Str2Bs(string[] args)
        {
            byte[] buffer = new byte[2016];
            string directory = $"{args[2]}-bs";

            Stream str = File.OpenRead(args[2]);
            long filesize = str.Length;
            if (filesize % 2048 != 0)
            {
                Console.Write($"File '{args[2]}; is not in STR format!");
                str.Close();
                return;
            }

            Directory.CreateDirectory(directory);

            long numblocks = filesize / 2048;
            Stream bs = File.OpenWrite(directory + "\\000001.bs");

            str.Seek(32, SeekOrigin.Current);
            _ = str.Read(buffer, 0, 2016);
            Console.WriteLine(directory);

            bs.Write(buffer, 0, 2016);
            Console.WriteLine("2");
            Console.WriteLine($"Creating: {directory}\\000001.bs");

            for (uint i = 1; i < numblocks; i++)
            {
                str.Seek(1, SeekOrigin.Current);

                byte[] byt = new byte[1];
                _ = str.Read(byt, 0, 1);

                if (byt[0] == 0)
                {
                    bs.Close();
                    bs = File.OpenWrite(directory + $"\\{i.ToString().PadLeft(6, '0')}.bs");
                    Console.WriteLine($"Creating: {directory}\\{i.ToString().PadLeft(6, '0')}.bs");
                }

                str.Seek(30, SeekOrigin.Current);
                _ = str.Read(buffer, 0, 2016);
                bs.Write(buffer, 0, 2016);
            }

            bs.Close();
            str.Close();

            Console.WriteLine();
            Console.WriteLine("Done!");

            return;
        }

        private static void Sub(string filename, string strsectors)
        {
            long sectors = long.Parse(strsectors);
            if (sectors == 0 || sectors == -1)
            {
                Console.WriteLine("Wrong size!");
                return;
            }

            Stream subchannel = File.Open(filename, FileMode.Create, FileAccess.Write);

            Console.Write($"File: {filename}");
            Console.Write($"Size (bytes): {sectors * 96}");
            Console.Write($"Size (sectors): {sectors}");

            byte[] buffer = new byte[10];
            buffer[0] = 0x41;
            buffer[1] = 0x01;
            buffer[2] = 0x01;
            buffer[6] = 0x00;

            for (long sector = 0, sector2 = 150; sector < sectors; sector++, sector2++)
            {
                /*if (sector2 == 4350) {
                    buffer[1] = 0x02;
                    sector = 0;
                }*/

                double mindbl = sector / 60 / 75;
                byte min = (byte)Math.Floor(mindbl);

                double secdbl = (sector - (min * 60 * 75)) / 75;
                byte sec = (byte)Math.Floor(secdbl);

                byte frame = (byte)(sector - (min * 60 * 75) - (sec * 75));

                buffer[3] = IntegerToBinary(min);
                buffer[4] = IntegerToBinary(sec);
                buffer[5] = IntegerToBinary(frame);

                mindbl = sector2 / 60 / 75;
                min = (byte)Math.Floor(mindbl);

                secdbl = (sector2 - (min * 60 * 75)) / 75;
                sec = (byte)Math.Floor(secdbl);

                frame = (byte)(sector2 - (min * 60 * 75) - (sec * 75));

                buffer[7] = IntegerToBinary(min);
                buffer[8] = IntegerToBinary(sec);
                buffer[9] = IntegerToBinary(frame);

                ushort crc = CRC16.Calculate(buffer, 0, 10);

                for (int i = 0; i < 12; i++)
                {
                    subchannel.WriteByte(0x00);
                }

                subchannel.Write(buffer, 0, 10);
                subchannel.WriteByte((byte)(crc >> 8));
                subchannel.WriteByte((byte)(crc & 0xFF));

                for (int i = 0; i < 72; i++)
                {
                    subchannel.WriteByte(0x00);
                }

                Console.Write($"Creating: {(100 * sector) / sectors}%\r");
            }

            subchannel.Seek(0, SeekOrigin.Begin);
            for (int i = 0; i < 12; i++)
            {
                subchannel.WriteByte(0xFF);
            }

            Console.WriteLine("Creating: 100%");
            subchannel.Close();
            Console.WriteLine("Done!");
            return;
        }

        private static void Zektor(string filename)
        {
            byte[] zero = [0x00, 0x00, 0x00, 0x00];

            Stream image = File.Open(filename, FileMode.Open, FileAccess.ReadWrite);
            long filesize = image.Length;
            if (filesize % 2352 != 0)
            {
                Console.WriteLine($"File '{filename}' is not Mode2/2352 image!");
                image.Close();
                return;
            }

            Console.WriteLine("Converting image...");

            long sectors = filesize / 2352;
            for (long sector = 0; sector < sectors; sector++)
            {
                image.Seek(sector * 2352 + 18, SeekOrigin.Begin);

                byte[] z = new byte[1];
                _ = image.Read(z, 0, 1);
                if ((z[0] >> 5 & 0x1) != 0)
                {
                    image.Seek(2329, SeekOrigin.Current);
                    image.Write(zero, 0, 4);
                }
            }

            image.Close();
            Console.WriteLine("Done!");
            return;
        }
    }
}
