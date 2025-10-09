#define DEBUG

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
            return GetListMusicas()
                .Select(i => i.Tag?.ToString())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList()!;
        }


        #endregion


        #region botoes
        private void button1_Click(object sender, EventArgs e)
        {
            foreach (string m in GetListMusicasPaths())
            {
                Console.WriteLine(m);
            }
        }


        private void btnRandomizar_Click(object sender, EventArgs e)
        {
            List<ListViewItem> musicas = GetListMusicas();
            if (musicas.Count == 0)
            {
                return;
            }

            // Embaralha usando seu método Fisher–Yates
            Util.Shuffle(musicas);

            // Mantém as pastas no topo (opcional)
            //List<ListViewItem> pastas = listView1.Items
            //    .Cast<ListViewItem>()
            //    .Where(i => i.Tag is string p && Directory.Exists(p))
            //    .ToList();

            listView1.BeginUpdate();
            listView1.Items.Clear();

            //foreach (var pasta in pastas) listView1.Items.Add((ListViewItem)pasta.Clone());
            foreach (var musica in musicas) listView1.Items.Add((ListViewItem)musica.Clone());

            listView1.EndUpdate();
        }

        // TODO: temporário
        private bool isplaying = false;
        private int indiceMusica = 0;
        private Task? playerTask = null;
        private bool skipToNext = false;


        private void btnVoltar_Click(object sender, EventArgs e)
        {
            if (listView1.Items.Count == 0) return;

            // Retrocede o índice
            indiceMusica--;
            if (indiceMusica < 0)
                indiceMusica = listView1.Items.Count - 1;

            if (isplaying)
            {
                // Se estiver tocando, sinaliza para o loop ir imediatamente para a música anterior
                skipToNext = true; // reutiliza o skip flag
            }

            // Atualiza seleção na UI
            AtualizarSelecaoMusicaAtual();
        }




        private void btnPlayPause_Click(object sender, EventArgs e)
        {
            if (listView1.Items.Count <= 0) return;

            isplaying = !isplaying;
            updateBtnPlayPause();

            if (isplaying && (playerTask == null || playerTask.IsCompleted))
            {
                playerTask = playMusic();
            }
        }

        private void btnProximo_Click(object sender, EventArgs e)
        {
            if (!isplaying)
            {
                // se o player estiver pausado, apenas muda a seleção
                indiceMusica++;
                var musicas = GetListMusicas();
                if (indiceMusica >= musicas.Count)
                    indiceMusica = 0;
                AtualizarSelecaoMusicaAtual();
                return;
            }

            // se estiver tocando, apenas sinaliza o salto
            skipToNext = true;
        }


        private void AtualizarSelecaoMusicaAtual()
        {
            List<ListViewItem> musicas = GetListMusicas();
            if (musicas.Count == 0 || indiceMusica < 0 || indiceMusica >= musicas.Count)
                return;

            var itemAtual = musicas[indiceMusica];

            InvokeAux.SetValue(listView1, c => {
                ((ListView)c).SelectedItems.Clear();
                itemAtual.Selected = true;
                itemAtual.EnsureVisible();
            });
        }



        private void updateBtnPlayPause() {
            if (listView1.Items.Count <= 0)
            {
                isplaying = false;
                btnPlayPause.Text = ">";
                return;
            }
            btnPlayPause.Text = isplaying ? "||" : ">";
        }

        private async Task playMusic()
        {
            List<ListViewItem> musicas = GetListMusicas();
            if (musicas.Count == 0) return;

            isplaying = true;

            while (isplaying)
            {
                if (indiceMusica >= musicas.Count)
                    indiceMusica = 0;

                ListViewItem itemAtual = musicas[indiceMusica];
                string? path = itemAtual.Tag?.ToString();

                Console.WriteLine($"🎵 Tocando música: {indiceMusica}, {path}");

                // Atualiza seleção visual
                AtualizarSelecaoMusicaAtual();

                // Simula "reprodução" com checagem frequente de pausa/skip
                for (int i = 0; i < 10 && isplaying && !skipToNext; i++)
                    await Task.Delay(100);

                if (!isplaying)
                    break;

                if (skipToNext)
                {
                    Console.WriteLine($"⏭ Pulando música {indiceMusica}...");
                    skipToNext = false;
                }
                else
                {
                    Console.WriteLine($"⏹ Música {indiceMusica} terminou...");
                }

                indiceMusica++;
            }

            Console.WriteLine("▶️ Reprodução pausada ou finalizada.");
        }



        #endregion


    }
}