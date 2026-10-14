using UnityEngine;
using UnityEngine.SceneManagement; // Kailangan para mabasa ang mga scenes
using System;

public class S16_AudioManager : MonoBehaviour
{
    private static S16_AudioManager _instance;
    
    public static S16_AudioManager instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<S16_AudioManager>();

                if (_instance == null)
                {
                    GameObject prefab = Resources.Load<GameObject>("AudioManager");
                    if (prefab != null)
                    {
                        Instantiate(prefab);
                    }
                    else
                    {
                        Debug.LogWarning("Walang nakitang 'AudioManager' prefab sa Resources folder!");
                    }
                }
            }
            return _instance;
        }
    }

    // AUTO-SPAWN FEATURE: Kusang binubuhay ang Audio Manager pag-open ng laro
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        _ = instance;
    }

    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
    }

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("BGM List (Main Menu, In Game 1-3, Game Over, Winner, Tutorial)")]
    public Sound[] bgmSounds;

    [Header("SFX List (Walk, Jump, High Jump, Booster, Death, Lost Life, Checkpoint, UI Click)")]
    public Sound[] sfxSounds;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject); 
        }
    }

    // AUTO-BGM FEATURE: Kusang nagpapalit ng kanta kapag nag-iba ang Scene
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // I-check ang pangalan ng active scene at i-play ang tamang BGM
        // Siguraduhing tugma ang spelling ng scene names sa loob ng quotation marks
        if (scene.name == "Main Menu") 
        {
            PlayBGM("Main Menu");
        }
        else if (scene.name == "Level 1") // Palitan ng exact name ng Level 1 scene mo
        {
            PlayBGM("In Game 1");
        }
        else if (scene.name == "Level 2") // Palitan ng exact name ng Level 2 scene mo
        {
            PlayBGM("In Game 2");
        }
        // Pwede mong kopyahin at dagdagan ang "else if" block na ito para sa iba pang scenes
    }

    public void PlayBGM(string name)
    {
        Sound s = System.Array.Find(bgmSounds, x => x.name == name);
        if (s != null)
        {
            bgmSource.clip = s.clip;
            bgmSource.Play();
        }
        else
        {
            Debug.LogWarning("BGM not found: " + name);
        }
    }

    public void PlaySFX(string name)
    {
        Sound s = System.Array.Find(sfxSounds, x => x.name == name);
        if (s != null)
        {
            sfxSource.PlayOneShot(s.clip);
        }
        else
        {
            Debug.LogWarning("SFX not found: " + name);
        }
    }
}