namespace Threshold.RichText
{
    internal readonly struct MeasuredText
    {
        public MeasuredText(BufferedText text, float width, float trailingSpaceWidth, float topToBaseline, float baselineToBottom)
        {
            Text = text;
            Width = width;
            TrailingSpaceWidth = trailingSpaceWidth;
            TopToBaseline = topToBaseline;
            BaselineToBottom = baselineToBottom;
        }

        public BufferedText Text { get; }
        public float Width { get; }
        public float TrailingSpaceWidth { get; }
        public float TopToBaseline { get; }
        public float BaselineToBottom { get; }
    }
}
