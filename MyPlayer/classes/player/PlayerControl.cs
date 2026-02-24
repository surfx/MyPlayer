using NAudio.Wave;
using Serilog;

namespace MyPlayer.classes.player
{
    public class PlayerControl : IDisposable
    {
        private MusicControl? _musicControl;
        private CancellationTokenSource? _cts;
        private bool _disposed = false;

        public event EventHandler<double>? EvtProgressUpdated;
        public event EventHandler? EvtPlaying;
        public event EventHandler? EvtPaused;
        public event EventHandler? EvtResume;
        public event EventHandler? EvtStop;
        public event EventHandler? EvtMusicEnded;
        public event EventHandler<Exception>? EvtError;

        public PlaybackState? PlaybackStateProp => _musicControl?.PlaybackStateProp;
        public bool IsPlaying => _musicControl?.IsPlaying ?? false;
        public bool IsPaused => _musicControl?.IsPaused ?? false;
        public bool IsStoped => _musicControl?.IsStoped ?? true;
        public bool IsValid => _musicControl?.IsValid ?? false;

        public TimeSpan MusicDuration => _musicControl?.TotalTime ?? TimeSpan.Zero;
        public TimeSpan CurrentTime => _musicControl?.GetCurrentTime() ?? TimeSpan.Zero;
        public AudioFileReader? AudioFileReaderProp => _musicControl?.AudioFile;

        public PlayerControl(string musicPath)
        {
            if (string.IsNullOrEmpty(musicPath))
                throw new ArgumentNullException(nameof(musicPath));

            if (!File.Exists(musicPath))
                throw new FileNotFoundException("Arquivo não encontrado", musicPath);

            try
            {
                _musicControl = new MusicControl(musicPath);
                
                if (!_musicControl.IsValid)
                    throw new InvalidOperationException("Não foi possível inicializar o arquivo de áudio");

                // ✅ Registra eventos
                RegisterEvents();

                Log.Information("PlayerControl inicializado: {Path}", musicPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao criar PlayerControl para: {Path}", musicPath);
                _musicControl?.Dispose();
                _musicControl = null;
                throw;
            }
        }

        private void RegisterEvents()
        {
            if (_musicControl == null) return;

            _musicControl.EvtPlaying += (s, e) => EvtPlaying?.Invoke(s, e);
            _musicControl.EvtPaused += (s, e) => EvtPaused?.Invoke(s, e);
            _musicControl.EvtResume += (s, e) => EvtResume?.Invoke(s, e);
            _musicControl.EvtStop += (s, e) => EvtStop?.Invoke(s, e);
        }

        private void UnregisterEvents()
        {
            if (_musicControl == null) return;

            _musicControl.EvtPlaying -= (s, e) => EvtPlaying?.Invoke(s, e);
            _musicControl.EvtPaused -= (s, e) => EvtPaused?.Invoke(s, e);
            _musicControl.EvtResume -= (s, e) => EvtResume?.Invoke(s, e);
            _musicControl.EvtStop -= (s, e) => EvtStop?.Invoke(s, e);
        }

        public void Play()
        {
            if (!IsValid || _musicControl == null) return;

            if (IsPlaying) Stop();

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                try
                {
                    _musicControl.Play();

                    while ((IsPlaying || IsPaused) 
                        && !_cts.Token.IsCancellationRequested 
                        && _musicControl.GetProgress() < 100.0)
                    {
                        if (!IsPaused && IsPlaying)
                        {
                            EvtProgressUpdated?.Invoke(this, _musicControl.GetProgress());
                        }
                        
                        await Task.Delay(200, _cts.Token);
                    }

                    // ✅ Atualiza progresso final
                    if (!_cts.Token.IsCancellationRequested)
                    {
                        EvtProgressUpdated?.Invoke(this, _musicControl.GetProgress());
                        EvtMusicEnded?.Invoke(this, EventArgs.Empty);
                    }
                }
                catch (TaskCanceledException)
                {
                    // Normal quando Stop() é chamado
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Erro durante reprodução");
                    EvtError?.Invoke(this, ex);
                }
            }, _cts.Token);
        }

        public void Pause()
        {
            if (!IsValid || !IsPlaying) return;
            _musicControl?.Pause();
        }

        public void Resume()
        {
            if (!IsValid || !IsPaused) return;
            _musicControl?.Resume();
        }

        public void Stop()
        {
            if (!IsValid) return;
            
            _cts?.Cancel();
            _musicControl?.Stop();
        }

        public void Seek(TimeSpan time)
        {
            if (!IsValid) return;
            _musicControl?.Seek(time);
        }

        public void SetPosition(int position)
        {
            if (!IsValid) return;
            _musicControl?.SetPosition(position);
        }

        public void SetPercent(double percent)
        {
            if (!IsValid) return;
            _musicControl?.SetPercent(percent);
        }

        public void Dispose()
        {
            if (_disposed) return;

            Stop();
            UnregisterEvents();
            
            _cts?.Cancel();
            _cts?.Dispose();
            _musicControl?.Dispose();

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}

/*
public event EventHandler<double> ProgressUpdated;
ProgressUpdated?.Invoke(this, progress);
player.ProgressUpdated += (s, progress) => { Console.WriteLine($"Progresso consultado: {progress:F2}%"); };
*/