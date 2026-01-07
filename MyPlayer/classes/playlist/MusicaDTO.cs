namespace MyPlayer.classes.playlist
{
    public class MusicaDTO
    {
        public string Text { get; set; }
        public int ImageIndex { get; set; }
        public string Tag { get; set; } // O caminho do arquivo
        public List<string> SubItems { get; set; } = new List<string>();
    }
}