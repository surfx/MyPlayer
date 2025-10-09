namespace MyPlayer.classes.util
{
    internal class Util
    {
        /**
         * Fisher–Yates
         */
        public static void Shuffle(List<ListViewItem> list)
        {
            Random rng = new();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }
    }
}
