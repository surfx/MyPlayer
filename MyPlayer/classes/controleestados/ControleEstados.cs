using System.Text.Json;

namespace MyPlayer.classes.controleestados
{
    internal class ControleEstados
    {
        private static readonly string EstadoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "estado.json");

        public static void SalvarEstado(FormularioEstado estado)
        {
            //if (File.Exists(EstadoPath)) { File.Delete(EstadoPath); }

            try
            {
                var serializavel = new SerializableFormularioEstado
                {
                    MusicPath = estado.MusicPath,
                    IndiceMusica = estado.IndiceMusica,
                    View = estado.ListVewStateProp.View,
                    ColumnWidths = estado.ListVewStateProp.ColumnWidths,
                    Musicas = new List<SerializableListViewItem>()
                };

                foreach (var item in estado.Musicas)
                {
                    if (item == null) continue;

                    string tag = item.Tag?.ToString() ?? "";

                    var sItem = new SerializableListViewItem
                    {
                        Text = item.Text ?? "",
                        ImageIndex = item.ImageIndex,
                        Tag = tag,
                        SubItems = new List<string>()
                    };

                    // Começa do índice 1, para não duplicar o texto principal
                    for (int i = 1; i < item.SubItems.Count; i++)
                    {
                        var sub = item.SubItems[i];
                        sItem.SubItems.Add(sub.Text);
                    }

                    serializavel.Musicas.Add(sItem);
                }

                var json = JsonSerializer.Serialize(serializavel, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(EstadoPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar estado: " + ex.Message);
            }

            //Console.WriteLine(estado);
            //Console.ReadKey();
        }

        public static FormularioEstado? RecuperarEstado()
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
                    ListVewStateProp = new(){ 
                        View = serializavel.View,
                        ColumnWidths = serializavel.ColumnWidths
                    },
                    Musicas = new List<ListViewItem>()
                };

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