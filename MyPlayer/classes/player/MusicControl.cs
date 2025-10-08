using NAudio.Wave;

namespace MyPlayer.classes.player
{
    public class MusicControl : IDisposable
    {

        public bool IsPlaying { get; private set; } = false;
        public bool IsPaused { get; private set; } = false;

        private AudioFileReader? audioFile { get; set; }
        private WaveOutEvent? waveOutEvent { get; set; }
        public bool IsValid => audioFile != null && waveOutEvent != null;

        public TimeSpan TotalTime { get; private set; }

        private bool disposed = false;

        public event EventHandler? EvtPlaying;
        public event EventHandler? EvtPaused;
        public event EventHandler? EvtResume;
        public event EventHandler? EvtStop;


        public MusicControl(string musicPath)
        {
            if (string.IsNullOrEmpty(musicPath) || !File.Exists(musicPath)) { return; }
            audioFile = new AudioFileReader(musicPath);
            waveOutEvent = new WaveOutEvent();
            if (!IsValid) { return; }
            waveOutEvent.Init(audioFile);
            IsPlaying = IsPaused = false;

            TotalTime = audioFile.TotalTime;
            waveOutEvent.PlaybackStopped += (s, e) =>
            {
                IsPlaying = false;
                IsPaused = false;
            };
        }


        public double GetProgress()
        {
            if (!IsValid || TotalTime.TotalSeconds <= 0) return 0.0;
            return (audioFile!.CurrentTime.TotalSeconds / TotalTime.TotalSeconds) * 100;
        }

        public TimeSpan GetCurrentTime() => !IsValid ? TimeSpan.Zero : audioFile!.CurrentTime;
        public long GetMaxPosition() => !IsValid ? 0 : audioFile!.Length;

        #region music control

        public void Play()
        {
            if (!IsValid) { return; }
            waveOutEvent!.Play();
            IsPlaying = true;
            IsPaused = false;

            EvtPlaying?.Invoke(this, EventArgs.Empty);
        }

        public void Pause()
        {
            if (!IsValid || !IsPlaying) { return; }
            waveOutEvent!.Pause();
            IsPaused = true;

            EvtPaused?.Invoke(this, EventArgs.Empty);
        }

        public void Resume()
        {
            if (!IsValid || !IsPaused) { return; }
            waveOutEvent!.Play();
            IsPaused = false;

            EvtResume?.Invoke(this, EventArgs.Empty);
        }

        public void Stop()
        {
            if (!IsValid) { return; }
            waveOutEvent!.Stop();
            audioFile!.Position = 0;
            IsPlaying = false;
            IsPaused = false;

            EvtStop?.Invoke(this, EventArgs.Empty);
        }

        public void Seek(TimeSpan time)
        {
            if (!IsValid) { return; }
            time = time < TimeSpan.Zero ? TimeSpan.Zero : time;
            time = time > audioFile!.TotalTime ? audioFile.TotalTime : time;
            audioFile!.CurrentTime = time;
        }

        /**
        ver GetMaxPosition()
        */
        public void SetPosition(long position)
        {
            if (!IsValid || position < 0) { return; }
            position = Math.Min(position, audioFile!.Length);
            audioFile!.Position = position;
        }

        public void SetPercent(double percent)
        {
            if (!IsValid) return;
            percent = Math.Clamp(percent, 0, 100);
            var targetTime = TimeSpan.FromSeconds(TotalTime.TotalSeconds * (percent / 100.0));
            // Garante que o tempo não ultrapasse o total
            audioFile!.CurrentTime = targetTime > TotalTime ? TotalTime : targetTime;
        }

        #endregion

        #region dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Libera recursos gerenciados
                waveOutEvent?.Dispose();
                audioFile?.Dispose();
            }

            disposed = true;
        }
        #endregion

    }
}
