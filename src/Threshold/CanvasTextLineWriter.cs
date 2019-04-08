using System;
using SkiaSharp;

namespace Threshold
{
    public struct CanvasTextLineWriter
    {
        private readonly SKCanvas canvas;

        public CanvasTextLineWriter(SKCanvas canvas)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            CurrentY = 0;
            CurrentBaseline = 0;
        }

        public float CurrentY { get; private set; }
        public float CurrentBaseline { get; private set; }

        public void DrawText(string text, float x, SKPaint paint, bool move = true)
        {
            if (move)
            {
                const float lineHeightFraction = 4 / 3f;

                var lineHeight = paint.TextSize * lineHeightFraction;
                CurrentBaseline = CurrentY + paint.TextSize;
                CurrentY += lineHeight;
            }

            canvas.DrawText(text, x, CurrentBaseline, paint);
        }

        public void Move(float height)
        {
            CurrentY += height;
            CurrentBaseline += height;
        }
    }
}
