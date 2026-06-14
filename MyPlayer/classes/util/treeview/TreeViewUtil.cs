using Avalonia.Controls;
using MyPlayer.classes.util.threads;

namespace MyPlayer.classes.util.treeview;

internal class TreeViewUtil
{
    public static void PreencherTreeView(TreeView treeView, string path)
    {
        InvokeAux.Access(treeView, tv =>
        {
            tv.Items.Clear();

            string[] partes = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            string acumulador = path.StartsWith(Path.DirectorySeparatorChar.ToString())
                ? Path.DirectorySeparatorChar.ToString()
                : partes[0] + Path.DirectorySeparatorChar;

            TreeViewItem? currentNode = null;

            for (int i = 0; i < partes.Length; i++)
            {
                string nome = partes[i];
                var node = new TreeViewItem { Header = nome, Tag = acumulador };

                if (currentNode == null)
                {
                    tv.Items.Add(node);
                }
                else
                {
                    currentNode.Items.Add(node);
                }

                currentNode = node;

                if (i < partes.Length - 1)
                {
                    acumulador = Path.Combine(acumulador, partes[i + 1]);
                }
            }

            if (Directory.Exists(path) && currentNode != null)
            {
                AdicionarPastasRecursivamente(currentNode, path);
            }

            foreach (var item in tv.Items)
            {
                if (item is TreeViewItem tvi)
                    tvi.IsExpanded = true;
            }
        });
    }

    private static void AdicionarPastasRecursivamente(TreeViewItem node, string path)
    {
        try
        {
            string[] subPastas = Directory.GetDirectories(path);

            foreach (string pasta in subPastas)
            {
                var subNode = new TreeViewItem
                {
                    Header = Path.GetFileName(pasta),
                    Tag = pasta
                };
                node.Items.Add(subNode);

                AdicionarPastasRecursivamente(subNode, pasta);
            }

            foreach (var item in node.Items)
            {
                if (item is TreeViewItem tvi)
                    tvi.IsExpanded = true;
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erro ao ler pastas");
        }
    }
}
