using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    static AudioManager s_Instance;
    public static AudioManager Instance => s_Instance;

    List<AudioObj> AudioObjs = new List<AudioObj>();

    private void Awake()
    {
        s_Instance = this;
    }
    private void Update()
    {
        for (int i = AudioObjs.Count - 1; i >= 0; i--)
        {
            AudioObjs[i].lifeTime -= Time.deltaTime;
            if (AudioObjs[i].lifeTime <= 0)
            {
                Destroy(AudioObjs[i].audioOwner);
                AudioObjs.RemoveAt(i);
            }
        }
    }

    public AudioSource Play(AudioClip clip, float volume, float speed, float startTime)
    {
        AudioObj audioObj = new AudioObj(clip, volume, speed, startTime);
        AudioObjs.Add(audioObj);
        return audioObj.audioSource;
    }

    class AudioObj
    {
        public GameObject audioOwner;
        public AudioSource audioSource;
        public float lifeTime;

        public AudioObj(AudioClip clip, float volume, float speed, float startTime)
        {
            audioOwner = new GameObject(clip.name);
            audioSource = audioOwner.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.Play();
            audioSource.volume = volume;
            audioSource.pitch = speed;
            audioSource.time = clip.length * startTime;
            lifeTime = clip.length - clip.length * startTime;
        }
    }
}
