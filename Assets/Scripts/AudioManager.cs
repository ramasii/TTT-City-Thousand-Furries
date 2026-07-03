using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Source")]
    public AudioSource audioSource;
    public AudioSource bgmSource;
    public AudioSource footstepSource;

    [Header("BGM")]
    public AudioClip bgm;

    [Header("SFX")]
    public AudioClip uiClick;
    public AudioClip playerJump;
    public AudioClip playerRun;
    // public AudioClip playerSwing;
    public AudioClip enemyAttack;
    public AudioClip enemyTakeDamage;
    public AudioClip bellRing;
    public AudioClip fallOff;
    public AudioClip compleatedSound;

    [Header("Jump Pitch Settings")]
    [Range(0.1f, 3f)] public float jumpMinPitch = 0.9f;
    [Range(0.1f, 3f)] public float jumpMaxPitch = 1.1f;


    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            GameObject rootObject = gameObject.transform.root.gameObject;
            DontDestroyOnLoad(rootObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = GetComponentInChildren<AudioSource>();
            }
        }

        if (audioSource == null)
        {
            Debug.LogWarning("AudioManager requires an AudioSource component on this object or one of its children.");
        }
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayBGM();
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource == null)
        {
            Debug.LogWarning("Cannot play SFX: AudioSource is missing.");
            return;
        }
        audioSource.PlayOneShot(clip);
    }

    // shortcut biar gampang dipanggil
    public void PlayUIClick() => PlaySFX(uiClick);
    public void PlayEnemyAttack() => PlaySFX(enemyAttack);
    public void PlayEnemyDamage() => PlaySFX(enemyTakeDamage);
    public void PlayBellRing() => PlaySFX(bellRing);
    public void PlayFallOff() => PlaySFX(fallOff);
    public void PlayCompleatedSound() => PlaySFX(compleatedSound);

    public void PlayBGM()
    {
        if (bgmSource == null || bgm == null)
            return;

        if (bgmSource.isPlaying && bgmSource.clip == bgm)
            return;

        bgmSource.clip = bgm;
        bgmSource.volume = 1f;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM(float fadeDuration = 1f)
    {
        if (bgmSource == null)
            return;

        bgmSource.DOKill();

        bgmSource.DOFade(0f, fadeDuration)
            .SetUpdate(true)   // <- tetap berjalan walaupun Time.timeScale = 0
            .OnComplete(() =>
            {
                bgmSource.Stop();
                bgmSource.volume = 1f;
            });
    }

    public void PlayJump()
    {
        if (playerJump == null) return;
        if (audioSource == null)
        {
            Debug.LogWarning("Cannot play SFX: AudioSource is missing.");
            return;
        }

        // Set random pitch, play, lalu reset ke normal
        audioSource.pitch = Random.Range(jumpMinPitch, jumpMaxPitch);
        audioSource.PlayOneShot(playerJump);
        audioSource.pitch = 1f;
    }

    public void PlayRun()
    {
        if (footstepSource == null || playerRun == null)
            return;

        if (footstepSource.isPlaying)
            return;

        footstepSource.clip = playerRun;
        footstepSource.loop = true;
        footstepSource.Play();
    }

    public void StopRun()
    {
        if (footstepSource == null)
            return;

        if (footstepSource.isPlaying)
            footstepSource.Stop();
    }
}
