namespace psxt001z
{
    /// <see href="https://github.com/Dremora/psxt001z/blob/master/libcrypt.h"/>
    public class ScsiPassThroughDirect
    {
        public ushort Length { get; set; }

        public byte ScsiStatus { get; set; }

        public byte PathId { get; set; }

        public byte TargetId { get; set; }

        public byte Lun { get; set; }

        public byte CDBLength { get; set; }

        public byte SenseInfoLength { get; set; }

        public byte DataIn { get; set; }

        public uint DataTransferLength { get; set; }

        public uint TimeOutValue { get; set; }

        public byte[]? DataBuffer { get; set; }

        public uint SenseInfoOffset { get; set; }

        public byte[] CDB { get; set; } = new byte[16];
    }
}
