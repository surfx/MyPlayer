#define DEBUG

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

        private int _indiceMusica = 0;
        private Task? _playerTask = null;
        private bool _skipToNext = false;
        private bool _skipToPrevious = false;
        private PlayerControl? _playerControl;
        //private bool isplaying = false;

        public frmMyPlayer()
        {
            InitializeComponent();
        }

        private void frmMyPlayer_Load(object sender, EventArgs e)
        {
#if DEBUG
            AllocConsole();
#endif

            string musicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            txtPathMusicas.Text = musicPath.EndsWith(@"\") ? musicPath : musicPath + @"\";
            PreencherTreeView(treeView1, txtPathMusicas.Text);

            lblStatus.Text = string.Empty;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;

            trackBar1.Minimum = 0;
            trackBar1.Maximum = 100;
            trackBar1.TickStyle = TickStyle.None;
        }

        private void btnOpenFolderMusics_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new())
            {
                if (!string.IsNullOrEmpty(txtPathMusicas.Text)) { folderDialog.SelectedPath = txtPathMusicas.Text; }
                DialogResult result = folderDialog.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
                {
                    txtPathMusicas.Text = folderDialog.SelectedPath.EndsWith(@"\") ? folderDialog.SelectedPath : folderDialog.SelectedPath + @"\";
                    PreencherTreeView(treeView1, txtPathMusicas.Text);
                }
            }
        }

        #region treeview
        public void PreencherTreeView(TreeView treeView, string path)
        {
            treeView.Nodes.Clear(); // Limpa a árvore

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
                    treeView.Nodes.Add(node);
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
            }

            treeView.ExpandAll(); // Expande todos os nós
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
            if (listView1.SelectedItems.Count == 0)
                return;

            // Atualiza o índice atual
            _indiceMusica = listView1.SelectedItems[0].Index;

            // Obtém o caminho do arquivo (armazenado no Tag)
            string? path = listView1.SelectedItems[0].Tag as string;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                MessageBox.Show("Arquivo de música inválido.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Libera o player anterior (caso exista)
            _playerControl?.Dispose();

            // Cria e conecta o novo player
            _playerControl = new PlayerControl(path);
            _playerControl.ProgressUpdated += Player_ProgressUpdated;
            _playerControl.EvtPlaying += Player_EvtPlaying;
            _playerControl.EvtStop += Player_EvtStop;

            // Inicia a reprodução
            _playerControl.Play();

            // Atualiza UI
            lblStatus.Text = $"Tocando: {Path.GetFileName(path)}";
            AtualizarSelecaoMusicaAtual();
        }

        private void ListarArquivos(string path)
        {
            const int maxFileStr = 100;

            listView1.Items.Clear();

            // Configuração inicial do ListView
            listView1.View = View.Details;
            listView1.SmallImageList = imageList1;
            listView1.Columns.Clear();
            listView1.Columns.Add("Nome", 150);
            listView1.Columns.Add("Tamanho (KB)", 100);
            listView1.Columns.Add("Data de Modificação", 150);

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
                    listView1.Items.Add(item);
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
                    listView1.Items.Add(item);
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Sem permissão para acessar alguns arquivos nesta pasta.");
            }

            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            // atualiza a lista de musicas
            _musicas = GetListMusicas();
        }

        // Retorna as músicas como ListViewItem (usado em UI)
        private List<ListViewItem> GetListMusicas()
        {
            List<ListViewItem> musicas = new();

            foreach (ListViewItem item in listView1.Items)
            {
                if (item.Tag == null) continue;

                string? path = item.Tag.ToString();
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;

                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (!ExtensoesPermitidas.Contains(ext)) continue;

                musicas.Add(item);
            }

            return musicas;
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

            // Mantém as pastas no topo (opcional)
            //List<ListViewItem> pastas = listView1.Items
            //    .Cast<ListViewItem>()
            //    .Where(i => i.Tag is string p && Directory.Exists(p))
            //    .ToList();

            listView1.BeginUpdate();
            listView1.Items.Clear();

            //foreach (var pasta in pastas) listView1.Items.Add((ListViewItem)pasta.Clone());
            foreach (var musica in _musicas) listView1.Items.Add((ListViewItem)musica.Clone());
            _musicas = GetListMusicas(); // atualiza

            listView1.EndUpdate();
        }

        private void btnVoltar_Click(object sender, EventArgs e)
        {
            _skipToPrevious = true;
            if (_playerControl != null && !_playerControl.IsPlaying) { _playerControl.Play(); }
            //isplaying = true;
        }

        private void btnPlayPause_Click(object sender, EventArgs e)
        {
            if (listView1.Items.Count <= 0) return;

            if (_playerControl != null)
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
            //isplaying = !isplaying;
            updateBtnPlayPause();

            //if (isplaying && (_playerTask == null || _playerTask.IsCompleted))
            //if ((_playerControl != null && _playerControl.IsPlaying) && (_playerTask == null || _playerTask.IsCompleted))
            //{
            //    _playerTask = playMusic();
            //}
            _playerTask = playMusic();
        }
        private void updateBtnPlayPause()
        {
            if (listView1.Items.Count <= 0)
            {
                if (_playerControl != null) { _playerControl.Pause(); }
                //isplaying = false;
                btnPlayPause.Text = ">";
                return;
            }
            //btnPlayPause.Text = isplaying ? "||" : ">";
            btnPlayPause.Text = (_playerControl != null && _playerControl.IsPlaying) ? "||" : ">";
        }


        private void btnProximo_Click(object sender, EventArgs e)
        {
            _skipToNext = true;
            if (_playerControl != null && !_playerControl.IsPlaying) { _playerControl.Play(); }
            //isplaying = true;
        }


        private void AtualizarSelecaoMusicaAtual()
        {
            _musicas ??= GetListMusicas();
            if (_musicas == null || _musicas.Count == 0 || _indiceMusica < 0 || _indiceMusica >= _musicas.Count)
                return;

            var itemAtual = _musicas[_indiceMusica];

            InvokeAux.SetValue(listView1, c =>
            {
                ((ListView)c).SelectedItems.Clear();
                itemAtual.Selected = true;
                itemAtual.EnsureVisible();
            });
        }


        private async Task playMusic()
        {
            _musicas ??= GetListMusicas();
            if (_musicas == null || _musicas.Count == 0) return;

            //isplaying = true;

            if (_indiceMusica >= _musicas.Count)
                _indiceMusica = 0;

            ListViewItem itemAtual = _musicas[_indiceMusica];
            string? path = itemAtual.Tag?.ToString();
            if (string.IsNullOrEmpty(path)) return;

            _playerControl?.Dispose();
            _playerControl = new PlayerControl(path);
            _playerControl.ProgressUpdated += Player_ProgressUpdated;
            _playerControl.EvtPlaying += Player_EvtPlaying;
            _playerControl.EvtStop += Player_EvtStop;
            _playerControl.Play();

            Console.WriteLine($"🎵 Tocando música: {_indiceMusica}, {path}");

            // Atualiza seleção visual
            AtualizarSelecaoMusicaAtual();

            // Simula "reprodução" com checagem frequente de pausa/skip
            // for (int i = 0; i < 10 && isplaying && !_skipToNext && !_skipToPrevious; i++)
            //for (int i = 0; i < 10 && (_playerControl != null && _playerControl.IsPlaying) && !_skipToNext && !_skipToPrevious; i++)
            await Util.WaitWhileAsync(() =>
                _playerControl != null &&
                _playerControl.IsPlaying &&
                !_skipToNext &&
                !_skipToPrevious, 100);

            //if (!isplaying)
            if (_playerControl == null || !_playerControl.IsPlaying)
            {
                Console.WriteLine("▶️ Reprodução pausada ou finalizada.");
                return;
            }

            if (_skipToNext)
            {
                analiseIndiceMusica();
                Console.WriteLine($"⏭ Pulando música {_indiceMusica}...");
                _skipToNext = false;
            }
            else if (_skipToPrevious)
            {
                analiseIndiceMusica(false);
                Console.WriteLine($"⏭ Voltando música {_indiceMusica}...");
                _skipToPrevious = false;
            }
            else
            {
                Console.WriteLine($"⏹ Música {_indiceMusica} terminou...");
                analiseIndiceMusica();
            }

            //if (isplaying)
            if (_playerControl != null && _playerControl.IsPlaying)
            {
                _playerControl?.Stop();
                _playerTask = playMusic();
                return;
            }

            _playerControl?.Stop();
        }


        private void Player_EvtPlaying(object? sender, EventArgs e)
        {
            if (_playerControl == null) return;

            //_totalTime = _playerControl.MusicDuration; // (veja abaixo)
            progressBar1.Value = 0;
            trackBar1.Value = 0;

            lblStatus.Text = $"00:00 | {_playerControl.MusicDuration:mm\\:ss}";
        }

        private void Player_EvtStop(object? sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Player_EvtStop(sender, e)));
                return;
            }

            lblStatus.Text = "Parado";
            progressBar1.Value = 0;
            trackBar1.Value = 0;
        }

        private void Player_ProgressUpdated(object? sender, double percent)
        {
            if (_playerControl == null) return;

            // A atualização da UI precisa ocorrer na thread principal
            if (InvokeRequired)
            {
                Invoke(new Action(() => Player_ProgressUpdated(sender, percent)));
                return;
            }

            if (percent >= 0 && percent <= 100)
            {
                progressBar1.Value = (int)percent;
                trackBar1.Value = (int)percent;
            }

            var current = _playerControl.CurrentTime;
            lblStatus.Text = $"{current:mm\\:ss} | {_playerControl.MusicDuration:mm\\:ss}";
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (_playerControl == null) return;
            double percent = trackBar1.Value;
            _playerControl.SetPercent(percent);
        }

        private void analiseIndiceMusica(bool avancar = true)
        {
            _indiceMusica = avancar ? _indiceMusica + 1 : _indiceMusica - 1;
            _musicas ??= GetListMusicas();
            if (_musicas == null || _musicas.Count == 0) { return; }
            if (_indiceMusica < 0)
                _indiceMusica = _musicas.Count - 1;
            if (_indiceMusica >= _musicas.Count)
                _indiceMusica = 0;

            AtualizarSelecaoMusicaAtual();
        }

        #endregion




    }
}