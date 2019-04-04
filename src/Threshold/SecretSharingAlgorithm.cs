using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Cryptography;

namespace Threshold
{
    public sealed class SecretSharingAlgorithm
    {
        private const uint Modulus = 0b01111111_11111111_11111111_11111111;
        private const int InputBitsPerBlock = 30;

        private readonly RandomNumberGenerator randomNumberGenerator;

        public SecretSharingAlgorithm(RandomNumberGenerator randomNumberGenerator)
        {
            this.randomNumberGenerator = randomNumberGenerator
                ?? throw new ArgumentNullException(nameof(randomNumberGenerator));
        }

        public ImmutableArray<SharedSecretPart> GenerateShares(ReadOnlySpan<byte> secret, int totalParts, int requiredParts)
        {
            if (totalParts >= Modulus)
                throw new ArgumentOutOfRangeException(nameof(totalParts), totalParts, "The total number of parts must be less than the modulus.");

            if (requiredParts < 1)
                throw new ArgumentOutOfRangeException(nameof(requiredParts), requiredParts, "At least one part is required.");

            if (totalParts < requiredParts)
                throw new ArgumentException("The total number of parts must not be less than the number of required parts.");

            var outputByteCount = (((secret.Length * 8) / InputBitsPerBlock) + 1) * 4;

            var parts = new byte[totalParts][];
            for (var i = 0; i < parts.Length; i++)
                parts[i] = new byte[outputByteCount];

            var coefficients = new uint[requiredParts];

            var paddedBitReader = new PaddingBitReader(secret);
            var offset = 0;

            while (paddedBitReader.TryGetNextBits(bitCount: InputBitsPerBlock, bits: out coefficients[0]))
            {
                for (var exponent = 1; exponent < coefficients.Length; exponent++)
                    coefficients[exponent] = GenerateCoefficient();

                for (var i = 0; i < parts.Length; i++)
                {
                    var x = (ulong)i + 1;
                    var y = 0ul;

                    for (var exponent = coefficients.Length - 1; exponent >= 0; exponent--)
                    {
                        // Cannot overflow with the current modulus.
                        y = ((y * x) + coefficients[exponent]) % Modulus;
                    }

                    BinaryPrimitives.WriteUInt32BigEndian(parts[i].AsSpan().Slice(offset), (uint)y);
                }

                offset += 4;
            }

            Array.Clear(coefficients, 0, coefficients.Length);

            var builder = ImmutableArray.CreateBuilder<SharedSecretPart>(parts.Length);

            for (var i = 0; i < parts.Length; i++)
                builder.Add(new SharedSecretPart(x: i + 1, y: parts[i]));

            return builder.MoveToImmutable();
        }

        private uint GenerateCoefficient()
        {
            var bits = (uint)0;

            unsafe
            {
                var span = new Span<byte>(&bits, sizeof(uint));

                while (true)
                {
                    randomNumberGenerator.GetBytes(span);

                    // Throw out bits that are guaranteed to yield a number greater than the modulus.
                    bits &= 0b01111111_11111111_11111111_11111111;

                    // Coefficients must be smaller than the modulus to ensure proper distribution.
                    if (bits < Modulus) return bits;
                }
            }
        }

        public ReadOnlyMemory<byte> Reconstruct(ImmutableArray<SharedSecretPart> requiredParts)
        {
            if (requiredParts.IsDefaultOrEmpty)
                throw new ArgumentException("At least one part is required.", nameof(requiredParts));

            var messageLength = requiredParts[0].Y.Length;
            if (messageLength % 4 != 0)
                throw new ArgumentException("Y values must be a multiple of four bytes.", nameof(requiredParts));

            var uniqueXValues = new HashSet<int>();
            foreach (var part in requiredParts)
            {
                if (!uniqueXValues.Add(part.X))
                    throw new ArgumentException("All parts must have unique X values.", nameof(requiredParts));

                if (part.Y.Length != messageLength)
                    throw new ArgumentException("All parts must have Y values of the same size.", nameof(requiredParts));
            }

            // https://mortendahl.github.io/2017/06/04/secret-sharing-part1/ was helpful.

            var lagrangeConstants = GetLagrangeConstantsForXValues(requiredParts);

            var blockCount = messageLength / 4;
            var maxPossibleSecretByteLength = ((blockCount * 30) - 1) / 8;
            var secretBuffer = new byte[maxPossibleSecretByteLength];

            var writer = new UnpaddingBitWriter(secretBuffer);

            var blockOffset = 0;

            while (true)
            {
                var y0 = 0uL;

                for (var i = 0; i < requiredParts.Length; i++)
                {
                    var sharedY = BinaryPrimitives.ReadUInt32BigEndian(requiredParts[i].Y.Span.Slice(blockOffset));

                    y0 = (y0 + (sharedY * (ulong)lagrangeConstants[i])) % Modulus;
                }

                blockOffset += 4;
                if (blockOffset < messageLength)
                {
                    writer.WriteBlock(bitCount: InputBitsPerBlock, (uint)y0);
                }
                else
                {
                    writer.WriteFinalBlock(bitCount: InputBitsPerBlock, (uint)y0, out var totalBitsWritten);

                    if ((totalBitsWritten & 0x7) != 0)
                        throw new NotSupportedException("Fractional-byte messages are not supported.");

                    return secretBuffer.AsMemory().Slice(0, totalBitsWritten / 8);
                }
            }
        }

        private static uint[] GetLagrangeConstantsForXValues(ImmutableArray<SharedSecretPart> parts)
        {
            var constants = new uint[parts.Length];

            for (var i = 0; i < parts.Length; i++)
            {
                var x = parts[i].X;

                var numerator = 1L;
                var denominator = 1L;
                for (var otherIndex = 0; otherIndex < parts.Length; otherIndex++)
                {
                    if (otherIndex == i) continue;

                    var otherX = parts[otherIndex].X;
                    numerator = (numerator * otherX) % Modulus;
                    denominator = (denominator * (otherX - x)) % Modulus;
                }

                if (denominator < 0) denominator += Modulus;

                constants[i] = (uint)((numerator * MathUtils.MultiplicativeInverse((uint)denominator, Modulus)) % Modulus);
            }

            return constants;
        }
    }
}
