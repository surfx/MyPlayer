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

    }
}