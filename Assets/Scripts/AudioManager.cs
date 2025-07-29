using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


[HideMonoScript]
public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Title("AUDIO-MANAGER", null, titleAlignment: TitleAlignments.Centered)]

    public AudioSource BGAudioSource;

    public AudioSource UIAudioSource;

    public AudioSource SFXAudioSource;

    public List<AudioClip> BGMusic;
    public List<AudioClip> UISounds;
    public List<AudioClip> SFXSounds;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        if(BGAudioSource.isPlaying)
            return;
        BGAudioSource.Play();
        BGAudioSource.loop = true;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        LoadSounds();
    }

    void LoadSounds()
    {
        BGAudioSource.volume = PlayerPrefs.GetFloat("BGAudioSourceVolume", 1);

        UIAudioSource.volume = PlayerPrefs.GetFloat("UIAudioSourceVolume", 1);

        SFXAudioSource.volume = PlayerPrefs.GetFloat("SFXAudioSourceVolume", 1);

        BGAudioSource.mute = PlayerPrefs.GetInt("BGAudioSourceMute", 0) == 1;

        UIAudioSource.mute = PlayerPrefs.GetInt("UIAudioSourceMute", 0) == 1;

        SFXAudioSource.mute = PlayerPrefs.GetInt("SFXAudioSourceMute", 0) == 1;
    }

    public void SetBGVol(float value)
    {
        BGAudioSource.volume = value;
        PlayerPrefs.SetFloat("BGAudioSourceVolume", value);
    }
    public void SetUIVol(float value)
    {
        UIAudioSource.volume = value;
        PlayerPrefs.SetFloat("UIAudioSourceVolume", value);
    }

    public void SetSFXVol(float value)
    {
        SFXAudioSource.volume = value;
        PlayerPrefs.SetFloat("SFXAudioSourceVolume", value);
    }

    public void SetBGMute(bool chk)
    {
        BGAudioSource.mute = chk;
        PlayerPrefs.SetInt("BGAudioSourceMute", chk ? 1 : 0);
    }

    public void SetUIMute(bool chk)
    {
        UIAudioSource.mute = chk;
        PlayerPrefs.SetInt("UIAudioSourceMute", chk ? 1 : 0);
    }

    public void SetSFXMute(bool chk)
    {
        SFXAudioSource.mute = chk;
        PlayerPrefs.SetInt("SFXAudioSourceMute", chk ? 1 : 0);
    }

    public void PlayBGMusic(int n)
    {
        BGAudioSource?.Stop();
        BGAudioSource.clip = BGMusic[n];
        BGAudioSource.Play();
    }

    public void PlayUISound(int n)
    {
        UIAudioSource.PlayOneShot(UISounds[n]);
    }


    public void PlaySFXSound(int n)
    {
        SFXAudioSource.PlayOneShot(SFXSounds[n]);
    }
}
