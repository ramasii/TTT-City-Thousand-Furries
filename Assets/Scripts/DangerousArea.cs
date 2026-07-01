using UnityEngine;

public class DangerousArea : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Handle player entering the dangerous area
            Debug.Log("Player has entered the dangerous area!");
            // You can add additional logic here, such as reducing health or triggering an event.
            GameManager.Instance.LoseGame(); // Call the LoseGame method from GameManager
        }
    }
}
