using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using MyPlayer.classes.player;
using MyPlayer.classes.playlist;
using MyPlayer.classes.util;
using MyPlayer.classes.waveimage;
using MyPlayer.classes.controleestados;
using MyPlayer.converters;
using Serilog;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;
using Microsoft.Win32;
using MyPlayer.classes.keyhook;
using System.Diagnostics;

namespace MyPlayer.viewmodels
{
    public class MainViewModel : BaseViewModel
    {
        private PlayerControl? _playerControl;
        private WaveImage? _waveImage;
        private string _stateFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appstate.json");

        private string _musicPath = string.Empty;
        private string _filterText = string.Empty;
        private MusicaDTO? _selectedMusica;
        private FolderNode? _selectedFolder;
        private double _progress;
        private string _currentTime = "00:00";
        private string _duration = "00:00";
        private string _statusText = "⏹ Parado";
        private bool _isDarkMode;
        private ImageSource? _waveformSource;
        private string _playPauseIcon = "/recursos/icones/icons8-play-20.png";
        
        private ObservableCollection<MusicaDTO> _playlist = new();
        private List<MusicaDTO> _fullPlaylist = new();
        private ObservableCollection<FolderNode> _folders = new();

        private double _windowWidth = 1100;
        private double _windowHeight = 700;
        private double _windowLeft = 100;
        private double _windowTop = 100;

        public MainViewModel()
        {
            LoadState();
            LoadFolders(_musicPath);

            PlayPauseCommand = new RelayCommand(_ => PlayPause());
            NextCommand = new RelayCommand(_ => NextMusic());
            PreviousCommand = new RelayCommand(_ => PreviousMusic());
            BrowseCommand = new RelayCommand(_ => BrowseFolder());
            ToggleThemeCommand = new RelayCommand(_ => ToggleTheme());
            ClearCommand = new RelayCommand(_ => ClearPlaylist());
            RemoveCommand = new RelayCommand(_ => RemoveSelected(), _ => SelectedMusica != null);
            SaveCommand = new RelayCommand(_ => SavePlaylistCommand());
            LoadCommand = new RelayCommand(_ => LoadPlaylistCommand());
            RandomizeCommand = new RelayCommand(_ => RandomizePlaylist());
            OpenFolderCommand = new RelayCommand(_ => OpenFolder(), _ => SelectedMusica != null);
            CopyFileNameCommand = new RelayCommand(_ => CopyFileName(), _ => SelectedMusica != null);

            GlobalKeyboardHook.SetHook(HandleGlobalKeyPress);
            Log.Information("MainViewModel inicializado");

            // ✅ Toca automaticamente a música restaurada no boot
            if (_selectedMusica != null)
            {
                PlayMusic(_selectedMusica.Tag);
            }
        }

        #region Propriedades

        public double WindowWidth { get => _windowWidth; set => SetProperty(ref _windowWidth, value); }
        public double WindowHeight { get => _windowHeight; set => SetProperty(ref _windowHeight, value); }
        public double WindowLeft { get => _windowLeft; set => SetProperty(ref _windowLeft, value); }
        public double WindowTop { get => _windowTop; set => SetProperty(ref _windowTop, value); }

        public string PlayPauseIcon { get => _playPauseIcon; set => SetProperty(ref _playPauseIcon, value); }
        public string MusicPath { get => _musicPath; set => SetProperty(ref _musicPath, value); }
        public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
        public bool IsDarkMode { get => _isDarkMode; set => SetProperty(ref _isDarkMode, value); }
        public ImageSource? WaveformSource { get => _waveformSource; set => SetProperty(ref _waveformSource, value); }
        public string CurrentTime { get => _currentTime; set => SetProperty(ref _currentTime, value); }
        public string Duration { get => _duration; set => SetProperty(ref _duration, value); }
        
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (!SetProperty(ref _filterText, value)) return;
                ApplyFilter();
            }
        }

        public MusicaDTO? SelectedMusica
        {
            get => _selectedMusica;
            set => SetProperty(ref _selectedMusica, value);
        }

        public FolderNode? SelectedFolder
        {
            get => _selectedFolder;
            set
            {
                if (!SetProperty(ref _selectedFolder, value)) return;
                if (_selectedFolder == null) return;
                LoadPlaylistFromFolder(_selectedFolder.Path);
            }
        }

        public double Progress
        {
            get => _progress;
            set
            {
                if (Math.Abs(_progress - value) < 0.5) return;
                _progress = value;
                OnPropertyChanged();
                _playerControl?.SetPercent(_progress);
            }
        }

        public ObservableCollection<MusicaDTO> Playlist
        {
            get => _playlist;
            set => SetProperty(ref _playlist, value);
        }

        public ObservableCollection<FolderNode> Folders { get => _folders; set => SetProperty(ref _folders, value); }

        #endregion

        #region Comandos
        public ICommand PlayPauseCommand { get; }
        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }
        public ICommand BrowseCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand LoadCommand { get; }
        public ICommand RandomizeCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand CopyFileNameCommand { get; }
        #endregion

        #region Persistência

        public void SaveState()
        {
            try
            {
                var estado = new FormularioEstado
                {
                    MusicPath = this.MusicPath,
                    SelectedMusicaTag = this.SelectedMusica?.Tag ?? string.Empty,
                    IsDarkMode = this.IsDarkMode,
                    Musicas = _fullPlaylist,
                    WindowWidth = this.WindowWidth,
                    WindowHeight = this.WindowHeight,
                    WindowLeft = this.WindowLeft,
                    WindowTop = this.WindowTop,
                    FiltroTexto = this.FilterText
                };

                string json = JsonSerializer.Serialize(estado, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_stateFilePath, json);
            }
            catch (Exception ex) { Log.Error(ex, "Erro ao salvar estado"); }
        }

        private void LoadState()
        {
            try
            {
                if (!File.Exists(_stateFilePath)) return;

                string json = File.ReadAllText(_stateFilePath);
                var estado = JsonSerializer.Deserialize<FormularioEstado>(json);
                if (estado == null) return;

                _musicPath = estado.MusicPath;
                _isDarkMode = estado.IsDarkMode;
                _fullPlaylist = estado.Musicas ?? new List<MusicaDTO>();
                _windowWidth = Math.Max(estado.WindowWidth, 350);
                _windowHeight = Math.Max(estado.WindowHeight, 300);
                _windowLeft = estado.WindowLeft;
                _windowTop = estado.WindowTop;
                _filterText = estado.FiltroTexto;

                ApplyFilter();
                ApplyTheme();

                if (!string.IsNullOrEmpty(estado.SelectedMusicaTag))
                {
                    var last = Playlist.FirstOrDefault(m => m.Tag == estado.SelectedMusicaTag);
                    if (last != null) 
                    {
                        _selectedMusica = last;
                        OnPropertyChanged(nameof(SelectedMusica));
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex, "Erro ao carregar estado"); }
        }

        #endregion

        #region Métodos

        private void UpdatePlayingStatus()
        {
            foreach (var musica in _fullPlaylist)
            {
                musica.IsPlaying = SelectedMusica != null && musica.Tag == SelectedMusica.Tag;
            }
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(_filterText))
            {
                Playlist = new ObservableCollection<MusicaDTO>(_fullPlaylist);
                return;
            }

            var filtered = _fullPlaylist
                .Where(m => m.Text.Contains(_filterText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Playlist = new ObservableCollection<MusicaDTO>(filtered);
        }

        private void OpenFolder()
        {
            if (SelectedMusica == null || !File.Exists(SelectedMusica.Tag)) return;
            Process.Start("explorer.exe", $"/select,\"{SelectedMusica.Tag}\"");
        }

        private void CopyFileName()
        {
            if (SelectedMusica == null) return;
            Clipboard.SetText(SelectedMusica.Text);
        }

        private void HandleGlobalKeyPress(Key key)
        {
            switch (key)
            {
                case Key.MediaPlayPause: 
                case Key.Play:
                case Key.Pause:
                    PlayPause(); break;
                case Key.MediaNextTrack: NextMusic(); break;
                case Key.MediaPreviousTrack: PreviousMusic(); break;
                case Key.MediaStop: StopMusic(); break;
            }
        }

        private void StopMusic()
        {
            _playerControl?.Stop();
        }

        private void LoadFolders(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                Folders.Clear();
                var root = new FolderNode { Name = Path.GetFileName(path.TrimEnd('\\')), Path = path, OnSelected = node => LoadPlaylistFromFolder(node.Path) };
                LoadSubFolders(root);
                Folders.Add(root);
            });
        }

        private void LoadSubFolders(FolderNode parent)
        {
            if (parent == null || !Directory.Exists(parent.Path)) return;

            try
            {
                foreach (var dir in Directory.GetDirectories(parent.Path))
                {
                    var node = new FolderNode { Name = Path.GetFileName(dir), Path = dir, OnSelected = n => LoadPlaylistFromFolder(n.Path) };
                    parent.SubFolders.Add(node);
                }
            }
            catch { }
        }

        private void BrowseFolder()
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() != true) return;

            MusicPath = dialog.FolderName;
            LoadFolders(MusicPath);
            LoadPlaylistFromFolder(MusicPath);
            SaveState();
        }

        private void LoadPlaylistFromFolder(string path)
        {
            if (!Directory.Exists(path)) return;

            var songs = new List<MusicaDTO>();
            string[] extensions = { ".mp3", ".wav", ".flac", ".aac" };
            foreach (var file in Directory.GetFiles(path))
            {
                if (!extensions.Contains(Path.GetExtension(file).ToLower())) continue;
                songs.Add(new MusicaDTO { Text = Path.GetFileName(file), Tag = file });
            }

            _fullPlaylist = songs;
            ApplyFilter();
            SaveState();
        }

        public void PlayMusic(string path)
        {
            _playerControl?.Dispose();
            _waveImage?.Dispose();

            try
            {
                _playerControl = new PlayerControl(path);
                StatusText = "⌛ Carregando...";

                UpdatePlayingStatus();

                if (_playerControl.AudioFileReaderProp != null)
                {
                    _waveImage = new WaveImage(_playerControl.AudioFileReaderProp, width: 800);
                    _waveImage.init(img => 
                    { 
                        if (img == null) return;
                        Application.Current.Dispatcher.Invoke(() => WaveformSource = img.ToBitmapImage()); 
                    });
                }

                _playerControl.EvtProgressUpdated += (s, p) => 
                {
                    _progress = p; OnPropertyChanged(nameof(Progress));
                    CurrentTime = _playerControl.CurrentTime.ToString(@"mm\:ss");
                    
                    var updatedImg = _waveImage?.getUpdateImage();
                    if (updatedImg == null) return;
                    Application.Current.Dispatcher.Invoke(() => WaveformSource = updatedImg.ToBitmapImage());
                };

                _playerControl.EvtPlaying += (s, e) => Application.Current.Dispatcher.Invoke(() => 
                {
                    Duration = _playerControl.MusicDuration.ToString(@"mm\:ss"); 
                    PlayPauseIcon = "/recursos/icones/icons8-pause-20.png"; 
                    StatusText = $"▶ {Path.GetFileNameWithoutExtension(path)}"; 
                });

                _playerControl.EvtPaused += (s, e) => Application.Current.Dispatcher.Invoke(() => 
                {
                    PlayPauseIcon = "/recursos/icones/icons8-play-20.png"; 
                    StatusText = "⏸ Pausado"; 
                });

                _playerControl.EvtResume += (s, e) => Application.Current.Dispatcher.Invoke(() => 
                {
                    PlayPauseIcon = "/recursos/icones/icons8-pause-20.png"; 
                    StatusText = $"▶ {Path.GetFileNameWithoutExtension(path)}"; 
                });

                _playerControl.EvtStop += (s, e) => Application.Current.Dispatcher.Invoke(() => 
                {
                    PlayPauseIcon = "/recursos/icones/icons8-play-20.png"; 
                    StatusText = "⏹ Parado"; 
                    Progress = 0;
                    CurrentTime = "00:00";
                });

                _playerControl.EvtMusicEnded += (s, e) => Application.Current.Dispatcher.Invoke(() => NextMusic());
                
                _playerControl.Play();
            }
            catch (Exception ex) 
            { 
                Log.Error(ex, "Erro ao tocar música"); 
                StatusText = "❌ Erro ao carregar"; 
            }
        }

        private void PlayPause() 
        { 
            if (_playerControl == null)
            {
                if (SelectedMusica != null) PlayMusic(SelectedMusica.Tag);
                return;
            }

            if (_playerControl.IsPlaying) 
            {
                _playerControl.Pause(); 
            }
            else 
            {
                if (_playerControl.IsPaused)
                {
                    _playerControl.Resume();
                }
                else
                {
                    _playerControl.Play();
                }
            }
        }

        private void NextMusic() 
        { 
            if (Playlist.Count == 0 || SelectedMusica == null) return; 
            int index = Playlist.IndexOf(SelectedMusica); 
            index = (index + 1) % Playlist.Count; 
            SelectedMusica = Playlist[index]; 
            PlayMusic(SelectedMusica.Tag);
        }

        private void PreviousMusic() 
        { 
            if (Playlist.Count == 0 || SelectedMusica == null) return; 
            int index = Playlist.IndexOf(SelectedMusica); 
            index = (index - 1 + Playlist.Count) % Playlist.Count; 
            SelectedMusica = Playlist[index]; 
            PlayMusic(SelectedMusica.Tag);
        }

        public void SeekToPercent(double percent)
        {
            if (_playerControl == null) return;
            _playerControl.SetPercent(percent);
            if (_playerControl.IsPaused) _playerControl.Resume();
        }

        private void ClearPlaylist() 
        { 
            if (MessageBox.Show("Deseja limpar a playlist?", "Limpar", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            _playerControl?.Stop(); 
            _fullPlaylist.Clear();
            ApplyFilter(); 
            SaveState(); 
        }

        private void RemoveSelected() 
        { 
            if (SelectedMusica == null) return;
            _fullPlaylist.Remove(SelectedMusica); 
            ApplyFilter();
            SaveState(); 
        }

        private void SavePlaylistCommand() 
        { 
            SaveFileDialog sfd = new() { Filter = "Playlist JSON|*.json" }; 
            if (sfd.ShowDialog() != true) return;
            PlayList.Salvar(sfd.FileName, _fullPlaylist); 
        }

        private void LoadPlaylistCommand() 
        { 
            OpenFileDialog ofd = new() { Filter = "Playlist JSON|*.json" }; 
            if (ofd.ShowDialog() != true) return;
            _fullPlaylist = PlayList.Carregar(ofd.FileName); 
            ApplyFilter(); 
            SaveState(); 
        }

        private void RandomizePlaylist() 
        { 
            var shuffled = Util.Shuffle(_fullPlaylist); 
            if (shuffled == null || shuffled.Count == 0) return;
            
            _fullPlaylist = shuffled;
            ApplyFilter(); 
            SaveState(); 

            // ✅ Toca automaticamente a primeira música após randomizar
            SelectedMusica = Playlist[0];
            PlayMusic(SelectedMusica.Tag);
        }

        private void ToggleTheme() { IsDarkMode = !IsDarkMode; ApplyTheme(); SaveState(); }

        private void ApplyTheme()
        {
            var app = Application.Current;
            if (app == null) return;

            // Remove apenas os dicionários de temas e estilos para evitar limpar outros recursos globais
            for (int i = app.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
            {
                var dict = app.Resources.MergedDictionaries[i];
                if (dict.Source != null && dict.Source.OriginalString.Contains("/themes/"))
                {
                    app.Resources.MergedDictionaries.RemoveAt(i);
                }
            }
            
            string themeName = IsDarkMode ? "DarkTheme" : "LightTheme";
            
            // Ordem é importante: Tema primeiro, Estilos depois (para sobrescrever)
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/themes/{themeName}.xaml") });
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/themes/Styles.xaml") });
        }

        #endregion
    }

    public class FolderNode : BaseViewModel
    {
        private bool _isSelected;
        private bool _isExpanded = true;
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public ObservableCollection<FolderNode> SubFolders { get; set; } = new();
        public Action<FolderNode>? OnSelected { get; set; }
        public bool IsExpanded { get => _isExpanded; set => SetProperty(ref _isExpanded, value); }
        public bool IsSelected { get => _isSelected; set { if (!SetProperty(ref _isSelected, value)) return; if (value) OnSelected?.Invoke(this); } }
    }
}
