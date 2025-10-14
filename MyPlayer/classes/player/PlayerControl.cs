using NAudio.Wave;

namespace MyPlayer.classes.player
{
    public class PlayerControl : IDisposable
    {
        private MusicControl _musicControl;
        private CancellationTokenSource? _cts;

        public event EventHandler<double>? EvtProgressUpdated;
        public event EventHandler? EvtPlaying;
        public event EventHandler? EvtPaused;
        public event EventHandler? EvtResume;
        public event EventHandler? EvtStop;
        public event EventHandler? EvtMusicEnded;

        public PlaybackState? PlaybackStateProp => _musicControl.PlaybackStateProp;
        public bool IsPlaying => _musicControl.IsPlaying;
        public bool IsPaused => _musicControl.IsPaused;
        public bool IsStoped => _musicControl.IsStoped;
        public bool IsValid => _musicControl.IsValid;

        public TimeSpan MusicDuration => _musicControl?.TotalTime ?? TimeSpan.Zero;
        public TimeSpan CurrentTime => _musicControl?.GetCurrentTime() ?? TimeSpan.Zero;


        public AudioFileReader? AudioFileReaderProp => _musicControl == null ? null : _musicControl.AudioFile;

        public PlayerControl(string musicPath)
        {
            if (string.IsNullOrEmpty(musicPath) || !File.Exists(musicPath))
                throw new ArgumentException("Arquivo inválido.", nameof(musicPath));

            _musicControl = new MusicControl(musicPath);
            _musicControl.EvtPlaying += (s, e) => EvtPlaying?.Invoke(s, e);
            _musicControl.EvtPaused += (s, e) => EvtPaused?.Invoke(s, e);
            _musicControl.EvtResume += (s, e) => EvtResume?.Invoke(s, e);
            _musicControl.EvtStop += (s, e) => EvtStop?.Invoke(s, e);

            if (!_musicControl.IsValid)
                throw new InvalidOperationException("Não foi possível inicializar o arquivo de áudio.");
        }

        public void Play()
        {
            if (!_musicControl.IsValid) return;

            if (IsPlaying) Stop(); // garante que não há múltiplas execuções

            _cts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                _musicControl.Play();

                try
                {

                    while ((IsPlaying || IsPaused)
                    && !_cts.Token.IsCancellationRequested
                    && !IsStoped
                    && (IsPlaying || IsPaused)
                    && _musicControl.GetProgress() < 100.0d)
                    {
                        //Console.WriteLine($"IsPlaying: {IsPlaying}, IsPaused: {IsPaused}, !cts.Token.IsCancellationRequested: {!_cts.Token.IsCancellationRequested}, PlaybackStateProp: {PlaybackStateProp}");
                        if (!IsPaused && IsPlaying) EvtProgressUpdated?.Invoke(this, _musicControl.GetProgress());
                        await Task.Delay(200, _cts.Token); // atualiza a cada 200ms
                    }

                    // Atualiza progresso final
                    Console.WriteLine("Final progress...");
                    EvtProgressUpdated?.Invoke(this, _musicControl.GetProgress());
                }
                catch (TaskCanceledException)
                {
                    // Thread cancelada, ignora
                }
                finally
                {
                    EvtMusicEnded?.Invoke(this, EventArgs.Empty);
                }
            });
        }

        public void Pause()
        {
            if (!_musicControl.IsValid || !IsPlaying) return;
            _musicControl.Pause();
        }

        public void Resume()
        {
            if (!_musicControl.IsValid || !IsPaused) return;
            _musicControl.Resume();
        }

        public void Stop()
        {
            if (!_musicControl.IsValid) return;
            _musicControl.Stop();
            _cts?.Cancel();
        }

        public void Seek(TimeSpan time)
        {
            if (!_musicControl.IsValid) return;
            _musicControl.Seek(time);
        }

        public void SetPosition(int position)
        {
            if (!_musicControl.IsValid) return;
            _musicControl.SetPosition(position);
        }

        public void SetPercent(double percent)
        {
            if (!_musicControl.IsValid) return;
            _musicControl.SetPercent(percent);
        }

        public void Dispose()
        {
            Stop();
            _musicControl.Dispose();
        }

    }
}

/*
public event EventHandler<double> ProgressUpdated;
ProgressUpdated?.Invoke(this, progress);
player.ProgressUpdated += (s, progress) => { Console.WriteLine($"Progresso consultado: {progress:F2}%"); };
*/