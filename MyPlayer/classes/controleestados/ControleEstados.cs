using MyPlayer.classes.playlist;
using System.Text.Json;

namespace MyPlayer.classes.controleestados;

internal class ControleEstados
{
    private static readonly string EstadoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "estado.json");

    public static void SalvarEstado(FormularioEstado estado)
    {
        try
        {
            var serializavel = new SerializableFormularioEstado
            {
                MusicPath = estado.MusicPath,
                IndiceMusica = estado.IndiceMusica,
                View = estado.ListVewStateProp.View,
                ColumnWidths = estado.ListVewStateProp.ColumnWidths,
                Musicas = estado.Musicas,
                IsDarkMode = estado.IsDarkMode
            };

            var json = JsonSerializer.Serialize(serializavel, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(EstadoPath, json);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erro ao salvar estado");
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
                Musicas = serializavel.Musicas,
                IsDarkMode = serializavel.IsDarkMode
            };
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erro ao recuperar estado");
            return null;
        }
    }

    private class SerializableFormularioEstado
    {
        public string MusicPath { get; set; } = string.Empty;
        public int IndiceMusica { get; set; }
        public int View { get; set; }
        public List<int> ColumnWidths { get; set; } = new();
        public List<MusicaDTO> Musicas { get; set; } = new();
        public bool IsDarkMode { get; set; }
    }
}
