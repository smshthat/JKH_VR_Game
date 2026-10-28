using UnityEngine;

// Шрифт и звуки, которые делаются прямо в коде (файлы не нужны).
public static class SimpleSound
{
    static Font font;

    // Шрифт Arial из системы, чтобы работала кириллица
    public static Font TextFont
    {
        get
        {
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 64);
            return font;
        }
    }

    // Победные фанфары для конца смены
    public static AudioClip Fanfare()
    {
        int rate = 44100;
        float[] freqs = { 523f, 659f, 784f, 1047f };
        float[] lens = { 0.16f, 0.16f, 0.16f, 0.6f };
        int total = 0;
        for (int i = 0; i < lens.Length; i++) total += (int)(rate * lens[i]);
        float[] data = new float[total];
        int pos = 0;
        for (int k = 0; k < freqs.Length; k++)
        {
            int n = (int)(rate * lens[k]);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Pow(1f - (float)i / n, 0.7f);
                float v = Mathf.Sin(2f * Mathf.PI * freqs[k] * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * freqs[k] * 2f * t) * 0.2f;
                data[pos + i] = v * env * 0.4f;
            }
            pos += n;
        }
        AudioClip clip = AudioClip.Create("fanfare", total, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
