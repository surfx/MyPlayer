namespace MyPlayer.classes.player
{
    public class PlayerControl : IDisposable
    {
        private MusicControl musicControl;
        private CancellationTokenSource? cts;

        public event EventHandler<double>? ProgressUpdated;
        public event EventHandler? EvtPlaying;
        public event EventHandler? EvtPaused;
        public event EventHandler? EvtResume;
        public event EventHandler? EvtStop;

        public bool IsPlaying => musicControl.IsPlaying;
        public bool IsPaused => musicControl.IsPaused;

        public TimeSpan MusicDuration => musicControl?.TotalTime ?? TimeSpan.Zero;
        public TimeSpan CurrentTime => musicControl?.GetCurrentTime() ?? TimeSpan.Zero;


        public PlayerControl(string musicPath)
        {
            if (string.IsNullOrEmpty(musicPath) || !File.Exists(musicPath))
                throw new ArgumentException("Arquivo inválido.", nameof(musicPath));

            musicControl = new MusicControl(musicPath);
            if (EvtPlaying != null) musicControl.EvtPlaying += (s, e) => EvtPlaying?.Invoke(s, e);
            if (EvtPaused != null) musicControl.EvtPaused += (s, e) => EvtPaused?.Invoke(s, e);
            if (EvtResume != null) musicControl.EvtResume += (s, e) => EvtResume?.Invoke(s, e);
            if (EvtStop != null) musicControl.EvtStop += (s, e) => EvtStop?.Invoke(s, e);

            if (!musicControl.IsValid)
                throw new InvalidOperationException("Não foi possível inicializar o arquivo de áudio.");
        }

        public void Play()
        {
            if (!musicControl.IsValid) return;

            if (IsPlaying) Stop(); // garante que não há múltiplas execuções

            cts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                musicControl.Play();

                try
                {
                    while (musicControl.IsPlaying && !cts.Token.IsCancellationRequested)
                    {
                        ProgressUpdated?.Invoke(this, musicControl.GetProgress());
                        await Task.Delay(200, cts.Token); // atualiza a cada 200ms
                    }

                    // Atualiza progresso final
                    ProgressUpdated?.Invoke(this, musicControl.GetProgress());
                }
                catch (TaskCanceledException)
                {
                    // Thread cancelada, ignora
                }
            });
        }

        public void Pause()
        {
            if (!musicControl.IsValid || !IsPlaying) return;
            musicControl.Pause();
        }

        public void Resume()
        {
            if (!musicControl.IsValid || !IsPaused) return;
            musicControl.Resume();
        }

        public void Stop()
        {
            if (!musicControl.IsValid) return;

            musicControl.Stop();
            cts?.Cancel();
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