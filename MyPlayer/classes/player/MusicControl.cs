using LibVLCSharp.Shared;
using NAudio.Wave;
using NLayer;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MyPlayer.classes.player
{
    public class MpegWaveStream : WaveStream
    {
        private readonly MpegFile _mpegFile;
        private readonly WaveFormat _waveFormat;

        public MpegWaveStream(string path)
        {
            _mpegFile = new MpegFile(path);
            _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(_mpegFile.SampleRate, _mpegFile.Channels);
        }

        public override WaveFormat WaveFormat => _waveFormat;

        public override long Length => _mpegFile.Length * _mpegFile.Channels * 4;

        public override long Position
        {
            get => _mpegFile.Position * _mpegFile.Channels * 4;
            set => _mpegFile.Position = value / (_mpegFile.Channels * 4);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesToRead = Math.Min(count, (int)(Length - Position));
            if (bytesToRead <= 0) return 0;

            float[] floatBuffer = new float[bytesToRead / 4];
            int samplesRead = _mpegFile.ReadSamples(floatBuffer, 0, floatBuffer.Length);
            Buffer.BlockCopy(floatBuffer, 0, buffer, offset, samplesRead * 4);
            return samplesRead * 4;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _mpegFile.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class MusicControl : IDisposable
    {
        private static readonly Lazy<LibVLC> _libVLC = new(() => new LibVLC("--quiet", "--no-video", "--no-sub-autodetect-file"));
        private static LibVLC LibVLCInstance => _libVLC.Value;

        private MediaPlayer? _mediaPlayer;
        private Media? _media;

        public WaveStream? AudioFile { get; private set; }
        
        public PlaybackState? PlaybackStateProp => _mediaPlayer?.State switch
        {
            VLCState.Playing => PlaybackState.Playing,
            VLCState.Paused => PlaybackState.Paused,
            _ => PlaybackState.Stopped
        };

        public bool IsPlaying => PlaybackStateProp == PlaybackState.Playing;
        public bool IsPaused => PlaybackStateProp == PlaybackState.Paused;
        public bool IsStoped => PlaybackStateProp == PlaybackState.Stopped;

        public bool IsValid => _mediaPlayer != null;

        public TimeSpan TotalTime { get; private set; }

        private bool disposed = false;
        private bool _endedTriggered = false;
        private readonly object _lock = new();

        public event EventHandler? EvtPlaying;
        public event EventHandler? EvtPaused;
        public event EventHandler? EvtResume;
        public event EventHandler? EvtStop;
        public event EventHandler? EvtMusicEnded;

        public MusicControl(string musicPath)
        {
            if (string.IsNullOrEmpty(musicPath))
                throw new ArgumentNullException(nameof(musicPath));

            if (!File.Exists(musicPath))
                throw new FileNotFoundException("Arquivo não encontrado", musicPath);

            try
            {
                if (musicPath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                {
                    AudioFile = new MpegWaveStream(musicPath);
                }
                else
                {
                    AudioFile = new AudioFileReader(musicPath);
                }

                TotalTime = AudioFile.TotalTime;

                _media = new Media(LibVLCInstance, musicPath, FromType.FromPath);
                _mediaPlayer = new MediaPlayer(_media);

                _mediaPlayer.EndReached += (s, e) =>
                {
                    lock (_lock)
                    {
                        if (_endedTriggered) return;
                        _endedTriggered = true;
                    }

                    Task.Run(() => 
                    {
                        EvtStop?.Invoke(this, EventArgs.Empty);
                        EvtMusicEnded?.Invoke(this, EventArgs.Empty);
                    });
                };

                _mediaPlayer.Playing += (s, e) => Task.Run(() => EvtPlaying?.Invoke(this, EventArgs.Empty));
                _mediaPlayer.Paused += (s, e) => Task.Run(() => EvtPaused?.Invoke(this, EventArgs.Empty));
            }
            catch (Exception ex)
            {
                AudioFile?.Dispose();
                _mediaPlayer?.Dispose();
                _media?.Dispose();
                AudioFile = null;
                _mediaPlayer = null;
                _media = null;
                throw new InvalidOperationException($"Erro ao inicializar áudio: {ex.Message}", ex);
            }
        }

        public double GetProgress()
        {
            if (!IsValid || _mediaPlayer == null) return 0.0;
            float pos = _mediaPlayer.Position;
            return pos > 0 ? pos * 100.0 : 0.0;
        }

        public TimeSpan GetCurrentTime()
        {
            if (!IsValid || _mediaPlayer == null) return TimeSpan.Zero;
            long timeMs = _mediaPlayer.Time;
            // LibVLC returns -1 if no media is loaded or if time is unknown
            return timeMs > 0 ? TimeSpan.FromMilliseconds(timeMs) : TimeSpan.Zero;
        }

        public long GetMaxPosition() => AudioFile?.Length ?? 0;

        #region music control

        public void Play()
        {
            if (!IsValid) return;
            _mediaPlayer?.Play();
        }

        public void Pause()
        {
            if (!IsValid || !IsPlaying) return;
            _mediaPlayer?.Pause();
        }

        public void Resume()
        {
            if (!IsValid || !IsPaused) return;
            _mediaPlayer?.Play();
            EvtResume?.Invoke(this, EventArgs.Empty);
        }

        public void Stop()
        {
            if (!IsValid) return;
            _mediaPlayer?.Stop();
            EvtStop?.Invoke(this, EventArgs.Empty);
        }

        public void Seek(TimeSpan time)
        {
            if (!IsValid || _mediaPlayer == null) return;
            _mediaPlayer.Time = (long)time.TotalMilliseconds;
        }

        public void SetPosition(long position)
        {
            if (!IsValid || _mediaPlayer == null || AudioFile == null) return;
            float percent = (float)position / AudioFile.Length;
            _mediaPlayer.Position = Math.Clamp(percent, 0f, 1f);
        }

        public void SetPercent(double percent)
        {
            if (!IsValid || _mediaPlayer == null) return;
            _mediaPlayer.Position = (float)(Math.Clamp(percent, 0, 100) / 100.0);
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
            if (disposed) return;

            if (disposing)
            {
                try
                {
                    if (_mediaPlayer != null)
                    {
                        _mediaPlayer.Stop();
                        _mediaPlayer.Dispose();
                    }
                    _media?.Dispose();
                    AudioFile?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao dispor MusicControl: {ex.Message}");
                }
            }

            disposed = true;
        }

        #endregion
    }
}
