using NAudio.Wave;

namespace NAudio.WaveFormRenderer
{
    public abstract class PeakProvider : IPeakProvider
    {
        protected ISampleProvider Provider { get; private set; } = default!;
        protected int SamplesPerPeak { get; private set; }
        protected float[] ReadBuffer { get; private set; } = default!;

        public void Init(ISampleProvider provider, int samplesPerPeak)
        {
            Provider = provider;
            SamplesPerPeak = samplesPerPeak;
            ReadBuffer = new float[samplesPerPeak];
        }

        public abstract PeakInfo GetNextPeak();
    }
}