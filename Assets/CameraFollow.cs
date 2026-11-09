using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float distance = 6.5f;
    public float lookHeight = 1.25f;
    public float sensitivity = 0.12f;
    public float followSharpness = 6.5f;
    public float pitch = 17f;
    public bool alignBehindPlayerMovement = true;
    public float movementAlignSharpness = 2.5f;

    private float yaw;

    private void Start()
    {
        if (target != null) yaw = target.eulerAngles.y;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            yaw += delta.x * sensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, 5f, 45f);
        }
        else if (alignBehindPlayerMovement && !HasMovementInput())
        {
            // Ease behind the last travel direction after movement stops.
            // Keeping yaw steady while moving preserves straight camera-relative
            // paths instead of bending the direction as the camera turns.
            float alignBlend = 1f - Mathf.Exp(-movementAlignSharpness * Time.deltaTime);
            yaw = Mathf.LerpAngle(yaw, target.eulerAngles.y, alignBlend);
        }

        Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focus = target.position + Vector3.up * lookHeight;
        Vector3 desired = focus - orbit * Vector3.forward * distance;
        float blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, blend);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(focus - transform.position), blend);
    }

    private static bool HasMovementInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return false;
        return keyboard.wKey.isPressed || keyboard.aKey.isPressed ||
               keyboard.sKey.isPressed || keyboard.dKey.isPressed ||
               keyboard.upArrowKey.isPressed || keyboard.leftArrowKey.isPressed ||
               keyboard.downArrowKey.isPressed || keyboard.rightArrowKey.isPressed;
    }
}
