using MyPlayer.classes.playlist;

namespace MyPlayer.classes.util;

internal class Util
{
    private static readonly Random _rng = new();

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
            return musicPath.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? musicPath
                : musicPath + Path.DirectorySeparatorChar;
        }
    }

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

    public static bool IsValidMusicFile(string path, string[] extensoesPermitidas)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return false;

        string ext = Path.GetExtension(path).ToLowerInvariant();
        return extensoesPermitidas.Contains(ext);
    }
}
