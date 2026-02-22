using System.Buffers;
using System.Runtime.InteropServices;
using Pv;

const int frameLength = 512;
const int durationSeconds = 5;
const string outputFile = "recording_span.wav";

using var mic = PvRecorder.Create(frameLength, 3);
int sampleRate = mic.SampleRate;
int totalFrames = (sampleRate * durationSeconds + frameLength - 1) / frameLength;
int totalSamples = totalFrames * frameLength;

Console.WriteLine($"Recording {durationSeconds}s from: {mic.SelectedDevice}");

short[] frameBuffer = ArrayPool<short>.Shared.Rent(frameLength);
short[] sampleBuffer = ArrayPool<short>.Shared.Rent(totalSamples);
try
{
    mic.Start();
    try
    {
        Span<short> frame = frameBuffer.AsSpan(0, frameLength);
        for (int i = 0; i < totalFrames; i++)
        {
            mic.Read(frame);
            frame.CopyTo(sampleBuffer.AsSpan(i * frameLength));
        }
    }
    finally
    {
        mic.Stop();
    }

    WriteWav(outputFile, sampleBuffer.AsSpan(0, totalSamples), sampleRate);
    Console.WriteLine($"Saved to {Path.GetFullPath(outputFile)}");
}
finally
{
    ArrayPool<short>.Shared.Return(frameBuffer);
    ArrayPool<short>.Shared.Return(sampleBuffer);
}

static void WriteWav(string path, ReadOnlySpan<short> samples, int sampleRate)
{
    const int channels = 1;
    const int bitsPerSample = 16;
    int blockAlign = channels * bitsPerSample / 8;
    int byteRate = sampleRate * blockAlign;
    int dataSize = samples.Length * blockAlign;

    using var writer = new BinaryWriter(File.Create(path));
    writer.Write("RIFF"u8);
    writer.Write(36 + dataSize);
    writer.Write("WAVE"u8);
    writer.Write("fmt "u8);
    writer.Write(16);               // PCM fmt chunk size
    writer.Write((short)1);         // PCM format
    writer.Write((short)channels);
    writer.Write(sampleRate);
    writer.Write(byteRate);
    writer.Write((short)blockAlign);
    writer.Write((short)bitsPerSample);
    writer.Write("data"u8);
    writer.Write(dataSize);
    writer.Write(MemoryMarshal.Cast<short, byte>(samples));
}
