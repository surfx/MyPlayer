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

        public void Filtrar(string music, ListView listView)
        {
            if (_estado == null || _estado.Musicas == null) return;
            if (_memory == null) { _memory = _estado.Musicas; }
            if (_memory == null) { return; }

            if (_estado.Musicas.Count != _memory.Count) _estado.Musicas = _memory;

            // Aplica filtro se houver texto
            if (!string.IsNullOrWhiteSpace(music))
            {
                string termo = music.Trim().ToLowerInvariant();
                _estado.Musicas = _estado.Musicas
                    .Where(item =>
                        item.Text.ToLowerInvariant().Contains(termo) ||
                        item.SubItems.Cast<ListViewItem.ListViewSubItem>()
                            .Any(sub => sub.Text.ToLowerInvariant().Contains(termo)))
                    .ToList();
            }

            // Atualiza ListView de forma thread-safe
            InvokeAux.Access(listView, lvw =>
            {
                lvw.BeginUpdate();
                try
                {
                    lvw.Items.Clear();

                    // Agora percorremos a lista de DTOs para criar os itens visuais
                    foreach (var mDto in _estado.Musicas)
                    {
                        // Coluna 1: Nome (Texto principal)
                        ListViewItem item = new ListViewItem(mDto.Text)
                        {
                            Tag = mDto.Tag,
                            ImageIndex = mDto.ImageIndex
                        };

                        // Adiciona os demais SubItems (Tamanho, Data, etc.)
                        // Como mDto.SubItems é uma List<string>, adicionamos direto
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