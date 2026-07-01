using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("SFX")]
    public AudioClip uiClick;
    public AudioClip playerJump;
    public AudioClip enemyAttack;
    public AudioClip enemyTakeDamage;

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
}
