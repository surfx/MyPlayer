using FuzzySharp;
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
        private const int FuzzyThreshold = 70; // ✅ Sensibilidade da busca fuzzy

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
        }

        /// <summary>
        /// ✅ Filtro com suporte a Fuzzy Search
        /// </summary>
        public void Filtrar(string music, ListView listView, bool useFuzzy = true)
        {
            if (_estado == null || _estado.Musicas == null) return;

            _memory ??= _estado.Musicas;

            List<MusicaDTO> listaParaFiltrar = _memory;

            if (!string.IsNullOrWhiteSpace(music))
            {
                string termo = music.Trim();

                if (useFuzzy)
                {
                    // ✅ Busca fuzzy (tolera erros de digitação)
                    listaParaFiltrar = listaParaFiltrar
                        .Select(item => new 
                        { 
                            Item = item,
                            Score = Math.Max(
                                Fuzz.PartialRatio(termo, item.Text),
                                item.SubItems.Any() 
                                    ? item.SubItems.Max(sub => Fuzz.PartialRatio(termo, sub))
                                    : 0
                            )
                        })
                        .Where(x => x.Score >= FuzzyThreshold)
                        .OrderByDescending(x => x.Score)
                        .Select(x => x.Item)
                        .ToList();
                }
                else
                {
                    // Busca exata (mais rápida)
                    string termoLower = termo.ToLowerInvariant();
                    listaParaFiltrar = listaParaFiltrar
                        .Where(item =>
                            (item.Text != null && item.Text.ToLowerInvariant().Contains(termoLower)) ||
                            (item.SubItems != null && item.SubItems.Any(sub => sub.ToLowerInvariant().Contains(termoLower))))
                        .ToList();
                }
            }

            _estado.Musicas = listaParaFiltrar;

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

                    Log.Debug("Filtro aplicado: {Termo} → {Resultados} resultados", music, _estado.Musicas.Count);
                }
                finally
                {
                    lvw.EndUpdate();
                }
            });
        }
    }
}