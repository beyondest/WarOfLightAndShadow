using UnityEngine;

public class AudioTest : MonoBehaviour
{
    void Start()
    {
        // 创建一个 AudioSource
        var source = gameObject.AddComponent<AudioSource>();
        source.loop = false;
        source.playOnAwake = false;
        source.clip = AudioClip.Create("TestTone", 44100, 1, 44100, false);
        
        // 填充一个正弦波（嗡嗡声）
        float frequency = 440f;
        float[] samples = new float[44100];
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * i / 44100);
        }
        source.clip.SetData(samples, 0);

        // 播放
        source.Play();
        Debug.Log("Playing test tone!");
    }
}