using MyPlayer.classes.playlist;
using Serilog;

namespace MyPlayer.classes.util
{
    internal class Util
    {
        private static readonly Random _rng = new();

        /// <summary>
        /// Fisher–Yates Shuffle adaptado para MusicaDTO
        /// </summary>
        public static List<MusicaDTO>? Shuffle(List<MusicaDTO>? list)
        {
            if (list == null || list.Count <= 1) return list;

            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
            return list;
        }

        public static async Task WaitWhileAsync(Func<bool> condition, int checkIntervalMs = 500)
        {
            while (condition())
                await Task.Delay(checkIntervalMs);
        }

        public static string MusicPath
        {
            get
            {
                string musicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                return musicPath.EndsWith(@"\") ? musicPath : musicPath + @"\";
            }
        }

        /// <summary>
        /// ✅ Cria uma cópia da ImageList com ícones brancos (para tema escuro)
        /// </summary>
        public static ImageList CreateWhiteImageList(ImageList original)
        {
            ImageList newList = new ImageList
            {
                ImageSize = original.ImageSize,
                ColorDepth = original.ColorDepth
            };

            foreach (Image img in original.Images)
            {
                using Bitmap bmp = new Bitmap(img);
                Bitmap whiteBmp = new Bitmap(bmp.Width, bmp.Height);
                
                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        Color p = bmp.GetPixel(x, y);
                        if (p.A > 0)
                        {
                            whiteBmp.SetPixel(x, y, Color.FromArgb(p.A, 255, 255, 255));
                        }
                    }
                }
                newList.Images.Add(whiteBmp);
            }
            return newList;
        }

        /// <summary>
        /// ✅ Formata tamanho de arquivo
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// ✅ Valida se o arquivo é uma música suportada
        /// </summary>
        public static bool IsValidMusicFile(string path, string[] extensoesPermitidas)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;

            string ext = Path.GetExtension(path).ToLowerInvariant();
            return extensoesPermitidas.Contains(ext);
        }
    }
}