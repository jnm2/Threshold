using SkiaSharp;

namespace Threshold.RichText
{
    internal readonly struct BufferedText
    {
        public string Text { get; }
        public TextAlign TextAlign { get; }
        public SKTypeface Typeface { get; }
        public float TextSize { get; }
        public bool Underlined { get; }
        public SKColor Color { get; }
        public string LinkAddress { get; }

        public BufferedText(string text, TextAlign textAlign, SKTypeface typeface, float textSize, bool underlined, SKColor color, string linkAddress)
        {
            Text = text;
            TextAlign = textAlign;
            Typeface = typeface;
            TextSize = textSize;
            Underlined = underlined;
            Color = color;
            LinkAddress = linkAddress;
        }

        public BufferedText WithText(string text)
        {
            return new BufferedText(text, TextAlign, Typeface, TextSize, Underlined, Color, LinkAddress);
        }
    }
}
