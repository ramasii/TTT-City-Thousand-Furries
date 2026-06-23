using UnityEngine;

public class FollowingObject : MonoBehaviour
{
    public Transform target; // The object to follow
    public Vector3 offset; // Offset from the target's position
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (target != null)
        {
            // Update the position of this object to follow the target with the specified offset
            transform.position = target.position + offset;
        }
    }
}
