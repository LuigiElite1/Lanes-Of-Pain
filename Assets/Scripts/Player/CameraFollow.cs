using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;

    private Vector3 offset;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Remember where the camera started relative to the player.
        offset = transform.position - player.position;
    }

    void LateUpdate()
    {
        // Keep that same relative position.
        transform.position = player.position + offset;
    }

}
