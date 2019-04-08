using System;
using System.IO;
using QRCoder;
using SkiaSharp;

namespace Threshold
{
    public static class ThresholdDocumentGenerator
    {
        private const float PageWidth = 8.5f * 72;
        private const float PageHeight = 11f * 72;
        private const float TitleTextSize = 20f;
        private const float SubtitleTextSize = 15f;
        private const float PageTopMargin = 0.5f * 72;
        private const float DataPageMargin = 12;

        public static void GeneratePdf(Stream stream, string title, ReadOnlySpan<byte> encryptedData)
        {
            using (var typeface = SKTypeface.FromFamilyName("Cambria", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright))
            using (var boldTypeface = SKTypeface.FromFamilyName("Cambria", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright))
            using (var document = SKDocument.CreatePdf(stream))
            {
                using (var canvas = document.BeginPage(PageWidth, PageHeight))
                {
                    DrawPageTop(canvas, title, subtitle: null, out var lineWriter, typeface, boldTypeface);

                    DrawBlanksForHandwrittenKeyPart(canvas, ref lineWriter, typeface);
                }

                document.EndPage();

                DrawDataPages(document, title, encryptedData, typeface, boldTypeface);
            }
        }

        private static void DrawPageTop(SKCanvas canvas, string title, string subtitle, out CanvasTextLineWriter lineWriter, SKTypeface typeface, SKTypeface boldTypeface)
        {
            using (var paint = new SKPaint { TextAlign = SKTextAlign.Center })
            {
                lineWriter = new CanvasTextLineWriter(canvas);
                lineWriter.Move(PageTopMargin);

                paint.Typeface = boldTypeface;
                paint.TextSize = TitleTextSize;
                lineWriter.DrawText(title, PageWidth / 2, paint);
                lineWriter.Move(8);

                if (!string.IsNullOrEmpty(subtitle))
                {
                    paint.Typeface = typeface;
                    paint.TextSize = SubtitleTextSize;
                    lineWriter.DrawText(subtitle, PageWidth / 2, paint);
                    lineWriter.Move(8);
                }
            }
        }

        private static void DrawBlanksForHandwrittenKeyPart(SKCanvas canvas, ref CanvasTextLineWriter lineWriter, SKTypeface typeface)
        {
            using (var paint = new SKPaint())
            {
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

                var labelLeft = (PageWidth - totalWidth) / 2;
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
        }

        private static void DrawDataPages(SKDocument document, string title, ReadOnlySpan<byte> encryptedData, SKTypeface typeface, SKTypeface boldTypeface)
        {
            const int maxBytesPerQRCode = 2953; // Assuming ECC level L
            const int headerBytesPerQRCode = 2;
            const int maxEncryptedDataBytesPerQRCode = maxBytesPerQRCode - headerBytesPerQRCode;
            const byte headerVersion = 1;

            var requiredPages = ((encryptedData.Length - 1) / maxEncryptedDataBytesPerQRCode) + 1;

            var encryptedDataBytesPerPage = ((encryptedData.Length - 1) / requiredPages) + 1;

            for (var pageNumber = 1; pageNumber <= requiredPages; pageNumber++)
            {
                using (var canvas = document.BeginPage(PageWidth, PageHeight))
                {
                    DrawPageTop(canvas, title, subtitle: $"Data page {pageNumber} of {requiredPages}", out var lineWriter, typeface, boldTypeface);

                    var encryptedBytesToTake = Math.Min(encryptedData.Length, encryptedDataBytesPerPage);

                    var buffer = new byte[headerBytesPerQRCode + encryptedBytesToTake];
                    buffer[0] = headerVersion;
                    buffer[1] = (byte)(requiredPages << 4 | pageNumber);
                    encryptedData.Slice(0, encryptedBytesToTake).CopyTo(buffer.AsSpan().Slice(headerBytesPerQRCode));

                    encryptedData = encryptedData.Slice(encryptedBytesToTake);

                    DrawQRCode(
                        canvas,
                        new SKRect(DataPageMargin, lineWriter.CurrentY, PageWidth - DataPageMargin, PageHeight - DataPageMargin),
                        buffer,
                        QRCodeGenerator.ECCLevel.L);
                }
            }

            document.EndPage();
        }

        private static void DrawQRCode(SKCanvas canvas, SKRect rect, byte[] data, QRCodeGenerator.ECCLevel eccLevel)
        {
            var size = Math.Min(rect.Width, rect.Height);
            rect = rect.AspectFit(new SKSize(size, size));

            using (var paint = new SKPaint())
            using (var qrGenerator = new QRCodeGenerator())
            using (var qrCode = qrGenerator.CreateQrCode(data, eccLevel))
            {
                var moduleCount = qrCode.ModuleMatrix.Count;
                var squareSize = size / moduleCount;

                for (var x = 0; x < moduleCount; x++)
                {
                    for (var y = 0; y < moduleCount; y++)
                    {
                        if (qrCode.ModuleMatrix[y][x])
                        {
                            canvas.DrawRect(
                                rect.Left + x * size / moduleCount,
                                rect.Top + y * size / moduleCount,
                                squareSize,
                                squareSize,
                                paint);
                        }
                    }
                }
            }
        }
    }
}
