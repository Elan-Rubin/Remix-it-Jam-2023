using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using static Unity.VisualScripting.Member;
using DG.Tweening;
using UnityEngine.Rendering;

public class SoundManager : MonoBehaviour
{
    [SerializeField][Range(0, 1)] private float volumeMultiplier = 1;

    private static SoundManager instance;
    public static SoundManager Instance { get { return instance; } }
    private void Awake()
    {
        if (instance != null && instance != this) Destroy(gameObject);
        else instance = this;
    }

    [SerializeField] private List<SoundEffect> soundEffects;
    [SerializeField] private List<AudioClip> musicLayers;
    private List<AudioSource> musicLayerSources = new();
    private int musicIndex;
    //public void OnValidate()
    //{
    //    for (int i = 0; i < soundEffects.Count; i++)
    //    {
    //        soundEffects[i].OnValidate();
    //    }
    //}

    private void Start()
    {
        for (int i = 0; i < musicLayers.Count; i++)
        {
            var l = new GameObject($"music layer #{i}");
            l.transform.parent = transform;
            var source = l.AddComponent<AudioSource>();
            source.loop = true;
            source.clip = musicLayers[i];
            source.volume = i == 0 ? 1 : 0;
            musicLayerSources.Add(source);
            source.Play();
        }
    }
    public void RetreatMusic() => AdvanceMusic(false);
    public void AdvanceMusic() => AdvanceMusic(true);
    public void AdvanceMusic(bool direction)
    {
        var tempIndex = musicIndex + (direction ? 1 : -1);
        if (direction && musicIndex == musicLayers.Count - 1) return;
        musicLayerSources[musicIndex].DOFade(0, 1);
        musicIndex = tempIndex;
        //musicIndex = direction ? musicIndex++ : musicIndex--;
        musicLayerSources[musicIndex].DOFade(1, 1);
    }
    public void ResetMusic() => StartCoroutine(nameof(ResetMusicCoroutine));
    private IEnumerator ResetMusicCoroutine()
    {
        /*Debug.LogError(musicIndex);
        for (int i = 0; i < musicIndex; i++)
        {
            Debug.LogError("called");
            RetreatMusic();
            yield return new WaitForSeconds(1f);
        }
        musicIndex = 0;*/
        yield return null;
        for (int i = 1; i < musicIndex; i++)
        {
            musicLayerSources[i].DOFade(1, 1).OnComplete(() =>
            {
                musicLayerSources[i].DOFade(0, 1);
            });
        }
        musicIndex = 0;
        musicLayerSources[musicIndex].DOFade(1, 1);
    }

    public void PlayClickSoundEffect() => PlaySoundEffect("click");
    /// <summary>
    /// Plays a certain sound from an instance of the SoundManager. Volume is constant
    /// </summary>
    /// <param name="soundType">The type of sound that will play</param>
    public void PlaySoundEffect(string soundName) => PlaySoundEffect(soundName, 0);
    /// <summary>
    /// Plays a certain sound from an instance of the SoundManager. Volume is constant
    /// </summary>
    /// <param name="soundType">The type of sound that will play</param>
    /// <param name="modifier">Modifier on the pitch of the sound</param>
    public void PlaySoundEffect(string soundName, int modifier)
    {
        var matchingEffects = soundEffects.Where(s => s.SoundName.Equals(soundName)).ToList();
        var soundEffect = matchingEffects[Random.Range(0, matchingEffects.Count)];
        var chosenClip = soundEffect.Clips[Random.Range(0, soundEffect.Clips.Count)];
        var newSoundEffect = new GameObject($"Sound: {soundName}, {chosenClip.length}s");
        newSoundEffect.transform.parent = transform;
        Destroy(newSoundEffect, chosenClip.length * 1.5f);
        var source = newSoundEffect.AddComponent<AudioSource>();
        source.clip = chosenClip;
        source.volume = soundEffect.Volume * volumeMultiplier;
        if (soundEffect.Vary) source.pitch += Random.Range(-0.1f, 0.1f);
        source.pitch += 0.05f * modifier;
        source.Play();
    }
}

[System.Serializable]
public struct SoundEffect
{
    //private string name;
    public string SoundName;
    public List<AudioClip> Clips;
    [Range(0, 1)] public float Volume;
    public bool Vary;
    //public void OnValidate() => name = Type.ToString();
}