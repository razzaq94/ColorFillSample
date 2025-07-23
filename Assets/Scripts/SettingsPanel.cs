using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    public static SettingsPanel instance;
    public Slider musicSlider;
    public Slider sfxSlider;
    //public Slider uiSlider;
    [SerializeField] private Toggle vibrationToggle;

    private void Awake()
    {
        instance = this;
        vibrationToggle.isOn = Haptics.Enabled;

        vibrationToggle.onValueChanged.AddListener(OnVibrationToggleChanged);
    }
    private void Start()
    {
        sfxSlider.value = PlayerPrefs.GetFloat("SFXAudioSourceVolume", 1);
        musicSlider.value = PlayerPrefs.GetFloat("BGAudioSourceVolume", 1);

        musicSlider.onValueChanged.AddListener(UpdateMusicVolume);
        sfxSlider.onValueChanged.AddListener(UpdateSFXVolume);
    }
    public static SettingsPanel ShowUI()
    {
        if (instance == null)
        {
            GameObject obj = Instantiate(Resources.Load("Settings")) as GameObject;

            obj.gameObject.transform.SetParent(GameObject.FindGameObjectWithTag("MainCanvas").transform, false);

            instance = obj.GetComponent<SettingsPanel>();
        }

        return instance;
    }


    private void UpdateMusicVolume(float value)
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetBGVol(value);
        }
    }

    private void UpdateSFXVolume(float value)
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetSFXVol(value);
            AudioManager.instance.SetUIVol(value);
        }
    }

    private void UpdateUIVolume(float value)
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetUIVol(value);
        }
    }
    public void MuteAllSounds(bool mute)
    {
        AudioManager.instance.SetBGMute(mute);
        AudioManager.instance.SetUIMute(mute);
        AudioManager.instance.SetSFXMute(mute);
        PlayerPrefs.SetInt("AllSoundsMute", mute ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        vibrationToggle.onValueChanged.RemoveListener(OnVibrationToggleChanged);
    }

    public void OnVibrationToggleChanged(bool isOn)
    {
        Haptics.SetEnabled(isOn);
    }
    public void ToggleAllSoundsMute()
    {
        bool isMuted = PlayerPrefs.GetInt("AllSoundsMute", 0) == 1;
        AudioManager.instance?.PlayUISound(0);
        MuteAllSounds(!isMuted);
    }

    public void BackBTNSettingsPanel()
    {
        Destroy(gameObject);
        if (Time.timeScale == 0) Time.timeScale = 1;
        AudioManager.instance?.PlayUISound(0);
    }


    public void PrivacyPolicy()
    {
        Application.OpenURL("https://sites.google.com/view/colorin3d?usp=sharing");
    }

}
