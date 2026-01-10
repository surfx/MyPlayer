using MyPlayer.classes.controleestados;
using MyPlayer.classes.filtrarmusicas;
using MyPlayer.classes.playlist;
using MyPlayer.classes.util.threads;

namespace MyPlayer.classes.util.form
{
    internal static class ListViewAux
    {
        public const int MaxFileStr = 100;

        public static void ListarArquivosListView(
            ref ListView listView,
            ref ImageList imageList,
            string[] ExtensoesPermitidas,
            ref FormularioEstado estadoAtual,
            ref FiltrarMusicas filtrarMusicas,
            string path, bool clearListView = false, bool addPastas = false
        )
        {
            var imageListAux = imageList;
            var estadoAtualAux = estadoAtual;
            var filtrarMusicasAux = filtrarMusicas;

            InvokeAux.Access(listView, lvw => {
                lvw.BeginUpdate();

                if (clearListView) lvw.Items.Clear();

                lvw.View = View.Details;
                lvw.SmallImageList = imageListAux;

                if (lvw.Columns.Count <= 0)
                {
                    ConfigurarColunasPadrao(lvw);
                }

                var caminhosExistentes = new HashSet<string>(
                    lvw.Items.Cast<ListViewItem>()
                             .Select(i => i.Tag?.ToString() ?? ""),
                    StringComparer.OrdinalIgnoreCase
                );

                // Pastas
                if (addPastas)
                {
                    string[] pastas = Directory.GetDirectories(path);
                    foreach (string pasta in pastas)
                    {
                        DirectoryInfo di = new(pasta);
                        string nome = di.Name;

                        if (nome.Length > MaxFileStr)
                            nome = string.Concat(nome.AsSpan(0, MaxFileStr), "...");

                        ListViewItem item = new(nome)
                        {
                            ImageIndex = 0, // folder fechado
                            Tag = di.FullName
                        };
                        item.SubItems.Add(""); // tamanho vazio para pastas
                        item.SubItems.Add(di.LastWriteTime.ToString());
                        lvw.Items.Add(item);
                    }
                }

                string[] arquivos = Directory.GetFiles(path);
                foreach (string arquivo in arquivos
                    .Where(arq => ExtensoesPermitidas.Contains(Path.GetExtension(arq).ToLowerInvariant()))
                    .Where(arq => !caminhosExistentes.Contains(arq))
                )
                {

                    FileInfo fi = new(arquivo);
                    var musicaDto = new MusicaDTO
                    {
                            Text = Path.GetFileNameWithoutExtension(fi.Name),
                            Tag = fi.FullName,
                            ImageIndex = 10,
                            SubItems = [
                                (fi.Length / 1024).ToString("N0") + " KB",
                                fi.LastWriteTime.ToString("dd/MM/yyyy HH:mm")
                            ]
                    };
                    lvw.Items.Add(ToListViewItem(musicaDto));
                }

                lvw.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                //lvw.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);

                lvw.EndUpdate();

                // atualiza a lista de musicas
                estadoAtualAux.Musicas = ListViewAux.GetListMusicas(ref lvw, ExtensoesPermitidas);
                estadoAtualAux.IndiceMusica = 0;
                filtrarMusicasAux.SetEstado(estadoAtualAux);
            });
        }

        // Retorna as músicas como ListViewItem (usado em UI)
        public static List<MusicaDTO> GetListMusicas(ref ListView listView1, string[] extensoesPermitidas)
        {
            return InvokeAux.GetValue(listView1, lv =>
            {
                List<MusicaDTO> rt = [];

                var itensValidos = lv.Items.Cast<ListViewItem>()
                    .Where(item => {
                        string? path = item.Tag?.ToString();
                        return !string.IsNullOrEmpty(path) &&
                               File.Exists(path) &&
                               extensoesPermitidas.Contains(Path.GetExtension(path).ToLowerInvariant());
                    });

                foreach (ListViewItem item in itensValidos)
                {
                    var musicaDto = new MusicaDTO
                    {
                        Text = item.Text,
                        ImageIndex = item.ImageIndex,
                        Tag = item?.Tag?.ToString() ?? "",
                        SubItems = []
                    };

                    if (item == null || item.SubItems == null) { continue; }
                    foreach (var sub in item.SubItems.Cast<ListViewItem.ListViewSubItem>().Skip(1))
                    {
                        musicaDto.SubItems.Add(sub.Text);
                    }
                    rt.Add(musicaDto);
                }
                return rt;
            });
        }

        // 🔁 Sobrecarga — retorna apenas os caminhos (List<string>)
        public static List<string> GetListMusicasPaths(ref FormularioEstado estadoAtual, ref ListView listView, string[] extensoesPermitidas)
        {
            estadoAtual.Musicas ??= GetListMusicas(ref listView, extensoesPermitidas);
            return (estadoAtual.Musicas ?? [])
                .Select(i => i.Tag?.ToString())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList()!;
        }

        #region conversores
        // MusicaDTO -> ListViewItem
        public static ListViewItem ToListViewItem(MusicaDTO dto)
        {
            string nomeExibicao = dto.Text;
            if (nomeExibicao.Length > MaxFileStr)
            {
                nomeExibicao = string.Concat(nomeExibicao.AsSpan(0, MaxFileStr), "...");
            }

            ListViewItem item = new(nomeExibicao)
            {
                Tag = dto.Tag,
                ImageIndex = dto.ImageIndex
            };
            foreach (var subText in dto.SubItems)
            {
                item.SubItems.Add(subText);
            }
            return item;
        }

        // Esqueleto do ListView (Colunas e Estilo)
        public static void ConfigurarColunasPadrao(ListView lvw, List<int>? larguras = null)
        {
            lvw.View = View.Details;
            lvw.FullRowSelect = true;
            lvw.CheckBoxes = true;
            lvw.Columns.Clear();

            // Se não houver larguras salvas, usa valores padrão
            lvw.Columns.Add("Nome", larguras?.ElementAtOrDefault(0) ?? 300);
            lvw.Columns.Add("Tamanho", larguras?.ElementAtOrDefault(1) ?? 100, HorizontalAlignment.Right);
            lvw.Columns.Add("Data de Modificação", larguras?.ElementAtOrDefault(2) ?? 150);
        }
        #endregion

    }
}