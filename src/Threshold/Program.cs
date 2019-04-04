using System;
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

                var secretSharingAlgorithm = new SecretSharingAlgorithm(rng);

                var shares = secretSharingAlgorithm.GenerateShares(key, 4, 2);

                Console.WriteLine("Shares:");

                foreach (var share in shares)
                {
                    Console.WriteLine();
                    Console.WriteLine(FormatSharedY(share));
                }
            }
        }

        private static string FormatSharedY(SharedSecretPart part)
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
