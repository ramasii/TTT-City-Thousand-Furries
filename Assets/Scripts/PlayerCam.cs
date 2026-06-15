using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCam : MonoBehaviour
{
    public Transform playerBody;
    public Transform camTarget;
    public float sensX;
    public float sensY;
    float xRotation;
    float yRotation;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        // rotasi camTarget
        camTarget.rotation = Quaternion.Euler(xRotation, yRotation, 0);
    }

    void LateUpdate()
    {
        // rotasi playerBody mengikuti camTarget pada sumbu Y
        playerBody.rotation = Quaternion.Euler(0, yRotation, 0);
    }

    public void OnLook(InputAction.CallbackContext value)
    {
        Vector2 mouseInput = value.ReadValue<Vector2>();
        // ambil input mouse
        float mouseX = mouseInput.x * sensX * Time.deltaTime;
        float mouseY = mouseInput.y * sensY * Time.deltaTime;

        yRotation += mouseX; // lihat kanan-kiri
        xRotation -= mouseY; // lihat atas-bawah
        xRotation = Mathf.Clamp(xRotation, -70f, 70f); // batasi rotasi vertikal agar tidak berputar penuh
    }
}
