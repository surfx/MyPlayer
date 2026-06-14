using System.Collections.ObjectModel;
using Avalonia.Controls;
using MyPlayer.classes.controleestados;
using MyPlayer.classes.filtrarmusicas;
using MyPlayer.classes.playlist;
using MyPlayer.classes.util.threads;
using MyPlayer.classes.util.treeview;
using Serilog;

namespace MyPlayer.classes.util.form;

internal static class EstadoFormAux
{
    private static Timer? _saveTimer;
    private static readonly object _saveLock = new();

    private static TextBox? _txtFiltroCapture;
    private static FiltrarMusicas? _filtrarMusicasCapture;
    private static ObservableCollection<MusicaItem>? _musicasCapture;
    private static ObservableCollection<MusicaItem>? _musicasFiltradasCapture;
    private static FormularioEstado? _estadoAtualCapture;
    private static bool _clearFilterCapture;

    public static void SalvarEstadoDoFormularioDebounced(
        ref TextBox txtFiltro,
        ref FiltrarMusicas filtrarMusicas,
        ref ObservableCollection<MusicaItem> musicas,
        ref ObservableCollection<MusicaItem> musicasFiltradas,
        ref FormularioEstado estadoAtual,
        bool clearFilter = true,
        int delayMs = 2000)
    {
        _txtFiltroCapture = txtFiltro;
        _filtrarMusicasCapture = filtrarMusicas;
        _musicasCapture = musicas;
        _musicasFiltradasCapture = musicasFiltradas;
        _estadoAtualCapture = estadoAtual;
        _clearFilterCapture = clearFilter;

        lock (_saveLock)
        {
            _saveTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _saveTimer?.Dispose();

            _saveTimer = new Timer(_ =>
            {
                if (_txtFiltroCapture != null &&
                    _filtrarMusicasCapture != null &&
                    _musicasCapture != null &&
                    _musicasFiltradasCapture != null &&
                    _estadoAtualCapture != null)
                {
                    SalvarEstadoInterno(
                        _txtFiltroCapture,
                        _filtrarMusicasCapture,
                        _musicasCapture,
                        _musicasFiltradasCapture,
                        _estadoAtualCapture,
                        _clearFilterCapture);
                }
            }, null, delayMs, Timeout.Infinite);
        }
    }

    public static void SalvarEstadoDoFormulario(
        ref TextBox txtFiltro,
        ref FiltrarMusicas filtrarMusicas,
        ref ObservableCollection<MusicaItem> musicas,
        ref ObservableCollection<MusicaItem> musicasFiltradas,
        ref FormularioEstado estadoAtual,
        bool clearFilter = true)
    {
        SalvarEstadoInterno(txtFiltro, filtrarMusicas, musicas, musicasFiltradas, estadoAtual, clearFilter);
    }

    private static void SalvarEstadoInterno(
        TextBox txtFiltro,
        FiltrarMusicas filtrarMusicas,
        ObservableCollection<MusicaItem> musicas,
        ObservableCollection<MusicaItem> musicasFiltradas,
        FormularioEstado estadoAtual,
        bool clearFilter)
    {
        try
        {
            if (clearFilter)
            {
                InvokeAux.Access(txtFiltro, txt => txt.Text = string.Empty);
                filtrarMusicas.ResetMemory();
            }
            else
            {
                string filtroAtual = InvokeAux.GetValue(txtFiltro, txt => txt.Text);
                estadoAtual.FiltroTexto = filtroAtual;
            }

            ControleEstados.SalvarEstado(estadoAtual);
            Log.Information("Estado salvo com sucesso");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao salvar estado do formulário");
        }
    }

    public static bool CarregarEstadoDoFormulario(
        ref FormularioEstado estadoAtual,
        ref FiltrarMusicas filtrarMusicas,
        ref ObservableCollection<MusicaItem> musicas,
        ref ObservableCollection<MusicaItem> musicasFiltradas,
        Action atualizarSelecao,
        ref TextBox txtPath,
        ref TreeView treeView,
        ref TextBox txtFiltro)
    {
        try
        {
            var estadoCarregado = ControleEstados.RecuperarEstado();
            if (estadoCarregado == null)
            {
                Log.Information("Nenhum estado anterior encontrado");
                return false;
            }

            estadoAtual.MusicPath = estadoCarregado.MusicPath;
            estadoAtual.IndiceMusica = estadoCarregado.IndiceMusica;
            estadoAtual.Musicas = estadoCarregado.Musicas;
            estadoAtual.IsDarkMode = estadoCarregado.IsDarkMode;
            string filtroTexto = estadoCarregado.FiltroTexto;

            filtrarMusicas.SetEstado(estadoAtual);

            musicas.Clear();
            musicasFiltradas.Clear();
            if (estadoAtual.Musicas != null)
            {
                foreach (var mDto in estadoAtual.Musicas)
                {
                    var item = new MusicaItem
                    {
                        Text = mDto.Text,
                        Tag = mDto.Tag,
                        ImageIndex = mDto.ImageIndex,
                        SubItems = mDto.SubItems,
                        Tamanho = mDto.Tamanho,
                        Data = mDto.Data,
                        IsChecked = false
                    };
                    musicas.Add(item);
                    musicasFiltradas.Add(item);
                }
            }

            atualizarSelecao();

            var pathLocal = estadoAtual.MusicPath;
            if (!string.IsNullOrEmpty(pathLocal))
            {
                InvokeAux.Access(txtPath, txt => txt.Text = pathLocal);
                TreeViewUtil.PreencherTreeView(treeView, pathLocal);
            }

            if (!string.IsNullOrEmpty(filtroTexto))
            {
                var musicasLocal = musicas;
                var musicasFiltradasLocal = musicasFiltradas;
                InvokeAux.Access(txtFiltro, txt => txt.Text = filtroTexto);
                filtrarMusicas.Filtrar(filtroTexto, ref musicasLocal, ref musicasFiltradasLocal);
            }

            Log.Information("Estado carregado: {Estado}", estadoAtual);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao carregar estado");
            return false;
        }
    }
}
