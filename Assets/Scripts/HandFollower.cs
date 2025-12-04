using UnityEngine;

public class HandFollower : MonoBehaviour
{
    public Transform target;

    void LateUpdate()
    {
        transform.position = target.position;
        transform.rotation = target.rotation;
    }
}