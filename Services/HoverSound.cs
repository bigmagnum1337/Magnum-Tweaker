using System.Diagnostics;
using System.IO;
using System.Media;
using System.Text;

namespace MagnumTweaker.Services;

internal enum InterfaceSound { Click, Hover, Success, ScanComplete }

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
        bool quiet = sound is InterfaceSound.Click or InterfaceSound.Hover;
        if (volume <= 0 || (quiet && Stopwatch.GetTimestamp() < _reservedUntil)) return;
        if (sound == InterfaceSound.Hover && _lastPlayed is long last && Stopwatch.GetElapsedTime(last).TotalMilliseconds < 85) return;

        try
        {
            if (_player is null || _loadedVolume != volume || _loadedSound != sound)
            {
                Dispose();
                _wave = CreateWave(volume, sound);
                _player = new SoundPlayer(_wave);
                _player.Load();
                _loadedVolume = volume;
                _loadedSound = sound;
            }
            _player.Play();
            _lastPlayed = Stopwatch.GetTimestamp();
            if (!quiet) _reservedUntil = _lastPlayed.Value + Stopwatch.Frequency;
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or TimeoutException)
        {
            Dispose();
        }
    }

    internal static MemoryStream CreateWave(int volume, InterfaceSound sound)
    {
        const int rate = 22050;
        int samples = sound switch { InterfaceSound.Hover => 1000, InterfaceSound.Click => 900, _ => 6500 };
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
                if (sound == InterfaceSound.Hover)
                {
                    tone = 0.58 * Math.Sin(2 * Math.PI * 530 * time);
                }
                else if (sound != InterfaceSound.Click)
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
