using System.Text.Json;

namespace MyPlayer.classes.controleestados
{
    internal class ControleEstados
    {
        private static readonly string EstadoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "estado.json");

        public void SalvarEstado(FormularioEstado estado, ListView listView)
        {
            try
            {
                var serializavel = new SerializableFormularioEstado
                {
                    MusicPath = estado.MusicPath,
                    IndiceMusica = estado.IndiceMusica,
                    View = (int)listView.View,
                    ColumnWidths = new List<int>(),
                    Musicas = new List<SerializableListViewItem>()
                };

                foreach (ColumnHeader col in listView.Columns)
                    serializavel.ColumnWidths.Add(col.Width);

                foreach (var item in estado.Musicas)
                {
                    var sItem = new SerializableListViewItem
                    {
                        Text = item.Text,
                        ImageIndex = item.ImageIndex,
                        Tag = item.Tag?.ToString(),
                        SubItems = new List<string>()
                    };

                    foreach (ListViewItem.ListViewSubItem sub in item.SubItems)
                        sItem.SubItems.Add(sub.Text);

                    serializavel.Musicas.Add(sItem);
                }

                var json = JsonSerializer.Serialize(serializavel, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(EstadoPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar estado: " + ex.Message);
            }
        }

        public FormularioEstado RecuperarEstado(ListView listView)
        {
            try
            {
                if (!File.Exists(EstadoPath))
                    return null;

                var json = File.ReadAllText(EstadoPath);
                var serializavel = JsonSerializer.Deserialize<SerializableFormularioEstado>(json);

                var estado = new FormularioEstado
                {
                    MusicPath = serializavel.MusicPath,
                    IndiceMusica = serializavel.IndiceMusica,
                    Musicas = new List<ListViewItem>()
                };

                listView.View = (View)serializavel.View;

                // Restaurar colunas
                for (int i = 0; i < serializavel.ColumnWidths.Count && i < listView.Columns.Count; i++)
                    listView.Columns[i].Width = serializavel.ColumnWidths[i];

                foreach (var sItem in serializavel.Musicas)
                {
                    var item = new ListViewItem(sItem.Text)
                    {
                        ImageIndex = sItem.ImageIndex,
                        Tag = sItem.Tag
                    };

                    foreach (var sub in sItem.SubItems)
                        item.SubItems.Add(sub);

                    estado.Musicas.Add(item);
                }

                return estado;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao recuperar estado: " + ex.Message);
                return null;
            }
        }

        // Classes auxiliares para serialização
        private class SerializableFormularioEstado
        {
            public string MusicPath { get; set; }
            public int IndiceMusica { get; set; }
            public int View { get; set; }
            public List<int> ColumnWidths { get; set; }
            public List<SerializableListViewItem> Musicas { get; set; }
        }

        private class SerializableListViewItem
        {
            public string Text { get; set; }
            public int ImageIndex { get; set; }
            public string Tag { get; set; }
            public List<string> SubItems { get; set; }
        }
    }
}
