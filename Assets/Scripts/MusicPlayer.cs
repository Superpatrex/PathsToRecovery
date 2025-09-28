using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip audioClip;
    public float volume = 0.5f;

    void Awake()
    {
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        this.audioSource.clip = this.audioClip;
        this.audioSource.volume = this.volume;
        this.audioSource.loop = true;
    }

    void Update()
    {
        if (!this.audioSource.isPlaying)
        {
            this.audioSource.Play();
        }
    }

    public void Stop()
    {
        if (this.audioSource.isPlaying)
        {
            this.audioSource.Stop();
        }
    }

    public void StartMusic()
    {
        if (!this.audioSource.isPlaying)
        {
            this.audioSource.Play();
        }
    }

    // method to slowly fade out the music
    public void FadeOut(float duration)
    {
        StartCoroutine(FadeOutCoroutine(duration));
    }

    public bool isMusicPlaying()
    {
        return this.audioSource.isPlaying;
    }

    private IEnumerator FadeOutCoroutine(float duration)
    {
        float startVolume = this.audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            this.audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        this.audioSource.Stop();
        this.audioSource.volume = startVolume;
    }
}
