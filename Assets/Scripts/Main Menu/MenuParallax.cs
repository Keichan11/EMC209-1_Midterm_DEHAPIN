using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System

public class MenuParallax : MonoBehaviour
{
    public float offsetMultiplier = 1f;
    public float smoothTime = .3f;

    private Vector2 startPosition;
    private Vector3 velocity;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        // Make sure a mouse is connected to prevent null reference errors
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 offset = new Vector2(
            mousePos.x / Screen.width,
            mousePos.y / Screen.height
        );

        transform.position = Vector3.SmoothDamp(
            transform.position, 
            startPosition + (offset * offsetMultiplier), 
            ref velocity, 
            smoothTime
        );
    }
}
