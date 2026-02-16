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
            string path, 
            bool clearListView = false, 
            bool addPastas = false
        )
        {
            var imageListAux = imageList;
            var estadoAtualAux = estadoAtual;
            var filtrarMusicasAux = filtrarMusicas;

            // ✅ HashSet para busca O(1)
            var extensoesSet = new HashSet<string>(ExtensoesPermitidas, StringComparer.OrdinalIgnoreCase);

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
                    try
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
                                ImageIndex = 0,
                                Tag = di.FullName
                            };
                            item.SubItems.Add("");
                            item.SubItems.Add(di.LastWriteTime.ToString("dd/MM/yyyy HH:mm"));
                            lvw.Items.Add(item);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Ignora pastas sem permissão
                    }
                }

                // Arquivos
                try
                {
                    string[] arquivos = Directory.GetFiles(path);
                    
                    var arquivosFiltrados = arquivos
                        .Where(arq => extensoesSet.Contains(Path.GetExtension(arq)))
                        .Where(arq => !caminhosExistentes.Contains(arq));

                    foreach (string arquivo in arquivosFiltrados)
                    {
                        FileInfo fi = new(arquivo);
                        var musicaDto = new MusicaDTO
                        {
                            Text = Path.GetFileNameWithoutExtension(fi.Name),
                            Tag = fi.FullName,
                            ImageIndex = 10,
                            SubItems = [
                                Util.FormatFileSize(fi.Length),
                                fi.LastWriteTime.ToString("dd/MM/yyyy HH:mm")
                            ]
                        };
                        lvw.Items.Add(ToListViewItem(musicaDto));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao listar arquivos: {ex.Message}", "Erro", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                lvw.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                lvw.EndUpdate();

                // Atualiza estado
                estadoAtualAux.Musicas = GetListMusicas(ref lvw, ExtensoesPermitidas);
                estadoAtualAux.IndiceMusica = 0;
                filtrarMusicasAux.SetEstado(estadoAtualAux);
            });
        }

        /// <summary>
        /// ✅ Otimizado com HashSet
        /// </summary>
        public static List<MusicaDTO> GetListMusicas(ref ListView listView1, string[] extensoesPermitidas)
        {
            return InvokeAux.GetValue(listView1, lv =>
            {
                var extensoesSet = new HashSet<string>(extensoesPermitidas, StringComparer.OrdinalIgnoreCase);

                return lv.Items.Cast<ListViewItem>()
                    .Where(item => {
                        string? path = item.Tag?.ToString();
                        return !string.IsNullOrEmpty(path) &&
                               File.Exists(path) &&
                               extensoesSet.Contains(Path.GetExtension(path));
                    })
                    .Select(item => new MusicaDTO
                    {
                        Text = item.Text,
                        ImageIndex = item.ImageIndex,
                        Tag = item.Tag?.ToString() ?? "",
                        SubItems = item.SubItems.Cast<ListViewItem.ListViewSubItem>()
                            .Skip(1)
                            .Select(sub => sub.Text)
                            .ToList()
                    })
                    .ToList();
            });
        }

        public static List<string> GetListMusicasPaths(ref FormularioEstado estadoAtual, ref ListView listView, string[] extensoesPermitidas)
        {
            estadoAtual.Musicas ??= GetListMusicas(ref listView, extensoesPermitidas);
            return (estadoAtual.Musicas ?? [])
                .Select(i => i.Tag)
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList()!;
        }

        #region conversores

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

        public static void ConfigurarColunasPadrao(ListView lvw, List<int>? larguras = null)
        {
            lvw.View = View.Details;
            lvw.FullRowSelect = true;
            lvw.CheckBoxes = true;
            lvw.Columns.Clear();

            lvw.Columns.Add("Nome", larguras?.ElementAtOrDefault(0) ?? 300);
            lvw.Columns.Add("Tamanho", larguras?.ElementAtOrDefault(1) ?? 100, HorizontalAlignment.Right);
            lvw.Columns.Add("Data de Modificação", larguras?.ElementAtOrDefault(2) ?? 150);
        }

        #endregion
    }
}