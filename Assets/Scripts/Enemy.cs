using System.Collections;
using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] protected int maxHealth = 1;
    protected int currentHealth;
    [SerializeField] protected float moveSpeed = 3f;

    [Header("Combat & AI")]
    [SerializeField] protected Vector3 initialPosition;
    [SerializeField] protected float chaseRadius = 10f;
    [SerializeField] protected float stopChaseRadius = 12f; // <- penting
    [SerializeField] protected float attackRadius = 1.5f;
    [SerializeField] protected int attackDamage = 1;
    [SerializeField] protected float attackCooldown = 3.5f;
    [SerializeField] protected bool isAttacking = false;

    [Header("References")]
    public ParticleSystem attackParticle;

    [Header("Animation")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected float lastAttackTime;

    [Header("Hitbox")]
    public Collider headCollider;

    protected Transform player;
    protected Rigidbody rb;

    bool isMoving;
    Vector3 lastPosition;

    enum EnemyState
    {
        Idle,
        Chase,
        Attack,
        Return
    }

    EnemyState state;

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        initialPosition = transform.position;
        rb = GetComponent<Rigidbody>();

        animator = GetComponentInChildren<Animator>();
        lastPosition = transform.position;
        // Mencari player menggunakan tag. Pastikan objek player kamu punya tag "Player"
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    protected virtual void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case EnemyState.Idle:
                if (distance <= chaseRadius)
                    state = EnemyState.Chase;
                break;

            case EnemyState.Chase:
                if (distance <= attackRadius)
                    state = EnemyState.Attack;
                else if (distance > chaseRadius + 2f)
                    state = EnemyState.Return;
                break;

            case EnemyState.Attack:
                if (distance > attackRadius)
                    state = EnemyState.Chase;
                break;

            case EnemyState.Return:
                if (Vector3.Distance(transform.position, initialPosition) < 1f)
                    state = EnemyState.Idle;
                break;
        }

        UpdateState();
        UpdateAnimator();
    }
    void UpdateState()
    {
        switch (state)
        {
            case EnemyState.Attack:
                AttackPlayer();
                break;

            case EnemyState.Chase:
                ChasePlayer();
                break;

            case EnemyState.Return:
                BackToInitialPosition();
                break;
        }
    }

    protected virtual void BackToInitialPosition()
    {
        isMoving = true;
        Vector3 direction = (initialPosition - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero) transform.rotation = Quaternion.LookRotation(direction);

        Vector3 movePosition = transform.position + transform.forward * moveSpeed * Time.deltaTime;
        if (Vector3.Distance(transform.position, initialPosition) < 1f)
        {
            // transform.position = initialPosition; 
        }
        else
        {
            rb.MovePosition(movePosition);
        }
    }

    protected virtual void ChasePlayer()
    {
        isMoving = true;
        // Melihat ke arah player
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Agar musuh tidak mendongak ke atas jika player melompat
        if (direction != Vector3.zero) transform.rotation = Quaternion.LookRotation(direction);

        // Bergerak maju
        Vector3 movePosition = transform.position + transform.forward * moveSpeed * Time.deltaTime;
        if (Vector3.Distance(transform.position, player.position) <= attackRadius)
        {
            // transform.position = player.position; 
        }
        else
        {
            rb.MovePosition(movePosition);
        }
    }

    protected virtual void AttackPlayer()
    {
        isMoving = false;
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Invoke(nameof(PlayAttackSound), 1.01f);
            // Lakukan serangan (misal: panggil fungsi TakeDamage di script Player)
            Debug.Log("Musuh menyerang player!");
            Invoke(nameof(SetAttack), 1.01f); // Delay sebelum mengaktifkan isAttacking
            animator.SetTrigger("Attack");
            Invoke(nameof(ResetAttack), 1.08f);

            lastAttackTime = Time.time;
            Invoke(nameof(ToggleAttackParticle), 1.03f); // Delay sebelum memunculkan partikel
        }
    }

    void PlayAttackSound()
    {
        AudioManager.instance.PlayEnemyAttack();
    }

    void SetAttack()
    {
        isAttacking = true;
    }

    void ResetAttack()
    {
        isAttacking = false;
    }

    public virtual void TakeDamage(int damage)
    {
        AudioManager.instance.PlayEnemyDamage();
        currentHealth -= damage;
        Debug.Log(gameObject.name + " kena damage. Sisa HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            StartCoroutine(Die());
        }
    }

    protected virtual IEnumerator Die()
    {
        Debug.Log(gameObject.name + " mati.");
        animator.SetTrigger("Dead");
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        // Visualisasi radius pengejaran dan serangan di editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }

    // --- MEKANIK MARIO STOMP ---
    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Ambil titik persis di mana mereka bersentuhan
            ContactPoint contact = collision.GetContact(0);

            // Cek apakah tabrakan datang dari arah atas (normal menunjuk ke bawah)
            // Nilai -0.5f adalah batas toleransi sudut. Semakin mendekati -1, semakin harus persis dari atas kepala.
            if (contact.normal.y < -0.5f)
            {
                Debug.Log("Player menginjak musuh dari atas!");
                TakeDamage(maxHealth); // Langsung mati (atau beri damage tertentu)

                // Opsional: Bikin player memantul ke atas setelah menginjak
                Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    playerRb.linearVelocity = new Vector3(playerRb.linearVelocity.x * 0.5f, 0, playerRb.linearVelocity.z * 0.5f);
                    // playerRb.AddForce(Vector3.up * 6f, ForceMode.Impulse);
                    if (player.GetComponent<PlayerMovement>())
                    {
                        player.GetComponent<PlayerMovement>().TryJump(false);
                    }
                }
            }
            else
            {
                // Jika disentuh dari samping, musuh yang menyerang player
                // Debug.Log("Player nabrak dari samping! Player yang kena damage.");
                // collision.gameObject.GetComponent<Player>().TakeDamage(attackDamage);
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("OnTriggerEnter: " + other.name);
        if (!isAttacking) return;
        if (!other.CompareTag("Player")) return;

        other.GetComponent<Player>().TakeDamage(attackDamage);
    }

    protected virtual void ToggleAttackParticle()
    {
        if (attackParticle != null)
        {
            if (!attackParticle.isPlaying) attackParticle.Play();
            else attackParticle.Stop();
        }
    }

    protected virtual void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetBool("IsMoving",
            state == EnemyState.Chase ||
            state == EnemyState.Return
        );
    }
}