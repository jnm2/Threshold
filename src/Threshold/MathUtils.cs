using System;

namespace Threshold
{
    internal static class MathUtils
    {
        /// <summary>
        /// Numerically solves (<paramref name="value"/> * <c>x</c>) % <paramref name="primeModulus"/> = 1 for <c>x</c>.
        /// </summary>
        public static uint MultiplicativeInverse(uint value, uint primeModulus)
        {
            // Followed https://en.wikipedia.org/wiki/Extended_Euclidean_algorithm#Modular_integers

            var t = 0;
            var newT = 1;

            // TODO: expand range
            var r = checked((int)primeModulus);
            var newR = checked((int)value);

            while (newR != 0)
            {
                var quotient = r / newR;

                (t, newT) = (newT, t - quotient * newT);
                (r, newR) = (newR, r - quotient * newR);
            }

            if (r > 1) throw new ArgumentException("Value is not invertible.");

            if (t < primeModulus) t += (int)primeModulus;
            return (uint)t;
        }
    }
}
