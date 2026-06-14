using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
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
using Avalonia.Platform.Storage;
using Avalonia.Controls.Primitives;

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
    private bool _isUpdatingProgress = false;

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
            await ShowMessageBox("Sucesso", "Arquivo deletado com sucesso!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao deletar arquivo");
        }
    }

    private async void MainWindow_OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
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
            _playerControl?.Stop();
            SalvarEstadoDoFormulario(false);
            Log.Information("Aplicação encerrada normalmente");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao encerrar aplicação");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    #region Estado

    private void SalvarEstadoDoFormulario(bool clearFilter)
    {
        EstadoFormAux.SalvarEstadoDoFormulario(
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
            return;
        }

        DisposePlayer();

        // Reset UI progress indicators
        _isUpdatingProgress = true;
        InvokeAux.Access(progressBar1, pg => pg.Value = 0);
        InvokeAux.Access(trackBar1, tck => tck.Value = 0);
        InvokeAux.Access(lblStatus, lbl => lbl.Text = "00:00 / 00:00");
        InvokeAux.Access(pictureBox1, pct => pct.Source = null);
        _isUpdatingProgress = false;

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
                // Get the actual width of the container
                double actualWidth = pictureBox1.Bounds.Width;
                if (actualWidth <= 0) actualWidth = this.Bounds.Width - 40; // Fallback to window width minus margins

                int w = (int)actualWidth;
                _wi = new WaveImage(_playerControl.AudioFileReaderProp, this, width: w);
                _wi?.Init(image =>
                {
                    if (image == null) return;
                    InvokeAux.Access(pictureBox1, pct => pct.Source = image);
                });
            }

            Log.Information("Reproduzindo: [{Indice}] {Path}", _estadoAtual.IndiceMusica, path);
            Dispatcher.UIThread.Post(() => AtualizarSelecaoMusicaAtual());
            SalvarEstadoDoFormulario(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao reproduzir música: {Path}", path);
        }
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

        var item = _musicasFiltradas[_estadoAtual.IndiceMusica];
        listView1.SelectedItem = item;
        listView1.ScrollIntoView(item);
    }

    #endregion

    #region Player Events

    private void SetPlayPauseIcon(bool isPause)
    {
        string iconPath = isPause ? "recursos/icones/icons8-pause-20.png" : "recursos/icones/icons8-play-20.png";
        InvokeAux.Access(imgPlayPause, img => 
            img.Source = new Bitmap(AssetLoader.Open(new Uri($"avares://MyPlayer/{iconPath}"))));
    }

    private void Player_EvtPlaying(object? sender, EventArgs e)
    {
        updateFormTitle();
        SetPlayPauseIcon(true);
    }

    private void Player_EvtPaused(object? sender, EventArgs e)
    {
        SetPlayPauseIcon(false);
    }

    private void Player_EvtResume(object? sender, EventArgs e)
    {
        updateFormTitle();
        SetPlayPauseIcon(true);
        Dispatcher.UIThread.Post(() => AtualizarSelecaoMusicaAtual());
    }

    private void Player_EvtStop(object? sender, EventArgs e)
    {
        if (!_isManualNavigation)
        {
            updateFormTitle(true);
            SetPlayPauseIcon(false);
            InvokeAux.Access(lblStatus, lbl => lbl.Text = "00:00 / 00:00");
            InvokeAux.Access(progressBar1, pg => pg.Value = 0);
            InvokeAux.Access(trackBar1, tck => tck.Value = 0);
        }
    }

    private void Player_EvtMusicEnded(object? sender, EventArgs e)
    {
        if (_isManualNavigation) return;

        Dispatcher.UIThread.Post(() => 
        {
            int count = _musicasFiltradas.Count;
            if (count == 0) return;

            Log.Debug("Música terminou naturalmente, avançando...");
            _estadoAtual.IndiceMusica = EncontrarProximoIndiceValido(_estadoAtual.IndiceMusica, true);
            playMusic();
        });
    }

    private void Player_ProgressUpdated(object? sender, double percent)
    {
        if (_playerControl == null) return;

        _isUpdatingProgress = true;
        InvokeAux.Access(progressBar1, pg => pg.Value = percent);
        InvokeAux.Access(trackBar1, tck => tck.Value = percent);
        _isUpdatingProgress = false;

        if (_wi != null)
        {
            var image = _wi.GetUpdateImage();
            if (image != null)
                InvokeAux.Access(pictureBox1, pct => pct.Source = image);
        }

        if (_playerControl != null)
        {
            TimeSpan currentTime = _playerControl.CurrentTime;
            TimeSpan musicDuration = _playerControl.MusicDuration;
            InvokeAux.Access(lblStatus, lbl =>
                lbl.Text = $"{currentTime:mm\\:ss} / {musicDuration:mm\\:ss}");
        }
    }

    #endregion

    #region UI Helpers

    private void updateFormTitle(bool reset = false)
    {
        string title = "My Player";
        if (!reset)
        {
            var item = GetCurrentItem();
            if (item != null)
                title = $"My Player | {item.Text}";
        }
        InvokeAux.Access(this, w => w.Title = title);
    }

    private void NormalizarIndice()
    {
        if (_musicasFiltradas.Count == 0)
        {
            _estadoAtual.IndiceMusica = 0;
            return;
        }

        if (_estadoAtual.IndiceMusica < 0)
            _estadoAtual.IndiceMusica = _musicasFiltradas.Count - 1;
        else if (_estadoAtual.IndiceMusica >= _musicasFiltradas.Count)
            _estadoAtual.IndiceMusica = 0;
    }

    private int EncontrarProximoIndiceValido(int atual, bool avancar)
    {
        int count = _musicasFiltradas.Count;
        if (count == 0) return 0;

        int novo = atual + (avancar ? 1 : -1);
        if (novo >= count) return 0;
        if (novo < 0) return count - 1;
        return novo;
    }

    private MusicaItem? GetCurrentItem()
    {
        if (_musicasFiltradas.Count == 0) return null;
        if (_estadoAtual.IndiceMusica < 0 || _estadoAtual.IndiceMusica >= _musicasFiltradas.Count)
            return null;
        return _musicasFiltradas[_estadoAtual.IndiceMusica];
    }

    private async Task<MessageBoxResult> ShowMessageBox(string title, string text, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        // Simplificado para fins de restauração
        return MessageBoxResult.Yes;
    }

    #endregion

    #region Eventos Controles

    private async void btnOpenFolderMusics_Click(object? sender, RoutedEventArgs e)
    {
        var result = await this.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Selecionar Pasta de Músicas",
            AllowMultiple = false
        });

        if (result.Count > 0)
        {
            string path = result[0].Path.LocalPath;
            InvokeAux.Access(txtPathMusicas, txt => txt.Text = path);
            TreeViewUtil.PreencherTreeView(treeView1, path);
            ListViewAux.ListarArquivosListView(
                ref _musicas, ref _musicasFiltradas, ExtensoesPermitidas,
                ref _estadoAtual, ref _filtrarMusicas, path, true, false);
            _estadoAtual.IndiceMusica = 0;
            playMusic();
        }
    }

    private void btnDarkMode_Click(object? sender, RoutedEventArgs e)
    {
        _isDarkMode = !_isDarkMode;
        _currentTheme = _isDarkMode ? ThemeType.Dark : ThemeType.Light;
        _estadoAtual.IsDarkMode = _isDarkMode;
        ApplyCurrentTheme();
        SalvarEstadoDoFormulario(false);
    }

    private void ApplyCurrentTheme()
    {
        ThemeManager.ApplyTheme(this, _currentTheme);
        InvokeAux.Access(btnDarkModeText, tbl => tbl.Text = _isDarkMode ? "\u2600" : "\u1F311");
    }

    private void btnRandomizar_Click(object? sender, RoutedEventArgs e)
    {
        if (_musicasFiltradas.Count <= 0) return;

        var random = new Random();
        var listaEmbaralhada = _musicasFiltradas.OrderBy(x => random.Next()).ToList();

        _musicasFiltradas.Clear();
        foreach (var item in listaEmbaralhada)
            _musicasFiltradas.Add(item);

        _estadoAtual.IndiceMusica = 0;
        playMusic();
    }

    private void btnVoltar_Click(object? sender, RoutedEventArgs e) => previousMusic();
    private void btnPlayPause_Click(object? sender, RoutedEventArgs e) => playPause();
    private void btnProximo_Click(object? sender, RoutedEventArgs e) => nextMusic();

    private void trackBar1_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_playerControl == null || _isManualNavigation || _isUpdatingProgress) return;
        
        _playerControl.SetPercent(e.NewValue);

        // Sync ProgressBar with Slider
        InvokeAux.Access(progressBar1, pg => pg.Value = e.NewValue);

        if (_wi != null)
        {
            var image = _wi.GetUpdateImage();
            if (image != null)
                InvokeAux.Access(pictureBox1, pct => pct.Source = image);
        }
    }

    private void txtFiltro_TextChanged(object? sender, TextChangedEventArgs e)
    {
        string termo = txtFiltro.Text ?? string.Empty;
        _filtrarMusicas.Filtrar(termo, ref _musicas, ref _musicasFiltradas);
    }

    private void txtFiltro_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) playMusic();
    }

    private void treeView1_AfterSelect(object? sender, SelectionChangedEventArgs e)
    {
        if (treeView1.SelectedItem is not TreeViewItem node) return;
        string? caminho = node.Tag?.ToString();
        if (!string.IsNullOrEmpty(caminho) && Directory.Exists(caminho))
        {
            _musicas.Clear();
            _musicasFiltradas.Clear();
            ListViewAux.ListarArquivosListView(
                ref _musicas, ref _musicasFiltradas, ExtensoesPermitidas,
                ref _estadoAtual, ref _filtrarMusicas, caminho, false, false);
            _estadoAtual.IndiceMusica = 0;
            playMusic();
            SalvarEstadoDoFormulario(true);
        }
    }

    private void listView1_DoubleClick(object? sender, TappedEventArgs e)
    {
        if (listView1.SelectedItem is not MusicaItem item) return;
        _isManualNavigation = true;
        _estadoAtual.IndiceMusica = _musicasFiltradas.IndexOf(item);
        playMusic();
        _isManualNavigation = false;
    }

    private void pictureBox1_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_wi == null || _playerControl == null) return;
        
        var bounds = pictureBox1.Bounds;
        if (bounds.Width <= 0) return;

        var pos = e.GetPosition(pictureBox1);
        double percent = (pos.X / bounds.Width) * 100.0;
        percent = Math.Clamp(percent, 0, 100);

        _isUpdatingProgress = true;
        _playerControl.SetPercent(percent);
        InvokeAux.Access(trackBar1, tck => tck.Value = percent);
        InvokeAux.Access(progressBar1, pg => pg.Value = percent);
        
        var image = _wi.GetUpdateImage();
        if (image != null)
            InvokeAux.Access(pictureBox1, pct => pct.Source = image);
            
        _isUpdatingProgress = false;
    }

    private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.MediaPlayPause || e.Key == Key.Space) playPause();
        else if (e.Key == Key.MediaNextTrack || (e.Key == Key.Right && e.KeyModifiers == KeyModifiers.Control)) nextMusic();
        else if (e.Key == Key.MediaPreviousTrack || (e.Key == Key.Left && e.KeyModifiers == KeyModifiers.Control)) previousMusic();
    }

    private async void btnClearPlayList_Click(object? sender, RoutedEventArgs e)
    {
        if (_musicasFiltradas.Count <= 0) return;

        var result = await ShowMessageBox("Limpar playlist", "Deseja limpar a playlist?",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result != MessageBoxResult.Yes) return;

        _isManualNavigation = true;
        DisposePlayer();

        _musicas.Clear();
        _musicasFiltradas.Clear();
        _estadoAtual.IndiceMusica = 0;
        _estadoAtual.MusicPath = string.Empty;

        updateFormTitle(true);
        InvokeAux.Access(lblStatus, lbl => lbl.Text = "\u23F9 Parado");
        InvokeAux.Access(progressBar1, pg => pg.Value = 0);
        InvokeAux.Access(trackBar1, tck => tck.Value = 0);
        InvokeAux.Access(pictureBox1, pct => pct.Source = null);

        SalvarEstadoDoFormulario(true);
        _isManualNavigation = false;
    }
    private async void btnExcluirMusicasPlayList_Click(object? sender, RoutedEventArgs e)
    {
        var itensParaRemover = _musicasFiltradas.Where(x => x.IsChecked).ToList();
        if (itensParaRemover.Count == 0) return;

        var result = await ShowMessageBox("Remover músicas", 
            $"Deseja remover {itensParaRemover.Count} músicas da lista?",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result != MessageBoxResult.Yes) return;

        foreach (var item in itensParaRemover)
        {
            _musicas.Remove(item);
            _musicasFiltradas.Remove(item);
        }

        SalvarEstadoDoFormulario(false);
    }

    private async void btnSalvarMusicasPlayList_Click(object? sender, RoutedEventArgs e)
    {
        if (_musicas.Count == 0) return;

        var file = await this.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Salvar Playlist",
            FileTypeChoices = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }],
            DefaultExtension = "json"
        });

        if (file != null)
        {
            var musicasDto = _musicas.Select(x => new MusicaDTO 
            { 
                Text = x.Text, 
                Tag = x.Tag, 
                ImageIndex = x.ImageIndex, 
                SubItems = x.SubItems 
            }).ToList();
            
            PlayList.Salvar(file.Path.LocalPath, musicasDto);
            await ShowMessageBox("Sucesso", "Playlist salva com sucesso!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async void btnCarregarMusicasPlayList_Click(object? sender, RoutedEventArgs e)
    {
        var files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Carregar Playlist",
            FileTypeFilter = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }],
            AllowMultiple = false
        });

        if (files.Count > 0)
        {
            var musicasDto = PlayList.Carregar(files[0].Path.LocalPath);
            if (musicasDto.Count > 0)
            {
                _musicas.Clear();
                _musicasFiltradas.Clear();
                foreach (var mDto in musicasDto)
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
                    _musicas.Add(item);
                    _musicasFiltradas.Add(item);
                }
                _estadoAtual.IndiceMusica = 0;
                playMusic();
                SalvarEstadoDoFormulario(false);
            }
        }
    }

    #endregion
}

public enum MessageBoxResult { Yes, No, OK, Cancel }
public enum MessageBoxButtons { YesNo, OK, OKCancel }
public enum MessageBoxIcon { Warning, Error, Question, Information }
