using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using NAudio.Wave;
using SkiaSharp;

namespace MyPlayer.classes.waveimage;

internal class WaveImage : IDisposable
{
    private AudioFileReader? _audioFile;
    private SKBitmap? _fullBitmap;
    private readonly int _topHeight;
    private readonly int _bottomHeight;
    private readonly int _width;

    private readonly SKColor primaryColor = new(14, 165, 233);
    private readonly SKColor secondaryColor = new(34, 197, 94);
    private readonly SKColor primaryDarkColor = new(2, 132, 199);
    private readonly SKColor secondaryDarkColor = new(22, 163, 74);

    public WaveImage(AudioFileReader audioFile, Window window, int topHeight = 32, int bottomHeight = 32, int width = 600)
    {
        _audioFile = audioFile;
        _topHeight = Math.Max(32, topHeight);
        _bottomHeight = Math.Max(32, bottomHeight);
        _width = Math.Max(600, width);
    }

    public void Init(Action<Bitmap?> consumer)
    {
        if (_audioFile == null || consumer == null) return;

        int totalHeight = _topHeight + _bottomHeight;
        _fullBitmap = new SKBitmap(_width, totalHeight);

        Task.Run(() =>
        {
            try
            {
                RenderWaveform(_fullBitmap);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao renderizar waveform");
            }

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                consumer(_fullBitmap != null ? SKBitmapToAvaloniaBitmap(_fullBitmap) : null);
            });
        });
    }

    private void RenderWaveform(SKBitmap bitmap)
    {
        if (_audioFile == null) return;

        _audioFile.Position = 0;
        int totalHeight = _topHeight + _bottomHeight;
        int channels = _audioFile.WaveFormat.Channels;
        int bytesPerSample = _audioFile.WaveFormat.BitsPerSample / 8;
        long totalSamples = _audioFile.Length / (bytesPerSample * channels);
        int samplesPerPixel = Math.Max(1, (int)(totalSamples / _width));

        int bufferSize = 1024;
        float[] sampleBuffer = new float[bufferSize];

        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        using var topPaint = new SKPaint
        {
            Shader = CreateGradientShader(bitmap.Width, totalHeight, primaryColor, secondaryColor),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true
        };

        using var bottomPaint = new SKPaint
        {
            Shader = CreateGradientShader(bitmap.Width, totalHeight, primaryDarkColor, secondaryDarkColor),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true
        };

        int midY = _topHeight;
        int samplesPerColumn = Math.Max(1, samplesPerPixel);

        for (int x = 0; x < _width; x++)
        {
            float maxSample = 0f;
            int samplesTotal = 0;

            while (samplesTotal < samplesPerColumn)
            {
                int toRead = Math.Min(bufferSize, samplesPerColumn - samplesTotal);
                int read = _audioFile.Read(sampleBuffer, 0, toRead * channels);
                if (read == 0) break;

                for (int i = 0; i < read; i++)
                {
                    float absSample = Math.Abs(sampleBuffer[i]);
                    if (absSample > maxSample)
                        maxSample = absSample;
                }
                samplesTotal += read / channels;
            }

            if (maxSample > 0)
            {
                float peakHeight = maxSample * _topHeight * 0.9f;
                canvas.DrawLine(x, midY - peakHeight, x, midY, topPaint);
                canvas.DrawLine(x, midY, x, midY + peakHeight, bottomPaint);
            }
        }

        _audioFile.Position = 0;
    }

    private static SKShader CreateGradientShader(int width, int height, SKColor startColor, SKColor endColor)
    {
        return SKShader.CreateLinearGradient(
            new SKPoint(0, 0),
            new SKPoint(width, height),
            [startColor, endColor],
            [0f, 1f],
            SKShaderTileMode.Clamp);
    }

    public Bitmap? GetUpdateImage()
    {
        if (_audioFile == null || _fullBitmap == null) return null;

        double progress = (_audioFile.Position * _fullBitmap.Width) / (double)_audioFile.Length;
        int pos = Math.Clamp((int)progress, 0, _fullBitmap.Width);

        var bitmap = _fullBitmap.Copy();
        using var canvas = new SKCanvas(bitmap);

        using var grayPaint = new SKPaint
        {
            Color = new SKColor(128, 128, 128, 200),
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRect(pos, 0, bitmap.Width - pos, bitmap.Height, grayPaint);

        return SKBitmapToAvaloniaBitmap(bitmap);
    }

    public void ClickPictureBox(int mouseX, int imageWidth)
    {
        if (_audioFile == null || imageWidth <= 0) return;

        double position = (mouseX * _audioFile.Length) / (double)imageWidth;
        _audioFile.Position = Math.Min(_audioFile.Length, Convert.ToInt64(position));
    }

    private static unsafe Bitmap SKBitmapToAvaloniaBitmap(SKBitmap skBitmap)
    {
        var size = new PixelSize(skBitmap.Width, skBitmap.Height);
        var dpi = new Vector(96, 96);
        var writableBitmap = new WriteableBitmap(size, dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);

        using var locked = writableBitmap.Lock();

        byte[] sourceBytes = skBitmap.Bytes;
        Marshal.Copy(sourceBytes, 0, locked.Address, sourceBytes.Length);

        return writableBitmap;
    }

    public void Dispose()
    {
        _fullBitmap?.Dispose();
        _fullBitmap = null;
        _audioFile = null;
    }
}
