using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using QRCoder;
using SkiaSharp;
using Threshold.RichText;

namespace Threshold
{
    public sealed class ThresholdDocumentGenerator : IDisposable
    {
        private const float PageWidth = 8.5f * 72;
        private const float PageHeight = 11f * 72;
        private const float TitleTextSize = 20f;
        private const float SubtitleTextSize = 15f;
        private const float PageTopMargin = 0.5f * 72;
        private const float DataPageMargin = 12;

        private readonly SKDocument document;
        private SKTypeface typeface;
        private SKTypeface boldTypeface;
        private SKTypeface italicTypeface;

        private readonly string title;

        private ThresholdDocumentGenerator(SKDocument document, string title)
        {
            this.document = document ?? throw new ArgumentNullException(nameof(document));
            this.title = title;

            SelectTypefaces();
        }

        private void SelectTypefaces()
        {
            var providedTypefaces = LoadProvidedTypefaces();

            var providedFamily = SelectFamily(providedTypefaces.Select(t => t.FamilyName));
            if (providedFamily != null)
            {
                var selectedTypefaces = providedTypefaces.Where(t => t.FamilyName.Equals(providedFamily)).ToList();

                typeface = SelectBestMatch(selectedTypefaces, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
                boldTypeface = SelectBestMatch(selectedTypefaces, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
                italicTypeface = SelectBestMatch(selectedTypefaces, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic);
            }
            else
            {
                var installedFamily = SelectFamily(SKFontManager.Default.FontFamilies);

                typeface = SKTypeface.FromFamilyName(installedFamily, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
                boldTypeface = SKTypeface.FromFamilyName(installedFamily, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
                italicTypeface = SKTypeface.FromFamilyName(installedFamily, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic);
            }
        }

        private static string SelectFamily(IEnumerable<string> familyNames)
        {
            var familyNamePreferences = new[] { "Cambria", "Libertine", "Serif" };

            return
                familyNamePreferences
                    .Select(name => familyNames.FirstOrDefault(family => family.Contains(name, StringComparison.OrdinalIgnoreCase)))
                    .FirstOrDefault(family => family != null)
                ?? familyNames.FirstOrDefault();
        }

        private static SKTypeface SelectBestMatch(IEnumerable<SKTypeface> typefaces, SKFontStyleWeight desiredWeight, SKFontStyleWidth desiredWidth, SKFontStyleSlant desiredSlant)
        {
            return typefaces.MinBy(t =>
                Math.Abs(t.FontWeight - (int)desiredWeight) / 100
                + Math.Abs(t.FontWidth - (int)desiredWidth)
                + (t.FontSlant == desiredSlant ? 0 : 5));
        }

        private static ImmutableArray<SKTypeface> LoadProvidedTypefaces()
        {
            var binDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var fontsDirectory = Path.Join(binDirectory, "fonts");

            string[] files;
            try
            {
                files = Directory.GetFiles(fontsDirectory);
            }
            catch (DirectoryNotFoundException)
            {
                return ImmutableArray<SKTypeface>.Empty;
            }

            var builder = ImmutableArray.CreateBuilder<SKTypeface>(files.Length);

            foreach (var file in files)
            {
                if (SKTypeface.FromFile(file) is { } typeface)
                    builder.Add(typeface);
            }

            return builder.ToImmutable();
        }

        public void Dispose()
        {
            typeface.Dispose();
            boldTypeface.Dispose();
            italicTypeface.Dispose();
        }

        public static void GeneratePdf(Stream stream, string title, ReadOnlySpan<byte> encryptedData)
        {
            using (var document = SKDocument.CreatePdf(stream))
            using (var generator = new ThresholdDocumentGenerator(document, title))
            {
                generator.DrawIntroductionPage(singleDataPage: CalculateRequiredPages(encryptedData) == 1);

                generator.DrawDataPages(encryptedData);
            }
        }

        private void DrawPageTop(CanvasRichTextWriter writer, string subtitle = null)
        {
            using (writer.Align(TextAlign.Center))
            {
                using (writer.Typeface(boldTypeface))
                using (writer.TextSize(TitleTextSize))
                {
                    writer.WriteLine(title);
                }

                if (string.IsNullOrEmpty(subtitle)) return;

                using (writer.Typeface(typeface))
                using (writer.TextSize(SubtitleTextSize))
                {
                    writer.WriteLine(subtitle);
                }
            }
        }

        private void DrawIntroductionPage(bool singleDataPage)
        {
            const float introPageMargin = PageTopMargin;
            var pageBounds = new SKRect(introPageMargin, introPageMargin, PageWidth - introPageMargin, PageHeight - introPageMargin);

            using (var canvas = document.BeginPage(PageWidth, PageHeight))
            using (var writer = new CanvasRichTextWriter(canvas, pageBounds))
            {
                DrawPageTop(writer);

                using (writer.Align(TextAlign.Justify))
                using (writer.TextSize(12))
                {
                    writer.WriteLine();

                    using (writer.Typeface(italicTypeface))
                        writer.WriteLine("Do not throw away. The contents may contain valuable information.");

                    using (writer.Typeface(typeface))
                    {
                        writer.Write(
                            "This backup is encrypted. The key has been split into multiple physical locations. " +
                            "In order to decrypt, a certain number of the parts of the key must be brought together. " +
                            "Software has been created for this purpose at ");

                        writer.WriteLink("https://github.com/jnm2/Threshold");

                        writer.WriteLine(
                            ". Start there if you need to restore the backup. The software will walk you through " +
                            "the use of each digital item in the backup once it is successfully decrypted.");

                        writer.WriteLine(
                            "If you are unable to run the software by accessing that URL, it is still possible to " +
                            "decrypt this backup if you have the right tools at your disposal. C# source code is included " +
                            "at the end of this document which could be compiled to obtain the software available at the " +
                            "URL above or could at least be used as a reference. The source code is preceded by a " +
                            "specification of the encoding and decoding process.");

                        writer.WriteLine();

                        using (writer.Typeface(boldTypeface))
                            writer.Write("Do not photograph or scan");

                        writer.Write(
                            " or otherwise copy any part of the secret key. Take measures to prevent this, even while " +
                            "photographing or scanning other pages as part of the recovery process. Keep the secret key parts " +
                            "physically secure until you can type them straight into the decryption software. ");

                        using (writer.Typeface(boldTypeface))
                            writer.Write("Do not type");

                        writer.WriteLine(
                            " any part of the secret key into a computer, unless:");

                        using (writer.ListItem())
                            writer.Write("The computer is permanently air-gapped (physically disabled from communicating on a network)");

                        using (writer.ListItem())
                            writer.Write("And, the computer has no internal hard drive (must be running from a live CD or USB)");

                        writer.WriteLine(
                            "Once the backup has been decrypted successfully, either destroy all parts of the secret key " +
                            "beyond recovery or separate them into physically distant storage locations.");

                        writer.WriteLine();

                        using (writer.TextSize(16))
                            writer.WriteLine("Table of Contents");

                        using (writer.ListItem("0."))
                        {
                            writer.Write("Introduction and secret key part ");

                            using (writer.TextSize(9))
                                writer.Write("(this page)");
                        }

                        using (writer.ListItem("1."))
                        {
                            writer.Write("Data ");
                            writer.Write(singleDataPage ? "page" : "pages");
                            writer.Write(" designed for scanning");
                        }

                        using (writer.ListItem("2."))
                            writer.Write("End-to-end specification");

                        using (writer.ListItem("3."))
                            writer.Write("Source code");

                        writer.WriteLine();

                        writer.WriteLine("The remainder of this page is a secret part of the encryption key. Do not photograph or scan this page.");

                        writer.WriteLine("Write one of the secret key parts (as directed by the software) directly on the paper in pen:");
                    }
                }

                DrawBlanksForHandwrittenKeyPart(canvas, pageBounds.Bottom - 28 * 4, typeface);
            }

            document.EndPage();
        }

        private static void DrawBlanksForHandwrittenKeyPart(SKCanvas canvas, float top, SKTypeface typeface)
        {
            using (var paint = new SKPaint())
            {
                paint.Typeface = typeface;
                paint.TextSize = 16;

                const float underlineXWidth = 18;
                const float underlineSectionWidth = 2 * 72;
                const float underlineThicknessFraction = 0.5f;

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

                var labelLeft = (PageWidth - totalWidth) / 2;
                var underlineStart = labelLeft + labelWidth + spaceWidth;

                var baseline = top + paint.TextSize;
                canvas.DrawText(xLabel, labelLeft, baseline, paint);

                var underlineTop = baseline + paint.FontMetrics.UnderlinePosition.Value;
                canvas.DrawRect(
                    underlineStart,
                    underlineTop,
                    underlineXWidth,
                    paint.FontMetrics.UnderlineThickness.Value * underlineThicknessFraction,
                    paint);

                baseline += 28;
                canvas.DrawText(yLabel, labelLeft, baseline, paint);

                for (var line = 0; line < 3; line++)
                {
                    underlineTop = baseline + paint.FontMetrics.UnderlinePosition.Value;

                    var x = underlineStart;

                    for (var i = 0; ; i++)
                    {
                        canvas.DrawRect(
                            x,
                            underlineTop,
                            underlineSectionWidth,
                            paint.FontMetrics.UnderlineThickness.Value * underlineThicknessFraction,
                            paint);

                        if (i >= 2) break;

                        x += underlineSectionWidth + spaceWidth;
                        canvas.DrawText(dash, x, baseline, paint);

                        x += dashWidth + spaceWidth;
                    }

                    baseline += 28;
                }
            }
        }

        private const int MaxBytesPerQRCode = 2953; // Assuming ECC level L
        private const int HeaderBytesPerQRCode = 2;
        private const int MaxEncryptedDataBytesPerQRCode = MaxBytesPerQRCode - HeaderBytesPerQRCode;

        private void DrawDataPages(ReadOnlySpan<byte> encryptedData)
        {
            const byte headerVersion = 1;

            var requiredPages = CalculateRequiredPages(encryptedData);

            var encryptedDataBytesPerPage = ((encryptedData.Length - 1) / requiredPages) + 1;

            var pageBounds = new SKRect(DataPageMargin, PageTopMargin, PageWidth - DataPageMargin, PageHeight - DataPageMargin);

            for (var pageNumber = 1; pageNumber <= requiredPages; pageNumber++)
            {
                using (var canvas = document.BeginPage(PageWidth, PageHeight))
                using (var writer = new CanvasRichTextWriter(canvas, pageBounds))
                {
                    DrawPageTop(writer, subtitle: $"Data page {pageNumber} of {requiredPages}");

                    var encryptedBytesToTake = Math.Min(encryptedData.Length, encryptedDataBytesPerPage);

                    var buffer = new byte[HeaderBytesPerQRCode + encryptedBytesToTake];
                    buffer[0] = headerVersion;
                    buffer[1] = (byte)(requiredPages << 4 | pageNumber);
                    encryptedData.Slice(0, encryptedBytesToTake).CopyTo(buffer.AsSpan().Slice(HeaderBytesPerQRCode));

                    encryptedData = encryptedData.Slice(encryptedBytesToTake);

                    DrawQRCode(
                        canvas,
                        new SKRect(pageBounds.Left, writer.Baseline, pageBounds.Right, pageBounds.Bottom),
                        buffer,
                        QRCodeGenerator.ECCLevel.L);
                }
            }

            document.EndPage();
        }

        private static int CalculateRequiredPages(ReadOnlySpan<byte> encryptedData)
        {
            return ((encryptedData.Length - 1) / MaxEncryptedDataBytesPerQRCode) + 1;
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
