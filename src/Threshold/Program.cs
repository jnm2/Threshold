using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Threshold
{
    public static class Program
    {
        public static void Main()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var key = new byte[32];
                rng.GetBytes(key);

                var packedDataToEncrypt = new byte[5000];
                var encryptedWithTag = new byte[packedDataToEncrypt.Length + 16];

                using (var aesGcm = new AesGcm(key))
                {
                    aesGcm.Encrypt(
                        nonce: new byte[12],
                        packedDataToEncrypt,
                        encryptedWithTag.AsSpan().Slice(0, packedDataToEncrypt.Length),
                        encryptedWithTag.AsSpan().Slice(packedDataToEncrypt.Length));
                }

                using (var pdfStream = File.Create("Threshold.pdf"))
                {
                    ThresholdDocumentGenerator.GeneratePdf(pdfStream, "Paper backup of Joseph’s digital information", encryptedWithTag);
                }

                var secretSharingAlgorithm = new SecretSharingAlgorithm(rng);

                var shares = secretSharingAlgorithm.GenerateShares(key, 4, 2);

                Console.WriteLine("Write each part on a separate printed copy of the PDF:");

                foreach (var share in shares)
                {
                    Console.WriteLine();
                    Console.WriteLine(FormatPart(share));
                }
            }
        }

        private static string FormatPart(SharedSecretPart part)
        {
            var builder = new StringBuilder();

            builder.Append("X: ").Append(part.X).AppendLine();
            builder.Append("Y: ");

            var span = part.Y.Span;
            for (var i = 0; i < span.Length; i++)
            {
                if (i != 0 && i % 4 == 0)
                {
                    if (i % 12 == 0)
                        builder.AppendLine().Append("   ");
                    else
                        builder.Append('-');
                }

                builder.Append(span[i].ToString("X2"));
            }

            return builder.ToString();
        }
    }
}
