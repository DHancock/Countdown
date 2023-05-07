using Countdown.ViewModels;
using Countdown.Utils;

namespace Countdown.Views;

internal class AudioHelper
{
    private const uint cSampleRate = 44100;
    private const uint cChannelCount = 2;
    private const uint cBitRate = 224000;
    private const uint cSamplesPerFrame = 1152; 
    private const uint cPadding = 0;
    private const uint cMinFrameSize = ((cSamplesPerFrame * cBitRate) / (cSampleRate * 8)) + cPadding;

    private readonly Stream? audioStream;
    private readonly MediaStreamSource? mediaStreamSource;
    private readonly MediaPlayer mediaPlayer = new MediaPlayer();

    private DateTime startTime;
    private readonly TimeSpan frameDuration = TimeSpan.FromSeconds(cSamplesPerFrame / (double)cSampleRate);
    
    private int frameIndex;
    private record FrameData(uint Position, uint Size);
    private readonly List<FrameData> frames = new List<FrameData>();


    public AudioHelper()
    { 
        audioStream = LoadEmbeddedResource();

        if (audioStream is not null)
        {
            ParseAudioFrames();

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
            frameIndex = 0;
            mediaPlayer.Position = TimeSpan.Zero;

            mediaPlayer.Play();
        }
    }

    public void Stop() => mediaPlayer.Pause();

    private void MediaStreamSource_SampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
    {
        Debug.Assert(audioStream is not null);

        if (frameIndex < frames.Count)
        {
            MediaStreamSourceSampleRequest request = args.Request;
            MediaStreamSourceSampleRequestDeferral deferal = request.GetDeferral();

            int frameSize = (int)frames[frameIndex].Size;
            byte[] buffer = new byte[frameSize];

            audioStream.ReadAll(buffer, frameSize);

            request.Sample = MediaStreamSample.CreateFromBuffer(buffer.AsBuffer(), frameIndex * frameDuration);
            request.Sample.Duration = frameDuration;

            ++frameIndex;

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

        // restart from the closest frame boundary
        TimeSpan elapsedTime = DateTime.UtcNow - startTime;
        frameIndex = (int)Math.Round(elapsedTime / frameDuration);

        if (frameIndex < frames.Count)
            audioStream.Position = frames[frameIndex].Position;

        args.Request.SetActualStartPosition(frameDuration * frameIndex);
    }

    private static Stream? LoadEmbeddedResource()
    {
        Stream? stream = typeof(App).Assembly.GetManifestResourceStream("Countdown.Resources.audio.dat");
        Debug.Assert(stream is not null);
        return stream;
    }

    private void ParseAudioFrames()
    {
        Debug.Assert(audioStream is not null);
        byte[] buffer = new byte[4];

        int ReadFramePaddingBit()
        {
            if (audioStream.ReadAll(buffer, 4) == 4)
            {
                // frame sync + mpeg version 1 + layer 3
                if ((buffer[0] == 0xFF) && ((buffer[1] & 0xFA) == 0xFA))
                    return buffer[2] & 0x02;
            }

            return -1;
        }

        int padding;
        uint position = 0;
        uint size;

        while ((padding = ReadFramePaddingBit()) >= 0)
        {
            if (padding > 0)
                size = cMinFrameSize + 1;
            else
                size = cMinFrameSize;

            frames.Add(new FrameData(position, size));

            position += size;
            audioStream.Position = position;
        }
    }
}

