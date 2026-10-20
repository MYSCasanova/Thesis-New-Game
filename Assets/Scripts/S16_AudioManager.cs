using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management
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
                        Debug.LogWarning("No 'AudioManager' prefab found in the Resources folder!");
                    }
                }
            }
            return _instance;
        }
    }

    // AUTO-SPAWN FEATURE: Automatically spawns the Audio Manager when the game opens
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

    // AUTO-BGM FEATURE: Automatically changes the song when the Active Scene changes
    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void Start()
    {
        // Manually trigger the BGM for the very first scene when the game boots up
        Scene currentScene = SceneManager.GetActiveScene();
        CheckAndPlaySceneBGM(currentScene);
    }

    private void OnActiveSceneChanged(Scene current, Scene next)
    {
        // Trigger BGM when transitioning between levels
        CheckAndPlaySceneBGM(next);
    }

    private void CheckAndPlaySceneBGM(Scene scene)
    {
        if (scene.name == "GYM_MainMenu") 
        {
            PlayBGM("Main Menu");
        }
        else if(scene.name == "GYM_TutorialGallery" || scene.name == "GYM_TutorialLevel")
        {
            PlayBGM("Tutorial");
        }
        else if(scene.name == "PersistentGameplay" || scene.name == "GYM_BetaGame")
        {
            if (PlayerPrefs.GetInt("tutorialCompleted", 0) == 1)
            {
                // Play Level 1 music immediately when the main game loads
                PlayBGM("Level 1"); 
            }
            else
            {
                PlayBGM("Tutorial");
            }
        }
    }

    public void PlayBGM(string name)
    {
        Sound s = System.Array.Find(bgmSounds, x => x.name == name);
        if (s != null)
        {
            // CRITICAL FIX: Do not restart the track if it is already playing
            if (bgmSource.clip == s.clip && bgmSource.isPlaying)
            {
                return; 
            }

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