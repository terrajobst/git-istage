using System;
using System.Text;

namespace GitIStage
{
    internal static class Whitespace
    {
        public static int LengthVisual(this string text)
        {
            var result = 0;
            for (var i = 0; i < text.Length; i++)
                result += LengthVisual(text, i);
            return result;
        }

        public static int LengthVisual(this string text, int index)
        {
            if (text[index] != '\t')
                return 1;

            return 4 - index % 4;
        }

        public static string ToVisual(this string text, bool visibleWhitespace)
        {
            var needsFixup = false;

            foreach (var c in text)
            {
                if (c == '\t' || (c == ' ' && visibleWhitespace))
                {
                    needsFixup = true;
                    break;
                }
            }

            if (!needsFixup)
                return text;

            var sb = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c == ' ' && visibleWhitespace)
                {
                    sb.Append('\u00b7');
                }
                else if (c == '\t')
                {
                    if (visibleWhitespace)
                        sb.Append("\u00bb");
                    else
                        sb.Append(' ');

                    var remaining = text.LengthVisual(i) - 1;
                    for (var j = 0; j < remaining; j++)
                        sb.Append(' ');
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}