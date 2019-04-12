using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Threshold.RichText
{
    internal sealed class LineBuffer : IDisposable
    {
        private const float JustifyMinSpaceWidthFraction = 5 / 8f;

        private readonly List<MeasuredText> buffer = new List<MeasuredText>();
        private readonly SKPaint paint;

        public LineBuffer()
        {
            paint = new SKPaint();
        }

        public void Dispose()
        {
            paint.Dispose();
        }

        public void Add(BufferedText text)
        {
            paint.Typeface = text.Typeface;
            paint.TextSize = text.TextSize;

            var words = text.Text.Split(' ');

            for (var i = 0; i < words.Length; i++)
            {
                var word = words[i];
                var bounds = default(SKRect);
                var width = 0f;

                if (word.Length != 0)
                    width = paint.MeasureText(word, ref bounds);

                var trailingSpaceWidth = i < words.Length - 1 ? paint.MeasureText(" ") : 0;

                buffer.Add(new MeasuredText(text.WithText(word), width, trailingSpaceWidth, -bounds.Top, bounds.Bottom));
            }
        }

        public bool TryGetFullLine(float lineWidth, out MeasuredText[] parts)
        {
            var nonSpaceWidth = 0f;
            var spaceWidth = 0f;
            var firstPartIsJustified = false;

            for (var i = 0; i < buffer.Count; i++)
            {
                var part = buffer[i];

                var isJustified = part.Text.TextAlign == TextAlign.Justify;
                if (i == 0)
                    firstPartIsJustified = isJustified;
                else if (isJustified != firstPartIsJustified)
                    throw new NotSupportedException("Switching between Justify and another text alignment in the same line is not supported.");

                if (nonSpaceWidth + spaceWidth + part.Width > lineWidth)
                {
                    if (isJustified && i > 0)
                    {
                        var previousSpaceWidth = spaceWidth - buffer[i - 1].TrailingSpaceWidth;
                        if (previousSpaceWidth > 0)
                        {
                            var spaceWidthFractionBreakingAfter = (lineWidth - (nonSpaceWidth + part.Width)) / spaceWidth;
                            if (spaceWidthFractionBreakingAfter >= JustifyMinSpaceWidthFraction)
                            {
                                var spaceWidthFractionBreakingBefore = (lineWidth - nonSpaceWidth) / previousSpaceWidth;

                                if (Math.Abs(1 - spaceWidthFractionBreakingAfter) <= Math.Abs(1 - spaceWidthFractionBreakingBefore))
                                {
                                    i++;
                                }
                            }
                        }
                    }

                    parts = new MeasuredText[i];

                    buffer.CopyTo(index: 0, parts, arrayIndex: 0, count: i);
                    buffer.RemoveRange(0, i);
                    return true;
                }

                nonSpaceWidth += part.Width;
                spaceWidth += part.TrailingSpaceWidth;
            }

            parts = null;
            return false;
        }

        public MeasuredText[] GetFullOrRemainingLine(float lineWidth)
        {
            if (TryGetFullLine(lineWidth, out var parts))
                return parts;

            parts = buffer.ToArray();
            buffer.Clear();
            return parts;
        }
    }
}
