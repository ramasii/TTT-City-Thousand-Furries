using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// This script handles the player's movement using Rigidbody physics. It allows the player to move in four directions (forward, backward, left, right) based on input from the keyboard. The movement is smooth and physics-based, giving a more realistic feel to the player's movement. The Rigidbody's rotation is frozen to prevent unwanted rotations due to physics interactions.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public Transform orientation;
    public float moveSpeed = 5f;
    public float groundDrag;
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump = true;

    [Header("GroundCheck")]
    public float playerHeight;
    public float checkExtend;
    public LayerMask groundLayer;
    [SerializeField] bool grounded;
    float horizontalInput;
    float verticalInput;
    Vector3 moveDirection;
    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Mencegah karakter berputar akibat interaksi fisika
    }

    void Update()
    {
        // cek grounded
        Vector3 rayOrigin = transform.position + Vector3.up * playerHeight;
        grounded = Physics.Raycast(rayOrigin, Vector3.down, playerHeight * 0.5f + checkExtend, groundLayer);

        SpeedControl();

        // tangani drag saat di tanah
        if (grounded)
        {
            rb.linearDamping = groundDrag;
        }
        else
        {
            rb.linearDamping = 0;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 rayOrigin = transform.position + Vector3.up * playerHeight;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * (playerHeight * 0.5f + checkExtend));
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    public void OnMove(InputAction.CallbackContext value)
    {
        Vector2 input = value.ReadValue<Vector2>();
        horizontalInput = input.x;
        verticalInput = input.y;
    }

    public void OnJump(InputAction.CallbackContext value)
    {
        if (value.started && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    void MovePlayer()
    {
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // gerak di tanah
        if(grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        
        // gerak di udara
        else if(!grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
    }

    void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }

    void Jump()
    {
        // reset y vel
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    void ResetJump()
    {
        readyToJump = true;
    }
}
