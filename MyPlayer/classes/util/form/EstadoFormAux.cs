using MyPlayer.classes.controleestados;
using MyPlayer.classes.filtrarmusicas;
using MyPlayer.classes.util.threads;
using MyPlayer.classes.util.treeview;

namespace MyPlayer.classes.util.form
{
    internal static class EstadoFormAux
    {
        public static void SalvarEstadoDoFormulario(
            ref TextBox txtFiltro, 
            ref FiltrarMusicas filtrarMusicas, 
            ref ListView listView, 
            ref FormularioEstado estadoAtual,
            bool clearFilter = true)
        {
            if (clearFilter)
            {
                InvokeAux.Access(txtFiltro, txt => txt.Text = string.Empty);
                filtrarMusicas.Filtrar(string.Empty, listView);
            }

            estadoAtual.ListVewStateProp ??= new();
            estadoAtual.ListVewStateProp.View = (int)listView.View;
            estadoAtual.ListVewStateProp.ColumnWidths = [];
            foreach (ColumnHeader col in listView.Columns)
                estadoAtual.ListVewStateProp.ColumnWidths.Add(col.Width);

            ControleEstados.SalvarEstado(estadoAtual);
        }

        public static bool CarregarEstadoDoFormulario(
            ref FormularioEstado estadoAtual, ref FiltrarMusicas filtrarMusicas, ref ListView listView, ref ImageList imageList,
            Action AtualizarSelecaoMusicaAtual, ref TextBox txtPathMusicas, ref TreeView treeView
        )
        {
            var aux = ControleEstados.RecuperarEstado();
            if (aux == null) { return false; }
            estadoAtual = aux;
            filtrarMusicas.SetEstado(estadoAtual);

            //_indiceMusica = _estadoAtual.IndiceMusica;

            estadoAtual.ListVewStateProp ??= new()
            {
                View = (int)View.Details,
                ColumnWidths = [100, 100, 150]
            };

            var estadoAtualAux = estadoAtual;
            var imageListAux = imageList;

            InvokeAux.Access(listView, lvw =>
            {
                lvw.BeginUpdate();
                lvw.Items.Clear();
                lvw.SmallImageList = imageListAux;

                // USA O MÉTODO DRY DE CONFIGURAÇÃO
                ListViewAux.ConfigurarColunasPadrao(lvw, estadoAtualAux.ListVewStateProp?.ColumnWidths);

                // USA O MÉTODO DRY DE MAPEAMENTO
                foreach (var musica in estadoAtualAux.Musicas)
                {
                    lvw.Items.Add(ListViewAux.ToListViewItem(musica));
                }

                lvw.EndUpdate();
            });

            AtualizarSelecaoMusicaAtual();

            if (!string.IsNullOrEmpty(estadoAtual.MusicPath))
            {
                InvokeAux.Access(txtPathMusicas, txt => txt.Text = estadoAtualAux.MusicPath);
                TreeViewUtil.PreencherTreeView(treeView, estadoAtual.MusicPath);
            }

            return true;
        }

    }
}