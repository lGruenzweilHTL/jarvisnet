namespace AssistantCore.Workers.Impl;

public class DummyTts : ITtsWorker
{
    public Task<byte[]> SynthesizeAsync(string inputText, CancellationToken token)
    {
        // Return sine wave
        var sampleRate = 16000;
        var frequency = 440.0;
        var durationSeconds = 2.0;
        var totalSamples = (int)(sampleRate * durationSeconds);

        var buffer = new byte[totalSamples * 2];
        for (int i = 0; i < totalSamples; i++)
        {
            var sample = (short)(Math.Sin((2 * Math.PI * frequency * i) / sampleRate) * short.MaxValue);
            buffer[i * 2] = (byte)(sample & 0xFF);
            buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }

        return Task.FromResult(buffer);
    }
}