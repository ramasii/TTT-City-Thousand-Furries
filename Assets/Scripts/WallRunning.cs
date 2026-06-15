using UnityEngine;
using UnityEngine.InputSystem;

public class WallRunning : MonoBehaviour
{
    [Header("Wall Running")]
    public LayerMask wallLayer;
    public LayerMask groundLayerMask;
    public float wallRunForce;
    public float maxWallRunTime;
    public float wallClimbSpeed;
    float wallRunTimer;

    [Header("Input")]
    float horizontalInput;
    float verticalInput;
    bool upwardsRunning;
    bool downwardsRunning;

    [Header("Detection")]
    public float wallCheckDistance;
    public float minJumpHeight;
    RaycastHit leftWallHit;
    RaycastHit rightWallHit;
    bool wallLeft;
    bool wallRight;

    [Header("References")]
    public Transform orientation;
    PlayerMovement pm;
    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        CheckForWall();
        StateMachine();
    }

    void FixedUpdate()
    {
        if(pm.wallRunning)
        {
            WallRunningMovement();
        }
    }

    void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance, wallLayer);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance, wallLayer);
    }

    bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, groundLayerMask);
    }

    void StateMachine()
    {
        // state 1 -  wall running
        if((wallLeft || wallRight) && verticalInput > 0 && AboveGround())
        {
            if(!pm.wallRunning) StartWallRun();
        }

        // state 2 - not wall running
        else
        {
            if(pm.wallRunning) StopWallRun();
        }
    }

    void StartWallRun()
    {
        pm.wallRunning = true;
    }

    void WallRunningMovement()
    {
        rb.useGravity = false;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        // memastikan wallForward mengarah ke arah yang benar
        if((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
        {
            wallForward = -wallForward;
        }

        // force ke depan
        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        // force ke atas/bawah
        if(upwardsRunning)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, wallClimbSpeed, rb.linearVelocity.z);
        }
        if(downwardsRunning)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, -wallClimbSpeed, rb.linearVelocity.z);
        }

        // force ke dinding
        if(!(wallLeft && horizontalInput > 0) && !(wallRight && horizontalInput < 0))
        {
            rb.AddForce(-wallNormal * 100, ForceMode.Force);
        }
    }

    void StopWallRun()
    {
        pm.wallRunning = false;
        rb.useGravity = true;
    }

    // ambil input
    public void OnMove(InputAction.CallbackContext value)
    {
        Vector2 input = value.ReadValue<Vector2>();
        horizontalInput = input.x;
        verticalInput = input.y;
    }

    public void OnSprint(InputAction.CallbackContext value)
    {
        if(value.started) upwardsRunning = true;
        else if(value.canceled) upwardsRunning = false;
    }

    public void OnCrouch(InputAction.CallbackContext value)
    {
        if(value.started) downwardsRunning = true;
        else if(value.canceled) downwardsRunning = false;
    }
}
