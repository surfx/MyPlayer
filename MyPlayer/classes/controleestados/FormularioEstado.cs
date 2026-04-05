using MyPlayer.classes.playlist;

namespace MyPlayer.classes.controleestados
{
    public class FormularioEstado
    {
        public string MusicPath { get; set; } = string.Empty;
        public string SelectedMusicaTag { get; set; } = string.Empty;
        public string FiltroTexto { get; set; } = string.Empty;
        public List<MusicaDTO> Musicas { get; set; } = new List<MusicaDTO>();
        public bool IsDarkMode { get; set; }
        
        public double WindowWidth { get; set; } = 1100;
        public double WindowHeight { get; set; } = 700;
        public double WindowLeft { get; set; } = 100;
        public double WindowTop { get; set; } = 100;
    }
}
