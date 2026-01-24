using MyPlayer.classes.playlist;

namespace MyPlayer.classes.util
{
    internal class Util
    {
        /**
         * Fisher–Yates Shuffle adaptado para MusicaDTO
         */
        public static List<MusicaDTO>? Shuffle(List<MusicaDTO> list)
        {
            // Se a lista for nula ou vazia, retorna como está
            if (list == null || list.Count <= 0) { return list; }

            Random rng = new();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                // Swap (Troca) usando tuplas, agora com DTOs
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

        public static ImageList CreateWhiteImageList(ImageList original)
        {
            ImageList newList = new ImageList();
            newList.ImageSize = original.ImageSize;
            newList.ColorDepth = original.ColorDepth;

            foreach (Image img in original.Images)
            {
                Bitmap bmp = new Bitmap(img);
                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        Color p = bmp.GetPixel(x, y);
                        // Se o pixel tem visibilidade (Alpha > 0), transforma em branco mantendo o Alpha
                        if (p.A > 0)
                        {
                            bmp.SetPixel(x, y, Color.FromArgb(p.A, 255, 255, 255));
                        }
                    }
                }
                newList.Images.Add(bmp);
            }
            return newList;
        }

    }
}