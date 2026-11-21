using UnityEngine;

public sealed class SoundManager : MonoBehaviour
{
    public const string SoundEnabledKey = "Options.SoundEnabled";
    public const string MaterialKey = "Options.Material";

    private static SoundManager instance;
    private AudioSource sfxSource;

    private AudioClip clickClip;
    private AudioClip dropClip;
    private AudioClip rotateClip;
    private AudioClip destroyClip;
    private AudioClip winClip;
    private AudioClip loseClip;
    private AudioClip drawClip;

    public static bool IsSoundEnabled => PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;

    public static SoundManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("SoundManager");
                instance = go.AddComponent<SoundManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        AudioListener.volume = IsSoundEnabled ? 1f : 0f;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        clickClip = Resources.Load<AudioClip>("Audio/sfx_click");
        dropClip = Resources.Load<AudioClip>("Audio/sfx_drop");
        rotateClip = Resources.Load<AudioClip>("Audio/sfx_rotate");
        destroyClip = Resources.Load<AudioClip>("Audio/sfx_destroy");
        winClip = Resources.Load<AudioClip>("Audio/sfx_win");
        loseClip = Resources.Load<AudioClip>("Audio/sfx_lose");
        drawClip = Resources.Load<AudioClip>("Audio/sfx_draw");

        AudioListener.volume = IsSoundEnabled ? 1f : 0f;
    }

    public static void SetSoundEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(SoundEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        AudioListener.volume = enabled ? 1f : 0f;
    }

    public static void PlayClick() => Play(Instance.clickClip, 0.7f);
    public static void PlayDrop() => Play(Instance.dropClip, 0.85f);
    public static void PlayRotate() => Play(Instance.rotateClip, 0.8f);
    public static void PlayDestroy() => Play(Instance.destroyClip, 0.85f);
    public static void PlayWin() => Play(Instance.winClip, 0.9f);
    public static void PlayLose() => Play(Instance.loseClip, 0.85f);
    public static void PlayDraw() => Play(Instance.drawClip, 0.8f);

    private static void Play(AudioClip clip, float volume)
    {
        if (!IsSoundEnabled || clip == null) return;
        SoundManager mgr = Instance;
        if (mgr != null && mgr.sfxSource != null)
            mgr.sfxSource.PlayOneShot(clip, volume);
    }
}
