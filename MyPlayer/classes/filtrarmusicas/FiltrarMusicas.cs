using MyPlayer.classes.controleestados;
using MyPlayer.classes.playlist;
using MyPlayer.classes.util.threads;

namespace MyPlayer.classes.filtrarmusicas
{
    /// <summary>
    /// filtrar músicas
    /// </summary>
    internal class FiltrarMusicas
    {
        private FormularioEstado? _estado;
        private List<MusicaDTO>? _memory;

        private static FiltrarMusicas? _instance = null;
        private FiltrarMusicas() { }
        public static FiltrarMusicas Instance
        {
            get {
                _instance ??= new();
                return _instance;
            }
        }

        public void SetEstado(FormularioEstado estado) {
            _estado = estado;
            if (estado == null) { return; }
            _memory = estado.Musicas;
        }

        public void ResetMemory() {
            _memory = null;
        }

        public void Filtrar(string music, ListView listView)
        {
            if (_estado == null || _estado.Musicas == null) return;

            // Inicializa a memória na primeira vez para não perder a lista original
            if (_memory == null) { _memory = _estado.Musicas; }

            // Sempre partimos da memória (lista completa) para aplicar um novo filtro
            List<MusicaDTO> listaParaFiltrar = _memory;

            // Aplica filtro se houver texto
            if (!string.IsNullOrWhiteSpace(music))
            {
                string termo = music.Trim().ToLowerInvariant();
                listaParaFiltrar = listaParaFiltrar
                    .Where(item =>
                        (item.Text != null && item.Text.ToLowerInvariant().Contains(termo)) ||
                        (item.SubItems != null && item.SubItems.Any(sub => sub.ToLowerInvariant().Contains(termo))))
                    .ToList();
            }

            // Atualiza o estado atual com o resultado do filtro
            _estado.Musicas = listaParaFiltrar;

            // Atualiza ListView de forma thread-safe
            InvokeAux.Access(listView, lvw =>
            {
                try
                {
                    lvw.BeginUpdate();
                    lvw.Items.Clear();

                    foreach (var mDto in _estado.Musicas)
                    {
                        ListViewItem item = new ListViewItem(mDto.Text)
                        {
                            Tag = mDto.Tag,
                            ImageIndex = mDto.ImageIndex
                        };

                        if (mDto.SubItems != null)
                        {
                            foreach (var subText in mDto.SubItems)
                            {
                                item.SubItems.Add(subText);
                            }
                        }

                        lvw.Items.Add(item);
                    }
                }
                finally
                {
                    lvw.EndUpdate();
                }
            });
        }

    }
}