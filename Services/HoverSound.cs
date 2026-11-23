using System.Diagnostics;
using System.IO;
using System.Media;
using System.Text;

namespace MagnumTweaker.Services;

internal enum InterfaceSound { Click, Success, ScanComplete, Startup }

internal sealed class HoverSound : IDisposable
{
    private SoundPlayer? _player;
    private MemoryStream? _wave;
    private int _loadedVolume;
    private InterfaceSound _loadedSound;
    private long _reservedUntil;
    private long? _lastPlayed;

    public void Play(int volume, InterfaceSound sound)
    {
        if (volume <= 0 || (sound == InterfaceSound.Click && Stopwatch.GetTimestamp() < _reservedUntil)) return;
        if (sound == InterfaceSound.Click && _lastPlayed is long last && Stopwatch.GetElapsedTime(last).TotalMilliseconds < 85) return;

        try
        {
            if (_player is null || _loadedVolume != volume || _loadedSound != sound)
            {
                Dispose();
                _wave = sound == InterfaceSound.Startup ? Voice(volume) : CreateWave(volume, sound);
                _player = new SoundPlayer(_wave);
                _player.Load();
                _loadedVolume = volume;
                _loadedSound = sound;
            }
            _player.Play();
            _lastPlayed = Stopwatch.GetTimestamp();
            if (sound != InterfaceSound.Click) _reservedUntil = _lastPlayed.Value + Stopwatch.Frequency * (sound == InterfaceSound.Startup ? 3 : 1);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or TimeoutException)
        {
            Dispose();
        }
    }

    private static MemoryStream Voice(int volume)
    {
        using var source = typeof(HoverSound).Assembly.GetManifestResourceStream("MagnumTweaker.Magnum.wav")!;
        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        byte[] bytes = buffer.ToArray();
        for (int chunk = 12; chunk + 8 <= bytes.Length;)
        {
            int length = BitConverter.ToInt32(bytes, chunk + 4);
            if (bytes.AsSpan(chunk, 4).SequenceEqual("data"u8))
            {
                for (int index = chunk + 8; index + 1 < Math.Min(bytes.Length, chunk + 8 + length); index += 2)
                {
                    short sample = (short)(BitConverter.ToInt16(bytes, index) * (volume / 100d) * 0.72);
                    bytes[index] = (byte)sample;
                    bytes[index + 1] = (byte)(sample >> 8);
                }
                break;
            }
            chunk += 8 + length + (length & 1);
        }
        return new MemoryStream(bytes);
    }

    private static MemoryStream CreateWave(int volume, InterfaceSound sound)
    {
        const int rate = 22050;
        int samples = sound == InterfaceSound.Click ? 900 : 6500;
        int dataBytes = samples * sizeof(short);
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
                if (sound != InterfaceSound.Click)
                {
                    bool second = time >= 0.095;
                    double beat = second ? time - 0.095 : time;
                    double frequency = sound == InterfaceSound.Success ? (second ? 880 : 659) : (second ? 659 : 523);
                    envelope = Math.Min(beat * 170, 1) * Math.Exp(-20 * beat);
                    tone = Math.Sin(2 * Math.PI * frequency * beat);
                }
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
