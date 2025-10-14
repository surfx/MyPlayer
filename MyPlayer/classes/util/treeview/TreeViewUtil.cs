using MyPlayer.classes.util.threads;

namespace MyPlayer.classes.util.treeview
{
    internal class TreeViewUtil
    {
        public static void PreencherTreeView(TreeView treeView, string path)
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
                }

                tv.ExpandAll(); // Expande todos os nós
            });
        }

        private static void AdicionarPastasRecursivamente(TreeNode node, string path)
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
    }
}
