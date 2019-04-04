using NUnit.Framework;
using Shouldly;

namespace Threshold.Tests
{
    public static class PaddingBitReaderTests
    {
        [Test]
        public static void Empty_message_is_padded()
        {
            var reader = new PaddingBitReader(new byte[0]);

            reader.TryGetNextBits(2, out var bits).ShouldBeTrue();
            bits.ShouldBe(0b10u);
        }

        [Test]
        public static void Padding_is_added_for_first_call_after_bits_are_used()
        {
            var reader = new PaddingBitReader(new byte[] { 0b00000000 });

            reader.TryGetNextBits(8, out _);
            reader.TryGetNextBits(8, out var bits).ShouldBeTrue();
            bits.ShouldBe(0b10000000u);
        }

        [Test]
        public static void Only_one_padding_bit_is_needed()
        {
            var reader = new PaddingBitReader(new byte[] { 0b00000000 });

            reader.TryGetNextBits(9, out var bits).ShouldBeTrue();
            bits.ShouldBe(0b00000000_1u);
            reader.TryGetNextBits(1, out _).ShouldBeFalse();
        }

        [Test]
        public static void Following_padding_bits_are_zero()
        {
            var reader = new PaddingBitReader(new byte[] { 0b00000000 });

            reader.TryGetNextBits(20, out var bits).ShouldBeTrue();
            bits.ShouldBe(0b00000000_10000000_0000u);
        }

        [Test]
        public static void Bits_are_enumerated_most_significant_first()
        {
            var reader = new PaddingBitReader(new byte[] { 0b10011010 });

            reader.TryGetNextBits(1, out var bits).ShouldBeTrue();
            bits.ShouldBe(0b1u);
            reader.TryGetNextBits(1, out bits).ShouldBeTrue();
            bits.ShouldBe(0b0u);
            reader.TryGetNextBits(1, out bits).ShouldBeTrue();
            bits.ShouldBe(0b0u);
            reader.TryGetNextBits(1, out bits).ShouldBeTrue();
            bits.ShouldBe(0b1u);
            reader.TryGetNextBits(1, out bits).ShouldBeTrue();
            bits.ShouldBe(0b1u);
            reader.TryGetNextBits(1, out bits).ShouldBeTrue();
            bits.ShouldBe(0b0u);
            reader.TryGetNextBits(1, out bits).ShouldBeTrue();
            bits.ShouldBe(0b1u);
            reader.TryGetNextBits(1, out bits).ShouldBeTrue();
            bits.ShouldBe(0b0u);
        }

        [Test]
        public static void Bytes_are_returned_in_order()
        {
            var reader = new PaddingBitReader(new byte[] { 0b11111111, 0b00000000 });

            reader.TryGetNextBits(8, out var bits).ShouldBeTrue();
            bits.ShouldBe(0b11111111u);
            reader.TryGetNextBits(8, out bits).ShouldBeTrue();
            bits.ShouldBe(0b00000000u);
        }

        [Test]
        public static void Returned_integer_is_big_endian_interpretation_of_ordered_bits()
        {
            var reader = new PaddingBitReader(new byte[] { 0b00111000, 0b01100110, 0b00010011 });

            reader.TryGetNextBits(4, out var bits).ShouldBeTrue();
            bits.ShouldBe(0b0011u);
            reader.TryGetNextBits(16, out bits).ShouldBeTrue();
            bits.ShouldBe(0b1000_01100110_0001u);
            reader.TryGetNextBits(4, out bits).ShouldBeTrue();
            bits.ShouldBe(0b0011u);
        }
    }
}
