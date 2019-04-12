using System;
using SkiaSharp;

namespace Threshold.RichText
{
    public sealed class CanvasRichTextWriter : IDisposable
    {
        private const float ParagraphSpacing = 8f;
        private const float LineHeightTopFactor = 1f;
        private const float LineHeightBottomFactor = LineHeightTopFactor / 3f;
        private const float LineHeightTotalFactor = LineHeightTopFactor + LineHeightBottomFactor;

        private readonly SKCanvas canvas;
        private readonly SKRect bounds;
        private readonly SKPaint paint;
        private readonly StyleStack<TextAlign> textAlignStack;
        private readonly StyleStack<SKTypeface> typefaceStack;
        private readonly StyleStack<float> textSizeStack;
        private readonly StyleStack<float> indentStack;

        private readonly LineBuffer lineBuffer = new LineBuffer();
        private BufferedText pendingBullet;
        private bool isCurrentLineEmpty = true;
        private bool lastLineWasItem;

        public float Baseline { get; private set; }

        public CanvasRichTextWriter(SKCanvas canvas, SKRect bounds)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.bounds = bounds.Standardized;
            paint = new SKPaint();

            Baseline = bounds.Top;

            textAlignStack = new StyleStack<TextAlign>(TextAlign.Left);
            typefaceStack = new StyleStack<SKTypeface>(paint.Typeface);
            textSizeStack = new StyleStack<float>(paint.TextSize);
            indentStack = new StyleStack<float>(0);
        }

        public void Dispose()
        {
            paint.Dispose();
        }

        public IDisposable Align(TextAlign textAlign) => textAlignStack.Apply(textAlign);

        public IDisposable Typeface(SKTypeface typeface) => typefaceStack.Apply(typeface);

        public IDisposable TextSize(float textSize) => textSizeStack.Apply(textSize);

        public void WriteLine()
        {
            if (isCurrentLineEmpty)
            {
                Baseline += textSizeStack.Current * LineHeightTotalFactor;
            }
            else
            {
                FlushLine();
            }

            Baseline += ParagraphSpacing;
            lastLineWasItem = false;
            isCurrentLineEmpty = true;
        }

        private void FlushLine()
        {
            while (lineBuffer.GetFullOrRemainingLine(bounds.Width - indentStack.Current) is var parts && parts.Length > 0)
            {
                DrawBufferedLine(parts);
            }
        }

        public void WriteLine(string text)
        {
            Write(text);
            WriteLine();
        }

        public void Write(string text)
        {
            if (!string.IsNullOrEmpty(text))
                Write(GetBufferedText(text, linkAddress: null));
        }

        private void Write(BufferedText text)
        {
            lineBuffer.Add(text);

            while (lineBuffer.TryGetFullLine(bounds.Width - indentStack.Current, out var parts))
            {
                DrawBufferedLine(parts);
            }

            isCurrentLineEmpty = false;
        }

        private BufferedText GetBufferedText(string text, string linkAddress)
        {
            return new BufferedText(
                text,
                textAlignStack.Current,
                typefaceStack.Current,
                textSizeStack.Current,
                underlined: linkAddress != null,
                linkAddress != null ? new SKColor(5, 99, 193) : SKColors.Black,
                linkAddress);
        }

        private void DrawBufferedLine(MeasuredText[] parts)
        {
            if (parts.Length == 0) return;

            var aboveBaseline = 0f;
            var belowBaseline = 0f;
            var totalPartWidth = 0f;
            var totalSpacing = 0f;
            var align = parts[0].Text.TextAlign;

            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (part.Text.TextAlign != align)
                    throw new NotImplementedException("Mixed text alignment within a line is not supported.");

                aboveBaseline = Math.Max(Math.Max(aboveBaseline, part.TopToBaseline), part.Text.TextSize * LineHeightTopFactor);
                belowBaseline = Math.Max(Math.Max(belowBaseline, part.BaselineToBottom), part.Text.TextSize * LineHeightBottomFactor);

                totalPartWidth += part.Width;
                if (i < parts.Length - 1)
                    totalSpacing += part.TrailingSpaceWidth;
            }

            if (!string.IsNullOrEmpty(pendingBullet.Text))
            {
                var bounds = default(SKRect);
                _ = paint.MeasureText(pendingBullet.Text, ref bounds);

                aboveBaseline = Math.Max(Math.Max(aboveBaseline, -bounds.Top), pendingBullet.TextSize * LineHeightTopFactor);
                belowBaseline = Math.Max(Math.Max(belowBaseline, bounds.Bottom), pendingBullet.TextSize * LineHeightBottomFactor);
            }

            Baseline += aboveBaseline;

            var x = bounds.Left + indentStack.Current;
            float spacingScaling;

            switch (align)
            {
                case TextAlign.Left:
                    spacingScaling = 1f;
                    break;
                case TextAlign.Center:
                    x += (bounds.Width - totalPartWidth - totalSpacing) / 2;
                    spacingScaling = 1f;
                    break;
                case TextAlign.Right:
                    x += bounds.Width - totalPartWidth - totalSpacing;
                    spacingScaling = 1f;
                    break;
                case TextAlign.Justify:
                    spacingScaling = (bounds.Width - totalPartWidth) / totalSpacing;
                    if (spacingScaling > 2f)
                        spacingScaling = 1;
                    break;
                default:
                    throw new NotSupportedException("Unrecognized TextAlign value.");
            }

            if (!string.IsNullOrEmpty(pendingBullet.Text))
            {
                paint.Typeface = pendingBullet.Typeface;
                paint.TextSize = pendingBullet.TextSize;
                canvas.DrawText(pendingBullet.Text, x - 0.25f * 72, Baseline, paint);
            }

            foreach (var part in parts)
            {
                paint.Typeface = part.Text.Typeface;
                paint.TextSize = part.Text.TextSize;
                paint.Color = part.Text.Color;

                if (part.Text.Underlined)
                {
                    var top = Baseline + paint.FontMetrics.UnderlinePosition.Value;
                    canvas.DrawRect(x, top, part.Width, paint.FontMetrics.UnderlineThickness.Value, paint);
                }

                canvas.DrawText(part.Text.Text, x, Baseline, paint);

                if (part.Text.LinkAddress != null)
                {
                    canvas.DrawUrlAnnotation(
                        new SKRect(x, Baseline - aboveBaseline, x + part.Width, Baseline + belowBaseline),
                        part.Text.LinkAddress);
                }

                x += part.Width + part.TrailingSpaceWidth * spacingScaling;
            }

            Baseline += belowBaseline;
            pendingBullet = default;
        }

        public void WriteLink(string address) => WriteLink(text: address, address);

        public void WriteLink(string text, string address)
        {
            if (!string.IsNullOrEmpty(text))
                Write(GetBufferedText(text, address));
        }

        public IDisposable ListItem(string bullet = "•")
        {
            FlushLine();

            if (lastLineWasItem)
                Baseline -= ParagraphSpacing;

            pendingBullet = GetBufferedText(bullet, linkAddress: null);

            return new AppliedListItem(this, indentStack.Apply(indentStack.Current + 0.5f * 72));
        }

        private sealed class AppliedListItem : IDisposable
        {
            private readonly CanvasRichTextWriter owner;
            private readonly IDisposable stackDisposable;

            public AppliedListItem(CanvasRichTextWriter owner, IDisposable stackDisposable)
            {
                this.owner = owner;
                this.stackDisposable = stackDisposable;
            }

            public void Dispose()
            {
                owner.FlushLine();
                owner.Baseline += ParagraphSpacing;
                owner.isCurrentLineEmpty = true;
                owner.lastLineWasItem = true;
                stackDisposable.Dispose();
            }
        }
    }
}
