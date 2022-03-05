using MatchResultPicture.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchResultPicture
{
    internal class Program
    {
        private static void Draw(Graphics graphics, string text, int x, int y, int fontSize)
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

        public static void PublishMatchResult()
        {
            Bitmap image = Image.FromHbitmap(Resources.template_match_result.GetHbitmap());
            Graphics graphics = Graphics.FromImage(image);

            // DRAWING TEAM NAMES
            DrawText(graphics, "NINE6NINE", new Rectangle(300, 300, 0, 200), StringAlignment.Center, 30, Brushes.White, FontStyle.Regular, "Play-Regular");
            DrawText(graphics, "STARFORCE", new Rectangle(990, 300, 0, 200), StringAlignment.Center, 30, Brushes.White, FontStyle.Regular, "Play-Regular");
            // DRAWING TEAM SCORES
            DrawText(graphics, "24", new Rectangle(300, 225, 0, 200), StringAlignment.Center, 36, Brushes.White, FontStyle.Regular, "Play-Regular");
            DrawText(graphics, "21", new Rectangle(985, 225, 0, 200), StringAlignment.Center, 36, Brushes.White, FontStyle.Regular, "Play-Regular");
            // DRAWING MAP NAME 
            DrawText(graphics, "DUST2", new Rectangle(650, 225, 0, 200), StringAlignment.Center, 36, Brushes.White, FontStyle.Regular, "Play-Regular");
            // DRAWING MVP PLAYER NAME 
            DrawText(graphics, "INZAME1337", new Rectangle(570, 403, 0, 200), StringAlignment.Center, 36, Brushes.White, FontStyle.Regular, "Play-Bold");
            // DRAWING FIRST BLOCK
            DrawText(graphics, "99%", new Rectangle(285, 540, 0, 200), StringAlignment.Center, 36, Brushes.White, FontStyle.Regular, "Play-Regular");
            DrawText(graphics, "140", new Rectangle(400, 640, 0, 200), StringAlignment.Far, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
            DrawText(graphics, "139", new Rectangle(400, 690, 0, 200), StringAlignment.Far, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
            DrawText(graphics, "1", new Rectangle(400, 738, 0, 200), StringAlignment.Far, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
            // DRAWING SECOND BLOCK
            DrawText(graphics, "75%", new Rectangle(670, 540, 0, 200), StringAlignment.Center, 36, Brushes.White, FontStyle.Regular, "Play-Regular");
            DrawText(graphics, "2.89", new Rectangle(795, 640, 0, 200), StringAlignment.Far, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
            DrawText(graphics, "27", new Rectangle(795, 690, 0, 200), StringAlignment.Far, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
            // DRAWING THIRD BLOCK
            graphics.DrawImage(Resources.lvl10, 875, 540, 60, 60);
            DrawText(graphics, "3000", new Rectangle(1050, 540, 0, 200), StringAlignment.Center, 36, Brushes.White, FontStyle.Regular, "Play-Regular");
            // DRAWING SCOREBOARD
            int yForNames = 910;
            int yForNumbers = 910;
            for (int i = 0; i < 5; i++)
            {
                Draw(graphics, "inzame1337", 100, yForNames, 16);
                DrawText(graphics, "9", new Rectangle(423, yForNumbers, 0, 200), StringAlignment.Center, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
                DrawText(graphics, "17", new Rectangle(505, yForNumbers, 0, 200), StringAlignment.Center, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
                DrawText(graphics, "0,51", new Rectangle(585, yForNumbers, 0, 200), StringAlignment.Center, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
                yForNames += 57;
                yForNumbers += 56;
            }

            yForNames = 910;
            yForNumbers = 910;
            for (int i = 0; i < 5; i++)
            {
                Draw(graphics, "kurwanator1337", 660, yForNames, 16);
                DrawText(graphics, "9", new Rectangle(985, yForNumbers, 0, 200), StringAlignment.Center, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
                DrawText(graphics, "17", new Rectangle(1067, yForNumbers, 0, 200), StringAlignment.Center, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
                DrawText(graphics, "0,51", new Rectangle(1147, yForNumbers, 0, 200), StringAlignment.Center, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
                yForNames += 57;
                yForNumbers += 56;
            }

            image.Save("upload.png", System.Drawing.Imaging.ImageFormat.Png);
            Process.Start("upload.png");
        }

        static void Main(string[] args)
        {
            PublishMatchResult();
        }
    }
}
