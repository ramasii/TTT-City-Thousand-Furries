using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private Animator animator;
    Rigidbody rb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if(animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Jump"))
        {
            Jump();
        }
    }

    void FixedUpdate()
    {
        // Move is called in Update to ensure input is responsive, but physics updates are handled in FixedUpdate
        Move();
    }

    public void Move()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(horizontal, 0, vertical).normalized;
        Vector3 moveDirection = MakeRelativeToCamera(input);
        
        Vector3 targetVelocity = moveDirection * speed;
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

        animator.SetFloat("Mag", input.magnitude);
        RotateTowardsMovementDirection(moveDirection);
        SetAnimationSpeed("Running", input.magnitude);
    }

    public void Jump()
    {
        if (animator.GetBool("Grounded"))
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    Vector3 MakeRelativeToCamera(Vector3 input)
    {
        Vector3 forward = Camera.main.transform.forward;
        forward.y = 0;
        forward.Normalize();
        Vector3 right = Camera.main.transform.right;
        right.y = 0;
        right.Normalize();
        return forward * input.z + right * input.x;
    }

    void RotateTowardsMovementDirection(Vector3 direction)
    {
        if (direction.magnitude > 0.01f)
        {
            // Calculate target rotation
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Smoothly rotate towards the target direction
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    void SetAnimationSpeed(string animName, float speed)
    {
        if (animator.GetAnimatorTransitionInfo(0).IsName(animName) || animator.GetCurrentAnimatorStateInfo(0).IsName(animName))
        {
            animator.speed = speed;
        }else{
            animator.speed = 1f;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ground"))
        {
            animator.SetBool("Grounded", true);
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ground"))
        {
            animator.SetBool("Grounded", false);
        }
    }
}
