using UnityEngine;
using UnityEngine.SceneManagement; 
using System;
using UnityEngine.UI; 
using System.Collections; 

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
    private AudioSource bgmSource2; // Generated at runtime for true crossfading
    private bool isUsingSource1 = true; // Tracks which source is currently active
    public AudioSource sfxSource;
    
    [Header("Transition Settings")]
    public float bgmFadeDuration = 1.0f; // How long the crossfade takes in seconds
    private Coroutine fadeCoroutine; 

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

            if (bgmSource == null)
            {
                Debug.LogError("AudioManager: BGM Source is missing!");
                return;
            }
            
            // --- NEW LOGIC: Generate a clone AudioSource for crossfading ---
            bgmSource2 = gameObject.AddComponent<AudioSource>();
            bgmSource2.outputAudioMixerGroup = bgmSource.outputAudioMixerGroup;
            bgmSource2.loop = bgmSource.loop;
            bgmSource2.ignoreListenerPause = bgmSource.ignoreListenerPause;
            bgmSource2.spatialBlend = bgmSource.spatialBlend;
            bgmSource2.volume = 0f; // Start silent
        }
        else if (_instance != this)
        {
            Destroy(gameObject); 
        }
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        CheckAndPlaySceneBGM(currentScene);
        HookAllUIButtons();
    }

    private void OnActiveSceneChanged(Scene current, Scene next)
    {
        CheckAndPlaySceneBGM(next);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HookAllUIButtons();
    }

    private void HookAllUIButtons()
    {
        Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();
        foreach (Button btn in allButtons)
        {
            btn.onClick.RemoveListener(PlayUIClickSound);
            btn.onClick.AddListener(PlayUIClickSound);
        }
    }

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
            // Determine which source is currently playing and which is free
            AudioSource activeSource = isUsingSource1 ? bgmSource : bgmSource2;
            AudioSource nextSource = isUsingSource1 ? bgmSource2 : bgmSource;

            if (activeSource.clip == s.clip && activeSource.isPlaying)
            {
                return; // Do nothing if the same song is already playing
            }

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            
            // Load the new clip into the silent source and start playing it
            nextSource.clip = s.clip;
            nextSource.Play();
            
            // Crossfade them
            fadeCoroutine = StartCoroutine(CrossfadeRoutine(activeSource, nextSource));
            
            // Swap the active tracker
            isUsingSource1 = !isUsingSource1;
        }
        else
        {
            Debug.LogWarning("BGM not found: " + name);
        }
    }

    // --- NEW BGM LOGIC: Simultaneous True Crossfade ---
    private IEnumerator CrossfadeRoutine(AudioSource fadingOut, AudioSource fadingIn)
    {
        float safeDuration = Mathf.Max(bgmFadeDuration, 0.1f);
        float startVolumeOut = fadingOut.volume;

        // Fade both sources at the exact same time
        for (float t = 0; t < safeDuration; t += Time.unscaledDeltaTime)
        {
            fadingOut.volume = Mathf.Lerp(startVolumeOut, 0f, t / safeDuration);
            // Change the target volume here to 0.8f
            fadingIn.volume = Mathf.Lerp(0f, 0.8f, t / safeDuration); 
            yield return null;
        }

        // Snap them precisely to their final values to ensure mathematical perfection
        fadingOut.volume = 0f;
        fadingOut.Stop(); // Stop the old track completely
        
        // Change the final snap volume here to 0.8f
        fadingIn.volume = 0.8f; 
    }

    public void PlaySFX(string name)
    {
        if (sfxSounds == null || sfxSource == null)
        {
            Debug.LogError("AudioManager: SFX configuration is missing!");
            return;
        }

        Sound s = System.Array.Find(sfxSounds, x => x.name == name);

        if (s != null && s.clip != null)
        {
            sfxSource.PlayOneShot(s.clip);
        }
        else
        {
            Debug.LogWarning("SFX not found or clip missing: " + name);
        }
    }
}