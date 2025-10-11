namespace MyPlayer.classes.player
{
    public class PlayerControl : IDisposable
    {
        private MusicControl musicControl;
        private CancellationTokenSource? cts;

        public event EventHandler<double>? EvtProgressUpdated;
        public event EventHandler? EvtPlaying;
        public event EventHandler? EvtPaused;
        public event EventHandler? EvtResume;
        public event EventHandler? EvtStop;
        public event EventHandler<EstadoPlayer>? EstadoAlterado;

        public enum EstadoPlayer : ushort
        {
            Play = 1,
            Pause = 2,
            Stop = 3,
            MusicaFinalizada = 4
        }

        public bool IsPlayingMusicControl => musicControl.IsPlaying;
        public bool IsPaused => musicControl.IsPaused;

        public bool IsPlaying => EstadoPlayerProp == EstadoPlayer.Play || EstadoPlayerProp == EstadoPlayer.MusicaFinalizada;

        public EstadoPlayer EstadoPlayerProp { get; set; } = EstadoPlayer.Stop;

        public TimeSpan MusicDuration => musicControl?.TotalTime ?? TimeSpan.Zero;
        public TimeSpan CurrentTime => musicControl?.GetCurrentTime() ?? TimeSpan.Zero;


        public PlayerControl(string musicPath)
        {
            if (string.IsNullOrEmpty(musicPath) || !File.Exists(musicPath))
                throw new ArgumentException("Arquivo inválido.", nameof(musicPath));

            musicControl = new MusicControl(musicPath);
            musicControl.EvtPlaying += (s, e) => EvtPlaying?.Invoke(s, e);
            musicControl.EvtPaused += (s, e) => EvtPaused?.Invoke(s, e);
            musicControl.EvtResume += (s, e) => EvtResume?.Invoke(s, e);
            musicControl.EvtStop += (s, e) => EvtStop?.Invoke(s, e);

            if (!musicControl.IsValid)
                throw new InvalidOperationException("Não foi possível inicializar o arquivo de áudio.");
        }

        public void Play()
        {
            if (!musicControl.IsValid) return;

            if (IsPlayingMusicControl) Stop(); // garante que não há múltiplas execuções

            cts = new CancellationTokenSource();

            SetEstado(EstadoPlayer.Play);

            Task.Run(async () =>
            {
                musicControl.Play();

                try
                {
                    while (musicControl.IsPlaying && !cts.Token.IsCancellationRequested)
                    {
                        EvtProgressUpdated?.Invoke(this, musicControl.GetProgress());
                        await Task.Delay(200, cts.Token); // atualiza a cada 200ms
                    }

                    // Atualiza progresso final
                    EvtProgressUpdated?.Invoke(this, musicControl.GetProgress());
                }
                catch (TaskCanceledException)
                {
                    // Thread cancelada, ignora
                }
                finally
                {
                    SetEstado(EstadoPlayer.MusicaFinalizada);
                }
            });
        }

        public void Pause()
        {
            if (!musicControl.IsValid || !IsPlayingMusicControl) return;
            musicControl.Pause();
            SetEstado(EstadoPlayer.Pause);
        }

        public void Resume()
        {
            if (!musicControl.IsValid || !IsPaused) return;
            musicControl.Resume();
            SetEstado(EstadoPlayer.Play);
        }

        public void Stop()
        {
            if (!musicControl.IsValid) return;
            musicControl.Stop();
            cts?.Cancel();
            SetEstado(EstadoPlayer.Stop);
        }

        public void Seek(TimeSpan time)
        {
            if (!musicControl.IsValid) return;
            musicControl.Seek(time);
        }

        public void SetPosition(int position)
        {
            if (!musicControl.IsValid) return;
            musicControl.SetPosition(position);
        }

        public void SetPercent(double percent)
        {
            if (!musicControl.IsValid) return;
            musicControl.SetPercent(percent);
        }

        private void SetEstado(EstadoPlayer novo)
        {
            if (EstadoPlayerProp == novo) { return; }
            EstadoPlayerProp = novo;
            EstadoAlterado?.Invoke(this, novo);
        }

        public void Dispose()
        {
            Stop();
            musicControl.Dispose();
        }

    }
}

/*
public event EventHandler<double> ProgressUpdated;
ProgressUpdated?.Invoke(this, progress);
player.ProgressUpdated += (s, progress) => { Console.WriteLine($"Progresso consultado: {progress:F2}%"); };
*/