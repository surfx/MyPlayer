using NAudio.Wave;
using NAudio.WaveFormRenderer;

namespace MyPlayer.classes.waveimage
{
    /// <summary>
    /// desenha a wavelength e atualiza de acordo com o passar da música
    /// </summary>
    internal class WaveImage : IDisposable
    {

        private WaveFormRenderer waveFormRenderer;
        private Image? image;
        private AudioFileReader audioFile;
        private readonly Form frm;

        private readonly Color cor1 = Color.BlueViolet; //Color.DarkGreen
        private readonly Color cor2 = Color.RebeccaPurple; //Color.Green

        private int tHeigth;
        private int bHeigth;
        private int width;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="audioFile"></param>
        /// <param name="frm"></param>
        /// <param name="tHeigth">altura da imagem deve ser: tHeigth + bHeigth</param>
        /// <param name="bHeigth"></param>
        /// <param name="width">width da imagem</param>
        public WaveImage(AudioFileReader audioFile, Form frm, int tHeigth = 32, int bHeigth = 32, int width = 600)
        {
            this.audioFile = audioFile;
            this.frm = frm;
            this.waveFormRenderer = new();
            this.tHeigth = Math.Max(32, tHeigth);
            this.bHeigth = Math.Max(32, bHeigth);
            this.width = Math.Max(600, width);
        }

        private IPeakProvider getPeakProvider()
        {
            //return new MaxPeakProvider();
            //return new RmsPeakProvider(200);
            return new SamplingPeakProvider(200);
            //return new AveragePeakProvider(4);
        }

        private WaveFormRendererSettings GetRendererSettings()
        {
            WaveFormRendererSettings settings = new StandardWaveFormRendererSettings() { Name = "Standard" };
            settings.TopHeight = (int)tHeigth;
            settings.BottomHeight = (int)bHeigth;
            settings.Width = (int)width;
            settings.DecibelScale = false;
            settings.BackgroundColor = Color.Transparent;
            settings.TopPeakPen = new Pen(cor1);
            settings.BottomPeakPen = new Pen(cor2);
            //if (imageFile != null) { settings.BackgroundImage = new Bitmap(imageFile); }
            return settings;
        }

        private void RenderWaveform(AudioFileReader waveStream, Action<Image?> consumer)
        {
            if (waveStream == null) return;

            var settings = GetRendererSettings();
            IPeakProvider peakProvider = getPeakProvider();

            using (AudioFileReader ar = new(waveStream.FileName))
            {
                Task.Factory.StartNew(() =>
                {
                    Image? image = null;
                    try
                    {
                        image = waveFormRenderer.Render(ar, peakProvider, settings);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    if (frm == null)
                    {
                        consumer(image); return;
                    }

                    frm.BeginInvoke(() => { consumer(image); });

                });
            }

        }

        /// <summary>
        /// retorna a imagem completa do gráfico da música. Padrão: tons de cinza
        /// </summary>
        /// <param name="consumer"></param>
        /// <param name="grayScale"></param>
        public void init(Action<Image?> consumer, bool grayScale = true)
        {
            if (audioFile == null || consumer == null) { return; }

            RenderWaveform(audioFile, (image) =>
            {
                this.image = image;
                if (image == null) { return; }

                // convert all to gray ?
                consumer(grayScale ? imageGrayScale(0) : image);
            });
        }

        /// <summary>
        /// conversão da imagem para tons de cinza
        /// ignora trechos transparentes
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private Image? imageGrayScale(int pos)
        {
            if (image == null || pos < 0 || pos > image.Width) { return image; }

            Bitmap myBitmap = new(image);
            for (int x = pos; x < myBitmap.Width; x++)
            {
                for (int y = 0; y < myBitmap.Height; y++)
                {
                    Color c = myBitmap.GetPixel(x, y);
                    if (c.A == 0 && c.R == 0 && c.G == 0 && c.B == 0) { continue; } // transparente
                    myBitmap.SetPixel(x, y, Color.Gray);
                }
            }
            return myBitmap;
        }

        /// <summary>
        /// retorna a imagem atualizada
        /// tons de cinza: parte da música não executada ainda
        /// </summary>
        /// <returns></returns>
        public Image? getUpdateImage()
        {
            if (audioFile == null || image == null) { return image; }
            double conversao = (audioFile.Position * image.Width) / audioFile.Length;
            return imageGrayScale((int)conversao);
        }

        #region picturebox click
        public void clickPictureBox(EventArgs e, Image image)
        {
            if (audioFile == null || e == null || image == null || image.Width <= 0) { return; }
            MouseEventArgs me = (MouseEventArgs)e;
            if (me == null || me.Location == null) { return; }
            Point coordinates = me.Location;
            if (coordinates == null) { return; }

            double position = (coordinates.X * audioFile.Length) / image.Width;
            audioFile.Position = Math.Min(audioFile.Length, Convert.ToInt64(position));
        }
        #endregion

        public void Dispose()
        {
            waveFormRenderer = null;
            image = null;
            audioFile = null;
        }
    }
}
