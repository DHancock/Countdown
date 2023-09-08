using Countdown.ViewModels;

namespace Countdown.Views;

internal class AudioHelper
{
    private readonly MediaPlayer mediaPlayer = new MediaPlayer();

    public AudioHelper()
    {
        Stream? stream = typeof(App).Assembly.GetManifestResourceStream("Countdown.Resources.audio.dat");

        if (stream is not null)
        {
            mediaPlayer.SetStreamSource(stream.AsRandomAccessStream());
            mediaPlayer.Volume = Settings.Data.VolumePercentage / 100.0;

            Settings.Data.VolumeChanged += (s, a) => mediaPlayer.Volume = Settings.Data.VolumePercentage / 100.0;
        }
    }

    public void Start()
    {
        mediaPlayer.Position = TimeSpan.Zero;
        mediaPlayer.Play();
    }

    public void Stop() => mediaPlayer.Pause();
}

