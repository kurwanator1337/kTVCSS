using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Drawing
{
    public static class Tools
    {
        public static void Draw(Graphics graphics, string text, int x, int y, int fontSize)
        {
            graphics.DrawString(text.ToUpper(), new Font("Play-Regular", fontSize), Brushes.White, x, y);
        }

        public static void DrawText(Graphics gr, string text, Rectangle rect, StringAlignment alignment, float fontSize, Brush color, FontStyle fontStyle, string fontName)
        {
            gr.DrawRectangle(Pens.Blue, rect);
            var font = new Font(fontName, fontSize, fontStyle);
            using (var stringFormat = new StringFormat())
            {
                stringFormat.Alignment = alignment;
                stringFormat.FormatFlags = StringFormatFlags.LineLimit;
                stringFormat.Trimming = StringTrimming.Word;
                gr.DrawString(text, font, color, rect, stringFormat);
            }
        }
    }
}
