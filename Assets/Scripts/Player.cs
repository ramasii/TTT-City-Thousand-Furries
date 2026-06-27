using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 3;

    [Header("Hit State")]
    public bool isHit;
    public float hitDuration = 2f;
    public Animator playerAnimator;
    private int currentHealth;
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;

    // ===== EVENTS =====
    public delegate void PlayerHealthChanged(int currentHealth, int maxHealth);
    public event PlayerHealthChanged OnPlayerHealthChanged;
    public delegate void PlayerDied();
    public event PlayerDied OnPlayerDied;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;
        playerMovement = GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void TakeDamage(int damage)
    {
        if (isHit) return; // Jika sedang dalam keadaan hit, abaikan serangan
        currentHealth -= damage;
        OnPlayerHealthChanged?.Invoke(currentHealth, maxHealth);
        GetHit();
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Logika ketika player mati, misalnya menampilkan UI Game Over atau memulai ulang level
        Debug.Log("Player has died!");
        // Untuk sementara, kita bisa menghancurkan objek player
        OnPlayerDied?.Invoke();
    }
    private void GetHit()
    {
        isHit = true;
        playerAnimator.SetTrigger("Hit");
        StartCoroutine(HitRoutine());
    }
    private System.Collections.IEnumerator HitRoutine()
    {
        yield return new WaitForSeconds(hitDuration);

        isHit = false;
    }

}
