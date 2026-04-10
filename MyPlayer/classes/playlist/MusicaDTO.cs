using MyPlayer.viewmodels;

namespace MyPlayer.classes.playlist
{
    public class MusicaDTO : BaseViewModel
    {
        private bool _isPlaying;

        public required string Text { get; set; }
        public int ImageIndex { get; set; }
        public required string Tag { get; set; } // O caminho do arquivo
        public List<string> SubItems { get; set; } = [];

        public bool IsPlaying
        {
            get => _isPlaying;
            set => SetProperty(ref _isPlaying, value);
        }
    }
}