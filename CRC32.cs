namespace psxt001z
{
    /// <see href="https://github.com/Dremora/psxt001z/blob/master/main.cpp"/>
    internal class CRC32
    {
        private const uint CRC_POLY = 0xEDB88320;

        private const uint CRC_MASK = 0xD202EF8D;

        private readonly uint[] table = new uint[256];

        public uint Hash { get; private set; }

        public CRC32()
        {
            for (uint i = 0; i < 256; i++)
            {
                uint r, j;
                for (r = i, j = 8; j != 0; j--)
                {
                    r = ((r & 1) != 0) ? (r >> 1) ^ CRC_POLY : r >> 1;
                }

                table[i] = r;
            }

            Hash = 0;
        }

        public void Calculate(byte[] pData, int pDataPtr, int nLen)
        {
            uint crc = Hash;
            while (nLen-- > 0)
            {
                crc = table[(byte)(crc ^ pData[pDataPtr++])] ^ crc >> 8;
                crc ^= CRC_MASK;
            }

            Hash = crc;
        }
    }
}
