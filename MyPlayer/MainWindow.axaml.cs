using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using MyPlayer.classes.controleestados;
using MyPlayer.classes.filtrarmusicas;
using MyPlayer.classes.keyhook;
using MyPlayer.classes.player;
using MyPlayer.classes.playlist;
using MyPlayer.classes.util;
using MyPlayer.classes.util.form;
using MyPlayer.classes.util.threads;
using MyPlayer.classes.util.treeview;
using MyPlayer.classes.waveimage;
using Serilog;

namespace MyPlayer;

public partial class MainWindow : Window
{
    private static readonly string[] ExtensoesPermitidas = [".mp3", ".mp4", ".wav", ".flac", ".aac", ".wma"];

    private PlayerControl? _playerControl;
    private WaveImage? _wi;
    private FiltrarMusicas _filtrarMusicas = FiltrarMusicas.Instance;
    private FormularioEstado _estadoAtual = new();
    private bool _isDarkMode = false;
    private ThemeType _currentTheme = ThemeType.Light;
    private bool _isManualNavigation = false;

    private ObservableCollection<MusicaItem> _musicas = new();
    private ObservableCollection<MusicaItem> _musicasFiltradas = new();
    private ContextMenu? _contextMenu;

    public MainWindow()
    {
        InitializeComponent();
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File("logs/myplayer-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        listView1.ItemsSource = _musicasFiltradas;
        CriarContextMenu();
    }

    private void CriarContextMenu()
    {
        _contextMenu = new ContextMenu();

        var abrirPasta = new MenuItem { Header = "Abrir pasta onde está o arquivo" };
        var copiarCaminho = new MenuItem { Header = "Copiar caminho do arquivo" };
        var deletarArquivo = new MenuItem { Header = "Deletar arquivo" };

        abrirPasta.Click += (_, _) =>
        {
            var item = listView1.SelectedItem as MusicaItem;
            if (item?.Tag == null) return;
            if (File.Exists(item.Tag))
                System.Diagnostics.Process.Start("xdg-open", Path.GetDirectoryName(item.Tag)!);
            else if (Directory.Exists(item.Tag))
                System.Diagnostics.Process.Start("xdg-open", item.Tag);
        };

        copiarCaminho.Click += async (_, _) =>
        {
            var item = listView1.SelectedItem as MusicaItem;
            if (item?.Tag != null && Clipboard != null)
                await Clipboard.SetTextAsync(item.Tag);
        };

        deletarArquivo.Click += (_, _) =>
        {
            var item = listView1.SelectedItem as MusicaItem;
            if (item?.Tag == null || !File.Exists(item.Tag)) return;
            ExcluirArquivo(item);
        };

        _contextMenu.Items.Add(abrirPasta);
        _contextMenu.Items.Add(copiarCaminho);
        _contextMenu.Items.Add(deletarArquivo);

        listView1.ContextMenu = _contextMenu;
    }

    private async void ExcluirArquivo(MusicaItem item)
    {
        var nome = Path.GetFileName(item.Tag);
        var result = await ShowMessageBox("Confirmar exclusão",
            $"Tem certeza que deseja deletar o arquivo:\n\n{nome}?",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            _playerControl?.Stop();
            File.Delete(item.Tag!);
            _musicas.Remove(item);
            _musicasFiltradas.Remove(item);
            await ShowMessageBox("Sucesso", "Arquivo deletado com sucesso!");
        }
        catch (Exception ex)
        {
            await ShowMessageBox("Erro", $"Erro ao deletar o arquivo:\n{ex.Message}",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void MainWindow_OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            _isDarkMode = ThemeManager.IsSystemDarkMode();
            _currentTheme = _isDarkMode ? ThemeType.Dark : ThemeType.Light;
            ApplyCurrentTheme();

            InvokeAux.Access(lblStatus, lbl => lbl.Text = string.Empty);
            InvokeAux.Access(progressBar1, pg =>
            {
                pg.Minimum = 0;
                pg.Maximum = 100;
            });
            InvokeAux.Access(trackBar1, tck =>
            {
                tck.Minimum = 0;
                tck.Maximum = 100;
            });

            string musicPath = Util.MusicPath;
            InvokeAux.Access(txtPathMusicas, txt => txt.Text = musicPath);
            TreeViewUtil.PreencherTreeView(treeView1, musicPath);

            if (!CarregarEstadoDoFormulario())
            {
                ListViewAux.ListarArquivosListView(
                    ref _musicas, ref _musicasFiltradas, ExtensoesPermitidas,
                    ref _estadoAtual, ref _filtrarMusicas, musicPath, true, false);
            }

            Log.Information("Aplicação iniciada com sucesso");
            playMusic();

            InvokeAux.Access(txtFiltro, txt => txt.Focus());
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Erro crítico ao iniciar aplicação");
            await ShowMessageBox("Erro Fatal", $"Erro ao iniciar: {ex.Message}",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        try
        {
            SalvarEstadoDoFormulario(true);
            DisposePlayer();
            Log.Information("Aplicação encerrada normalmente");
            Log.CloseAndFlush();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao fechar aplicação");
        }
        base.OnClosing(e);
    }

    private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.F3:
                InvokeAux.Access(txtFiltro, txt => txt.Focus());
                break;
            case Key.MediaPlayPause:
                playPause();
                break;
            case Key.MediaNextTrack:
                nextMusic();
                break;
            case Key.MediaPreviousTrack:
                previousMusic();
                break;
            case Key.MediaStop:
                stop();
                break;
        }
    }

    #region Theme

    private void btnDarkMode_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _currentTheme = _currentTheme == ThemeType.Light ? ThemeType.Dark : ThemeType.Light;
        _isDarkMode = _currentTheme == ThemeType.Dark;
        ApplyCurrentTheme();
        _estadoAtual.IsDarkMode = _isDarkMode;
        SalvarEstadoDoFormulario(false);
    }

    private void ApplyCurrentTheme()
    {
        ThemeManager.ApplyTheme(this, _currentTheme);
        btnDarkModeText.Text = _currentTheme == ThemeType.Dark ? "\u2600" : "\uD83C\uDF11";
    }

    #endregion

    #region Estado

    private void SalvarEstadoDoFormulario(bool clearFilter = true)
    {
        _estadoAtual.IsDarkMode = _isDarkMode;
        if (_filtrarMusicas.GetAllItems() != null)
        {
            _estadoAtual.Musicas = ListViewAux.FromMusicaItems(_filtrarMusicas.GetAllItems()!);
        }
        else
        {
            _estadoAtual.Musicas = ListViewAux.GetListMusicas(_musicasFiltradas, ExtensoesPermitidas);
        }
        EstadoFormAux.SalvarEstadoDoFormulario(
            ref txtFiltro, ref _filtrarMusicas, ref _musicas, ref _musicasFiltradas,
            ref _estadoAtual, clearFilter);
    }

    private void SalvarEstadoDebounced(bool clearFilter = false)
    {
        _estadoAtual.IsDarkMode = _isDarkMode;
        if (_filtrarMusicas.GetAllItems() != null)
        {
            _estadoAtual.Musicas = ListViewAux.FromMusicaItems(_filtrarMusicas.GetAllItems()!);
        }
        else
        {
            _estadoAtual.Musicas = ListViewAux.GetListMusicas(_musicasFiltradas, ExtensoesPermitidas);
        }
        EstadoFormAux.SalvarEstadoDoFormularioDebounced(
            ref txtFiltro, ref _filtrarMusicas, ref _musicas, ref _musicasFiltradas,
            ref _estadoAtual, clearFilter);
    }

    private bool CarregarEstadoDoFormulario()
    {
        bool carregou = EstadoFormAux.CarregarEstadoDoFormulario(
            ref _estadoAtual, ref _filtrarMusicas, ref _musicas, ref _musicasFiltradas,
            AtualizarSelecaoMusicaAtual, ref txtPathMusicas, ref treeView1, ref txtFiltro);

        if (carregou)
        {
            _isDarkMode = _estadoAtual.IsDarkMode;
            _currentTheme = _isDarkMode ? ThemeType.Dark : ThemeType.Light;
            ApplyCurrentTheme();
        }
        return carregou;
    }

    #endregion

    #region Player

    private void playMusic()
    {
        NormalizarIndice();

        var item = GetCurrentItem();
        if (item == null)
        {
            Log.Warning("Tentativa de tocar música com lista vazia");
            return;
        }

        string? path = item.Tag;

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Log.Warning("Arquivo não encontrado: {Path}", path);
            _ = ShowMessageBox("Erro", "Arquivo de música não encontrado!",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DisposePlayer();

        InvokeAux.Access(lblStatus, lbl => lbl.Text = "Carregando...");
        InvokeAux.Access(btnPlayPause, btn => btn.IsEnabled = false);

        try
        {
            _playerControl = new PlayerControl(path);

            _playerControl.EvtProgressUpdated += Player_ProgressUpdated;
            _playerControl.EvtPlaying += Player_EvtPlaying;
            _playerControl.EvtPaused += Player_EvtPaused;
            _playerControl.EvtResume += Player_EvtResume;
            _playerControl.EvtStop += Player_EvtStop;
            _playerControl.EvtMusicEnded += Player_EvtMusicEnded;

            _playerControl.Play();

            if (_playerControl.AudioFileReaderProp != null)
            {
                _wi?.Dispose();
                int w = Math.Max(600, (int)pictureBox1.Bounds.Width);
                _wi = new WaveImage(_playerControl.AudioFileReaderProp, this, width: w);
                _wi?.Init(image =>
                {
                    if (image == null) return;
                    InvokeAux.Access(pictureBox1, pct => pct.Source = image);
                });
            }

            Log.Information("Reproduzindo: [{Indice}] {Path}", _estadoAtual.IndiceMusica, path);

            AtualizarSelecaoMusicaAtual();
            SalvarEstadoDebounced(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao reproduzir música: {Path}", path);
            _ = ShowMessageBox("Erro", $"Erro ao carregar música:\n{ex.Message}",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            DisposePlayer();
        }
        finally
        {
            InvokeAux.Access(btnPlayPause, btn => btn.IsEnabled = true);
        }
    }

    private void NormalizarIndice()
    {
        int count = _musicasFiltradas.Count;
        if (count == 0)
        {
            _estadoAtual.IndiceMusica = 0;
            return;
        }

        if (_estadoAtual.IndiceMusica < 0)
            _estadoAtual.IndiceMusica = count - 1;

        if (_estadoAtual.IndiceMusica >= count)
            _estadoAtual.IndiceMusica = 0;

        if (!_filtrarMusicas.ItemCorrespondeFiltro(GetCurrentItem()!))
        {
            _estadoAtual.IndiceMusica = EncontrarProximoIndiceValido(_estadoAtual.IndiceMusica, true);
        }
    }

    private int EncontrarProximoIndiceValido(int inicio, bool forward)
    {
        int count = _musicasFiltradas.Count;
        if (count == 0) return 0;

        int startIdx = forward ? (inicio + 1) % count : (inicio - 1 + count) % count;

        for (int i = 0; i < count; i++)
        {
            int idx = (startIdx + i * (forward ? 1 : -1) + count) % count;
            if (_filtrarMusicas.ItemCorrespondeFiltro(_musicasFiltradas[idx]))
                return idx;
        }
        return 0;
    }

    private MusicaDTO? GetCurrentItem()
    {
        if (_musicasFiltradas.Count == 0 || _estadoAtual.IndiceMusica < 0 || _estadoAtual.IndiceMusica >= _musicasFiltradas.Count)
            return null;
        return _musicasFiltradas[_estadoAtual.IndiceMusica];
    }

    private void DisposePlayer()
    {
        try
        {
            if (_playerControl != null)
            {
                _playerControl.EvtProgressUpdated -= Player_ProgressUpdated;
                _playerControl.EvtPlaying -= Player_EvtPlaying;
                _playerControl.EvtPaused -= Player_EvtPaused;
                _playerControl.EvtResume -= Player_EvtResume;
                _playerControl.EvtStop -= Player_EvtStop;
                _playerControl.EvtMusicEnded -= Player_EvtMusicEnded;
                _playerControl.Dispose();
                _playerControl = null;
            }

            _wi?.Dispose();
            _wi = null;

            InvokeAux.Access(pictureBox1, pct => pct.Source = null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao dispor player");
        }
    }

    private void playPause()
    {
        if (_musicasFiltradas.Count <= 0)
        {
            stop();
            return;
        }

        if (_playerControl == null || !_playerControl.IsValid)
        {
            playMusic();
        }
        else if (_playerControl.IsPlaying)
        {
            _playerControl.Pause();
        }
        else if (_playerControl.IsPaused)
        {
            _playerControl.Resume();
        }
        else
        {
            playMusic();
        }
    }

    private void stop()
    {
        _isManualNavigation = true;
        DisposePlayer();
        updateFormTitle(true);
        SetPlayPauseIcon(false);
        InvokeAux.Access(lblStatus, lbl => lbl.Text = "\u23F9 Parado");
        InvokeAux.Access(progressBar1, pg => pg.Value = 0);
        InvokeAux.Access(trackBar1, tck => tck.Value = 0);
        _isManualNavigation = false;
    }

    private void nextMusic()
    {
        if (_musicasFiltradas.Count == 0) return;
        _isManualNavigation = true;
        DisposePlayer();
        _estadoAtual.IndiceMusica = EncontrarProximoIndiceValido(_estadoAtual.IndiceMusica, true);
        playMusic();
        _isManualNavigation = false;
    }

    private void previousMusic()
    {
        if (_musicasFiltradas.Count == 0) return;
        _isManualNavigation = true;
        DisposePlayer();
        _estadoAtual.IndiceMusica = EncontrarProximoIndiceValido(_estadoAtual.IndiceMusica, false);
        playMusic();
        _isManualNavigation = false;
    }

    private void AtualizarSelecaoMusicaAtual()
    {
        if (_musicasFiltradas.Count == 0 || _estadoAtual.IndiceMusica < 0 || _estadoAtual.IndiceMusica >= _musicasFiltradas.Count)
            return;

        listView1.SelectedItem = _musicasFiltradas[_estadoAtual.IndiceMusica];
        listView1.ScrollIntoView(listView1.SelectedItem);
    }

    #endregion

    #region Player Events

    private void SetPlayPauseIcon(bool isPause)
    {
        string icon = isPause ? "icons8-pause-20.png" : "icons8-play-20.png";
        InvokeAux.Access(imgPlayPause, img =>
        {
            img.Source = new Bitmap(AssetLoader.Open(
                new Uri($"avares://MyPlayer/recursos/icones/{icon}")));
        });
    }

    private void Player_EvtPlaying(object? sender, EventArgs e)
    {
        updateFormTitle();
        SetPlayPauseIcon(true);
        AtualizarSelecaoMusicaAtual();

        if (_playerControl == null) return;

        InvokeAux.Access(progressBar1, pg => pg.Value = 0);
        InvokeAux.Access(trackBar1, tck => tck.Value = 0);

        TimeSpan musicDuration = _playerControl.MusicDuration;
        InvokeAux.Access(lblStatus, lbl => lbl.Text = $"00:00 / {musicDuration:mm\\:ss}");
    }

    private void Player_EvtPaused(object? sender, EventArgs e)
    {
        updateFormTitle(status: "\u23F8 Pausado");
        SetPlayPauseIcon(false);
    }

    private void Player_EvtResume(object? sender, EventArgs e)
    {
        updateFormTitle();
        SetPlayPauseIcon(true);
        AtualizarSelecaoMusicaAtual();
    }

    private void Player_EvtStop(object? sender, EventArgs e)
    {
        if (!_isManualNavigation)
        {
            updateFormTitle(true);
            SetPlayPauseIcon(false);
            InvokeAux.Access(lblStatus, lbl => lbl.Text = "\u23F9 Parado");
            InvokeAux.Access(progressBar1, pg => pg.Value = 0);
            InvokeAux.Access(trackBar1, tck => tck.Value = 0);
        }
    }

    private void Player_EvtMusicEnded(object? sender, EventArgs e)
    {
        if (_isManualNavigation) return;

        int count = _musicasFiltradas.Count;
        if (count == 0) return;

        Log.Debug("Música terminou naturalmente, avançando...");

        _estadoAtual.IndiceMusica = EncontrarProximoIndiceValido(_estadoAtual.IndiceMusica, true);

        if (!_filtrarMusicas.ItemCorrespondeFiltro(GetCurrentItem()!))
        {
            _estadoAtual.IndiceMusica = 0;
            updateFormTitle(true);
            return;
        }

        playMusic();
    }

    private void Player_ProgressUpdated(object? sender, double percent)
    {
        if (_playerControl == null) return;

        if (percent >= 0 && percent <= 100)
        {
            InvokeAux.Access(progressBar1, pg => pg.Value = (int)percent);
            InvokeAux.Access(trackBar1, tck => tck.Value = (int)percent);
        }

        TimeSpan currentTime = _playerControl.CurrentTime;
        TimeSpan musicDuration = _playerControl.MusicDuration;
        InvokeAux.Access(lblStatus, lbl =>
            lbl.Text = $"{currentTime:mm\\:ss} / {musicDuration:mm\\:ss}");

        UpdateWaveImage();
    }

    private void UpdateWaveImage()
    {
        var image = _wi?.GetUpdateImage();
        if (image == null) return;

        InvokeAux.Access(pictureBox1, pct => pct.Source = image);
    }

    private void updateFormTitle(bool reset = false, string status = "")
    {
        if (reset)
        {
            InvokeAux.Access(this, frm => frm.Title = "My Player");
            return;
        }

        var item = GetCurrentItem();
        if (item != null)
        {
            string nomeSemExtensao = Path.GetFileNameWithoutExtension(item.Text);
            string title = $"My Player | {nomeSemExtensao}";
            if (!string.IsNullOrEmpty(status))
                title = $"{title} | {status}";
            InvokeAux.Access(this, frm => frm.Title = title);
            return;
        }

        InvokeAux.Access(this, frm => frm.Title = "My Player");
    }

    #endregion

    #region Botoes

    private async void btnOpenFolderMusics_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _musicas.Clear();
        _musicasFiltradas.Clear();

        var dialog = new OpenFolderDialog();
        string currentPath = InvokeAux.GetValue(txtPathMusicas, txt => txt.Text);
        if (!string.IsNullOrEmpty(currentPath))
            dialog.Directory = currentPath;

        var result = await dialog.ShowAsync(this);

        if (!string.IsNullOrWhiteSpace(result))
        {
            InvokeAux.Access(txtPathMusicas, txt =>
            {
                txt.Text = result;
                _estadoAtual.MusicPath = txt.Text;
                TreeViewUtil.PreencherTreeView(treeView1, txt.Text);
                SalvarEstadoDoFormulario(true);
            });
        }
    }

    private void btnRandomizar_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _estadoAtual.Musicas ??= ListViewAux.GetListMusicas(_musicasFiltradas, ExtensoesPermitidas);

        if (_estadoAtual.Musicas == null || _estadoAtual.Musicas.Count == 0) return;

        _filtrarMusicas.ResetMemory();

        _isManualNavigation = true;
        DisposePlayer();

        _estadoAtual.Musicas = Util.Shuffle(_estadoAtual.Musicas) ?? [];
        _estadoAtual.IndiceMusica = 0;
        _filtrarMusicas.SetEstado(_estadoAtual);

        _musicas.Clear();
        _musicasFiltradas.Clear();
        foreach (var mDto in _estadoAtual.Musicas)
        {
            var item = new MusicaItem
            {
                Text = mDto.Text,
                Tag = mDto.Tag,
                ImageIndex = mDto.ImageIndex,
                SubItems = mDto.SubItems,
                IsChecked = false
            };
            _musicas.Add(item);
            _musicasFiltradas.Add(item);
        }

        SalvarEstadoDoFormulario(true);

        _isManualNavigation = false;
        playMusic();

        Log.Information("Playlist embaralhada: {Total} músicas", _estadoAtual.Musicas.Count);
    }

    private void btnVoltar_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => previousMusic();
    private void btnPlayPause_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => playPause();
    private void btnProximo_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => nextMusic();

    private void trackBar1_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_playerControl == null) return;
        _playerControl.SetPercent(InvokeAux.GetValue(trackBar1, tck => tck.Value));
    }

    #endregion

    #region TreeView

    private void treeView1_AfterSelect(object? sender, SelectionChangedEventArgs e)
    {
        if (treeView1.SelectedItem is not TreeViewItem node) return;

        string? caminho = node.Tag?.ToString();
        InvokeAux.Access(treeView1, tv => tv.SelectedItem = null);

        if (!string.IsNullOrEmpty(caminho) && Directory.Exists(caminho))
        {
            ListViewAux.ListarArquivosListView(
                ref _musicas, ref _musicasFiltradas, ExtensoesPermitidas,
                ref _estadoAtual, ref _filtrarMusicas, caminho, false, false);

            _estadoAtual.IndiceMusica = 0;
            SalvarEstadoDoFormulario(true);

            if (_estadoAtual.Musicas != null && _estadoAtual.Musicas.Count > 0)
                playMusic();
        }
    }

    #endregion

    #region ListView

    private void listView1_DoubleClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (listView1.SelectedItem == null) return;

        _isManualNavigation = true;
        DisposePlayer();

        _estadoAtual.IndiceMusica = _musicasFiltradas.IndexOf((MusicaItem)listView1.SelectedItem);

        _isManualNavigation = false;
        playMusic();
    }

    #endregion

    #region Playlist

    private async void btnClearPlayList_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_musicasFiltradas.Count <= 0) return;

        var result = await ShowMessageBox("Limpar playlist", "Deseja limpar a playlist?",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result != MessageBoxResult.Yes) return;

        stop();
        _musicas.Clear();
        _musicasFiltradas.Clear();

        _estadoAtual.Musicas = new();
        _estadoAtual.IndiceMusica = 0;
        SalvarEstadoDoFormulario(true);
        Log.Information("Playlist limpa");
    }

    private async void btnExcluirMusicasPlayList_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var marcadas = _musicasFiltradas.Where(m => m.IsChecked).ToList();
        int totalMarcadas = marcadas.Count;
        if (totalMarcadas == 0) return;

        string mensagem = totalMarcadas == 1
            ? "Deseja remover a música selecionada da playlist?"
            : $"Deseja remover as {totalMarcadas} músicas selecionadas da playlist?";

        var result = await ShowMessageBox("Confirmar Exclusão", mensagem,
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result != MessageBoxResult.Yes) return;

        stop();

        var caminhosParaRemover = marcadas.Select(x => x.Tag).ToHashSet();

        foreach (var item in marcadas)
        {
            _musicas.Remove(item);
            _musicasFiltradas.Remove(item);
        }

        _estadoAtual.Musicas?.RemoveAll(m => caminhosParaRemover.Contains(m.Tag));
        _estadoAtual.IndiceMusica = 0;
        SalvarEstadoDoFormulario(true);

        Log.Information("{Total} músicas removidas da playlist", totalMarcadas);
    }

    private async void btnSalvarMusicasPlayList_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        AtualizarInterfaceListView();

        if (_musicasFiltradas.Count == 0)
        {
            await ShowMessageBox("Aviso", "Não há músicas na lista para salvar.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Directory = Util.MusicPath,
            Filters = [new FileDialogFilter { Name = "Playlist JSON", Extensions = ["json"] }],
            Title = "Salvar Playlist",
            InitialFileName = "playlist"
        };

        var result = await dialog.ShowAsync(this);
        if (string.IsNullOrEmpty(result)) return;

        try
        {
            List<MusicaDTO> dadosParaSalvar = ListViewAux.GetListMusicas(_musicasFiltradas, ExtensoesPermitidas);
            PlayList.Salvar(result, dadosParaSalvar);

            await ShowMessageBox("Sucesso", $"Playlist salva com sucesso!\n{dadosParaSalvar.Count} músicas");
            Log.Information("Playlist salva: {Path} ({Total} músicas)", result, dadosParaSalvar.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao salvar playlist");
            await ShowMessageBox("Erro", $"Erro ao salvar: {ex.Message}",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void btnCarregarMusicasPlayList_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Directory = Util.MusicPath,
            Filters = [new FileDialogFilter { Name = "Playlist JSON", Extensions = ["json"] }],
            Title = "Selecionar Playlist",
            AllowMultiple = false
        };

        var results = await dialog.ShowAsync(this);
        if (results == null || results.Length == 0) return;

        try
        {
            List<MusicaDTO> musicasCarregadas = PlayList.Carregar(results[0]);

            if (musicasCarregadas == null || musicasCarregadas.Count == 0)
            {
                await ShowMessageBox("Aviso", "A playlist selecionada está vazia ou é inválida.");
                return;
            }

            stop();

            _estadoAtual.Musicas = musicasCarregadas;
            _filtrarMusicas.SetEstado(_estadoAtual);
            _estadoAtual.IndiceMusica = 0;

            AtualizarInterfaceListView();
            SalvarEstadoDoFormulario(true);

            await ShowMessageBox("Sucesso", $"{musicasCarregadas.Count} músicas carregadas com sucesso!");
            Log.Information("Playlist carregada: {Path} ({Total} músicas)", results[0], musicasCarregadas.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao carregar playlist");
            await ShowMessageBox("Erro", $"Erro ao carregar playlist: {ex.Message}",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AtualizarInterfaceListView()
    {
        InvokeAux.Access(txtFiltro, txt => txt.Text = string.Empty);
        _filtrarMusicas.ResetMemory();

        _musicas.Clear();
        _musicasFiltradas.Clear();

        if (_estadoAtual.Musicas == null) return;

        foreach (var mDto in _estadoAtual.Musicas)
        {
            var item = new MusicaItem
            {
                Text = mDto.Text,
                Tag = mDto.Tag,
                ImageIndex = mDto.ImageIndex,
                SubItems = mDto.SubItems,
                IsChecked = false
            };
            _musicas.Add(item);
            _musicasFiltradas.Add(item);
        }
    }

    #endregion

    #region Filtro

    private void txtFiltro_TextChanged(object? sender, TextChangedEventArgs e)
    {
        string termo = InvokeAux.GetValue(txtFiltro, txt => txt.Text);
        _filtrarMusicas.Filtrar(termo, ref _musicas, ref _musicasFiltradas);
    }

    private void txtFiltro_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (_musicasFiltradas.Count <= 0) return;
            playPause();
        }
        else if (e.Key == Key.Escape)
        {
            InvokeAux.Access(txtFiltro, txt => txt.Clear());
            _filtrarMusicas.Filtrar(string.Empty, ref _musicas, ref _musicasFiltradas);
        }
        else if (e.Key == Key.Up || e.Key == Key.Left)
        {
            previousMusic();
        }
        else if (e.Key == Key.Down || e.Key == Key.Right)
        {
            nextMusic();
        }
    }

    #endregion

    #region WaveForm

    private void pictureBox1_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_wi == null || pictureBox1.Source == null) return;

        var point = e.GetPosition(pictureBox1);
        _wi.ClickPictureBox((int)point.X, (int)pictureBox1.Bounds.Width);
    }

    #endregion

    #region Helpers

    private async Task<MessageBoxResult> ShowMessageBox(string title, string message,
        MessageBoxButtons buttons = MessageBoxButtons.OK,
        MessageBoxIcon icon = MessageBoxIcon.Information)
    {
        var msgBox = new MessageBoxDialog(title, message, buttons, icon);
        return await msgBox.ShowDialog<MessageBoxResult>(this);
    }

    #endregion
}

public enum MessageBoxButtons { OK, YesNo }
public enum MessageBoxResult { OK, Yes, No }
public enum MessageBoxIcon { Information, Question, Warning, Error }

public class MessageBoxDialog : Window
{
    public MessageBoxResult Result { get; private set; }

    public MessageBoxDialog(string title, string message, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        Title = title;
        Width = 400;
        Height = 160;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        SizeToContent = SizeToContent.Manual;

        var stack = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 15
        };

        var textBlock = new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };
        stack.Children.Add(textBlock);

        var btnPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 10
        };

        if (buttons == MessageBoxButtons.OK)
        {
            var btnOk = new Button { Content = "OK", Width = 80 };
            btnOk.Click += (_, _) => { Result = MessageBoxResult.OK; Close(); };
            btnPanel.Children.Add(btnOk);
        }
        else
        {
            var btnSim = new Button { Content = "Sim", Width = 80 };
            btnSim.Click += (_, _) => { Result = MessageBoxResult.Yes; Close(); };
            btnPanel.Children.Add(btnSim);

            var btnNao = new Button { Content = "Não", Width = 80 };
            btnNao.Click += (_, _) => { Result = MessageBoxResult.No; Close(); };
            btnPanel.Children.Add(btnNao);
        }

        stack.Children.Add(btnPanel);
        Content = stack;
    }
}
