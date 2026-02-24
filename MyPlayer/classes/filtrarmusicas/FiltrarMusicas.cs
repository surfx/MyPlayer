using MyPlayer.classes.util.threads;
using Serilog;

namespace MyPlayer.classes.filtrarmusicas
{
    internal class FiltrarMusicas
    {
        private List<ListViewItem>? _originalItems;

        private static FiltrarMusicas? _instance = null;
        private FiltrarMusicas() { }

        public string TermoFiltro { get; private set; } = string.Empty;
        
        public static FiltrarMusicas Instance
        {
            get {
                _instance ??= new();
                return _instance;
            }
        }

        public void SetEstado(MyPlayer.classes.controleestados.FormularioEstado estado) 
        {
        }

        public void ResetMemory() 
        {
            TermoFiltro = string.Empty;
            _originalItems = null;
        }

        public void Filtrar(string termo, ListView listView)
        {
            InvokeAux.Access(listView, lvw =>
            {
                lvw.BeginUpdate();

                TermoFiltro = termo?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(TermoFiltro))
                {
                    if (_originalItems != null)
                    {
                        lvw.Items.Clear();
                        lvw.Items.AddRange(_originalItems.ToArray());
                        _originalItems = null;
                    }
                    lvw.EndUpdate();
                    return;
                }

                if (_originalItems == null)
                {
                    _originalItems = lvw.Items.Cast<ListViewItem>().ToList();
                }

                lvw.Items.Clear();

                string termoLower = TermoFiltro.ToLowerInvariant();
                var filtrados = _originalItems.Where(item =>
                    item.Text.ToLowerInvariant().Contains(termoLower) ||
                    (item.SubItems != null && item.SubItems.Cast<ListViewItem.ListViewSubItem>()
                        .Any(sub => sub.Text.ToLowerInvariant().Contains(termoLower)))
                ).ToList();

                lvw.Items.AddRange(filtrados.ToArray());

                lvw.EndUpdate();
            });
        }

        public List<ListViewItem>? GetAllItems()
        {
            return _originalItems;
        }

        public bool ItemCorrespondeFiltro(ListViewItem item)
        {
            if (string.IsNullOrWhiteSpace(TermoFiltro))
                return true;

            string termoLower = TermoFiltro.ToLowerInvariant();
            
            if (item.Text.ToLowerInvariant().Contains(termoLower))
                return true;

            if (item.SubItems != null)
            {
                return item.SubItems.Cast<ListViewItem.ListViewSubItem>()
                    .Any(sub => sub.Text.ToLowerInvariant().Contains(termoLower));
            }

            return false;
        }
    }
}
