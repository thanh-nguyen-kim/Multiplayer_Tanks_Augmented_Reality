using UnityEngine;
using UnityEngine.UI;
public class SoundManagerHook : MonoBehaviour
{
    public Toggle m_MusicToggle, m_AudioToggle;
    void Start()
    {
        m_MusicToggle.isOn = PlayerPrefs.GetInt("Music", 1) == 1 ? true : false;
        m_AudioToggle.isOn = PlayerPrefs.GetInt("Audio", 1) == 1 ? true : false;
    }
    public void ButtonClickSound()
    {
        if (SoundManager.Instance!=null)
            SoundManager.Instance.PlayClickButton();
    }
    public void PlayCameraShutter()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayCameraShutter();
    }
    public void OnMusicToggleChanged(Toggle musicToggle)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.Music = musicToggle.isOn;
    }

    public void OnSoundToggleChanged(Toggle soundToggle)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.Audio = soundToggle.isOn;
    }
}
