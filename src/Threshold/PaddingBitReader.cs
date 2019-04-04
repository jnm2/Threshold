using System;

namespace Threshold
{
    /// <summary>
    /// Reads bits as a big-endian integers while padding the bit sequence with
    /// an ending 1 bit and zero or more 0 bits to fill the final block.
    /// </summary>
    internal ref struct PaddingBitReader
    {
        private readonly ReadOnlySpan<byte> data;
        private int bitsRead;

        public PaddingBitReader(ReadOnlySpan<byte> data)
        {
            this.data = data;
            bitsRead = 0;
        }

        public bool TryGetNextBits(int bitCount, out uint bits)
        {
            if (bitCount <= 0 || bitCount > 32)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            bits = 0;

            if (bitsRead == -1) return false;

            var bitsToRead = Math.Min(bitCount, data.Length * 8 - bitsRead);
            var paddingBits = bitCount - bitsToRead;

            var remainingBitsInPreviousByte = 7 - ((bitsRead - 1) & 0x7);
            if (remainingBitsInPreviousByte != 0)
            {
                var bitsToReadInPreviousByte = Math.Min(bitsToRead, remainingBitsInPreviousByte);

                bits |= (uint)((data[bitsRead / 8] << (bitsRead & 0x7)) & 0xFF) >> (8 - bitsToReadInPreviousByte);
                bitsRead += bitsToReadInPreviousByte;

                if (bitsToRead < remainingBitsInPreviousByte) return true;
                bitsToRead -= remainingBitsInPreviousByte;
            }

            while (bitsToRead >= 8)
            {
                bits <<= 8;
                bits |= data[bitsRead / 8];
                bitsRead += 8;
                bitsToRead -= 8;
            }

            if (bitsToRead != 0)
            {
                bits <<= bitsToRead;
                bits |= (uint)data[bitsRead / 8] >> (8 - bitsToRead);
                bitsRead += bitsToRead;
            }

            if (paddingBits != 0)
            {
                bits <<= 1;
                bits |= 1;
                bits <<= paddingBits - 1;
                bitsRead = -1;
            }

            return true;
        }
    }
}
