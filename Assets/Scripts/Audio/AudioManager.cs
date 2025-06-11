using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum AudioId {Hit ,UISelecet, Faint, ExpGain, ItemObtained, PokemonObtained}

public class AudioManager : MonoBehaviour{
    [SerializeField] AudioSource musicPlayer;
    [SerializeField] AudioSource sfxPlayer;

    [SerializeField] float fadeDuration = 0.75f;

    [Header ("Common Audios")]
    [SerializeField] List<AudioData> sfxList;

    float originalMusicVol;
    AudioClip currentClip;
    Dictionary<AudioId, AudioData> sfxLookUp;

    public static AudioManager i {get; private set;}

    private void Awake(){
        i = this;
    }

    private void Start(){
        originalMusicVol = musicPlayer.volume;
        sfxLookUp = sfxList.ToDictionary(x => x.id);
    }

    public void PlayMusic(AudioClip clip, bool loop = true, bool fade = false){
        if(clip == null || currentClip == clip){
            return;
        }
        currentClip = clip;
        StartCoroutine(PlayMusicAsync(clip, loop, fade));
    }

    public void PlaySfx(AudioClip clip, bool pauseMusic = false){
        if(clip == null){
            return;
        }
        if(pauseMusic){
            musicPlayer.Pause();
            StartCoroutine(UnPauseMusic(clip.length));
        }

        sfxPlayer.PlayOneShot(clip);
    }

    public void PlaySfx(AudioId audioId, bool pauseMusic = false){
        if(!sfxLookUp.ContainsKey(audioId)){
            return;
        }

        var audioData = sfxLookUp[audioId];
        PlaySfx(audioData.clip, pauseMusic);
    }

    IEnumerator PlayMusicAsync(AudioClip clip, bool loop, bool fade){
        if(fade){
            yield return musicPlayer.DOFade(0, fadeDuration).WaitForCompletion();
        }
        musicPlayer.clip = clip;
        musicPlayer.loop = loop;
        musicPlayer.Play();

        yield return musicPlayer.DOFade(originalMusicVol, fadeDuration).WaitForCompletion();
    }

    IEnumerator UnPauseMusic(float delay){
        yield return new WaitForSeconds(delay);
        musicPlayer.volume = 0;
        musicPlayer.UnPause();
        musicPlayer.DOFade(originalMusicVol, fadeDuration);
    }
}

[System.Serializable]
public class AudioData{
    public AudioId id;
    public AudioClip clip;
}
