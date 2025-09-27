using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip audioClip;
    public float volume = 0.5f;

    void Start()
    {
        this.audioSource.clip = this.audioClip;
        this.audioSource.volume = this.volume;
        this.audioSource.Play();
        this.audioSource.loop = true;
    }

    void Update()
    {
        if (!this.audioSource.isPlaying)
        {
            this.audioSource.Play();
        }
    }
}
