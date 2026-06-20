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
    [SerializeField] protected float attackRadius = 1.5f;
    [SerializeField] protected int attackDamage = 1;
    [SerializeField] protected float attackCooldown = 2f;
    protected float lastAttackTime;

    protected Transform player;
    protected Rigidbody rb;

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        initialPosition = transform.position;
        rb = GetComponent<Rigidbody>();
        // Mencari player menggunakan tag. Pastikan objek player kamu punya tag "Player"
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    protected virtual void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRadius)
        {
            AttackPlayer();
        }
        else if (distanceToPlayer <= chaseRadius)
        {
            ChasePlayer();
        }else if(distanceToPlayer > chaseRadius)
        {
            BackToInitialPosition();
        }
    }

    protected virtual void BackToInitialPosition()
    {
        Vector3 direction = (initialPosition - transform.position).normalized;
        direction.y = 0; 
        if (direction != Vector3.zero) transform.rotation = Quaternion.LookRotation(direction);

        Vector3 movePosition = transform.position + transform.forward * moveSpeed * Time.deltaTime;
        if(Vector3.Distance(transform.position, initialPosition) < 1f)
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
        // Melihat ke arah player
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Agar musuh tidak mendongak ke atas jika player melompat
        if (direction != Vector3.zero) transform.rotation = Quaternion.LookRotation(direction);

        // Bergerak maju
        Vector3 movePosition = transform.position + transform.forward * moveSpeed * Time.deltaTime;
        if(Vector3.Distance(transform.position, player.position) <= attackRadius)
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
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            // Lakukan serangan (misal: panggil fungsi TakeDamage di script Player)
            Debug.Log("Musuh menyerang player!");
            
            lastAttackTime = Time.time;
        }
    }

    public virtual void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log(gameObject.name + " kena damage. Sisa HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        Debug.Log(gameObject.name + " mati.");
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
                    playerRb.linearVelocity = new Vector3(playerRb.linearVelocity.x*0.5f, 0, playerRb.linearVelocity.z*0.5f);
                    playerRb.AddForce(Vector3.up * 6f, ForceMode.Impulse);
                }
            }
            else
            {
                // Jika disentuh dari samping, musuh yang menyerang player
                Debug.Log("Player nabrak dari samping! Player yang kena damage.");
                // collision.gameObject.GetComponent<PlayerScript>().TakeDamage(attackDamage);
            }
        }
    }
}