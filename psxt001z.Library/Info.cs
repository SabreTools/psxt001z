using System;
using System.IO;
using System.Linq;
using SabreTools.IO.Extensions;

namespace psxt001z
{
    /// <see href="https://github.com/Dremora/psxt001z/blob/master/info.cpp"/>
    public partial class LibCrypt
    {
        private static readonly byte[] edc_form_2 = [0x3F, 0x13, 0xB0, 0xBE];

        private static readonly byte[] syncheader = [0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00];

        private static readonly byte[] subheader = [0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x20, 0x00];

        public static bool Info(string filename, bool fix)
        {
            #region Variables

            bool errors = false;
            byte[] buffer = new byte[2352], buffer2 = new byte[2352];

            #endregion

            #region Opening image

            Stream image;
            try
            {
                FileAccess open_mode = fix ? FileAccess.ReadWrite : FileAccess.Read;
                image = File.Open(filename, FileMode.Open, open_mode);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return false;
            }

            long size = image.Length;
            Console.WriteLine($"File: {filename}");

            #endregion

            #region Type

            _ = image.Read(buffer, 0, 12);

            int sectorsize;
            if (buffer.Take(12).SequenceEqual(syncheader.Take(12)))
            {
                sectorsize = 2352;
            }
            else
            {
                sectorsize = 2048;
            }
            if (size % sectorsize != 0)
            {
                Console.WriteLine($"{filename}: not ModeX/{sectorsize} image!");
                return false;
            }

            long sectors = size / sectorsize;

            #endregion

            #region Mode

            if (sectorsize == 2352)
            {
                image.Seek(0x0F, SeekOrigin.Begin);
                syncheader[15] = (byte)image.ReadByte();
                if (syncheader[15] != 1 && syncheader[15] != 2)
                {
                    Console.WriteLine($"{filename}: unknown mode!");
                    return false;
                }
            }
            else
            {
                syncheader[15] = 0xFF;
            }

            #endregion

            #region Size

            image.Seek(sectorsize * 16 + ((syncheader[15] == 2) ? 24 : ((syncheader[15] == 1) ? 16 : 0)) + 0x50, SeekOrigin.Begin);

            // ISO size
            int realsectors = image.ReadInt32();
            image.Seek(0, SeekOrigin.Begin);
            int realsize = realsectors * sectorsize;

            if (sectors == realsectors)
            {
                Console.WriteLine($"Size (bytes):   {size} (OK)");
                Console.WriteLine($"Size (sectors): {sectors} (OK)");
            }
            else
            {
                Console.WriteLine($"Size (bytes):   {size}");
                Console.WriteLine($"From image:     {realsize}");
                Console.WriteLine($"Size (sectors): {sectors}");
                Console.WriteLine($"From image:     {realsectors}");
            }

            #endregion

            #region Mode

            if (syncheader[15] > 0)
                Console.WriteLine($"Mode: {syncheader[15]}");

            if (syncheader[15] == 2)
            {
                #region EDC in Form 2

                bool imageedc = GetEDC(image);
                Console.WriteLine($"EDC in Form 2 sectors: {(imageedc ? "YES" : "NO")}");

                #endregion

                #region Sysarea

                string systemArea = "System area: ";
                image.Seek(0, SeekOrigin.Begin);

                var crc = new CRC32();
                for (int i = 0; i < 16; i++)
                {
                    _ = image.Read(buffer, 0, 2352);
                    crc.Calculate(buffer, 0, 2352);
                }

                uint imagecrc = crc.Hash;
                systemArea += imagecrc switch
                {
                    0x11e3052d => "Eu EDC",
                    0x808c19f6 => "Eu NoEDC",
                    0x70ffa73e => "Eu Alt NoEDC",
                    0x7f9a25b1 => "Eu Alt 2 EDC",
                    0x783aca30 => "Jap EDC",
                    0xe955d6eb => "Jap NoEDC",
                    0x9b519a2e => "US EDC",
                    0x0a3e86f5 => "US NoEDC",
                    0x6773d4db => "US Alt NoEDC",
                    _ => $"Unknown, crc {imagecrc:8x}",
                };
                Console.WriteLine(systemArea);

                #endregion

                #region Postgap

                image.Seek((sectors - 150) * sectorsize + 16, SeekOrigin.Begin);
                _ = image.Read(buffer, 0, 2336);

                string postgap = "Postgap type: Form ";
                if ((buffer[2] >> 5 & 0x01) != 0)
                {
                    postgap += "2";
                    if (buffer.Take(8).SequenceEqual(subheader))
                        postgap += ", zero subheader";
                    else
                        postgap += ", non-zero subheader";

                    if (ZeroCompare(buffer, 8, 2324))
                        postgap += ", zero data";
                    else
                        postgap += ", non-zero data";

                    if (ZeroCompare(buffer, 2332, 4))
                        postgap += ", no EDC";
                    else
                        postgap += ", EDC";
                }
                else
                {
                    postgap += "1";
                    if (ZeroCompare(buffer, 0, 8))
                        postgap += ", zero subheader";
                    else
                        postgap += ", non-zero subheader";

                    if (ZeroCompare(buffer, 8, 2328))
                        postgap += ", zero data";
                    else
                        postgap += ", non-zero data";
                }

                Console.WriteLine(postgap);
                Array.Copy(buffer, buffer2, 2336);

                #endregion
            }

            if (syncheader[15] < 0)
                return true;

            for (long sector = sectors - 150; sector < sectors; sector++)
            {
                bool bad = false;
                image.Seek(sector * sectorsize, SeekOrigin.Begin);
                _ = image.Read(buffer, 0, sectorsize);

                string sectorInfo = string.Empty;

                #region Sync

                MSF(sector, syncheader, 12);
                if (!syncheader.SequenceEqual(buffer.Take(16)))
                {
                    sectorInfo += $"Sector {sector}: Sync/Header";
                    bad = true;
                    if (fix)
                    {
                        image.Seek(sector * sectorsize, SeekOrigin.Begin);
                        image.Write(syncheader, 0, 16);
                        sectorInfo += (" (fixed)");
                    }
                }

                #endregion

                #region Mode 2

                if (syncheader[15] == 2 && buffer.Skip(16).Take(2336).SequenceEqual(buffer2))
                {
                    if (bad)
                    {
                        sectorInfo += ", Subheader/Data/EDC/ECC";
                    }
                    else
                    {
                        sectorInfo += $"\nSector {sector}: Subheader/Data/EDC/ECC";
                        bad = true;
                    }

                    if (fix)
                    {
                        image.Seek(sector * sectorsize + 16, SeekOrigin.Begin);
                        image.Write(buffer2, 0, 2336);
                        sectorInfo += " (fixed)";
                    }
                }

                #endregion

                Console.WriteLine(sectorInfo);

                if (bad && (sector + 1 != sectors))
                    errors = true;
            }

            if (errors)
            {
                Console.WriteLine("NOTICE: One or more errors were found not in the last sector.");
                Console.WriteLine("Please mention this when submitting dump info.");
            }
            else
            {
                Console.WriteLine("Done.");
            }

            #endregion

            return true;
        }
    }
}
