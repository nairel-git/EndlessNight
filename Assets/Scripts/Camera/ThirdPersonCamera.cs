using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform target;

    [Header("Settings")]
    [SerializeField] Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] float distance = 4f;

    [Header("Sensitivity")]
    [SerializeField] float lookSensitivity = 0.025f;

    [Header("Limits")]
    [SerializeField] float minPitch = -35f;
    [SerializeField] float maxPitch = 70f;

    [Header("Smoothing")]
    [SerializeField] float rotationSmoothTime = 0.12f;
    [SerializeField] float positionSmoothTime = 0.1f;

    [Header("Collision")]
    [SerializeField] float collisionRadius = 0.15f;
    [SerializeField] LayerMask collisionMask;

    float yaw;
    float pitch;
    float currentDistance;

    Vector3 posVelocity;
    float yawVelocity;
    float pitchVelocity;
    float distVelocity;

    float smoothYaw;
    float smoothPitch;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Vector3 angles = transform.eulerAngles;
        yaw = smoothYaw = angles.y;
        pitch = smoothPitch = angles.x;
        currentDistance = distance;
    }

    void LateUpdate()
    {

        if (Time.deltaTime == 0)
            return;

        HandleRotation();
        HandlePosition();
    }

    void HandleRotation()
    {
        Vector2 input = InputManager.Instance.Look();
        
        yaw += input.x * lookSensitivity;
        pitch -= input.y * lookSensitivity;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Smooth rotation
        smoothYaw = Mathf.SmoothDampAngle(smoothYaw, yaw, ref yawVelocity, rotationSmoothTime);
        smoothPitch = Mathf.SmoothDampAngle(smoothPitch, pitch, ref pitchVelocity, rotationSmoothTime);

        // Apply rotation immediately
        transform.rotation = Quaternion.Euler(smoothPitch, smoothYaw, 0f);
    }


    void HandlePosition()
    {

        // Standard pivot logic
        Vector3 pivot = target.position + pivotOffset;
        Vector3 dir = Quaternion.Euler(smoothPitch, smoothYaw, 0f) * Vector3.back;

        // Collision Check
        float targetDist = distance;
        if (Physics.SphereCast(pivot, collisionRadius, dir, out RaycastHit hit, distance, collisionMask))
        {
            // Ensure we don't snap inside the player head (min 0.5 distance)
            targetDist = Mathf.Max(0.5f, hit.distance - 0.05f);
        }

        currentDistance = Mathf.SmoothDamp(currentDistance, targetDist, ref distVelocity, 0.05f);

        Vector3 desiredPos = pivot + dir * currentDistance;
        if (!float.IsNaN(desiredPos.x))
            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref posVelocity, positionSmoothTime, float.PositiveInfinity, Time.deltaTime);
    }
}