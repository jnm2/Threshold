using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using SkiaSharp;

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

                using (var pdfStream = File.Create("Threshold.pdf"))
                {
                    GeneratePdf(pdfStream);
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

        private static void GeneratePdf(Stream stream)
        {
            const float pageWidth = 8.5f * 72;
            const float pageHeight = 11f * 72;

            using (var typeface = SKTypeface.FromFamilyName("Cambria", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright))
            using (var boldTypeface = SKTypeface.FromFamilyName("Cambria", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright))
            using (var paint = new SKPaint())
            using (var document = SKDocument.CreatePdf(stream))
            {
                using (var canvas = document.BeginPage(pageWidth, pageHeight))
                {
                    var lineWriter = new CanvasTextLineWriter(canvas);
                    lineWriter.Move(12);

                    paint.Typeface = boldTypeface;
                    paint.TextSize = 24;
                    paint.TextAlign = SKTextAlign.Center;
                    lineWriter.DrawText("Split secret backup", pageWidth / 2, paint);
                    lineWriter.Move(8);

                    paint.Typeface = typeface;
                    paint.TextSize = 16;

                    const float underlineXWidth = 18;
                    const float underlineSectionWidth = 2 * 72;
                    const float underlineThickness = 0.75f;

                    const string xLabel = "X:";
                    const string yLabel = "Y:";
                    const string dash = "–";
                    var spaceWidth = paint.MeasureText(" ");
                    var dashWidth = paint.MeasureText(dash);

                    var labelWidth = Math.Max(paint.MeasureText(xLabel), paint.MeasureText(yLabel));

                    var totalWidth =
                        labelWidth
                        + spaceWidth
                        + underlineSectionWidth
                        + spaceWidth
                        + dashWidth
                        + spaceWidth
                        + underlineSectionWidth
                        + spaceWidth
                        + dashWidth
                        + spaceWidth
                        + underlineSectionWidth;

                    paint.TextAlign = SKTextAlign.Left;
                    paint.StrokeWidth = underlineThickness;

                    var labelLeft = (pageWidth - totalWidth) / 2;
                    var underlineStart = labelLeft + labelWidth + spaceWidth;

                    lineWriter.DrawText(xLabel, labelLeft, paint);

                    var y = lineWriter.CurrentBaseline + underlineThickness / 2;
                    canvas.DrawLine(underlineStart, y, underlineStart + underlineXWidth, y, paint);

                    lineWriter.Move(28);
                    lineWriter.DrawText(yLabel, labelLeft, paint, move: false);

                    for (var line = 0; line < 3; line++)
                    {
                        y = lineWriter.CurrentBaseline + underlineThickness / 2;

                        var x = underlineStart;
                        canvas.DrawLine(x, y, x + underlineSectionWidth, y, paint);

                        for (var i = 0; i < 2; i++)
                        {
                            x += underlineSectionWidth + spaceWidth;
                            lineWriter.DrawText(dash, x, paint, move: false);

                            x += dashWidth + spaceWidth;
                            canvas.DrawLine(x, y, x + underlineSectionWidth, y, paint);
                        }

                        lineWriter.Move(28);
                    }
                }

                document.EndPage();
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
