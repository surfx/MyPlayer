using System.Collections.ObjectModel;
using MyPlayer.classes.controleestados;
using MyPlayer.classes.filtrarmusicas;
using MyPlayer.classes.playlist;
using MyPlayer.classes.util.threads;

namespace MyPlayer.classes.util.form;

internal static class ListViewAux
{
    public const int MaxFileStr = 100;

    public static void ListarArquivosListView(
        ref ObservableCollection<MusicaItem> musicas,
        ref ObservableCollection<MusicaItem> musicasFiltradas,
        string[] ExtensoesPermitidas,
        ref FormularioEstado estadoAtual,
        ref FiltrarMusicas filtrarMusicas,
        string path,
        bool clearListView = false,
        bool addPastas = false)
    {
        var extensoesSet = new HashSet<string>(ExtensoesPermitidas, StringComparer.OrdinalIgnoreCase);

        if (clearListView)
        {
            musicas.Clear();
            musicasFiltradas.Clear();
        }

        if (!Directory.Exists(path)) return;

        try
        {
            if (addPastas)
            {
                string[] pastas = Directory.GetDirectories(path);
                foreach (string pasta in pastas)
                {
                    DirectoryInfo di = new(pasta);
                    string nome = di.Name;
                    if (nome.Length > MaxFileStr)
                        nome = string.Concat(nome.AsSpan(0, MaxFileStr), "...");

                    var item = new MusicaItem
                    {
                        Text = nome,
                        Tag = di.FullName,
                        ImageIndex = 0,
                        SubItems = ["", di.LastWriteTime.ToString("dd/MM/yyyy HH:mm")],
                        Tamanho = "",
                        Data = di.LastWriteTime.ToString("dd/MM/yyyy HH:mm"),
                        IsChecked = false
                    };
                    musicas.Add(item);
                    musicasFiltradas.Add(item);
                }
            }

            string[] arquivos = Directory.GetFiles(path);
            var arquivosFiltrados = arquivos
                .Where(arq => extensoesSet.Contains(Path.GetExtension(arq)));

            foreach (string arquivo in arquivosFiltrados)
            {
                FileInfo fi = new(arquivo);
                var item = new MusicaItem
                {
                    Text = Path.GetFileNameWithoutExtension(fi.Name),
                    Tag = fi.FullName,
                    ImageIndex = 10,
                    SubItems = [Util.FormatFileSize(fi.Length), fi.LastWriteTime.ToString("dd/MM/yyyy HH:mm")],
                    Tamanho = $"{(fi.Length / 1024.0 / 1024.0):F2} MB",
                    Data = fi.LastWriteTime.ToString("dd/MM/yyyy HH:mm"),
                    IsChecked = false
                };
                musicas.Add(item);
                musicasFiltradas.Add(item);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erro ao listar arquivos");
        }

        estadoAtual.Musicas = GetListMusicas(musicasFiltradas, ExtensoesPermitidas);
        estadoAtual.IndiceMusica = 0;
        filtrarMusicas.SetEstado(estadoAtual);
    }

    public static List<MusicaDTO> GetListMusicas(ObservableCollection<MusicaItem> musicas, string[] extensoesPermitidas)
    {
        var extensoesSet = new HashSet<string>(extensoesPermitidas, StringComparer.OrdinalIgnoreCase);

        return musicas
            .Where(item =>
            {
                string? path = item.Tag;
                return !string.IsNullOrEmpty(path) &&
                       File.Exists(path) &&
                       extensoesSet.Contains(Path.GetExtension(path));
            })
            .Select(item => new MusicaDTO
            {
                Text = item.Text,
                ImageIndex = item.ImageIndex,
                Tag = item.Tag ?? "",
                SubItems = item.SubItems?.ToList() ?? [],
                Tamanho = item.Tamanho,
                Data = item.Data
            })
            .ToList();
    }

    public static List<string> GetListMusicasPaths(ref FormularioEstado estadoAtual, ref ObservableCollection<MusicaItem> musicas, string[] extensoesPermitidas)
    {
        estadoAtual.Musicas ??= GetListMusicas(musicas, extensoesPermitidas);
        return (estadoAtual.Musicas ?? [])
            .Select(i => i.Tag)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList()!;
    }

    public static List<MusicaDTO> FromMusicaItems(List<MusicaItem> items)
    {
        return items.Select(item => new MusicaDTO
        {
            Text = item.Text,
            Tag = item.Tag ?? "",
            ImageIndex = item.ImageIndex,
            SubItems = item.SubItems?.ToList() ?? [],
            Tamanho = item.Tamanho,
            Data = item.Data
        }).ToList();
    }
}
