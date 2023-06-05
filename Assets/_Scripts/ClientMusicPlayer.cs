using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientMusicPlayer : Singleton<ClientMusicPlayer>
{
    [SerializeField]private AudioClip nomAudioClip;
    private AudioSource _audioSource;

    public override void Awake() {
        base.Awake();
        _audioSource =  GetComponent<AudioSource>();
    }

    public void PlayNomAudio(){
        _audioSource.clip = nomAudioClip;
        _audioSource.Play();
    }
}
