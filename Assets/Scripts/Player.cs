using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Hit State")]
    public bool isHit;
    public float hitDuration = 2f;
    public Animator playerAnimator;
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void TakeDamage(int damage)
    {
        if (isHit) return; // Jika sedang dalam keadaan hit, abaikan serangan
        GetHit();
        playerMovement.TriggerBoinkEffect(); // Efek visual saat terkena serangan
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
