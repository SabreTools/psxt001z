namespace psxt001z
{
    /// <see href="https://github.com/Dremora/psxt001z/blob/master/common.h"/>
    public partial class LibCrypt
    {
        internal const int ZERO = 0;

        internal const string VERSION = "v0.21 beta 1";

        /// <summary>
        /// BCD to Byte
        /// </summary>
        internal static byte btoi(byte b) => (byte)(b / 16 * 10 + b % 16);

        /// <summary>
        /// Byte to BCD
        /// </summary>
        internal static byte itob(byte i) => (byte)(i / 10 * 16 + i % 10);
    }
}
