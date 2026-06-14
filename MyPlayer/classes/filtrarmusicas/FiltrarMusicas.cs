using System.Collections.ObjectModel;
using MyPlayer.classes.playlist;
using MyPlayer.classes.util.threads;

namespace MyPlayer.classes.filtrarmusicas;

internal class FiltrarMusicas
{
    private List<MusicaItem>? _originalItems;

    private static FiltrarMusicas? _instance = null;
    private FiltrarMusicas() { }

    public string TermoFiltro { get; private set; } = string.Empty;

    public static FiltrarMusicas Instance
    {
        get
        {
            _instance ??= new();
            return _instance;
        }
    }

    public void SetEstado(MyPlayer.classes.controleestados.FormularioEstado estado)
    {
    }

    public void ResetMemory()
    {
        TermoFiltro = string.Empty;
        _originalItems = null;
    }

    public void Filtrar(string termo, ref ObservableCollection<MusicaItem> musicas, ref ObservableCollection<MusicaItem> musicasFiltradas)
    {
        TermoFiltro = termo?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(TermoFiltro))
        {
            if (_originalItems != null)
            {
                musicasFiltradas.Clear();
                foreach (var item in _originalItems)
                {
                    musicasFiltradas.Add(item);
                }
                _originalItems = null;
            }
            return;
        }

        if (_originalItems == null)
        {
            _originalItems = musicasFiltradas.ToList();
        }

        musicasFiltradas.Clear();

        string termoLower = TermoFiltro.ToLowerInvariant();
        var filtrados = _originalItems.Where(item =>
            item.Text.ToLowerInvariant().Contains(termoLower) ||
            (item.SubItems != null && item.SubItems
                .Any(sub => sub.ToLowerInvariant().Contains(termoLower)))
        ).ToList();

        foreach (var item in filtrados)
        {
            musicasFiltradas.Add(item);
        }
    }

    public List<MusicaItem>? GetAllItems()
    {
        return _originalItems;
    }

    public bool ItemCorrespondeFiltro(MusicaDTO item)
    {
        if (string.IsNullOrWhiteSpace(TermoFiltro))
            return true;

        string termoLower = TermoFiltro.ToLowerInvariant();

        if (item.Text.ToLowerInvariant().Contains(termoLower))
            return true;

        if (item.SubItems != null)
        {
            return item.SubItems
                .Any(sub => sub.ToLowerInvariant().Contains(termoLower));
        }

        return false;
    }
}
