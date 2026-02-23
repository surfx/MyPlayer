using MyPlayer.classes.controleestados;
using MyPlayer.classes.playlist;
using MyPlayer.classes.util.threads;
using Serilog;

namespace MyPlayer.classes.filtrarmusicas
{
    internal class FiltrarMusicas
    {
        private FormularioEstado? _estado;
        private List<MusicaDTO>? _memory;
        private List<MusicaDTO>? _filteredList;

        public List<MusicaDTO>? FilteredList => _filteredList;

        private static FiltrarMusicas? _instance = null;
        private FiltrarMusicas() { }
        
        public static FiltrarMusicas Instance
        {
            get {
                _instance ??= new();
                return _instance;
            }
        }

        public void SetEstado(FormularioEstado estado) 
        {
            _estado = estado;
            if (estado != null)
            {
                _memory = estado.Musicas;
            }
        }

        public void ResetMemory() 
        {
            _memory = null;
            _filteredList = null;
        }

        public void Filtrar(string music, ListView listView)
        {
            if (_estado == null || _estado.Musicas == null) return;

            _memory ??= _estado.Musicas;

            List<MusicaDTO> listaParaFiltrar = _memory;

            if (!string.IsNullOrWhiteSpace(music))
            {
                string termo = music.Trim().ToLowerInvariant();

                listaParaFiltrar = listaParaFiltrar
                    .Where(item =>
                        (item.Text != null && item.Text.ToLowerInvariant().Contains(termo)) ||
                        (item.SubItems != null && item.SubItems.Any(sub => sub.ToLowerInvariant().Contains(termo))))
                    .ToList();
            }

            _filteredList = listaParaFiltrar;

            InvokeAux.Access(listView, lvw =>
            {
                try
                {
                    lvw.BeginUpdate();
                    lvw.Items.Clear();

                    foreach (var mDto in _filteredList)
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

                    Log.Debug("Filtro aplicado: {Termo} → {Resultados} resultados", music, _filteredList.Count);
                }
                finally
                {
                    lvw.EndUpdate();
                }
            });
        }
    }
}