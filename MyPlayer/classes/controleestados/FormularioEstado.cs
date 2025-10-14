namespace MyPlayer.classes.controleestados
{
    internal class FormularioEstado
    {
        public string MusicPath { get; set; }
        public int IndiceMusica { get; set; }
        public List<ListViewItem> Musicas { get; set; }

        public ListVewState ListVewStateProp { get; set; }

        public override string ToString()
        {
            return $"MusicPath: {MusicPath}, IndiceMusica: {IndiceMusica}, Musicas: {(Musicas == null ? 0 : Musicas.Count)}, ListVewStateProp: {ListVewStateProp}";
        }
    }
}

internal class ListVewState
{
    public int View { get; set; }
    public List<int> ColumnWidths { get; set; }

    public override string ToString()
    {
        // Usa string.Join para concatenar os elementos da lista com uma vírgula e espaço
        string columnWidthsString = ColumnWidths != null ? string.Join(", ", ColumnWidths) : "N/A";
        return $"View: {View}, ColumnWidths: [{columnWidthsString}]";
    }
}