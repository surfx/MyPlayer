#define DEBUG

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
            const int maxFileStr = 50;

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

            //listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }
        #endregion

        private List<string> GetListMusicas() {
            List<string> rt = [];
            foreach (ListViewItem item in listView1.Items)
            {
                if (item.Tag == null) continue;

                string? path = item?.Tag?.ToString();
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) { continue; }

                // Verifica se é arquivo e se a extensão é permitida
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (!ExtensoesPermitidas.Contains(ext)) { continue; }
                rt.Add(path);
            }
            return rt;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (string m in GetListMusicas())
            {
                Console.WriteLine(m);
            }
        }

    }
}