// based on https://github.com/microsoftarchive/msdn-code-gallery-microsoft/blob/master/Official%20Windows%20Platform%20Sample/MediaStreamSource%20streaming%20sample/%5BC%23%5D%5BC%2B%2B%5D-MediaStreamSource%20streaming%20sample/C%23%20and%20C%2B%2B/Shared/Scenario1_LoadMP3FileForAudioStream.xaml.cs

using Countdown.ViewModels;

namespace Countdown.Views;

internal class AudioHelper
{
    private const uint cSampleRate = 44100;
    private const uint cChannelCount = 2;
    private const uint cBitRate = 224000;
    private const int cSampleSize = 1260;
    private readonly TimeSpan sampleDuration = TimeSpan.FromMilliseconds(70);
    private readonly TimeSpan songDuration = TimeSpan.FromTicks(308244642);

    private readonly Stream? audioStream;
    private readonly MediaPlayer mediaPlayer = new MediaPlayer();

    private long byteOffset = 0;
    private TimeSpan timeOffset = TimeSpan.Zero;
    private MediaStreamSource? mediaStreamSource = null;

    public AudioHelper()
    {
        audioStream = LoadEmbeddedResource();

        if (audioStream is not null)
        {
            AudioEncodingProperties audioProps = AudioEncodingProperties.CreateMp3(cSampleRate, cChannelCount, cBitRate);

            mediaStreamSource = new MediaStreamSource(new AudioStreamDescriptor(audioProps));
            mediaStreamSource.CanSeek = true;
            mediaStreamSource.Duration = songDuration;

            mediaStreamSource.Starting += MediaStreamSource_Starting;
            mediaStreamSource.SampleRequested += MediaStreamSource_SampleRequested;
            mediaStreamSource.Closed += MediaStreamSource_Closed;

            mediaPlayer.Source = MediaSource.CreateFromIMediaSource(mediaStreamSource);
        }
    }

    public void Start() => mediaPlayer.Play();

    public void Stop()
    {
        mediaPlayer.Pause();
        mediaPlayer.Position = TimeSpan.Zero;
    }

    private void MediaStreamSource_Closed(Windows.Media.Core.MediaStreamSource sender, MediaStreamSourceClosedEventArgs args)
    {
        Debug.Fail($"mediaStreamSource has closed, reason: {args.Request.Reason}");

        if (sender == mediaStreamSource)
        {
            sender.SampleRequested -= MediaStreamSource_SampleRequested;
            sender.Starting -= MediaStreamSource_Starting;
            sender.Closed -= MediaStreamSource_Closed;

            mediaStreamSource = null;

            mediaPlayer.Pause();
            mediaPlayer.Source = null;
        }
    }

    private void MediaStreamSource_SampleRequested(Windows.Media.Core.MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
    {
        Debug.Assert(audioStream is not null);

        if (byteOffset < audioStream.Length)
        {
            MediaStreamSourceSampleRequest request = args.Request;
            MediaStreamSourceSampleRequestDeferral deferal = request.GetDeferral();

            byte[] buffer = new byte[cSampleSize];
            int bytesRead = audioStream.Read(buffer, 0, cSampleSize);

            if (bytesRead < cSampleSize)
                Array.Clear(buffer, bytesRead, cSampleSize - bytesRead);

            MediaStreamSample sample = MediaStreamSample.CreateFromBuffer(buffer.AsBuffer(), timeOffset);
            sample.Duration = sampleDuration;
            sample.KeyFrame = true;

            request.Sample = sample;

            // increment the time and byte offset
            byteOffset += cSampleSize;
            timeOffset += sampleDuration;

            mediaPlayer.Volume = Settings.Data.VolumePercentage / 100.0;

            deferal.Complete();
        }
    }

    private void MediaStreamSource_Starting(Windows.Media.Core.MediaStreamSource sender, MediaStreamSourceStartingEventArgs args)
    {
        MediaStreamSourceStartingRequest request = args.Request;

        Debug.Assert(request.StartPosition is not null);
        Debug.Assert(request.StartPosition == TimeSpan.Zero);
        Debug.Assert(audioStream is not null);

        byteOffset = 0;
        timeOffset = TimeSpan.Zero;
        audioStream.Position = 0;

        request.SetActualStartPosition(TimeSpan.Zero);
    }

    private static Stream? LoadEmbeddedResource()
    {
        Stream? stream = typeof(App).Assembly.GetManifestResourceStream("Countdown.Resources.audio.dat");
        Debug.Assert(stream is not null);
        return stream;
    }
}

