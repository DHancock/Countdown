using Countdown.ViewModels;

namespace Countdown.Views;

internal class AudioHelper
{
    private const uint cSampleRate = 44100;
    private const uint cChannelCount = 2;
    private const uint cBitRate = 224000;
    private const uint cSamplesPerFrame = 1152; 
    private const uint cPadding = 0;
    private const uint cFrameSize = ((cSamplesPerFrame * cBitRate) / (cSampleRate * 8)) + cPadding;

    private readonly Stream? audioStream;
    private readonly MediaStreamSource? mediaStreamSource;
    private readonly MediaPlayer mediaPlayer = new MediaPlayer();

    private readonly TimeSpan frameDuration = TimeSpan.FromSeconds(cSamplesPerFrame / (double)cSampleRate);
    private TimeSpan timestamp;
    private DateTime startTime;

    public AudioHelper()
    { 
        audioStream = LoadEmbeddedResource();

        if (audioStream is not null)
        {
            AudioEncodingProperties audioProps = AudioEncodingProperties.CreateMp3(cSampleRate, cChannelCount, cBitRate);

            mediaStreamSource = new MediaStreamSource(new AudioStreamDescriptor(audioProps));
            mediaStreamSource.CanSeek = true;
            mediaStreamSource.Starting += MediaStreamSource_Starting;
            mediaStreamSource.SampleRequested += MediaStreamSource_SampleRequested;

            mediaPlayer.Source = MediaSource.CreateFromIMediaSource(mediaStreamSource);
            mediaPlayer.Volume = Settings.Data.VolumePercentage / 100.0;

            Settings.Data.VolumeChanged += (s, a) => mediaPlayer.Volume = Settings.Data.VolumePercentage / 100.0;
        }
    }

    public void Start()
    {
        if (audioStream is not null)
        {
            startTime = DateTime.UtcNow;
            timestamp = TimeSpan.Zero;
            mediaPlayer.Position = TimeSpan.Zero;

            mediaPlayer.Play();
        }
    }

    public void Stop() => mediaPlayer.Pause();

    private void MediaStreamSource_SampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
    {
        Debug.Assert(audioStream is not null);

        if (audioStream.Position < audioStream.Length)
        {
            MediaStreamSourceSampleRequest request = args.Request;
            MediaStreamSourceSampleRequestDeferral deferal = request.GetDeferral();

            byte[] buffer = new byte[cFrameSize];
            audioStream.Read(buffer, 0, (int)cFrameSize);

            request.Sample = MediaStreamSample.CreateFromBuffer(buffer.AsBuffer(), timestamp);
            request.Sample.Duration = frameDuration;

            timestamp += frameDuration;

            deferal.Complete();
        }
    }

    private void MediaStreamSource_Starting(MediaStreamSource sender, MediaStreamSourceStartingEventArgs args)
    {
        // This can also be called by the system if global audio properties change
        // such as stereo vs. mono or if the output device is changed.
        // This proportional scheme will only work for constant bit rate data and
        // after any ID3 tags have been removed from the start of the data.
        Debug.Assert(audioStream is not null);

        // restart from the start of the current frame
        TimeSpan elapsedTime = DateTime.UtcNow - startTime;
        int frameCount = (int)Math.Floor(elapsedTime / frameDuration);

        timestamp = frameDuration * frameCount;
        audioStream.Position = cFrameSize * frameCount;

        args.Request.SetActualStartPosition(timestamp);
    }

    private static Stream? LoadEmbeddedResource()
    {
        Stream? stream = typeof(App).Assembly.GetManifestResourceStream("Countdown.Resources.audio.dat");
        Debug.Assert(stream is not null);
        return stream;
    }
}

