using MyPlayer.classes.player;
using MyPlayer.classes.util.threads;

namespace MyPlayer.classes.util.form
{
    internal class ContextMenuStripAux
    {
        private ListView _listView1;
        private ContextMenuStrip _contextMenuStrip;
        private PlayerControl? _playerControl;

        public ContextMenuStripAux(ref ListView listView, ref ContextMenuStrip contextMenuStrip, PlayerControl? playerControl) { 
            _listView1 = listView;
            _contextMenuStrip = contextMenuStrip;
            _playerControl = playerControl;
        }

        public void UpdateContextMenuStrip()
        {
            var abrirPasta = new ToolStripMenuItem("Abrir pasta onde está o arquivo");
            var copiarCaminho = new ToolStripMenuItem("Copiar caminho do arquivo");
            var deletarArquivo = new ToolStripMenuItem("Deletar arquivo");

            abrirPasta.Click += AbrirPasta_Click;
            copiarCaminho.Click += CopiarCaminho_Click;
            deletarArquivo.Click += DeletarArquivo_Click;

            _contextMenuStrip.Items.AddRange([
                abrirPasta,
                copiarCaminho,
                deletarArquivo
            ]);

        }

        private void AbrirPasta_Click(object? sender, EventArgs e)
        {
            if (InvokeAux.GetValue(_listView1, lvw => lvw.SelectedItems.Count) == 0) return;
            string? caminho = InvokeAux.GetValue(_listView1, lvw => lvw.SelectedItems[0].Tag?.ToString());

            if (string.IsNullOrEmpty(caminho)) return;

            if (File.Exists(caminho))
            {
                string argumento = $"/select,\"{caminho}\"";
                System.Diagnostics.Process.Start("explorer.exe", argumento);
            }
            else if (Directory.Exists(caminho))
            {
                System.Diagnostics.Process.Start("explorer.exe", $"\"{caminho}\"");
            }
        }

        private void CopiarCaminho_Click(object? sender, EventArgs e)
        {
            if (InvokeAux.GetValue(_listView1, lvw => lvw.SelectedItems.Count) == 0) return;
            string? caminho = InvokeAux.GetValue(_listView1, lvw => lvw.SelectedItems[0].Tag?.ToString());
            if (!string.IsNullOrEmpty(caminho))
            {
                Clipboard.SetText(caminho);
            }
        }

        private void DeletarArquivo_Click(object? sender, EventArgs e)
        {
            if (InvokeAux.GetValue(_listView1, lvw => lvw.SelectedItems.Count) == 0) return;
            string? caminho = InvokeAux.GetValue(_listView1, lvw => lvw.SelectedItems[0].Tag?.ToString());
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
                    InvokeAux.Access(_listView1, lvw => lvw.Items.Remove(_listView1.SelectedItems[0]));
                    MessageBox.Show("Arquivo deletado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao deletar o arquivo:\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

    }
}
