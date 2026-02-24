using MyPlayer.classes.playlist;

namespace MyPlayer.classes.controleestados
{
    internal class FormularioEstado
    {
        public string MusicPath { get; set; } = string.Empty;
        public int IndiceMusica { get; set; }
        public string FiltroTexto { get; set; } = string.Empty;
        public List<MusicaDTO> Musicas { get; set; } = new List<MusicaDTO>();
        public bool IsDarkMode { get; set; }
        public ListVewState ListVewStateProp { get; set; } = new();

        public override string ToString()
        {
            return $"MusicPath: {MusicPath}, IndiceMusica: {IndiceMusica}, Musicas: {(Musicas == null ? 0 : Musicas.Count)}, ListVewStateProp: {ListVewStateProp}";
        }
    }
}

internal class ListVewState
{
    public int View { get; set; }
    public List<int> ColumnWidths { get; set; } = new List<int>();

    public override string ToString()
    {
        // Usa string.Join para concatenar os elementos da lista com uma vírgula e espaço
        string columnWidthsString = ColumnWidths != null ? string.Join(", ", ColumnWidths) : "N/A";
        return $"View: {View}, ColumnWidths: [{columnWidthsString}]";
    }
}