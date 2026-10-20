using UnityEngine;
using UnityEngine.SceneManagement; 
using System;
using UnityEngine.UI; // REQUIRED for finding UI Buttons

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

    [Header("BGM List")]
    public Sound[] bgmSounds;

    [Header("SFX List")]
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

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        // --- NEW LOGIC: Listen for when any scene finishes loading ---
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        // --- NEW LOGIC: Stop listening when disabled ---
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        CheckAndPlaySceneBGM(currentScene);
        
        // Hook buttons in the very first scene on boot
        HookAllUIButtons();
    }

    private void OnActiveSceneChanged(Scene current, Scene next)
    {
        CheckAndPlaySceneBGM(next);
    }

    // --- NEW LOGIC: Trigger button hook when additive scenes (like Settings) load ---
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HookAllUIButtons();
    }

    // --- NEW LOGIC: Finds all buttons and attaches the click sound automatically ---
    private void HookAllUIButtons()
    {
        // FindObjectsOfType with 'true' finds even hidden/inactive buttons (like paused menus)
        Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();
        
        foreach (Button btn in allButtons)
        {
            // Remove listener first to prevent the sound from playing multiple times if scenes reload
            btn.onClick.RemoveListener(PlayUIClickSound);
            btn.onClick.AddListener(PlayUIClickSound);
        }
    }

    // --- NEW LOGIC: The actual method the buttons will call ---
    public void PlayUIClickSound()
    {
        PlaySFX("Click");
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