#define DEBUG

using MyPlayer.classes.controleestados;
using MyPlayer.classes.player;
using MyPlayer.classes.util;
using MyPlayer.classes.util.threads;

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

        private List<ListViewItem>? _musicas = null;
        private readonly ControleEstados _controleEstados = new();


        private int _indiceMusica = 0;
        private bool _skipToNext = false;
        private bool _skipToPrevious = false;
        private PlayerControl? _playerControl;
        private bool listViewDblClick = false;

        public frmMyPlayer()
        {
            InitializeComponent();
        }

        private void frmMyPlayer_Load(object sender, EventArgs e)
        {
#if DEBUG
            AllocConsole();
#endif

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

            var estado = _controleEstados.RecuperarEstado(listView1);
            if (estado != null)
            {
                _indiceMusica = estado.IndiceMusica;

                InvokeAux.Access(listView1, lvw =>
                {
                    lvw.BeginUpdate();
                    lvw.Items.Clear();
                    lvw.View = View.Details; // importante restaurar a View
                    lvw.SmallImageList = imageList1;

                    // Restaurar colunas se quiser
                    lvw.Columns.Clear();
                    lvw.Columns.Add("Nome", 150);
                    lvw.Columns.Add("Tamanho (KB)", 100);
                    lvw.Columns.Add("Data de Modificação", 150);

                    _musicas = new List<ListViewItem>();
                    foreach (var sItem in estado.Musicas)
                    {
                        ListViewItem item = new(sItem.Text)
                        {
                            Tag = sItem.Tag,
                            ImageIndex = sItem.ImageIndex
                        };
                        foreach (var sub in sItem.SubItems)
                            item.SubItems.Add((ListViewItem.ListViewSubItem)sub);

                        lvw.Items.Add(item);
                        _musicas.Add(item); // atualiza a lista interna
                    }

                    lvw.EndUpdate();
                });

                AtualizarSelecaoMusicaAtual();

                if (!string.IsNullOrEmpty(estado.MusicPath))
                    InvokeAux.Access(txtPathMusicas, txt => txt.Text = estado.MusicPath);

                return;
            }

            string musicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            musicPath = musicPath.EndsWith(@"\") ? musicPath : musicPath + @"\";
            InvokeAux.Access(txtPathMusicas, txt => txt.Text = musicPath);
            PreencherTreeView(treeView1, musicPath);
        }

        private void frmMyPlayer_FormClosing(object sender, FormClosingEventArgs e)
        {
            _musicas ??= GetListMusicas();

            var estado = new FormularioEstado
            {
                MusicPath = InvokeAux.GetValue(txtPathMusicas, txt => txt.Text),
                IndiceMusica = _indiceMusica,
                Musicas = _musicas
            };

            _controleEstados.SalvarEstado(estado, listView1);
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
                        PreencherTreeView(treeView1, txt.Text);
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
        public void PreencherTreeView(TreeView treeView, string path)
        {
            InvokeAux.Access(treeView, tv =>
            {
                tv.Nodes.Clear(); // Limpa a árvore

                // Obter todos os níveis acima do caminho fornecido
                string[] partes = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
                string acumulador = path.StartsWith(Path.DirectorySeparatorChar.ToString()) ? Path.DirectorySeparatorChar.ToString() : partes[0] + @"\";

                TreeNode? currentNode = null;

                for (int i = 0; i < partes.Length; i++)
                {
                    string nome = partes[i];
                    TreeNode node = new(nome) { Tag = acumulador };

                    if (currentNode == null)
                    {
                        tv.Nodes.Add(node);
                    }
                    else
                    {
                        currentNode.Nodes.Add(node);
                    }

                    currentNode = node;

                    if (i < partes.Length - 1)
                    {
                        acumulador = Path.Combine(acumulador, partes[i + 1]);
                    }
                }

                // Agora currentNode é o nó raiz da pasta fornecida
                if (Directory.Exists(path) && currentNode != null)
                {
                    AdicionarPastasRecursivamente(currentNode, path);

                    ListarArquivos(path);
                }

                tv.ExpandAll(); // Expande todos os nós
            });
        }

        private void AdicionarPastasRecursivamente(TreeNode node, string path)
        {
            try
            {
                string[] subPastas = Directory.GetDirectories(path);

                foreach (string pasta in subPastas)
                {
                    TreeNode subNode = new(Path.GetFileName(pasta))
                    {
                        Tag = pasta
                    };
                    node.Nodes.Add(subNode);

                    // Chamada recursiva para subpastas
                    AdicionarPastasRecursivamente(subNode, pasta);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Ignora pastas sem permissão
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao ler pastas: " + ex.Message);
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string? caminho = e?.Node?.Tag as string;

            if (!string.IsNullOrEmpty(caminho) && Directory.Exists(caminho))
            {
                ListarArquivos(caminho);
            }
        }
        #endregion

        #region listview

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (InvokeAux.GetValue(listView1, lvw => listView1.SelectedItems.Count) == 0) return;

            listViewDblClick = true;

            // Atualiza o índice atual
            _indiceMusica = InvokeAux.GetValue(listView1, lvw => lvw.SelectedItems[0].Index);
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
                            ImageIndex = 2, // ícone de arquivo
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
                _musicas = GetListMusicas();
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
            _musicas ??= GetListMusicas();
            return (_musicas ?? [])
                .Select(i => i.Tag?.ToString())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList()!;
        }

        #endregion


        #region botoes

        private void btnRandomizar_Click(object sender, EventArgs e)
        {
            _musicas ??= GetListMusicas();
            if (_musicas == null || _musicas.Count == 0) { return; }

            // Embaralha usando seu método Fisher–Yates
            Util.Shuffle(_musicas);
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
                foreach (var musica in _musicas) lvw.Items.Add((ListViewItem)musica.Clone());

                _musicas = GetListMusicas();
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
            _skipToPrevious = true;
            //if (_playerControl != null && !_playerControl.IsPlaying) { _playerControl.Play(); }
            _playerControl?.Stop();

            if (_playerControl == null || !_playerControl.IsValid)
            {
                playMusic();
            }
        }

        private void btnPlayPause_Click(object sender, EventArgs e)
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

        private void btnProximo_Click(object sender, EventArgs e)
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


        private void AtualizarSelecaoMusicaAtual()
        {
            _musicas ??= GetListMusicas();
            if (_musicas == null || _musicas.Count == 0 || _indiceMusica < 0 || _indiceMusica >= _musicas.Count)
                return;

            var itemAtual = _musicas[_indiceMusica];

            InvokeAux.Access(listView1, lv =>
            {
                lv.SelectedItems.Clear();
                itemAtual.Selected = true;
                itemAtual.EnsureVisible();
            });
        }


        private void playMusic()
        {
            _musicas ??= GetListMusicas();
            if (_musicas == null || _musicas.Count == 0) return;

            if (_indiceMusica < 0) _indiceMusica = _musicas.Count - 1;
            if (_indiceMusica >= _musicas.Count) _indiceMusica = 0;

            ListViewItem itemAtual = _musicas[_indiceMusica];
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


            Console.WriteLine($"🎵 Tocando música: {_indiceMusica}, {path}");

            // Atualiza seleção visual
            AtualizarSelecaoMusicaAtual();
        }


        private void Player_EvtPlaying(object? sender, EventArgs e)
        {
            InvokeAux.Access(btnPlayPause, btn => btn.Text = "||");
            AtualizarSelecaoMusicaAtual();

            if (_playerControl == null) return;

            InvokeAux.Access(progressBar1, pg => pg.Value = 0);
            InvokeAux.Access(trackBar1, tckbar => tckbar.Value = 0);

            TimeSpan musicDuration = _playerControl.MusicDuration;
            InvokeAux.Access(lblStatus, lbl => lbl.Text = $"00:00 | {musicDuration:mm\\:ss}");
        }

        private void Player_EvtPaused(object? sender, EventArgs e)
        {
            InvokeAux.Access(btnPlayPause, btn => btn.Text = ">");
        }

        private void Player_EvtResume(object? sender, EventArgs e)
        {
            InvokeAux.Access(btnPlayPause, btn => btn.Text = "||");
            AtualizarSelecaoMusicaAtual();
        }

        private void Player_EvtStop(object? sender, EventArgs e)
        {
            InvokeAux.Access(btnPlayPause, btn => btn.Text = ">");

            InvokeAux.Access(lblStatus, lbl => lbl.Text = "Parado");
            InvokeAux.Access(progressBar1, pg => pg.Value = 0);
            InvokeAux.Access(trackBar1, tckbar => tckbar.Value = 0);
        }

        private void Player_EvtMusicEnded(object? sender, EventArgs e)
        {
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

            //Console.WriteLine($"percent: {percent}");
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (_playerControl == null) return;
            _playerControl.SetPercent(InvokeAux.GetValue(trackBar1, tckbar => tckbar.Value));
        }

        private void analiseIndiceMusica()
        {
            if (listViewDblClick) { listViewDblClick = false; return; }
            _musicas ??= GetListMusicas();
            if (_musicas == null || _musicas.Count == 0) return;

            bool avancar = (_skipToNext && !_skipToPrevious) || (!_skipToNext && !_skipToPrevious);
            _skipToNext = _skipToPrevious = false;
            //Console.WriteLine($"_skipToNext: {_skipToNext}, _skipToPrevious: {_skipToPrevious}, avancar: {avancar}");

            _indiceMusica += avancar ? 1 : -1;
            if (_indiceMusica < 0) _indiceMusica = _musicas.Count - 1;
            if (_indiceMusica >= _musicas.Count) _indiceMusica = 0;

            AtualizarSelecaoMusicaAtual();
        }

        #endregion




    }
}