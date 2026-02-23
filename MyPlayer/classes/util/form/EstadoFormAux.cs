using MyPlayer.classes.controleestados;
using MyPlayer.classes.filtrarmusicas;
using MyPlayer.classes.util.threads;
using MyPlayer.classes.util.treeview;
using Serilog;

namespace MyPlayer.classes.util.form
{
    internal static class EstadoFormAux
    {
        private static System.Threading.Timer? _saveTimer;
        private static readonly object _saveLock = new();

        // ✅ Variáveis para captura no debounce
        private static TextBox? _txtFiltroCapture;
        private static FiltrarMusicas? _filtrarMusicasCapture;
        private static ListView? _listViewCapture;
        private static FormularioEstado? _estadoAtualCapture;
        private static bool _clearFilterCapture;

        /// <summary>
        /// ✅ Salva com debouncing (aguarda 2 segundos de inatividade)
        /// </summary>
        public static void SalvarEstadoDoFormularioDebounced(
            ref TextBox txtFiltro,
            ref FiltrarMusicas filtrarMusicas,
            ref ListView listView,
            ref FormularioEstado estadoAtual,
            bool clearFilter = true,
            int delayMs = 2000)
        {
            // ✅ Captura variáveis ANTES do lambda
            _txtFiltroCapture = txtFiltro;
            _filtrarMusicasCapture = filtrarMusicas;
            _listViewCapture = listView;
            _estadoAtualCapture = estadoAtual;
            _clearFilterCapture = clearFilter;

            lock (_saveLock)
            {
                _saveTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _saveTimer?.Dispose();

                _saveTimer = new System.Threading.Timer(_ =>
                {
                    // ✅ Usa variáveis capturadas
                    if (_txtFiltroCapture != null && 
                        _filtrarMusicasCapture != null && 
                        _listViewCapture != null && 
                        _estadoAtualCapture != null)
                    {
                        SalvarEstadoInterno(
                            _txtFiltroCapture, 
                            _filtrarMusicasCapture, 
                            _listViewCapture, 
                            _estadoAtualCapture, 
                            _clearFilterCapture);
                    }
                }, null, delayMs, Timeout.Infinite);
            }
        }

        /// <summary>
        /// ✅ Salvamento imediato
        /// </summary>
        public static void SalvarEstadoDoFormulario(
            ref TextBox txtFiltro,
            ref FiltrarMusicas filtrarMusicas,
            ref ListView listView,
            ref FormularioEstado estadoAtual,
            bool clearFilter = true)
        {
            SalvarEstadoInterno(txtFiltro, filtrarMusicas, listView, estadoAtual, clearFilter);
        }

        /// <summary>
        /// ✅ Método interno que faz o salvamento real (sem ref)
        /// </summary>
        private static void SalvarEstadoInterno(
            TextBox txtFiltro,
            FiltrarMusicas filtrarMusicas,
            ListView listView,
            FormularioEstado estadoAtual,
            bool clearFilter)
        {
            try
            {
                if (clearFilter)
                {
                    InvokeAux.Access(txtFiltro, txt => txt.Text = string.Empty);
                    filtrarMusicas.ResetMemory();
                }
                else
                {
                    string filtroAtual = InvokeAux.GetValue(txtFiltro, txt => txt.Text);
                    estadoAtual.FiltroTexto = filtroAtual;
                }

                // Salva larguras das colunas
                estadoAtual.ListVewStateProp.ColumnWidths = InvokeAux.GetValue(listView, lv =>
                    lv.Columns.Cast<ColumnHeader>().Select(c => c.Width).ToList()
                );

                estadoAtual.ListVewStateProp.View = (int)InvokeAux.GetValue(listView, lv => lv.View);

                ControleEstados.SalvarEstado(estadoAtual);
                Log.Information("Estado salvo com sucesso");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao salvar estado do formulário");
            }
        }

        /// <summary>
        /// ✅ Carrega estado do formulário
        /// </summary>
        public static bool CarregarEstadoDoFormulario(
            ref FormularioEstado estadoAtual,
            ref FiltrarMusicas filtrarMusicas,
            ref ListView listView,
            ref ImageList imageList,
            Action atualizarSelecao,
            ref TextBox txtPath,
            ref TreeView treeView,
            ref TextBox txtFiltro)
        {
            try
            {
                var estadoCarregado = ControleEstados.RecuperarEstado();
                if (estadoCarregado == null)
                {
                    Log.Information("Nenhum estado anterior encontrado");
                    return false;
                }

                // ✅ Atualiza por valor (não precisa de ref dentro do lambda)
                estadoAtual.MusicPath = estadoCarregado.MusicPath;
                estadoAtual.IndiceMusica = estadoCarregado.IndiceMusica;
                estadoAtual.Musicas = estadoCarregado.Musicas;
                estadoAtual.IsDarkMode = estadoCarregado.IsDarkMode;
                estadoAtual.ListVewStateProp = estadoCarregado.ListVewStateProp;
                string filtroTexto = estadoCarregado.FiltroTexto;

                filtrarMusicas.SetEstado(estadoAtual);

                // ✅ Captura variáveis locais para uso no lambda
                var estadoLocal = estadoAtual;
                var imageListLocal = imageList;
                var filtrarMusicasLocal = filtrarMusicas;
                var txtFiltroLocal = txtFiltro;

                InvokeAux.Access(listView, lv =>
                {
                    lv.BeginUpdate();
                    lv.Items.Clear();

                    ListViewAux.ConfigurarColunasPadrao(lv, estadoLocal.ListVewStateProp.ColumnWidths);

                    foreach (var musicaDto in estadoLocal.Musicas)
                    {
                        lv.Items.Add(ListViewAux.ToListViewItem(musicaDto));
                    }

                    lv.View = (View)estadoLocal.ListVewStateProp.View;
                    lv.SmallImageList = imageListLocal;
                    lv.EndUpdate();
                });

                atualizarSelecao();

                // ✅ Captura variáveis para uso nos lambdas
                var musicPath = estadoAtual.MusicPath;
                
                if (!string.IsNullOrEmpty(musicPath))
                {
                    InvokeAux.Access(txtPath, txt => txt.Text = musicPath);
                    TreeViewUtil.PreencherTreeView(treeView, musicPath);
                }

                if (!string.IsNullOrEmpty(filtroTexto))
                {
                    InvokeAux.Access(txtFiltroLocal, txt => txt.Text = filtroTexto);
                    filtrarMusicasLocal.Filtrar(filtroTexto, listView);
                }

                Log.Information("Estado carregado: {Estado}", estadoAtual);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao carregar estado");
                return false;
            }
        }
    }
}