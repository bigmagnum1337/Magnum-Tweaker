using System.Diagnostics;
using System.IO;
using System.Media;
using System.Text;

namespace MagnumTweaker.Services;

internal sealed class HoverSound : IDisposable
{
    private SoundPlayer? _player;
    private MemoryStream? _wave;
    private int _loadedVolume;
    private long? _lastPlayed;

    public void Play(int volume)
    {
        if (volume <= 0) return;
        if (_lastPlayed is long last && Stopwatch.GetElapsedTime(last).TotalMilliseconds < 85) return;

        try
        {
            if (_player is null || _loadedVolume != volume)
            {
                Dispose();
                _wave = CreateWave(volume);
                _player = new SoundPlayer(_wave);
                _player.Load();
                _loadedVolume = volume;
            }
            _player.Play();
            _lastPlayed = Stopwatch.GetTimestamp();
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or TimeoutException)
        {
            Dispose();
        }
    }

    private static MemoryStream CreateWave(int volume)
    {
        const int rate = 22050;
        const int samples = 1200;
        const int dataBytes = samples * sizeof(short);
        var stream = new MemoryStream(44 + dataBytes);
        using (var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true))
        {
            writer.Write("RIFF"u8);
            writer.Write(36 + dataBytes);
            writer.Write("WAVEfmt "u8);
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(rate);
            writer.Write(rate * sizeof(short));
            writer.Write((short)sizeof(short));
            writer.Write((short)16);
            writer.Write("data"u8);
            writer.Write(dataBytes);
            for (int i = 0; i < samples; i++)
            {
                double time = (double)i / rate;
                double envelope = Math.Min(i / 95d, 1) * Math.Exp(-78 * time) * (1 - (double)i / (samples - 1));
                double tone = Math.Sin(2 * Math.PI * 740 * time) + 0.23 * Math.Sin(2 * Math.PI * 1110 * time);
                writer.Write((short)(short.MaxValue * 0.13 * (volume / 100d) * tone * envelope));
            }
        }
        stream.Position = 0;
        return stream;
    }

    public void Dispose()
    {
        _player?.Dispose();
        _wave?.Dispose();
        _player = null;
        _wave = null;
    }
}
