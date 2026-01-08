using MyPlayer.classes.playlist;
using System.Text.Json;

namespace MyPlayer.classes.controleestados
{
    internal class ControleEstados
    {
        private static readonly string EstadoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "estado.json");

        public static void SalvarEstado(FormularioEstado estado)
        {
            try
            {
                // Note que agora usamos MusicaDTO diretamente na classe de serialização
                var serializavel = new SerializableFormularioEstado
                {
                    MusicPath = estado.MusicPath,
                    IndiceMusica = estado.IndiceMusica,
                    View = estado.ListVewStateProp.View,
                    ColumnWidths = estado.ListVewStateProp.ColumnWidths,
                    // Se estado.Musicas já for List<MusicaDTO>, basta atribuir
                    Musicas = estado.Musicas
                };

                var json = JsonSerializer.Serialize(serializavel, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(EstadoPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar estado: " + ex.Message);
            }
        }

        public static FormularioEstado? RecuperarEstado()
        {
            try
            {
                if (!File.Exists(EstadoPath)) return null;

                var json = File.ReadAllText(EstadoPath);
                var serializavel = JsonSerializer.Deserialize<SerializableFormularioEstado>(json);

                if (serializavel == null) return null;

                return new FormularioEstado
                {
                    MusicPath = serializavel.MusicPath,
                    IndiceMusica = serializavel.IndiceMusica,
                    ListVewStateProp = new()
                    {
                        View = serializavel.View,
                        ColumnWidths = serializavel.ColumnWidths
                    },
                    // Reutiliza a lista diretamente do JSON
                    Musicas = serializavel.Musicas
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao recuperar estado: " + ex.Message);
                return null;
            }
        }

        // Classes auxiliares simplificadas
        private class SerializableFormularioEstado
        {
            public string MusicPath { get; set; }
            public int IndiceMusica { get; set; }
            public int View { get; set; }
            public List<int> ColumnWidths { get; set; }
            public List<MusicaDTO> Musicas { get; set; }
        }
    }
}