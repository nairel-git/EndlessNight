using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CharacterController controller;
    [SerializeField] Animator anim;
    [SerializeField] Transform cameraTransform;

    [Header("Movement Settings")]
    [SerializeField] float walkSpeed = 4.0f;
    [SerializeField] float runSpeed = 7.0f;
    [SerializeField] float gravity = -9.81f;

    [Header("Input")]
    [SerializeField] float smoothInputSpeed = 0.1f;
    [SerializeField] float rotationSpeed = 600.0f;

    [Header("Settings")]
    [SerializeField] LayerMask groundLayerMask;
    

    //Internal

    [Header("Status")]
    public bool isGrounded;
    public bool isCarrying;

    Vector2 horizontalInput;
    Vector2 smoothInput;
    Vector2 smoothInputVelocity;
    float verticalMovement;
    float currentSpeed;

    void Update()
    {
        if (!controller.enabled)
            return;


        // --- 1. Input Processing ---
        horizontalInput = InputManager.Instance.PlayerMovement();
        smoothInput = Vector2.SmoothDamp(smoothInput, horizontalInput, ref smoothInputVelocity, smoothInputSpeed);
        
        // --- 2. Gravity Check ---
        // Ground Check: Use a slightly smaller radius (0.9f) to avoid treating walls as ground
        Vector3 spherePosition = transform.position + Vector3.down * (controller.skinWidth + 0.05f);
        isGrounded = Physics.CheckSphere(spherePosition, controller.radius * 0.9f, groundLayerMask, QueryTriggerInteraction.Ignore);

        // Apply Gravity
        if (isGrounded && verticalMovement < 0)
            verticalMovement = -1f; // Slight downward force helps keep the player snapped to the floor
        else
            verticalMovement += gravity * Time.deltaTime;

        // Optional: Clamp falling speed (Terminal Velocity) so you don't fall infinitely fast
        if (verticalMovement < -20f) 
            verticalMovement = -20f;


        // --- 3. Determine Speed ---
        float targetMaxSpeed = InputManager.Instance.PlayerSprint() ? runSpeed : walkSpeed;
        currentSpeed = Mathf.Lerp(currentSpeed, targetMaxSpeed, 10.0f * Time.deltaTime);

        float finalSpeed = currentSpeed;

        // --- 4. Calculate Movement Direction ---
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 wishDir = camForward * smoothInput.y + camRight * smoothInput.x;

        if (wishDir.magnitude > 1f)
            wishDir.Normalize();

        Vector3 finalMove = wishDir * finalSpeed;

        // Assign vertical velocity to the Y axis
        finalMove.y = verticalMovement;

        // --- 5. Apply Movement ---
        // Note: CharacterController.Move requires displacement (Velocity * Time)
        controller.Move(finalMove * Time.deltaTime);


        //Animate Walk
        anim.SetFloat("velocityXZ",wishDir.magnitude * finalSpeed / runSpeed);

        // --- 7. Rotation ---
        if (wishDir.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(wishDir.x, 0, wishDir.z));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }




}