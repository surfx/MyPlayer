//#define DEBUG
#undef DEBUG

using MyPlayer.classes.controleestados;
using MyPlayer.classes.filtrarmusicas;
using MyPlayer.classes.keyhook;
using MyPlayer.classes.player;
using MyPlayer.classes.util;
using MyPlayer.classes.util.threads;
using MyPlayer.classes.util.treeview;
using MyPlayer.classes.waveimage;

namespace MyPlayer
{
    public partial class frmMyPlayer : Form
    {
#if DEBUG
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
#endif

        // extensões permitidas para tocar pelo NAudio
        private static readonly string[] ExtensoesPermitidas = [".mp3", ".mp4", ".wav", ".flac", ".aac", ".wma"];

        private bool _skipToNext = false;
        private bool _skipToPrevious = false;
        private PlayerControl? _playerControl;
        private bool listViewDblClick = false;
        private const bool PermitirSystray = false;
        private bool _skipStopAnaliseMusica = false;
        private bool _skipPlayMusica = false;

        private WaveImage _wi;

        private FiltrarMusicas _filtrarMusicas = FiltrarMusicas.Instance;

        private enum EImageIndex : int { play = 3, pause = 9 }

        #region form

        public frmMyPlayer()
        {
            InitializeComponent();
        }

        private void frmMyPlayer_Load(object sender, EventArgs e)
        {
#if DEBUG
            AllocConsole();
#endif
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

            updateContextMenuStrip();

            _skipToNext = _skipToPrevious = false;

            if (CarregarEstadoDoFormulario())
            {
                playMusic();
                return;
            }

            string musicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            musicPath = musicPath.EndsWith(@"\") ? musicPath : musicPath + @"\";
            InvokeAux.Access(txtPathMusicas, txt => txt.Text = musicPath);
            TreeViewUtil.PreencherTreeView(treeView1, musicPath);
            ListarArquivos(musicPath);
            playMusic();
        }

        private void frmMyPlayer_Shown(object sender, EventArgs e)
        {
            InvokeAux.Access(txtFiltro, txt => txt.Focus());
        }

        #region systray
        private void frmMyPlayer_Resize(object sender, EventArgs e)
        {
            //if (!chkSysTray.Checked) { return; }
            if (FormWindowState.Minimized == this.WindowState)
            {
                notifyIcon1.Visible = true;
                //notifyIcon1.ShowBalloonTip(500);

                if (!PermitirSystray) { return; }
                //this.Hide();
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
            SalvarEstadoDoFormulario(true);
            GlobalKeyboardHook.Unhook();
        }

        #endregion


        #region controle estados
        private FormularioEstado _estadoAtual = new();

        private void SalvarEstadoDoFormulario(bool clearFilter = true)
        {
            if (clearFilter)
            {
                InvokeAux.Access(txtFiltro, txt => txt.Text = string.Empty);
                _filtrarMusicas.Filtrar(string.Empty, listView1);
            }

            if (_estadoAtual.ListVewStateProp == null) { _estadoAtual.ListVewStateProp = new(); }
            _estadoAtual.ListVewStateProp.View = (int)listView1.View;
            _estadoAtual.ListVewStateProp.ColumnWidths = [];
            foreach (ColumnHeader col in listView1.Columns)
                _estadoAtual.ListVewStateProp.ColumnWidths.Add(col.Width);

            ControleEstados.SalvarEstado(_estadoAtual);
        }

        private bool CarregarEstadoDoFormulario()
        {
            var aux = ControleEstados.RecuperarEstado();
            if (aux == null) { return false; }
            _estadoAtual = aux;
            _filtrarMusicas.SetEstado(_estadoAtual);

            //_indiceMusica = _estadoAtual.IndiceMusica;

            if (_estadoAtual.ListVewStateProp == null)
            {
                _estadoAtual.ListVewStateProp = new()
                {
                    View = (int)View.Details,
                    ColumnWidths = [150, 100, 150]
                };
            }

            InvokeAux.Access(listView1, lvw =>
            {
                lvw.BeginUpdate();
                lvw.Items.Clear();
                lvw.View = (View)_estadoAtual.ListVewStateProp.View; // importante restaurar a View
                lvw.SmallImageList = imageList1;

                // Restaurar colunas se quiser
                lvw.Columns.Clear();
                lvw.Columns.Add("Nome", _estadoAtual.ListVewStateProp.ColumnWidths[0]);
                lvw.Columns.Add("Tamanho (KB)", _estadoAtual.ListVewStateProp.ColumnWidths[1]);
                lvw.Columns.Add("Data de Modificação", _estadoAtual.ListVewStateProp.ColumnWidths[2]);

                foreach (var sItem in _estadoAtual.Musicas)
                {
                    ListViewItem item = new(sItem.Text)
                    {
                        Tag = sItem.Tag,
                        ImageIndex = sItem.ImageIndex
                    };
                    foreach (var sub in sItem.SubItems)
                        item.SubItems.Add((ListViewItem.ListViewSubItem)sub);

                    lvw.Items.Add(item);
                }

                lvw.EndUpdate();
            });

            AtualizarSelecaoMusicaAtual();

            if (!string.IsNullOrEmpty(_estadoAtual.MusicPath))
            {
                InvokeAux.Access(txtPathMusicas, txt => txt.Text = _estadoAtual.MusicPath);
                TreeViewUtil.PreencherTreeView(treeView1, _estadoAtual.MusicPath);
            }

            return true;
        }
        #endregion


        #region keypress
        private void handleKeyPress(Keys keys)
        {
            switch (keys)
            {
                case Keys.MediaStop: stop(); break;
                case Keys.MediaPlayPause: playPause(); break;
                case Keys.MediaNextTrack: nextMusic(); break;
                case Keys.MediaPreviousTrack: previousMusic(); break;
                case Keys.F3:
                    {
                        InvokeAux.Access(txtFiltro, txt => txt.Focus());
                        break;
                    }
            }
        }
        #endregion

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (_wi == null) { return; }
            _wi.clickPictureBox(e, pictureBox1.Image);
        }

        private void btnOpenFolderMusics_Click(object sender, EventArgs e)
        {
            InvokeAux.Access(listView1, lvw => lvw.Items.Clear());

            using (FolderBrowserDialog folderDialog = new())
            {
                if (!string.IsNullOrEmpty(InvokeAux.GetValue(txtPathMusicas, txt => txt.Text))) { folderDialog.SelectedPath = InvokeAux.GetValue(txtPathMusicas, txt => txt.Text); }
                DialogResult result = folderDialog.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
                {
                    InvokeAux.Access(txtPathMusicas, txt =>
                    {
                        txt.Text = folderDialog.SelectedPath.EndsWith(@"\") ? folderDialog.SelectedPath : folderDialog.SelectedPath + @"\";
                        _estadoAtual.MusicPath = txt.Text;
                        TreeViewUtil.PreencherTreeView(treeView1, txt.Text);

                        SalvarEstadoDoFormulario(true);
                    });
                }
            }
        }

        #region ContextMenuStrip
        private void updateContextMenuStrip()
        {
            var abrirPasta = new ToolStripMenuItem("Abrir pasta onde está o arquivo");
            var copiarCaminho = new ToolStripMenuItem("Copiar caminho do arquivo");
            var deletarArquivo = new ToolStripMenuItem("Deletar arquivo");

            abrirPasta.Click += AbrirPasta_Click;
            copiarCaminho.Click += CopiarCaminho_Click;
            deletarArquivo.Click += DeletarArquivo_Click;

            contextMenuStrip1.Items.AddRange(new ToolStripItem[]
            {
                abrirPasta,
                copiarCaminho,
                deletarArquivo
            });

        }

        private void AbrirPasta_Click(object? sender, EventArgs e)
        {
            if (InvokeAux.GetValue(listView1, lvw => lvw.SelectedItems.Count) == 0) return;
            string? caminho = InvokeAux.GetValue(listView1, lvw => lvw.SelectedItems[0].Tag?.ToString());
            if (string.IsNullOrEmpty(caminho)) return;

            if (File.Exists(caminho))
            {
                string? pasta = Path.GetDirectoryName(caminho);
                if (pasta != null)
                    System.Diagnostics.Process.Start("explorer.exe", pasta);
            }
            else if (Directory.Exists(caminho))
            {
                System.Diagnostics.Process.Start("explorer.exe", caminho);
            }
        }

        private void CopiarCaminho_Click(object? sender, EventArgs e)
        {
            if (InvokeAux.GetValue(listView1, lvw => lvw.SelectedItems.Count) == 0) return;
            string? caminho = InvokeAux.GetValue(listView1, lvw => lvw.SelectedItems[0].Tag?.ToString());
            if (!string.IsNullOrEmpty(caminho))
            {
                Clipboard.SetText(caminho);
            }
        }

        private void DeletarArquivo_Click(object? sender, EventArgs e)
        {
            if (InvokeAux.GetValue(listView1, lvw => lvw.SelectedItems.Count) == 0) return;
            string? caminho = InvokeAux.GetValue(listView1, lvw => lvw.SelectedItems[0].Tag?.ToString());
            if (string.IsNullOrEmpty(caminho) || !File.Exists(caminho)) return;

            var nome = Path.GetFileName(caminho);
            var result = MessageBox.Show(
                $"Tem certeza que deseja deletar o arquivo:\n\n{nome}?",
                "Confirmar exclusão",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                try
                {
                    _playerControl?.Stop();

                    File.Delete(caminho);
                    InvokeAux.Access(listView1, lvw => lvw.Items.Remove(listView1.SelectedItems[0]));
                    MessageBox.Show("Arquivo deletado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao deletar o arquivo:\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        #endregion

        #region treeview
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string? caminho = e?.Node?.Tag as string;

            if (!string.IsNullOrEmpty(caminho) && Directory.Exists(caminho))
            {
                ListarArquivos(caminho);
                SalvarEstadoDoFormulario(true);
            }
        }
        #endregion

        #region listview

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (InvokeAux.GetValue(listView1, lvw => listView1.SelectedItems.Count) == 0) return;

            listViewDblClick = true;

            // Atualiza o índice atual
            _estadoAtual.IndiceMusica = InvokeAux.GetValue(listView1, lvw => lvw.SelectedItems[0].Index);
            _playerControl?.Stop();

            if (_playerControl == null || !_playerControl.IsValid)
            {
                playMusic();
            }
        }

        private void listView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var info = InvokeAux.GetValue(listView1, lvw => lvw.HitTest(e.Location));
                if (info.Item != null)
                {
                    info.Item.Selected = true;
                }
                else
                {
                    // se clicar em área vazia
                    InvokeAux.Access(listView1, lvw => lvw.SelectedItems.Clear());
                }
            }
        }

        private void ListarArquivos(string path)
        {
            const int maxFileStr = 100;

            InvokeAux.Access(listView1, lvw =>
            {
                lvw.Items.Clear();

                // Configuração inicial do ListView
                lvw.View = View.Details;
                lvw.SmallImageList = imageList1;
                lvw.Columns.Clear();
                lvw.Columns.Add("Nome", 150);
                lvw.Columns.Add("Tamanho (KB)", 100);
                lvw.Columns.Add("Data de Modificação", 150);

                try
                {
                    // Pastas
                    string[] pastas = Directory.GetDirectories(path);
                    foreach (string pasta in pastas)
                    {
                        DirectoryInfo di = new(pasta);
                        string nome = di.Name;

                        if (nome.Length > maxFileStr)
                            nome = nome.Substring(0, maxFileStr) + "...";

                        ListViewItem item = new(nome)
                        {
                            ImageIndex = 0, // folder fechado
                            Tag = di.FullName
                        };
                        item.SubItems.Add(""); // tamanho vazio para pastas
                        item.SubItems.Add(di.LastWriteTime.ToString());
                        lvw.Items.Add(item);
                    }

                    // Arquivos — filtra apenas extensões permitidas
                    string[] arquivos = Directory.GetFiles(path);
                    foreach (string arquivo in arquivos)
                    {
                        string extensao = Path.GetExtension(arquivo).ToLowerInvariant();

                        // só adiciona se a extensão estiver na lista permitida
                        if (!ExtensoesPermitidas.Contains(extensao))
                            continue;

                        FileInfo fi = new(arquivo);
                        string nome = fi.Name;

                        if (nome.Length > maxFileStr)
                            nome = nome.Substring(0, maxFileStr) + "...";

                        ListViewItem item = new(nome)
                        {
                            ImageIndex = 10, // ícone de arquivo mp3
                            Tag = fi.FullName
                        };
                        item.SubItems.Add((fi.Length / 1024).ToString());
                        item.SubItems.Add(fi.LastWriteTime.ToString());
                        lvw.Items.Add(item);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Sem permissão para acessar alguns arquivos nesta pasta.");
                }

                lvw.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

                // atualiza a lista de musicas
                _estadoAtual.Musicas = GetListMusicas();
                _estadoAtual.IndiceMusica = 0;
                _filtrarMusicas.SetEstado(_estadoAtual);
            });
        }

        // Retorna as músicas como ListViewItem (usado em UI)
        private List<ListViewItem> GetListMusicas()
        {
            return InvokeAux.GetValue(listView1, lv =>
                {
                    List<ListViewItem> rt = [];
                    foreach (ListViewItem item in listView1.Items)
                    {
                        if (item.Tag == null) continue;

                        string? path = item.Tag.ToString();
                        if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;

                        string ext = Path.GetExtension(path).ToLowerInvariant();
                        if (!ExtensoesPermitidas.Contains(ext)) continue;

                        rt.Add(item);
                    }
                    return rt;
                });
        }

        // 🔁 Sobrecarga — retorna apenas os caminhos (List<string>)
        private List<string> GetListMusicasPaths()
        {
            _estadoAtual.Musicas ??= GetListMusicas();
            return (_estadoAtual.Musicas ?? [])
                .Select(i => i.Tag?.ToString())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList()!;
        }

        #endregion


        #region botoes

        private void btnRandomizar_Click(object sender, EventArgs e)
        {
            _estadoAtual.Musicas ??= GetListMusicas();
            if (_estadoAtual.Musicas == null || _estadoAtual.Musicas.Count == 0) { return; }

            // Embaralha usando seu método Fisher–Yates
            _estadoAtual.Musicas = Util.Shuffle(_estadoAtual.Musicas) ?? [];
            _estadoAtual.IndiceMusica = 0;
            _filtrarMusicas.SetEstado(_estadoAtual);
            _skipStopAnaliseMusica = true;

            SalvarEstadoDoFormulario(true);

            _skipToNext = _skipToPrevious = false;

            // Mantém as pastas no topo (opcional)
            //List<ListViewItem> pastas = listView1.Items
            //    .Cast<ListViewItem>()
            //    .Where(i => i.Tag is string p && Directory.Exists(p))
            //    .ToList();

            InvokeAux.Access(listView1, lvw =>
            {
                lvw.BeginUpdate();
                lvw.Items.Clear();

                //foreach (var pasta in pastas) lv.Items.Add((ListViewItem)pasta.Clone());
                foreach (var musica in _estadoAtual.Musicas) lvw.Items.Add((ListViewItem)musica.Clone());

                _estadoAtual.Musicas = GetListMusicas();
                lvw.EndUpdate();
            });

            _playerControl?.Stop();
            if (_playerControl == null || !_playerControl.IsValid)
            {
                playMusic();
            }
        }

        private void btnVoltar_Click(object sender, EventArgs e)
        {
            previousMusic();
        }

        private void btnPlayPause_Click(object sender, EventArgs e)
        {
            playPause();
        }

        private void btnProximo_Click(object sender, EventArgs e)
        {
            nextMusic();
        }

        #region btn aux
        private void playPause()
        {
            if (InvokeAux.GetValue(listView1, lvw => lvw.Items.Count) <= 0)
            {
                _playerControl?.Stop();
                InvokeAux.Access(btnPlayPause, btn => btn.Text = ">");
                return;
            }

            if (_playerControl == null)
            {
                playMusic();
            }
            else if (_playerControl != null)
            {
                if (_playerControl.IsPlaying)
                {
                    _playerControl.Pause();
                }
                else
                {
                    _playerControl.Resume();
                }

            }

            //Console.WriteLine($"_playerControl.IsPlaying: {_playerControl?.IsPlaying}, PlaybackStateProp: {_playerControl?.PlaybackStateProp}");
        }

        private void stop()
        {
            updateFormTitle(true);
            _skipStopAnaliseMusica = _skipPlayMusica = true;
            Player_ProgressUpdated(null, 0.0d);
            InvokeAux.Access(pictureBox1, pct => pct.Image = null);

            _playerControl?.Stop();
            _playerControl?.Dispose();
            _playerControl = null;
            //InvokeAux.Access(btnPlayPause, btn => btn.ImageIndex = (int)EImageIndex.play);
        }

        private void nextMusic()
        {
            _skipToNext = true;
            _playerControl?.Stop();

            if (_playerControl == null || !_playerControl.IsValid)
            {
                playMusic();
            }

            //if (_playerControl != null && !_playerControl.IsPlaying) { _playerControl.Play(); }
            //isplaying = true;
        }

        private void previousMusic()
        {
            _skipToPrevious = true;
            //if (_playerControl != null && !_playerControl.IsPlaying) { _playerControl.Play(); }
            _playerControl?.Stop();

            if (_playerControl == null || !_playerControl.IsValid)
            {
                playMusic();
            }
        }

        #endregion

        private void AtualizarSelecaoMusicaAtual()
        {
            InvokeAux.Access(listView1, lv =>
            {
                if (lv.Items.Count == 0 || _estadoAtual.IndiceMusica < 0 || _estadoAtual.IndiceMusica >= lv.Items.Count)
                    return;

                lv.BeginUpdate();

                // limpa seleção anterior
                lv.SelectedItems.Clear();

                // pega o item real do ListView
                var itemAtual = lv.Items[_estadoAtual.IndiceMusica];

                // seleciona e foca
                itemAtual.Selected = true;
                itemAtual.Focused = true;

                // scroll até ele
                itemAtual.EnsureVisible();

                // foca o ListView para mostrar azul
                lv.Focus();

                lv.EndUpdate();
            });
        }


        private void playMusic()
        {
            if (_skipPlayMusica) { _skipPlayMusica = false; return; }

            _estadoAtual.Musicas ??= GetListMusicas();
            if (_estadoAtual.Musicas == null || _estadoAtual.Musicas.Count == 0) return;

            if (_estadoAtual.IndiceMusica < 0) _estadoAtual.IndiceMusica = _estadoAtual.Musicas.Count - 1;
            if (_estadoAtual.IndiceMusica >= _estadoAtual.Musicas.Count) _estadoAtual.IndiceMusica = 0;

            ListViewItem itemAtual = _estadoAtual.Musicas[_estadoAtual.IndiceMusica];
            string? path = itemAtual.Tag?.ToString();
            if (string.IsNullOrEmpty(path)) return;

            _playerControl?.Dispose();
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
                _wi = new(_playerControl.AudioFileReaderProp, this, width: pictureBox1.Width);

                _wi.init(image =>
                {
                    if (image == null) { return; }
                    pictureBox1.Image = image;
                });
            }

            Console.WriteLine($"🎵 Tocando música: {_estadoAtual.IndiceMusica}, {path}");

            // Atualiza seleção visual
            AtualizarSelecaoMusicaAtual();

            SalvarEstadoDoFormulario(false);
        }

        #region eventos player
        private void updateFormTitle(bool reset = false, string status = "")
        {
            if (reset)
            {
                InvokeAux.Access(this, frm => frm.Text = "My Player");
            }
            if (_estadoAtual.IndiceMusica >= 0 && _estadoAtual.IndiceMusica < _estadoAtual.Musicas.Count)
            {
                ListViewItem itemAtual = _estadoAtual.Musicas[_estadoAtual.IndiceMusica];
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

        private void Player_EvtPlaying(object? sender, EventArgs e)
        {
            updateFormTitle();

            //InvokeAux.Access(btnPlayPause, btn => btn.Text = "||");
            InvokeAux.Access(btnPlayPause, btn => btn.ImageIndex = (int)EImageIndex.pause);
            AtualizarSelecaoMusicaAtual();

            if (_playerControl == null) return;

            InvokeAux.Access(progressBar1, pg => pg.Value = 0);
            InvokeAux.Access(trackBar1, tckbar => tckbar.Value = 0);

            TimeSpan musicDuration = _playerControl.MusicDuration;
            InvokeAux.Access(lblStatus, lbl => lbl.Text = $"00:00 | {musicDuration:mm\\:ss}");
        }

        private void Player_EvtPaused(object? sender, EventArgs e)
        {
            updateFormTitle(status: "pause");
            //InvokeAux.Access(btnPlayPause, btn => btn.Text = ">");
            InvokeAux.Access(btnPlayPause, btn => btn.ImageIndex = (int)EImageIndex.play);
        }

        private void Player_EvtResume(object? sender, EventArgs e)
        {
            updateFormTitle();
            //InvokeAux.Access(btnPlayPause, btn => btn.Text = "||");
            InvokeAux.Access(btnPlayPause, btn => btn.ImageIndex = (int)EImageIndex.pause);
            AtualizarSelecaoMusicaAtual();
        }

        private void Player_EvtStop(object? sender, EventArgs e)
        {
            updateFormTitle(true);

            //InvokeAux.Access(btnPlayPause, btn => btn.Text = ">");
            InvokeAux.Access(btnPlayPause, btn => btn.ImageIndex = (int)EImageIndex.play);

            InvokeAux.Access(lblStatus, lbl => lbl.Text = "Parado");
            InvokeAux.Access(progressBar1, pg => pg.Value = 0);
            InvokeAux.Access(trackBar1, tckbar => tckbar.Value = 0);
        }

        private void Player_EvtMusicEnded(object? sender, EventArgs e)
        {
            updateFormTitle(true);
            analiseIndiceMusica();
            playMusic();
        }

        private void Player_ProgressUpdated(object? sender, double percent)
        {
            if (_playerControl == null) return;

            // Use InvokeAux para definir os valores na thread da UI
            if (percent >= 0 && percent <= 100)
            {
                InvokeAux.Access(progressBar1, pg => pg.Value = (int)percent);
                InvokeAux.Access(trackBar1, tckbar => tckbar.Value = (int)percent);
            }

            TimeSpan currentTime = _playerControl.CurrentTime;
            TimeSpan musicDuration = _playerControl.MusicDuration;
            InvokeAux.Access(lblStatus, lbl => lbl.Text = $"{currentTime:mm\\:ss} | {musicDuration:mm\\:ss}");

            waveImageUpdate();

            //Console.WriteLine($"percent: {percent}");
        }

        private void waveImageUpdate()
        {
            //InvokeRequiredUtil.InvokeIfRequired(trackBar, () =>{try { trackBar.Value = Convert.ToInt32(Math.Min(audioFile.Position, audioFile.Length)); } catch { }});

            //var audioFile = _playerControl.AudioFileReaderProp;

            //double ms = audioFile.Position * 1000.0 / audioFile.WaveFormat.BitsPerSample / audioFile.WaveFormat.Channels * 8 / audioFile.WaveFormat.SampleRate;
            //double maxMs = audioFile.Length * 1000.0 / audioFile.WaveFormat.BitsPerSample / audioFile.WaveFormat.Channels * 8 / audioFile.WaveFormat.SampleRate;
            //musicTime($"{TimeSpan.FromMilliseconds(ms).ToString(@"hh\:mm\:ss")} - {TimeSpan.FromMilliseconds(maxMs).ToString(@"hh\:mm\:ss")}");

            #region pictureBox1
            Image? image = _wi.getUpdateImage();
            if (image == null) { return; }

            InvokeAux.Access(pictureBox1, pct => pct.Image = image);
            #endregion
        }
        #endregion

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (_playerControl == null) return;
            _playerControl.SetPercent(InvokeAux.GetValue(trackBar1, tckbar => tckbar.Value));
        }

        private void analiseIndiceMusica()
        {
            if (_skipStopAnaliseMusica) { _skipStopAnaliseMusica = false; return; }
            if (listViewDblClick) { listViewDblClick = false; return; }
            _estadoAtual.Musicas ??= GetListMusicas();
            if (_estadoAtual.Musicas == null || _estadoAtual.Musicas.Count == 0) return;

            bool avancar = (_skipToNext && !_skipToPrevious) || (!_skipToNext && !_skipToPrevious);
            _skipToNext = _skipToPrevious = false;
            //Console.WriteLine($"_skipToNext: {_skipToNext}, _skipToPrevious: {_skipToPrevious}, avancar: {avancar}");

            _estadoAtual.IndiceMusica += avancar ? 1 : -1;
            if (_estadoAtual.IndiceMusica < 0) _estadoAtual.IndiceMusica = _estadoAtual.Musicas.Count - 1;
            if (_estadoAtual.IndiceMusica >= _estadoAtual.Musicas.Count) _estadoAtual.IndiceMusica = 0;

            AtualizarSelecaoMusicaAtual();
        }

        #endregion

        #region txtfiltro
        private void txtFiltro_TextChanged(object sender, EventArgs e)
        {
            _filtrarMusicas.Filtrar(txtFiltro.Text, listView1);
        }

        private void txtFiltro_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (listView1.Items.Count <= 0) { return; }
                playPause();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                txtFiltro.Clear();
                _filtrarMusicas.Filtrar(string.Empty, listView1);
                //filtrarMusicasDataGrid?.filtrarMusicas(txtFiltro.Text);
            }
            else if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Left)
            {
                previousMusic();
            }
            else if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Right)
            {
                nextMusic();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                playMusic();
            }
        }
        #endregion

    }
}