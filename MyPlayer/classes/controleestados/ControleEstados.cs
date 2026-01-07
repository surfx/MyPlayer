using MyPlayer.classes.playlist;
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
                        sItem.SubItems.Add(sub);
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
                // O JSON agora mapeia diretamente para a estrutura de DTOs
                var serializavel = JsonSerializer.Deserialize<SerializableFormularioEstado>(json);

                if (serializavel == null) return null;

                var estado = new FormularioEstado
                {
                    MusicPath = serializavel.MusicPath,
                    IndiceMusica = serializavel.IndiceMusica,
                    ListVewStateProp = new()
                    {
                        View = serializavel.View,
                        ColumnWidths = serializavel.ColumnWidths
                    },
                    // Inicializamos a lista de DTOs
                    Musicas = new List<MusicaDTO>()
                };

                // Mapeamos os itens do JSON para a nossa lista de Musicas (DTO)
                foreach (var sItem in serializavel.Musicas)
                {
                    var musicaDto = new MusicaDTO
                    {
                        Text = sItem.Text,
                        ImageIndex = sItem.ImageIndex,
                        Tag = sItem.Tag,
                        // Clonamos os subitens para a lista de strings
                        SubItems = sItem.SubItems.ToList()
                    };

                    estado.Musicas.Add(musicaDto);
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