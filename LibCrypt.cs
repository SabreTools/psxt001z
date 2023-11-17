using System;
using System.IO;
using System.Linq;
using System.Text;
using static psxt001z.Constants;

namespace psxt001z
{
    /// <see href="https://github.com/Dremora/psxt001z/blob/master/libcrypt.cpp"/>
    public partial class LibCrypt
    {
        // TODO: Implement
        internal static byte libcryptdrv(string[] args)
        {
            Console.WriteLine("Not implemented, requires direct drive access");
            return 0x00;
        }

        // TODO: Implement
        internal static byte libcryptdrvfast(string[] args)
        {
            Console.WriteLine("Not implemented, requires direct drive access");
            return 0x00;
        }

        // TODO: Implement
        internal static void ReadSub(byte[] buffer, uint sector, Stream f, byte offset, IntPtr hDevice, ScsiPassThroughDirect SRB)
        {
            Console.WriteLine("Not implemented, requires direct drive access");
        }

        // TODO: Implement
        internal static void ClearCache(byte[] buffer, Stream f, byte offset, IntPtr hDevice, ScsiPassThroughDirect SRB)
        {
            Console.WriteLine("Not implemented, requires direct drive access");
        }

        public static bool Matrix(byte[] buffer, byte[] buffer2, byte[] buffer3, byte[] buffer4, uint length)
        {
            for (int i = 0; i < length; i++)
            {
                if (buffer[i] == buffer2[i])
                {
                    if (buffer[i] == buffer3[i])
                        continue;
                    if (buffer[i] == buffer4[i])
                        continue;
                }
                else if (buffer[i] == buffer3[i] && buffer[i] == buffer4[i])
                {
                    continue;
                }
                else if (buffer2[i] == buffer3[i] && buffer2[i] == buffer4[i])
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        public static void Deinterleave(byte[] buffer)
        {
            byte[] buffertmp = new byte[12];
            for (int i = 0; i < 12; i++)
            {
                buffertmp[i] |= (byte)((buffer[i * 8] & 0x40) << 1);
                buffertmp[i] |= (byte)((buffer[i * 8 + 1] & 0x40));
                buffertmp[i] |= (byte)((buffer[i * 8 + 2] & 0x40) >> 1);
                buffertmp[i] |= (byte)((buffer[i * 8 + 3] & 0x40) >> 2);
                buffertmp[i] |= (byte)((buffer[i * 8 + 4] & 0x40) >> 3);
                buffertmp[i] |= (byte)((buffer[i * 8 + 5] & 0x40) >> 4);
                buffertmp[i] |= (byte)((buffer[i * 8 + 6] & 0x40) >> 5);
                buffertmp[i] |= (byte)((buffer[i * 8 + 7] & 0x40) >> 6);
            }

            Array.Copy(buffertmp, buffer, 12);
            return;
        }

        public static bool DetectLibCrypt(string[] args)
        {
            if (args.Length != 1 && args.Length != 2)
            {
                Console.WriteLine("LibCrypt detector\nUsage: psxt001z.exe --libcrypt <sub> [<sbi>]");
                Console.WriteLine("  Check subchannel for LibCrypt protection.");
                Console.WriteLine("  [in]  <sub>  Specifies the subchannel file to be scanned.");
                Console.WriteLine("  [out] <sbi>  Specifies the subchannel file in SBI format where protected\n               sectors will be written.\n");
                return false;
            }

            // Variables
            byte[] buffer = new byte[16], sub = new byte[16];//, pregap = 0;
            uint sector, psectors = 0, tpos = 0;

            // Opening .sub
            Stream subfile = File.OpenRead(args[0]);

            // Checking extension
            if (Path.GetExtension(args[0]).TrimStart('.').ToLowerInvariant() != "sub")
            {
                Console.WriteLine($"{args[0]}: unknown file extension");
                return false;
            }

            // filesize
            long size = subfile.Length;
            if (size % 96 != 0)
            {
                Console.WriteLine($"{subfile}: wrong size");
                return false;
            }

            // sbi
            Stream? sbi = null;
            if (args.Length > 1 && args[1] != null)
            {
                sbi = File.OpenWrite(args[1]);
                sbi.Write(Encoding.ASCII.GetBytes("SBI\0"), 0, 4);
            }

            for (sector = 150; sector < ((size / 96) + 150); sector++)
            {
                subfile.Seek(12, SeekOrigin.Current);
                if (subfile.Read(buffer, 0, 12) != 12)
                    return true;

                subfile.Seek(72, SeekOrigin.Current);

                // New track
                if ((btoi(buffer[1]) == (btoi(sub[1]) + 1)) && (buffer[2] == 0 || buffer[2] == 1))
                {
                    Array.Copy(buffer, sub, 6);
                    tpos = (uint)((btoi((byte)(buffer[3] * 60)) + btoi(buffer[4])) * 75) + btoi(buffer[5]);
                }

                // New index
                else if (btoi(buffer[2]) == (btoi(sub[2]) + 1) && buffer[1] == sub[1])
                {
                    Array.Copy(buffer, 2, sub, 2, 4);
                    tpos = (uint)((btoi((byte)(buffer[3] * 60)) + btoi(buffer[4])) * 75) + btoi(buffer[5]);
                }

                // MSF1 [3-5]
                else
                {
                    if (sub[2] == 0)
                        tpos--;
                    else
                        tpos++;

                    sub[3] = itob((byte)(tpos / 60 / 75));
                    sub[4] = itob((byte)((tpos / 75) % 60));
                    sub[5] = itob((byte)(tpos % 75));
                }

                //MSF2 [7-9]
                sub[7] = itob((byte)(sector / 60 / 75));
                sub[8] = itob((byte)((sector / 75) % 60));
                sub[9] = itob((byte)(sector % 75));

                // CRC-16 [10-11]
                ushort crc = CRC16.Calculate(sub, 0, 10);
                sub[10] = (byte)(crc >> 8);
                sub[11] = (byte)(crc & 0xFF);

                //if (buffer[10] != sub[10] && buffer[11] != sub[11] && (buffer[3] != sub[3] || buffer[7] != sub[7] || buffer[4] != sub[4] || buffer[8] != sub[8] || buffer[5] != sub[5] || buffer[9] != sub[9])) {
                //if (buffer[10] != sub[10] || buffer[11] != sub[11] || buffer[3] != sub[3] || buffer[7] != sub[7] || buffer[4] != sub[4] || buffer[8] != sub[8] || buffer[5] != sub[5] || buffer[9] != sub[9]) {
                if (!buffer.Take(6).SequenceEqual(sub.Take(6)) || !buffer.Skip(7).Take(5).SequenceEqual(sub.Skip(7).Take(5)))
                {
                    Console.WriteLine($"MSF: {sub[7]:2x}:{sub[8]:2x}:{sub[9]:2x} Q-Data: {buffer[0]:2x}{buffer[1]:2x}{buffer[2]:2x} {buffer[3]:2x}:{buffer[4]:2x}:{buffer[5]:2x} {buffer[6]:2x} {buffer[7]:2x}:{buffer[8]:2x}:{buffer[9]:2x} {buffer[10]:2x}{buffer[11]:2x}  xor {crc ^ ((buffer[10] << 8) + buffer[11]):4x} {CRC16.Calculate(buffer, 0, 10) ^ ((buffer[10] << 8) + buffer[11]):4x}");
                    //Console.WriteLine("\nMSF: %02x:%02x:%02x Q-Data: %02x%02x%02x %02x:%02x:%02x %02x %02x:%02x:%02x %02x%02x", sub[7], sub[8], sub[9], sub[0], sub[1], sub[2], sub[3], sub[4], sub[5], sub[6], sub[7], sub[8], sub[9], sub[10], sub[11]);

                    if (buffer[3] != sub[3] && buffer[7] != sub[7] && buffer[4] == sub[4] && buffer[8] == sub[8] && buffer[5] == sub[5] && buffer[9] == sub[9])
                        Console.WriteLine($" P1 xor {buffer[3] ^ sub[3]:2x} {buffer[7] ^ sub[7]:2x}");
                    else if (buffer[3] == sub[3] && buffer[7] == sub[7] && buffer[4] != sub[4] && buffer[8] != sub[8] && buffer[5] == sub[5] && buffer[9] == sub[9])
                        Console.WriteLine($" P2 xor {buffer[4] ^ sub[4]:2x} {buffer[8] ^ sub[8]:2x}");
                    else if (buffer[3] == sub[3] && buffer[7] == sub[7] && buffer[4] == sub[4] && buffer[8] == sub[8] && buffer[5] != sub[5] && buffer[9] != sub[9])
                        Console.WriteLine($" P3 xor {buffer[5] ^ sub[5]:2x} {buffer[9] ^ sub[9]:2x}");
                    else
                        Console.WriteLine(" ?");

                    Console.WriteLine("");
                    psectors++;
                    if (sbi != null)
                    {
                        sbi.Write(sub, 7, 3);
                        sbi.Write(new byte[] { 0x01 }, 0, 1);
                        sbi.Write(buffer, 0, 10);
                    }
                }
            }
            // }

            Console.WriteLine($"Number of modified sectors: {psectors}");
            return true;
        }

        public static int XorLibCrypt()
        {
            sbyte b;
            byte d;
            byte i, a, x;
            byte[] sub = new byte[12]
            {
                0x41, 0x01, 0x01, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00
            };

            ushort crc;
            for (i = 0; i < LIBCRYPT_NUM_SECTORS; i++)
            {
                sub[3] = itob((byte)(lc_addresses[i] / 60 / 75));
                sub[4] = itob((byte)((lc_addresses[i] / 75) % 60));
                sub[5] = itob((byte)(lc_addresses[i] % 75));
                sub[7] = itob((byte)((lc_addresses[i] + 150) / 60 / 75));
                sub[8] = itob((byte)(((lc_addresses[i] + 150) / 75) % 60));
                sub[9] = itob((byte)((lc_addresses[i] + 150) % 75));
                crc = CRC16.Calculate(sub, 0, 10);
                sub[10] = (byte)(crc >> 8);
                sub[11] = (byte)(crc & 0xFF);

                Console.WriteLine($"%u %02x:%02x:%02x", lc_addresses[i], sub[7], sub[8], sub[9]);
                Console.WriteLine($" %02x%02x%02x%02x%02x%02x%02x%02x%02x%02x %02x%02x", sub[0], sub[1], sub[2], sub[3], sub[4], sub[5], sub[6], sub[7], sub[8], sub[9], sub[10], sub[11]);
                Console.WriteLine($" %02x%02x%02x%02x%02x%02x%02x%02x%02x%02x %02x%02x", lc1_sectors_contents[i * 12], lc1_sectors_contents[(i * 12) + 1], lc1_sectors_contents[(i * 12) + 2], lc1_sectors_contents[(i * 12) + 3], lc1_sectors_contents[(i * 12) + 4], lc1_sectors_contents[(i * 12) + 5], lc1_sectors_contents[(i * 12) + 6], lc1_sectors_contents[(i * 12) + 7], lc1_sectors_contents[(i * 12) + 8], lc1_sectors_contents[(i * 12) + 9], lc1_sectors_contents[(i * 12) + 10], lc1_sectors_contents[(i * 12) + 11]);

                d = 0;

                for (a = 3; a < 12; a++)
                {
                    x = (byte)(lc1_sectors_contents[(i * 12) + a] ^ sub[a]);
                    Console.WriteLine($" %x%x%x%x%x%x%x%x", (x >> 7) & 0x1, (x >> 6) & 0x1, (x >> 5) & 0x1, (x >> 4) & 0x1, (x >> 3) & 0x1, (x >> 2) & 0x1, (x >> 1) & 0x1, x & 0x1);
                    if (x == 0)
                        continue;
                    for (b = 7; b >= 0; b--)
                    {
                        if (((x >> b) & 0x1) != 0)
                        {
                            d = (byte)(d << 1);
                            d |= (byte)((sub[a] >> b) & 0x1);
                        }
                    }
                }

                Console.WriteLine($" {(d >> 3) & 0x1:x}{(d >> 2) & 0x1:x}{(d >> 1) & 0x1:x}{d & 0x1}");
            }

            return 1;
        }
    }
}
