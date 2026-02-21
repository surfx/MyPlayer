//#define DEBUG
#undef DEBUG

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

namespace MyPlayer
{
    public partial class frmMyPlayer : Form
    {
#if DEBUG
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
#endif

        // Extensões permitidas para tocar pelo NAudio
        private static readonly string[] ExtensoesPermitidas = [".mp3", ".mp4", ".wav", ".flac", ".aac", ".wma"];

        private PlayerControl? _playerControl;
        private static readonly bool PermitirSystray = false;

        private WaveImage? _wi;
        private FiltrarMusicas _filtrarMusicas = FiltrarMusicas.Instance;
        private FormularioEstado _estadoAtual = new();

        private bool _isDarkMode = false;
        private ImageList? _lightImageList;
        private ImageList? _darkImageList;

        // ✅ Flags de controle simplificadas
        private bool _isManualNavigation = false;  // True quando usuário clica próximo/anterior
        private bool _isDoubleClick = false;       // True quando usuário dá duplo clique na lista

        private enum EImageIndex : int { play = 3, pause = 9 }

        #region form

        public frmMyPlayer()
        {
            InitializeComponent();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("logs/myplayer-.txt", rollingInterval: Serilog.RollingInterval.Day)
                .CreateLogger();
        }

        private void frmMyPlayer_Load(object sender, EventArgs e)
        {
#if DEBUG
            AllocConsole();
#endif
            try
            {
                _lightImageList = imageList1;
                _darkImageList = Util.CreateWhiteImageList(_lightImageList);

                _isDarkMode = ThemeManager.IsSystemDarkMode();
                ApplyCurrentTheme();

                GlobalKeyboardHook.SetHook(handleKeyPress);

                InvokeAux.Access(lblStatus, lbl => lbl.Text = string.Empty);
                InvokeAux.Access(progressBar1, pg =>
                {
                    pg.Minimum = 0;
                    pg.Maximum = 100;
                });
                InvokeAux.Access(trackBar1, tckbar =>
                {
                    tckbar.Minimum = 0;
                    tckbar.Maximum = 100;
                    tckbar.TickStyle = TickStyle.None;
                });

                new ContextMenuStripAux(ref listView1, ref contextMenuStrip1, _playerControl).UpdateContextMenuStrip();

                string musicPath = Util.MusicPath;
                InvokeAux.Access(txtPathMusicas, txt => txt.Text = musicPath);
                TreeViewUtil.PreencherTreeView(treeView1, musicPath);

                if (!CarregarEstadoDoFormulario())
                {
                    ListViewAux.ListarArquivosListView(
                        ref listView1, ref imageList1, ExtensoesPermitidas,
                        ref _estadoAtual, ref _filtrarMusicas, musicPath, true, false);
                }

                Log.Information("Aplicação iniciada com sucesso");
                playMusic();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Erro crítico ao iniciar aplicação");
                MessageBox.Show($"Erro ao iniciar: {ex.Message}", "Erro Fatal",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void frmMyPlayer_Shown(object sender, EventArgs e)
        {
            InvokeAux.Access(txtFiltro, txt => txt.Focus());
        }

        #region systray

        private void frmMyPlayer_Resize(object sender, EventArgs e)
        {
            if (!PermitirSystray) return;

            if (FormWindowState.Minimized == this.WindowState)
            {
                notifyIcon1.Visible = true;
                this.Hide();
            }
            else if (FormWindowState.Normal == this.WindowState)
            {
                notifyIcon1.Visible = false;
            }
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
            this.Focus();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            notifyIcon1.Visible = false;
        }

        #endregion

        private void frmMyPlayer_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                SalvarEstadoDoFormulario(true);
                GlobalKeyboardHook.Unhook();
                DisposePlayer();
                Log.Information("Aplicação encerrada normalmente");
                Log.CloseAndFlush();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao fechar aplicação");
            }
        }

        #endregion

        #region Theme

        private void btnDarkMode_Click(object sender, EventArgs e)
        {
            _isDarkMode = !_isDarkMode;
            ApplyCurrentTheme();
            _estadoAtual.IsDarkMode = _isDarkMode;
            SalvarEstadoDoFormulario(false);
        }

        private void ApplyCurrentTheme()
        {
            ThemeManager.ApplyTheme(this, _isDarkMode);

            ImageList? targetList = _isDarkMode ? _darkImageList : _lightImageList;
            if (targetList != null)
            {
                btnOpenFolderMusics.ImageList = targetList;
                btnRandomizar.ImageList = targetList;
                btnVoltar.ImageList = targetList;
                btnPlayPause.ImageList = targetList;
                btnProximo.ImageList = targetList;
                btnClearPlayList.ImageList = targetList;
                btnExcluirMusicasPlayList.ImageList = targetList;
                btnSalvarMusicasPlayList.ImageList = targetList;
                btnCarregarMusicasPlayList.ImageList = targetList;

                treeView1.ImageList = targetList;
                listView1.SmallImageList = targetList;
                listView1.LargeImageList = targetList;
            }

            btnDarkMode.Text = _isDarkMode ? "☀" : "🌙";
        }

        #endregion

        #region controle estados

        private void SalvarEstadoDoFormulario(bool clearFilter = true)
        {
            _estadoAtual.IsDarkMode = _isDarkMode;
            _estadoAtual.Musicas = ListViewAux.GetListMusicas(ref listView1, ExtensoesPermitidas);
            EstadoFormAux.SalvarEstadoDoFormulario(
                ref txtFiltro, ref _filtrarMusicas, ref listView1, ref _estadoAtual, clearFilter);
        }

        private void SalvarEstadoDebounced(bool clearFilter = false)
        {
            _estadoAtual.IsDarkMode = _isDarkMode;
            _estadoAtual.Musicas = ListViewAux.GetListMusicas(ref listView1, ExtensoesPermitidas);
            EstadoFormAux.SalvarEstadoDoFormularioDebounced(
                ref txtFiltro, ref _filtrarMusicas, ref listView1, ref _estadoAtual, clearFilter);
        }

        private bool CarregarEstadoDoFormulario()
        {
            bool carregou = EstadoFormAux.CarregarEstadoDoFormulario(
                ref _estadoAtual, ref _filtrarMusicas, ref listView1, ref imageList1,
                AtualizarSelecaoMusicaAtual, ref txtPathMusicas, ref treeView1);

            if (carregou)
            {
                _isDarkMode = _estadoAtual.IsDarkMode;
                ApplyCurrentTheme();
            }
            return carregou;
        }

        #endregion

        #region keypress

        private void handleKeyPress(Keys keys)
        {
            switch (keys)
            {
                case Keys.MediaStop:
                    stop();
                    break;
                case Keys.MediaPlayPause:
                    playPause();
                    break;
                case Keys.MediaNextTrack:
                    nextMusic();
                    break;
                case Keys.MediaPreviousTrack:
                    previousMusic();
                    break;
                case Keys.F3:
                    InvokeAux.Access(txtFiltro, txt => txt.Focus());
                    break;
            }
        }

        #endregion

        #region PictureBox

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (_wi == null || pictureBox1.Image == null) return;
            _wi?.clickPictureBox(e, pictureBox1.Image);
        }

        #endregion

        #region Player Control

        /// <summary>
        /// ✅ Toca a música no índice atual
        /// </summary>
        private void playMusic()
        {
            _estadoAtual.Musicas ??= ListViewAux.GetListMusicas(ref listView1, ExtensoesPermitidas);

            if (_estadoAtual.Musicas == null || _estadoAtual.Musicas.Count == 0)
            {
                Log.Warning("Tentativa de tocar música com lista vazia");
                return;
            }

            // Normaliza índice
            NormalizarIndice();

            var musicaAtual = _estadoAtual.Musicas[_estadoAtual.IndiceMusica];
            string? path = musicaAtual.Tag;

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Log.Warning("Arquivo não encontrado: {Path}", path);
                MessageBox.Show("Arquivo de música não encontrado!", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Dispose do player anterior
            DisposePlayer();

            // Feedback visual
            InvokeAux.Access(lblStatus, lbl => lbl.Text = "Carregando...");
            InvokeAux.Access(btnPlayPause, btn => btn.Enabled = false);

            try
            {
                _playerControl = new PlayerControl(path);

                // Registra eventos
                _playerControl.EvtProgressUpdated += Player_ProgressUpdated;
                _playerControl.EvtPlaying += Player_EvtPlaying;
                _playerControl.EvtPaused += Player_EvtPaused;
                _playerControl.EvtResume += Player_EvtResume;
                _playerControl.EvtStop += Player_EvtStop;
                _playerControl.EvtMusicEnded += Player_EvtMusicEnded;

                _playerControl.Play();

                // Visualização de onda
                if (_playerControl.AudioFileReaderProp != null)
                {
                    _wi?.Dispose();
                    _wi = new(_playerControl.AudioFileReaderProp, this, width: pictureBox1.Width);
                    _wi?.init(image =>
                    {
                        if (image == null) return;
                        InvokeAux.Access(pictureBox1, pct =>
                        {
                            pct.Image?.Dispose();
                            pct.Image = image;
                        });
                    });
                }

                Log.Information("Reproduzindo: [{Indice}] {Path}", _estadoAtual.IndiceMusica, path);

                AtualizarSelecaoMusicaAtual();
                SalvarEstadoDebounced(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao reproduzir música: {Path}", path);
                MessageBox.Show($"Erro ao carregar música:\n{ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                DisposePlayer();
            }
            finally
            {
                InvokeAux.Access(btnPlayPause, btn => btn.Enabled = true);
            }
        }

        /// <summary>
        /// ✅ Normaliza o índice para ficar dentro dos limites
        /// </summary>
        private void NormalizarIndice()
        {
            if (_estadoAtual.Musicas == null || _estadoAtual.Musicas.Count == 0)
            {
                _estadoAtual.IndiceMusica = 0;
                return;
            }

            if (_estadoAtual.IndiceMusica < 0)
                _estadoAtual.IndiceMusica = _estadoAtual.Musicas.Count - 1;
            
            if (_estadoAtual.IndiceMusica >= _estadoAtual.Musicas.Count)
                _estadoAtual.IndiceMusica = 0;
        }

        /// <summary>
        /// ✅ Dispose seguro de todos os recursos do player
        /// </summary>
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

                InvokeAux.Access(pictureBox1, pct =>
                {
                    pct.Image?.Dispose();
                    pct.Image = null;
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao dispor player");
            }
        }

        private void playPause()
        {
            if (InvokeAux.GetValue(listView1, lvw => lvw.Items.Count) <= 0)
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

        /// <summary>
        /// ✅ Para a reprodução completamente
        /// </summary>
        private void stop()
        {
            _isManualNavigation = true; // Evita que EvtMusicEnded avance para próxima
            
            DisposePlayer();
            
            updateFormTitle(true);
            InvokeAux.Access(btnPlayPause, btn => btn.ImageIndex = (int)EImageIndex.play);
            InvokeAux.Access(lblStatus, lbl => lbl.Text = "⏹ Parado");
            InvokeAux.Access(progressBar1, pg => pg.Value = 0);
            InvokeAux.Access(trackBar1, tckbar => tckbar.Value = 0);
            
            _isManualNavigation = false;
        }

        /// <summary>
        /// ✅ Avança para próxima música
        /// </summary>
        private void nextMusic()
        {
            _estadoAtual.Musicas ??= ListViewAux.GetListMusicas(ref listView1, ExtensoesPermitidas);
            if (_estadoAtual.Musicas == null || _estadoAtual.Musicas.Count == 0) return;

            _isManualNavigation = true;
            
            // Para o player atual
            DisposePlayer();
            
            // Avança o índice
            _estadoAtual.IndiceMusica++;
            NormalizarIndice();
            
            // Toca a próxima
            playMusic();
            
            _isManualNavigation = false;
        }

        /// <summary>
        /// ✅ Volta para música anterior
        /// </summary>
        private void previousMusic()
        {
            _estadoAtual.Musicas ??= ListViewAux.GetListMusicas(ref listView1, ExtensoesPermitidas);
            if (_estadoAtual.Musicas == null || _estadoAtual.Musicas.Count == 0) return;

            _isManualNavigation = true;
            
            // Para o player atual
            DisposePlayer();
            
            // Volta o índice
            _estadoAtual.IndiceMusica--;
            NormalizarIndice();
            
            // Toca a anterior
            playMusic();
            
            _isManualNavigation = false;
        }

        private void AtualizarSelecaoMusicaAtual()
        {
            InvokeAux.Access(listView1, lv =>
            {
                if (lv.Items.Count == 0 || _estadoAtual.IndiceMusica < 0 || _estadoAtual.IndiceMusica >= lv.Items.Count)
                    return;

                lv.BeginUpdate();
                lv.SelectedItems.Clear();

                var itemAtual = lv.Items[_estadoAtual.IndiceMusica];
                itemAtual.Selected = true;
                itemAtual.Focused = true;
                itemAtual.EnsureVisible();
                lv.Focus();

                lv.EndUpdate();
            });
        }

        #endregion

        #region Player Events

        private void Player_EvtPlaying(object? sender, EventArgs e)
        {
            updateFormTitle();
            InvokeAux.Access(btnPlayPause, btn => btn.ImageIndex = (int)EImageIndex.pause);
            AtualizarSelecaoMusicaAtual();

            if (_playerControl == null) return;

            InvokeAux.Access(progressBar1, pg => pg.Value = 0);
            InvokeAux.Access(trackBar1, tckbar => tckbar.Value = 0);

            TimeSpan musicDuration = _playerControl.MusicDuration;
            InvokeAux.Access(lblStatus, lbl => lbl.Text = $"00:00 / {musicDuration:mm\\:ss}");
        }

        private void Player_EvtPaused(object? sender, EventArgs e)
        {
            updateFormTitle(status: "⏸ Pausado");
            InvokeAux.Access(btnPlayPause, btn => btn.ImageIndex = (int)EImageIndex.play);
        }

        private void Player_EvtResume(object? sender, EventArgs e)
        {
            updateFormTitle();
            InvokeAux.Access(btnPlayPause, btn => btn.ImageIndex = (int)EImageIndex.pause);
            AtualizarSelecaoMusicaAtual();
        }

        private void Player_EvtStop(object? sender, EventArgs e)
        {
            // ✅ Só atualiza UI se não for navegação manual
            if (!_isManualNavigation)
            {
                updateFormTitle(true);
                InvokeAux.Access(btnPlayPause, btn => btn.ImageIndex = (int)EImageIndex.play);
                InvokeAux.Access(lblStatus, lbl => lbl.Text = "⏹ Parado");
                InvokeAux.Access(progressBar1, pg => pg.Value = 0);
                InvokeAux.Access(trackBar1, tckbar => tckbar.Value = 0);
            }
        }

        /// <summary>
        /// ✅ Chamado quando a música termina NATURALMENTE
        /// </summary>
        private void Player_EvtMusicEnded(object? sender, EventArgs e)
        {
            // Se foi navegação manual (próximo/anterior/stop), ignora
            if (_isManualNavigation) return;
            
            // Se foi duplo clique, ignora (playMusic já foi chamado)
            if (_isDoubleClick)
            {
                _isDoubleClick = false;
                return;
            }

            Log.Debug("Música terminou naturalmente, avançando...");
            
            // Avança para próxima música automaticamente
            _estadoAtual.IndiceMusica++;
            NormalizarIndice();
            
            updateFormTitle(true);
            playMusic();
        }

        private void Player_ProgressUpdated(object? sender, double percent)
        {
            if (_playerControl == null) return;

            if (percent >= 0 && percent <= 100)
            {
                InvokeAux.Access(progressBar1, pg => pg.Value = (int)percent);
                InvokeAux.Access(trackBar1, tckbar => tckbar.Value = (int)percent);
            }

            TimeSpan currentTime = _playerControl.CurrentTime;
            TimeSpan musicDuration = _playerControl.MusicDuration;
            InvokeAux.Access(lblStatus, lbl => lbl.Text = $"{currentTime:mm\\:ss} / {musicDuration:mm\\:ss}");

            UpdateWaveImage();
        }

        private void UpdateWaveImage()
        {
            Image? image = _wi?.getUpdateImage();
            if (image == null) return;

            InvokeAux.Access(pictureBox1, pct =>
            {
                var oldImage = pct.Image;
                pct.Image = image;

                if (oldImage != null && oldImage != image)
                {
                    oldImage.Dispose();
                }
            });
        }

        private void updateFormTitle(bool reset = false, string status = "")
        {
            if (reset)
            {
                InvokeAux.Access(this, frm => frm.Text = "My Player");
                return;
            }

            if (_estadoAtual.Musicas != null && 
                _estadoAtual.IndiceMusica >= 0 && 
                _estadoAtual.IndiceMusica < _estadoAtual.Musicas.Count)
            {
                MusicaDTO itemAtual = _estadoAtual.Musicas[_estadoAtual.IndiceMusica];
                string nomeSemExtensao = Path.GetFileNameWithoutExtension(itemAtual.Text);
                string title = $"My Player | {nomeSemExtensao}";

                if (!string.IsNullOrEmpty(status))
                {
                    title = $"{title} | {status}";
                }

                InvokeAux.Access(this, frm => frm.Text = title);
                return;
            }

            InvokeAux.Access(this, frm => frm.Text = "My Player");
        }

        #endregion

        #region Botoes

        private void btnOpenFolderMusics_Click(object sender, EventArgs e)
        {
            InvokeAux.Access(listView1, lvw => lvw.Items.Clear());

            using (FolderBrowserDialog folderDialog = new())
            {
                string currentPath = InvokeAux.GetValue(txtPathMusicas, txt => txt.Text);

                if (!string.IsNullOrEmpty(currentPath))
                {
                    folderDialog.SelectedPath = currentPath;
                }

                DialogResult result = folderDialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
                {
                    InvokeAux.Access(txtPathMusicas, txt =>
                    {
                        txt.Text = folderDialog.SelectedPath.EndsWith(@"\")
                            ? folderDialog.SelectedPath
                            : folderDialog.SelectedPath + @"\";

                        _estadoAtual.MusicPath = txt.Text;
                        TreeViewUtil.PreencherTreeView(treeView1, txt.Text);
                        SalvarEstadoDoFormulario(true);
                    });
                }
            }
        }

        private void btnRandomizar_Click(object sender, EventArgs e)
        {
            _estadoAtual.Musicas ??= ListViewAux.GetListMusicas(ref listView1, ExtensoesPermitidas);

            if (_estadoAtual.Musicas == null || _estadoAtual.Musicas.Count == 0) return;

            // Para a música atual
            _isManualNavigation = true;
            DisposePlayer();

            // Embaralha
            _estadoAtual.Musicas = Util.Shuffle(_estadoAtual.Musicas) ?? [];
            _estadoAtual.IndiceMusica = 0;
            _filtrarMusicas.SetEstado(_estadoAtual);

            // Atualiza ListView
            InvokeAux.Access(listView1, lvw =>
            {
                lvw.BeginUpdate();
                try
                {
                    lvw.Items.Clear();
                    foreach (var musicaDto in _estadoAtual.Musicas)
                    {
                        lvw.Items.Add(ListViewAux.ToListViewItem(musicaDto));
                    }
                }
                finally
                {
                    lvw.EndUpdate();
                }
            });

            SalvarEstadoDoFormulario(true);

            // Toca a primeira
            _isManualNavigation = false;
            playMusic();

            Log.Information("Playlist embaralhada: {Total} músicas", _estadoAtual.Musicas.Count);
        }

        private void btnVoltar_Click(object sender, EventArgs e) => previousMusic();
        private void btnPlayPause_Click(object sender, EventArgs e) => playPause();
        private void btnProximo_Click(object sender, EventArgs e) => nextMusic();

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (_playerControl == null) return;
            _playerControl.SetPercent(InvokeAux.GetValue(trackBar1, tckbar => tckbar.Value));
        }

        #endregion

        #region TreeView

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string? caminho = e?.Node?.Tag as string;
            treeView1.SelectedNode = null;

            if (!string.IsNullOrEmpty(caminho) && Directory.Exists(caminho))
            {
                ListViewAux.ListarArquivosListView(
                    ref listView1, ref imageList1, ExtensoesPermitidas,
                    ref _estadoAtual, ref _filtrarMusicas, caminho, false, false);

                SalvarEstadoDoFormulario(true);
            }
        }

        #endregion

        #region ListView

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (InvokeAux.GetValue(listView1, lvw => lvw.SelectedItems.Count) == 0) return;

            _isDoubleClick = true;
            _isManualNavigation = true;
            
            // Para o player atual
            DisposePlayer();
            
            // Atualiza o índice para o item clicado
            _estadoAtual.IndiceMusica = InvokeAux.GetValue(listView1, lvw => lvw.SelectedItems[0].Index);
            
            // Toca a música selecionada
            _isManualNavigation = false;
            playMusic();
        }

        private void listView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            var info = InvokeAux.GetValue(listView1, lvw => lvw.HitTest(e.Location));

            if (info.Item != null)
            {
                info.Item.Selected = true;
            }
            else
            {
                InvokeAux.Access(listView1, lvw => lvw.SelectedItems.Clear());
            }
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column != 0) return;

            bool todosMarcados = listView1.CheckedItems.Count == listView1.Items.Count;

            listView1.BeginUpdate();
            foreach (ListViewItem item in listView1.Items)
            {
                item.Checked = !todosMarcados;
            }
            listView1.EndUpdate();
        }

        private void listView1_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            btnExcluirMusicasPlayList.Enabled = listView1.CheckedItems.Count > 0;
        }

        #endregion

        #region Playlist

        private void btnClearPlayList_Click(object sender, EventArgs e)
        {
            if (listView1.Items.Count <= 0) return;

            DialogResult dr = MessageBox.Show("Deseja limpar a playlist?", "Limpar playlist",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (dr != DialogResult.Yes) return;

            stop();

            InvokeAux.Access(listView1, lvw =>
            {
                lvw.BeginUpdate();
                lvw.Items.Clear();
                lvw.EndUpdate();
            });

            _estadoAtual.Musicas = new();
            _estadoAtual.IndiceMusica = 0;

            SalvarEstadoDoFormulario(true);

            Log.Information("Playlist limpa");
        }

        private void btnExcluirMusicasPlayList_Click(object sender, EventArgs e)
        {
            int totalMarcadas = listView1.CheckedItems.Count;
            if (totalMarcadas == 0) return;

            string mensagem = totalMarcadas == 1
                ? "Deseja remover a música selecionada da playlist?"
                : $"Deseja remover as {totalMarcadas} músicas selecionadas da playlist?";

            DialogResult dr = MessageBox.Show(mensagem, "Confirmar Exclusão",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (dr != DialogResult.Yes) return;

            stop();

            listView1.BeginUpdate();

            var caminhosParaRemover = listView1.CheckedItems
                .Cast<ListViewItem>()
                .Select(x => x.Tag?.ToString())
                .ToHashSet();

            foreach (ListViewItem item in listView1.CheckedItems.Cast<ListViewItem>().ToList())
            {
                listView1.Items.Remove(item);
            }

            _estadoAtual.Musicas?.RemoveAll(m => caminhosParaRemover.Contains(m.Tag));

            listView1.EndUpdate();

            _estadoAtual.IndiceMusica = 0;
            SalvarEstadoDoFormulario(true);

            Log.Information("{Total} músicas removidas da playlist", totalMarcadas);
        }

        private void btnSalvarMusicasPlayList_Click(object sender, EventArgs e)
        {
            AtualizarInterfaceListView();

            if (listView1.Items.Count == 0)
            {
                MessageBox.Show("Não há músicas na lista para salvar.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new())
            {
                sfd.InitialDirectory = Util.MusicPath;
                sfd.Filter = "Playlist JSON|*.json";
                sfd.Title = "Salvar Playlist";
                sfd.FileName = "playlist";

                if (sfd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    List<MusicaDTO> dadosParaSalvar = ListViewAux.GetListMusicas(ref listView1, ExtensoesPermitidas);
                    PlayList.Salvar(sfd.FileName, dadosParaSalvar);

                    MessageBox.Show($"Playlist salva com sucesso!\n{dadosParaSalvar.Count} músicas",
                        "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    Log.Information("Playlist salva: {Path} ({Total} músicas)", sfd.FileName, dadosParaSalvar.Count);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Erro ao salvar playlist");
                    MessageBox.Show($"Erro ao salvar: {ex.Message}", "Erro",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnCarregarMusicasPlayList_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new())
            {
                ofd.InitialDirectory = Util.MusicPath;
                ofd.Filter = "Playlist JSON|*.json";
                ofd.Title = "Selecionar Playlist";

                if (ofd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    List<MusicaDTO> musicasCarregadas = PlayList.Carregar(ofd.FileName);

                    if (musicasCarregadas == null || musicasCarregadas.Count == 0)
                    {
                        MessageBox.Show("A playlist selecionada está vazia ou é inválida.", "Aviso",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    stop();

                    _estadoAtual.Musicas = musicasCarregadas;
                    _filtrarMusicas.SetEstado(_estadoAtual);
                    _estadoAtual.IndiceMusica = 0;

                    AtualizarInterfaceListView();

                    SalvarEstadoDoFormulario(true);

                    MessageBox.Show($"{musicasCarregadas.Count} músicas carregadas com sucesso!",
                        "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    Log.Information("Playlist carregada: {Path} ({Total} músicas)", ofd.FileName, musicasCarregadas.Count);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Erro ao carregar playlist");
                    MessageBox.Show($"Erro ao carregar playlist: {ex.Message}", "Erro",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void AtualizarInterfaceListView()
        {
            InvokeAux.Access(txtFiltro, txt => txt.Text = string.Empty);
            _filtrarMusicas.ResetMemory();

            InvokeAux.Access(listView1, lvw =>
            {
                try
                {
                    lvw.BeginUpdate();
                    lvw.Items.Clear();

                    if (_estadoAtual.Musicas == null) return;

                    foreach (var mDto in _estadoAtual.Musicas)
                    {
                        lvw.Items.Add(ListViewAux.ToListViewItem(mDto));
                    }
                }
                finally
                {
                    lvw.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                    lvw.EndUpdate();
                }
            });
        }

        #endregion

        #region Filtro

        private void txtFiltro_TextChanged(object sender, EventArgs e)
        {
            _filtrarMusicas.Filtrar(txtFiltro.Text, listView1);
        }

        private void txtFiltro_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (listView1.Items.Count <= 0) return;
                playPause();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                txtFiltro.Clear();
                _filtrarMusicas.Filtrar(string.Empty, listView1);
            }
            else if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Left)
            {
                previousMusic();
            }
            else if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Right)
            {
                nextMusic();
            }
        }

        #endregion
    }
}