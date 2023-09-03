using Countdown.ViewModels;
using Countdown.Utils;

namespace Countdown.Views;

internal class AudioHelper
{
    private const int cSampleRate = 44100;
    private const int cChannelCount = 2;
    private const int cBitRate = 224000;
    private const int cSamplesPerFrame = 1152;
    private const int cPadding = 1;
    private const int cMaxFrameSize = ((cSamplesPerFrame * cBitRate) / (cSampleRate * 8)) + cPadding;
    private const int cFrameHeaderSize = 4;

    private readonly Stream? audioStream;
    private readonly MediaStreamSource? mediaStreamSource;
    private readonly MediaPlayer mediaPlayer = new MediaPlayer();
    private readonly TimeSpan frameDuration = TimeSpan.FromSeconds(cSamplesPerFrame / (double)cSampleRate);

    private int frameIndex;
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
            mediaPlayer.Position = TimeSpan.Zero;
            mediaPlayer.Play();
        }
    }

    public void Stop() => mediaPlayer.Pause();
 
    private void MediaStreamSource_SampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
    {
        Debug.Assert(audioStream is not null);

        byte[] buffer = ArrayPool<byte>.Shared.Rent(cMaxFrameSize);

        int frameSize = ReadFrameSize(buffer);

        if (frameSize > 0)
        {
            int bytesToRead = frameSize - cFrameHeaderSize;

            if (audioStream.ReadAll(buffer, cFrameHeaderSize, bytesToRead) == bytesToRead)
            {
                args.Request.Sample = MediaStreamSample.CreateFromBuffer(buffer.AsBuffer(0, frameSize), frameDuration * frameIndex++);

                args.Request.Sample.Processed += (s, e) =>
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                };
            }
            else
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        else
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private void MediaStreamSource_Starting(MediaStreamSource sender, MediaStreamSourceStartingEventArgs args)
    {
        // This can also be called by the system if global audio properties change
        // such as stereo vs. mono or if the output device is changed.
        // This proportional scheme only works for constant bit rate data.
        Debug.Assert(audioStream is not null);

        // restart from the closest frame boundary
        frameIndex = (int)Math.Round((DateTime.UtcNow - startTime) / frameDuration);

        audioStream.Position = 0;  // assumes all ID3 tags have been removed

        if (frameIndex > 0)
        {
            byte[] buffer = new byte[cFrameHeaderSize];

            for (int frameCount = 0; frameCount < frameIndex; frameCount++)
                audioStream.Position += ReadFrameSize(buffer);
        }

        args.Request.SetActualStartPosition(frameDuration * frameIndex);
    }

    private static Stream? LoadEmbeddedResource()
    {
        Stream? stream = typeof(App).Assembly.GetManifestResourceStream("Countdown.Resources.audio.dat");
        Debug.Assert(stream is not null);
        return stream;
    }

    private int ReadFrameSize(byte[] buffer)
    {
        Debug.Assert(audioStream is not null);
        Debug.Assert(buffer.Length >= cFrameHeaderSize);

        if (audioStream.ReadAll(buffer, 0, cFrameHeaderSize) == cFrameHeaderSize)
        {
            // frame sync + mpeg version 1 + layer 3 + no CRC
            Debug.Assert((buffer[0] == 0xFF) && ((buffer[1] & 0xFB) == 0xFB));

            if ((buffer[2] & 0x02) == 0x02)
                return cMaxFrameSize;

            return cMaxFrameSize - 1;
        }

        return 0;
    }
}

