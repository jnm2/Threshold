using System;
using System.Runtime.Intrinsics.X86;

namespace Threshold
{
    internal ref struct UnpaddingBitWriter
    {
        private readonly Span<byte> buffer;
        private int bitsWritten;

        public UnpaddingBitWriter(Span<byte> buffer)
        {
            this.buffer = buffer;
            bitsWritten = 0;
        }

        public void WriteBlock(int bitCount, uint bits)
        {
            if (bitCount <= 0 || bitCount > 32)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            if (bitsWritten + bitCount > buffer.Length * 8)
                throw new InvalidOperationException("Attempted to write past the end of the buffer.");

            if (bitsWritten == -1)
                throw new InvalidOperationException("The final block has already been written.");

            var bitsToWrite = bitCount;

            var remainingBitsInPreviousByte = 7 - ((bitsWritten - 1) & 0x7);

            if (remainingBitsInPreviousByte != 0)
            {
                if (bitCount <= remainingBitsInPreviousByte)
                {
                    buffer[bitsWritten / 8] |= (byte)(bits << (remainingBitsInPreviousByte - bitCount));
                    bitsWritten += bitCount;
                    return;
                }

                buffer[bitsWritten / 8] |= (byte)(bits >> (bitCount - remainingBitsInPreviousByte));
                bitsWritten += remainingBitsInPreviousByte;
                bitsToWrite -= remainingBitsInPreviousByte;
            }

            while (bitsToWrite > 8)
            {
                buffer[bitsWritten / 8] = (byte)(bits >> (bitsToWrite - 8));
                bitsWritten += 8;
                bitsToWrite -= 8;
            }

            if (bitsToWrite != 0)
            {
                buffer[bitsWritten / 8] = (byte)(bits << (8 - bitsToWrite));
                bitsWritten += bitsToWrite;
            }
        }

        public void WriteFinalBlock(int bitCount, uint bits, out int totalBitsWritten)
        {
            if (bitCount <= 0 || bitCount > 32)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            // In next preview/release of .NET Core, start using BitOperations.CountTrailingZeros.
            var trailingZeroCount = (int)Bmi1.TrailingZeroCount(bits);
            if (trailingZeroCount >= bitCount)
                throw new ArgumentException("Invalid padding for a final block.");

            if (bitsWritten == -1)
                throw new InvalidOperationException("The final block has already been written.");

            var removedPaddingBitCount = trailingZeroCount + 1;

            var toWrite = bitCount - removedPaddingBitCount;
            if (toWrite != 0) WriteBlock(toWrite, bits >> removedPaddingBitCount);

            totalBitsWritten = bitsWritten;
            bitsWritten = -1;
        }
    }
}
