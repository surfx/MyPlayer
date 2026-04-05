using NAudio.Wave;
using NAudio.WaveFormRenderer;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MyPlayer.classes.waveimage
{
    internal class WaveImage : IDisposable
    {
        private WaveFormRenderer? waveFormRenderer;
        private Image? image;
        private AudioFileReader? audioFile;

        private readonly Color primaryColor = Color.FromArgb(14, 165, 233);
        private readonly Color secondaryColor = Color.FromArgb(34, 197, 94);
        private readonly Color primaryDarkColor = Color.FromArgb(2, 132, 199);
        private readonly Color secondaryDarkColor = Color.FromArgb(22, 163, 74);

        private int tHeigth;
        private int bHeigth;
        private int width;

        public WaveImage(AudioFileReader audioFile, int tHeigth = 32, int bHeigth = 32, int width = 600)
        {
            this.audioFile = audioFile;
            this.waveFormRenderer = new();
            this.tHeigth = Math.Max(32, tHeigth);
            this.bHeigth = Math.Max(32, bHeigth);
            this.width = Math.Max(600, width);
        }

        private IPeakProvider getPeakProvider() => new SamplingPeakProvider(200);

        private WaveFormRendererSettings GetRendererSettings()
        {
            var settings = new StandardWaveFormRendererSettings() 
            { 
                Name = "Standard",
                TopHeight = tHeigth,
                BottomHeight = bHeigth,
                Width = width,
                DecibelScale = false,
                BackgroundColor = Color.Transparent
            };

            var brushRect = new Rectangle(0, 0, width, tHeigth + bHeigth);
            settings.TopPeakPen = new Pen(new LinearGradientBrush(brushRect, primaryColor, secondaryColor, 135f));
            settings.BottomPeakPen = new Pen(new LinearGradientBrush(brushRect, primaryDarkColor, secondaryDarkColor, 135f));
            
            return settings;
        }

        private void RenderWaveform(AudioFileReader waveStream, Action<Image?> consumer)
        {
            if (waveStream == null) return;

            var settings = GetRendererSettings();
            var peakProvider = getPeakProvider();

            using var ar = new AudioFileReader(waveStream.FileName);
            Task.Factory.StartNew(() =>
            {
                Image? renderedImage = null;
                try { renderedImage = waveFormRenderer?.Render(ar, peakProvider, settings); }
                catch (Exception e) { Console.WriteLine(e); }
                consumer(renderedImage);
            });
        }

        public void init(Action<Image?> consumer, bool grayScale = true)
        {
            if (audioFile == null || consumer == null) return;

            RenderWaveform(audioFile, (renderedImage) =>
            {
                this.image = renderedImage;
                if (renderedImage == null) return;
                consumer(grayScale ? imageGrayScale(0) : renderedImage);
            });
        }

        private Image? imageGrayScale(int pos)
        {
            if (image == null) return null;
            if (pos < 0 || pos > image.Width) return image;

            var myBitmap = new Bitmap(image);
            for (int x = pos; x < myBitmap.Width; x++)
            {
                for (int y = 0; y < myBitmap.Height; y++)
                {
                    var c = myBitmap.GetPixel(x, y);
                    if (c.A == 0 && c.R == 0 && c.G == 0 && c.B == 0) continue;
                    myBitmap.SetPixel(x, y, Color.Gray);
                }
            }
            return myBitmap;
        }

        public Image? getUpdateImage()
        {
            if (audioFile == null || image == null) return image;
            
            double conversao = (audioFile.Position * image.Width) / audioFile.Length;
            return imageGrayScale((int)conversao);
        }

        public void Dispose()
        {
            waveFormRenderer = null;
            image = null;
            audioFile = null;
        }
    }
}
