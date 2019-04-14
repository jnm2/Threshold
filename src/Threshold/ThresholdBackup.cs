using System;
using System.Collections.Immutable;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Threshold
{
    public static class ThresholdBackup
    {
        public static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public static ImmutableArray<SharedSecretPart> Save(ImmutableArray<BackupItem> backupItems, int totalParts, int requiredParts, string documentTitle, Stream documentOutput)
        {
            byte[] packedDataToEncrypt;
            using (var buffer = new MemoryStream())
            {
                using (var writer = new BinaryWriter(buffer, Utf8NoBom, leaveOpen: true))
                {
                    foreach (var item in backupItems)
                    {
                        writer.Write((byte)item.ContentType);
                        writer.Write(item.Description);
                        writer.Write(item.FileName ?? string.Empty);
                        writer.Write(item.Content.Length);
                        writer.Write(item.Content.Span);
                    }
                }

                packedDataToEncrypt = buffer.ToArray();
            }

            using (var rng = RandomNumberGenerator.Create())
            {
                var key = new byte[32];
                rng.GetBytes(key);

                var encryptedWithTag = new byte[packedDataToEncrypt.Length + 16];

                using (var aesGcm = new AesGcm(key))
                {
                    aesGcm.Encrypt(
                        nonce: new byte[12],
                        packedDataToEncrypt,
                        encryptedWithTag.AsSpan().Slice(0, packedDataToEncrypt.Length),
                        encryptedWithTag.AsSpan().Slice(packedDataToEncrypt.Length));
                }

                ThresholdDocumentGenerator.GeneratePdf(documentOutput, documentTitle, encryptedWithTag);

                var secretSharingAlgorithm = new SecretSharingAlgorithm(rng);

                return secretSharingAlgorithm.GenerateShares(key, totalParts, requiredParts);
            }
        }
    }
}
