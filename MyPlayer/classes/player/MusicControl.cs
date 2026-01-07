using NAudio.Wave;

namespace MyPlayer.classes.player
{
    public class MusicControl : IDisposable
    {

        public AudioFileReader? AudioFile { get; set; }
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
            if (string.IsNullOrEmpty(musicPath) || !File.Exists(musicPath)) { return; }
            AudioFile = new AudioFileReader(musicPath);
            _waveOutEvent = new WaveOutEvent();
            if (!IsValid) { return; }
            _waveOutEvent.Init(AudioFile);
            //IsPlaying = IsPaused = false;

            TotalTime = AudioFile.TotalTime;
            _waveOutEvent.PlaybackStopped += (s, e) =>
            {
                //IsPlaying = false;
                //IsPaused = false;
                EvtStop?.Invoke(this, e);
            };
        }


        public double GetProgress()
        {
            if (!IsValid || TotalTime.TotalSeconds <= 0) return 0.0;
            return (AudioFile!.CurrentTime.TotalSeconds / TotalTime.TotalSeconds) * 100;
        }

        public TimeSpan GetCurrentTime() => !IsValid ? TimeSpan.Zero : AudioFile!.CurrentTime;
        public long GetMaxPosition() => !IsValid ? 0 : AudioFile!.Length;

        #region music control

        public void Play()
        {
            if (!IsValid) { return; }
            _waveOutEvent!.Play();
            //IsPlaying = true;
            //IsPaused = false;

            EvtPlaying?.Invoke(this, EventArgs.Empty);
        }

        public void Pause()
        {
            if (!IsValid || !IsPlaying) { return; }
            _waveOutEvent!.Pause();
            //IsPaused = true;
            //IsPlaying = false;

            EvtPaused?.Invoke(this, EventArgs.Empty);
        }

        public void Resume()
        {
            if (!IsValid || !IsPaused) { return; }
            _waveOutEvent!.Play();
            //IsPaused = false;
            //IsPlaying = true;

            EvtResume?.Invoke(this, EventArgs.Empty);
        }

        public void Stop()
        {
            //IsPlaying = false;
            //IsPaused = false;

            if (!IsValid) { return; }
            if (_waveOutEvent != null) _waveOutEvent.Stop();
            try { if (AudioFile != null && AudioFile.Position != null) AudioFile.Position = 0; } catch (Exception) { }

            EvtStop?.Invoke(this, EventArgs.Empty);
        }

        public void Seek(TimeSpan time)
        {
            if (!IsValid) { return; }
            time = time < TimeSpan.Zero ? TimeSpan.Zero : time;
            time = time > AudioFile!.TotalTime ? AudioFile.TotalTime : time;
            AudioFile!.CurrentTime = time;
        }

        /**
        ver GetMaxPosition()
        */
        public void SetPosition(long position)
        {
            if (!IsValid || position < 0) { return; }
            position = Math.Min(position, AudioFile!.Length);
            AudioFile!.Position = position;
        }

        public void SetPercent(double percent)
        {
            if (!IsValid || AudioFile == null) return;
            percent = Math.Clamp(percent, 0, 100);
            var targetTime = TimeSpan.FromSeconds(TotalTime.TotalSeconds * (percent / 100.0));
            // Garante que o tempo não ultrapasse o total
            AudioFile.CurrentTime = targetTime > TotalTime ? TotalTime : targetTime;
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
                _waveOutEvent?.Dispose();
                AudioFile?.Dispose();
            }

            disposed = true;
        }
        #endregion

    }
}
