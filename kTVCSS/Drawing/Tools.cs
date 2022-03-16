using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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

        public static Image GetRankImage(string rankName)
        {
            switch (rankName)
            {
                case "LEVEL-I":
                    {
                        return Image.FromFile(Path.Combine("Pictures", "lvl1.png"));
                    }
                case "LEVEL-II":
                    {
                        return Image.FromFile(Path.Combine("Pictures", "lvl2.png"));
                    }
                case "LEVEL-III":
                    {
                        return Image.FromFile(Path.Combine("Pictures", "lvl3.png"));
                    }
                case "LEVEL-IV":
                    {
                        return Image.FromFile(Path.Combine("Pictures", "lvl4.png"));
                    }
                case "LEVEL-V":
                    {
                        return Image.FromFile(Path.Combine("Pictures", "lvl5.png"));
                    }
                case "LEVEL-VI":
                    {
                        return Image.FromFile(Path.Combine("Pictures", "lvl6.png"));
                    }
                case "LEVEL-VII":
                    {
                        return Image.FromFile(Path.Combine("Pictures", "lvl7.png"));
                    }
                case "LEVEL-VIII":
                    {
                        return Image.FromFile(Path.Combine("Pictures", "lvl8.png"));
                    }
                case "LEVEL-IX":
                    {
                        return Image.FromFile(Path.Combine("Pictures", "lvl9.png"));
                    }
                case "LEVEL-X":
                    {
                        return Image.FromFile(Path.Combine("Pictures", "lvl10.png"));
                    }
                default:
                    {
                        return Image.FromFile(Path.Combine("Pictures", "lvl0.png"));
                    }
            }
        }
    }
}
