using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Drawing
{
    /// <summary>
    /// Набор инструментов для работы с графикой
    /// </summary>
    public static class Tools
    {
        /// <summary>
        /// Нарисовать текст
        /// </summary>
        /// <param name="graphics">Изображение</param>
        /// <param name="text">Текст</param>
        /// <param name="x">Координата Х</param>
        /// <param name="y">Координата У</param>
        /// <param name="fontSize">Размер шрифта</param>
        public static void Draw(Graphics graphics, string text, int x, int y, int fontSize)
        {
            graphics.DrawString(text.ToUpper(), new Font("Play-Regular", fontSize), Brushes.White, x, y);
        }
        /// <summary>
        /// Нарисовать текст
        /// </summary>
        /// <param name="gr">Изображение</param>
        /// <param name="text">Текст</param>
        /// <param name="rect">Треугольник (хуй знает зачем)</param>
        /// <param name="alignment">Выравнивание</param>
        /// <param name="fontSize">Размер шрифта</param>
        /// <param name="color">Цвет текста</param>
        /// <param name="fontStyle">Стиль шрифта</param>
        /// <param name="fontName">Название шрифта</param>
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
        /// <summary>
        /// Получить картинку по названию ранга
        /// </summary>
        /// <param name="rankName">Название ранга</param>
        /// <returns></returns>
        public static Image GetRankImage(string rankName)
        {
            switch (rankName)
            {
                case "LEVEL-I":
                    {
                        return Image.FromFile(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Pictures", "lvl1.png"));
                    }
                case "LEVEL-II":
                    {
                        return Image.FromFile(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Pictures", "lvl2.png"));
                    }
                case "LEVEL-III":
                    {
                        return Image.FromFile(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Pictures", "lvl3.png"));
                    }
                case "LEVEL-IV":
                    {
                        return Image.FromFile(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Pictures", "lvl4.png"));
                    }
                case "LEVEL-V":
                    {
                        return Image.FromFile(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Pictures", "lvl5.png"));
                    }
                case "LEVEL-VI":
                    {
                        return Image.FromFile(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Pictures", "lvl6.png"));
                    }
                case "LEVEL-VII":
                    {
                        return Image.FromFile(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Pictures", "lvl7.png"));
                    }
                case "LEVEL-VIII":
                    {
                        return Image.FromFile(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Pictures", "lvl8.png"));
                    }
                case "LEVEL-IX":
                    {
                        return Image.FromFile(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Pictures", "lvl9.png"));
                    }
                case "LEVEL-X":
                    {
                        return Image.FromFile(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Pictures", "lvl10.png"));
                    }
                default:
                    {
                        return Image.FromFile(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Pictures", "lvl0.png"));
                    }
            }
        }
    }
}
