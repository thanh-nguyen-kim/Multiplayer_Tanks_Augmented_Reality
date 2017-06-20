using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;
    private AudioSource audioSource, bgAudioSource;
    public AudioClip m_ClickButtonAClip, m_PickItemAClip, m_DropItemAClip, m_CameraShutterAClip, m_LobbyAClip, m_SingleAClip, m_MultiAClip;
    private bool music, bAudio;
    int lastScene = -1;
    public static SoundManager Instance
    {
        get
        {
            return instance;
        }
    }

    public bool Music
    {
        get
        {
            return music;
        }

        set
        {
            this.music = value;
            if (value)
            {
                if (!bgAudioSource.clip)
                {
                    PlayBackGroundAudio(0);
                }
                else
                    bgAudioSource.Play();
            }
            else
                bgAudioSource.Stop();
            PlayerPrefs.SetInt("Music", value ? 1 : 0);
        }
    }

    public bool Audio
    {
        get
        {
            return bAudio;
        }

        set
        {
            this.bAudio = value;

            PlayerPrefs.SetInt("Audio", value ? 1 : 0);
        }
    }

    void Start()
    {
        if (instance == null)
        {
            instance = this;
            music = PlayerPrefs.GetInt("Music", 1) == 1 ? true : false;
            bAudio = PlayerPrefs.GetInt("Audio", 1) == 1 ? true : false;
            audioSource = GetComponents<AudioSource>()[0];
            bgAudioSource = GetComponents<AudioSource>()[1];
            PlayBackGroundAudio(0);
            DontDestroyOnLoad(gameObject);
            StartCoroutine(RegisterDel());
        }
        else
            DestroyImmediate(gameObject);
    }

    IEnumerator RegisterDel()
    {
        do
        {
            yield return new WaitForSeconds(2f);
        }
        while (Prototype.NetworkLobby.LobbyManager.s_Singleton == null);
        Prototype.NetworkLobby.LobbyManager.s_Singleton.bgSound = PlayBackGroundAudio;
    }
    public void PlayOneShot(AudioClip clip)
    {
        if (!bAudio) return;
        audioSource.PlayOneShot(clip);
    }

    public void PlayClickButton()
    {
        if (!bAudio) return;
        audioSource.PlayOneShot(m_ClickButtonAClip);
    }

    public void PlayCameraShutter()
    {
        if (!bAudio) return;
        audioSource.PlayOneShot(m_CameraShutterAClip);
    }

    public void PlayBackGroundAudio(int type)
    {
        //0:lobby,1:singleplay,2:multi play
        if (!music) return;
        bgAudioSource.Stop();
        if (type == 0)
            bgAudioSource.clip = m_LobbyAClip;
        else
            if (type == 1)
            bgAudioSource.clip = m_SingleAClip;
        else
            bgAudioSource.clip = m_MultiAClip;
        bgAudioSource.loop = true;
        bgAudioSource.priority = 0;
        bgAudioSource.volume = 0.5f;
        bgAudioSource.Play();
    }

    public void PlayPickItemAudio()
    {
        if (bAudio)
            audioSource.PlayOneShot(m_PickItemAClip, 0.5f);
    }

    public void PlayDropItem()
    {
        if (bAudio)
            audioSource.PlayOneShot(m_DropItemAClip, 0.5f);
    }
}

