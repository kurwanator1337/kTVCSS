using PlayerResultTest.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerResultTest
{
    class Program
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

        static void Main(string[] args)
        {
            Bitmap image = Image.FromHbitmap(Resources.MESSAGE_MAIN_00000.GetHbitmap());
            Graphics graphics = Graphics.FromImage(image);
            // upper block
            DrawText(graphics, "AWARD esp", new Rectangle(220, 139, 0, 200), StringAlignment.Near, 30, Brushes.White, FontStyle.Regular, "Play-Regular");
            DrawText(graphics, DateTime.Now.ToString("dd-MM-yyyy HH:mm"), new Rectangle(220, 242, 0, 200), StringAlignment.Near, 30, Brushes.White, FontStyle.Regular, "Play-Regular");
            // first block
            DrawText(graphics, "WON", new Rectangle(275, 370, 0, 200), StringAlignment.Center, 36, Brushes.LimeGreen, FontStyle.Bold, "Play-Regular");
            DrawText(graphics, "KILLS", new Rectangle(383, 457, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
            DrawText(graphics, "DEATHS", new Rectangle(383, 504, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
            DrawText(graphics, "HSP%", new Rectangle(383, 552, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
            // second block
            graphics.DrawImage(Resources.lvl10, 485, 360, 70, 70);
            DrawText(graphics, "ELO", new Rectangle(700, 365, 0, 200), StringAlignment.Center, 36, Brushes.White, FontStyle.Bold, "Play-Regular");
            DrawText(graphics, "TOTAL", new Rectangle(790, 457, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
            DrawText(graphics, "WON", new Rectangle(790, 504, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
            DrawText(graphics, "LOST", new Rectangle(790, 552, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
            // third block 
            DrawText(graphics, "KDR", new Rectangle(383, 787, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
            DrawText(graphics, "AVG", new Rectangle(383, 834, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
            DrawText(graphics, "ACES", new Rectangle(383, 882, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
            DrawText(graphics, "QUADRA", new Rectangle(790, 787, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
            DrawText(graphics, "TRIPPLE", new Rectangle(790, 834, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
            DrawText(graphics, "OPENS", new Rectangle(790, 882, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");

            image.Save("player_result.png", System.Drawing.Imaging.ImageFormat.Png);
            Process.Start("player_result.png");
        }
    }
}
