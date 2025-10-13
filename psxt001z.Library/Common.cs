using System;

namespace psxt001z
{
    /// <see href="https://github.com/Dremora/psxt001z/blob/master/common.h"/>
    public partial class LibCrypt
    {
        internal const int ZERO = 0;

        public const string VERSION = "v0.21 beta 1";

        /// <summary>
        /// BCD to Byte
        /// </summary>
        public static byte BinaryToInteger(byte b) => (byte)(b / 16 * 10 + b % 16);

        /// <summary>
        /// Byte to BCD
        /// </summary>
        public static byte IntegerToBinary(byte i) => (byte)(i / 10 * 16 + i % 10);

        /// <summary>
        /// Get a santized hex string from an input byte array
        /// </summary>
        public static string GetHexString(byte[] bytes, int startIndex, int length) =>
            BitConverter.ToString(bytes, startIndex, length).Replace("-", string.Empty);
    }
}
