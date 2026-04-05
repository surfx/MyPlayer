using NAudio.Wave;
using System.IO;

namespace MyPlayer.classes.player
{
    public class MusicControl : IDisposable
    {
        public AudioFileReader? AudioFile { get; private set; }
        private WaveOutEvent? _waveOutEvent { get; set; }

        public PlaybackState? PlaybackStateProp => _waveOutEvent?.PlaybackState;
        public bool IsPlaying => PlaybackStateProp == PlaybackState.Playing;
        public bool IsPaused => PlaybackStateProp == PlaybackState.Paused;
        public bool IsStoped => PlaybackStateProp == PlaybackState.Stopped;

        public bool IsValid => AudioFile != null && _waveOutEvent != null;

        public TimeSpan TotalTime { get; private set; }

        private bool disposed = false;

        public event EventHandler? EvtPlaying;
        public event EventHandler? EvtPaused;
        public event EventHandler? EvtResume;
        public event EventHandler? EvtStop;

        public MusicControl(string musicPath)
        {
            if (string.IsNullOrEmpty(musicPath))
                throw new ArgumentNullException(nameof(musicPath), "Caminho da música não pode ser vazio");

            if (!File.Exists(musicPath))
                throw new FileNotFoundException("Arquivo de música não encontrado", musicPath);

            InitializeAudio(musicPath);
        }

        private void InitializeAudio(string musicPath)
        {
            try
            {
                AudioFile = new AudioFileReader(musicPath);

                if (AudioFile.TotalTime == TimeSpan.Zero)
                {
                    AudioFile.Dispose();
                    throw new InvalidDataException("Arquivo de áudio inválido ou corrompido");
                }

                _waveOutEvent = new WaveOutEvent();
                _waveOutEvent.Init(AudioFile);

                TotalTime = AudioFile.TotalTime;
                _waveOutEvent.PlaybackStopped += (s, e) => EvtStop?.Invoke(this, e);
            }
            catch (Exception ex) when (ex is not FileNotFoundException and not ArgumentNullException)
            {
                CleanupResources();
                if (ex is InvalidDataException) throw;
                throw new InvalidOperationException($"Erro ao inicializar áudio: {ex.Message}", ex);
            }
        }

        private void CleanupResources()
        {
            AudioFile?.Dispose();
            _waveOutEvent?.Dispose();
            AudioFile = null;
            _waveOutEvent = null;
        }

        public double GetProgress()
        {
            if (!IsValid || TotalTime.TotalSeconds <= 0) return 0.0;
            return (AudioFile!.CurrentTime.TotalSeconds / TotalTime.TotalSeconds) * 100;
        }

        public TimeSpan GetCurrentTime() => !IsValid ? TimeSpan.Zero : AudioFile!.CurrentTime;
        public long GetMaxPosition() => !IsValid ? 0 : AudioFile!.Length;

        public void Play()
        {
            if (!IsValid) return;
            
            try
            {
                _waveOutEvent!.Play();
                EvtPlaying?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao reproduzir: {ex.Message}");
                Stop();
            }
        }

        public void Pause()
        {
            if (!IsValid || !IsPlaying) return;
            _waveOutEvent!.Pause();
            EvtPaused?.Invoke(this, EventArgs.Empty);
        }

        public void Resume()
        {
            if (!IsValid || !IsPaused) return;
            
            try
            {
                _waveOutEvent!.Play();
                EvtResume?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao retomar: {ex.Message}");
                Stop();
            }
        }

        public void Stop()
        {
            if (!IsValid) return;
            
            _waveOutEvent?.Stop();
            ResetPosition();
            EvtStop?.Invoke(this, EventArgs.Empty);
        }

        private void ResetPosition()
        {
            if (AudioFile == null) return;
            try { AudioFile.Position = 0; } 
            catch (Exception ex) { Console.WriteLine($"Erro ao resetar posição: {ex.Message}"); }
        }

        public void Seek(TimeSpan time)
        {
            if (!IsValid) return;
            
            if (time < TimeSpan.Zero) time = TimeSpan.Zero;
            if (time > AudioFile!.TotalTime) time = AudioFile.TotalTime;
            
            AudioFile!.CurrentTime = time;
        }

        public void SetPosition(long position)
        {
            if (!IsValid || position < 0) return;
            AudioFile!.Position = Math.Min(position, AudioFile!.Length);
        }

        public void SetPercent(double percent)
        {
            if (!IsValid || AudioFile == null) return;
            
            percent = Math.Clamp(percent, 0, 100);
            var targetTime = TimeSpan.FromSeconds(TotalTime.TotalSeconds * (percent / 100.0));
            AudioFile.CurrentTime = targetTime > TotalTime ? TotalTime : targetTime;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                try
                {
                    _waveOutEvent?.Stop();
                    CleanupResources();
                }
                catch (Exception ex) { Console.WriteLine($"Erro ao dispor MusicControl: {ex.Message}"); }
            }

            disposed = true;
        }
    }
}
