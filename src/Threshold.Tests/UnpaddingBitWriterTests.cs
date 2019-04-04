using NUnit.Framework;
using Shouldly;

namespace Threshold.Tests
{
    public static class UnpaddingBitWriterTests
    {
        [Test]
        public static void Padding_is_properly_removed_from_empty_message_when_last_block_is_single_bit()
        {
            var writer = new UnpaddingBitWriter(new byte[0]);

            writer.WriteFinalBlock(1, bits: 0b1, out var totalBitsWritten);
            totalBitsWritten.ShouldBe(0);
        }

        [Test]
        public static void Padding_is_properly_removed_from_empty_message_when_last_block_is_13_bits()
        {
            var writer = new UnpaddingBitWriter(new byte[0]);

            writer.WriteFinalBlock(13, bits: 0b10000000_00000, out var totalBitsWritten);
            totalBitsWritten.ShouldBe(0);
        }

        [Test]
        public static void Padding_is_properly_removed_from_empty_message_when_last_block_has_all_bits_set()
        {
            var writer = new UnpaddingBitWriter(new byte[2]);

            writer.WriteFinalBlock(13, bits: 0b11111111_11111, out var totalBitsWritten);
            totalBitsWritten.ShouldBe(12);
        }

        [Test]
        public static void Padding_is_properly_removed_from_empty_message_when_last_block_has_only_the_last_bit_set()
        {
            var writer = new UnpaddingBitWriter(new byte[2]);

            writer.WriteFinalBlock(13, bits: 0b00000000_00001, out var totalBitsWritten);
            totalBitsWritten.ShouldBe(12);
        }

        [Test]
        public static void Padding_is_properly_removed_when_last_block_is_all_padding()
        {
            var writer = new UnpaddingBitWriter(new byte[1]);

            writer.WriteBlock(8, bits: 0b00000000);
            writer.WriteFinalBlock(8, bits: 0b10000000, out var totalBitsWritten);
            totalBitsWritten.ShouldBe(8);
        }

        [Test]
        public static void Bits_are_written_most_significant_first()
        {
            var buffer = new byte[1];
            var writer = new UnpaddingBitWriter(buffer);

            writer.WriteBlock(1, bits: 1);
            writer.WriteBlock(1, bits: 0);
            writer.WriteBlock(1, bits: 0);
            writer.WriteBlock(1, bits: 1);
            writer.WriteBlock(1, bits: 1);
            writer.WriteBlock(1, bits: 0);
            writer.WriteBlock(1, bits: 1);
            writer.WriteBlock(1, bits: 0);

            buffer[0].ShouldBe<byte>(0b10011010);
        }

        [Test]
        public static void Bytes_are_written_in_order()
        {
            var buffer = new byte[2];
            var writer = new UnpaddingBitWriter(buffer);

            writer.WriteBlock(8, bits: 0b11111111);

            writer.WriteBlock(8, bits: 0b00000000);

            buffer.ShouldBe(new byte[] { 0b11111111, 0b00000000 });
        }

        [Test]
        public static void Returned_integer_is_big_endian_interpretation_of_ordered_bits()
        {
            var buffer = new byte[3];
            var writer = new UnpaddingBitWriter(buffer);

            writer.WriteBlock(4, bits: 0b0011);
            writer.WriteBlock(16, bits: 0b1000_01100110_0001);
            writer.WriteBlock(4, bits: 0b0011);

            buffer.ShouldBe(new byte[] { 0b00111000, 0b01100110, 0b00010011 });
        }
    }
}
