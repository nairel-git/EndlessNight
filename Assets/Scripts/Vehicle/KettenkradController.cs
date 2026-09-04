using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KettenkradController : MonoBehaviour
{

    private Rigidbody rb;
    private Animator anim;
    private KettenkradStats stats;


    [Header("1. Setup")]
    [SerializeField] LayerMask ground;
    [SerializeField] Vector3 centerOfMass = new Vector3(0, -0.4f, 0.15f);

    [Header("2. Dimensions")]
    [SerializeField] float trackWidth = 1.1f;
    [SerializeField] float trackLength = 2.4f;
    [SerializeField] float rayOffset = 0.5f;
    [Range(2, 12)][SerializeField] int raysPerSide = 6;

    [Header("3. Suspension")]
    [SerializeField] float suspensionLen = 0.6f;
    [SerializeField] float springForce = 35000f;
    [SerializeField] float damperForce = 4500f;

    [Header("4. Driving")]
    [SerializeField] float acceleration = 12000f;
    [SerializeField] float maxSpeed = 16f;

    [Header("5. Turning")]
    [SerializeField] float turnTorque = 12000f;
    [SerializeField] float turnGripLoss = 0.2f;
    [SerializeField] float airborneDrag = 10.0f;
    [SerializeField] float minTurnSpeed = 0.5f;

    [Header("6. Braking")]
    [SerializeField] float brakeForce = 18000f;
    [SerializeField] float brakeAngularDrag = 6f;
    [SerializeField] float brakeSideFriction = 2.5f;



    // Inputs
    private float moveInput;
    private float turnInput;
    private float currentSpeed;
    private float groundedRatio;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        stats = GetComponent<KettenkradStats>();

        rb.centerOfMass = centerOfMass;
    }

    void FixedUpdate()
    {

        if(!stats.IsEngineOn || InputManager.Instance.VehicleBreak())
            Break();


        
        currentSpeed = Vector3.Dot(transform.forward, rb.velocity);

        int totalRays = raysPerSide * 2;
        int hitCount = 0;

        hitCount += ProcessSide(-1); // Left
        hitCount += ProcessSide(1);  // Right

        groundedRatio = (float)hitCount / totalRays;

        AnimateParts();
        ApplyTurning();

        if (stats.CanDrive)
        {
            Vector2 Movement = InputManager.Instance.VehicleMovement();
            moveInput = Movement.y;
            turnInput = Movement.x;
        }
    
        stats.ConsumeFuel(moveInput);

    }


    void Break()
    {
 
        moveInput = 0f;
        turnInput = 0f;

        float grip = Mathf.Clamp01(groundedRatio);

        Vector3 forwardVel = Vector3.Project(rb.velocity, transform.forward);

        if (forwardVel.sqrMagnitude > 0.01f)
        {
            Vector3 brakeDir = -forwardVel.normalized;
            rb.AddForce(brakeDir * brakeForce * grip, ForceMode.Force);
        }

        Vector3 localVel = transform.InverseTransformDirection(rb.velocity);
        Vector3 sideVel = transform.right * localVel.x;

        rb.AddForce(-sideVel * brakeForce * brakeSideFriction * grip, ForceMode.Force);

        rb.angularVelocity = Vector3.Lerp(
            rb.angularVelocity,
            new Vector3(rb.angularVelocity.x, 0f, rb.angularVelocity.z),
            Time.fixedDeltaTime * brakeAngularDrag * grip
        );
    }



    int ProcessSide(int sideSign)
    {
        int hits = 0;
        float xOffset = trackWidth * 0.5f * sideSign;
        float segmentLen = trackLength / (raysPerSide - 1);

        for (int i = 0; i < raysPerSide; i++)
        {
            float zPos = -(trackLength * 0.5f) + (segmentLen * i);
            Vector3 origin = transform.TransformPoint(new Vector3(xOffset, rayOffset, zPos));

            if (Physics.Raycast(origin, -transform.up, out RaycastHit hit, suspensionLen, ground))
            {
                hits++;

                // --- SUSPENSION ---
                Vector3 velAtPoint = rb.GetPointVelocity(origin);
                float compression = 1.0f - (hit.distance / suspensionLen);
                float velUp = Vector3.Dot(transform.up, velAtPoint);

                float force = (compression * springForce) - (velUp * damperForce);
                rb.AddForceAtPosition(transform.up * force, hit.point);

                // --- FRICTION ---
                Vector3 localVel = transform.InverseTransformDirection(velAtPoint);
                float gripMod = (Mathf.Abs(turnInput) > 0.1f) ? (1f - turnGripLoss) : 1f;

                Vector3 sideForce = -transform.right * localVel.x * (springForce / 12f) * gripMod;
                rb.AddForceAtPosition(sideForce, hit.point);

                // --- ACCELERATION ---
                if (Mathf.Abs(moveInput) > 0.1f && Mathf.Abs(currentSpeed) < maxSpeed)
                    rb.AddForceAtPosition(transform.forward * moveInput * (acceleration / (raysPerSide * 2)), hit.point);
            }
        }

        
        return hits;
    }

    void AnimateParts()
    {
        anim.SetFloat("steer", turnInput, 0.15f, Time.deltaTime);

        float signedSpeed = Vector3.Dot(transform.forward, rb.velocity);

        if (groundedRatio > 0)
            anim.SetFloat("speed", signedSpeed * 0.25f);
        else
            anim.SetFloat("speed", moveInput);

    }

    void ApplyTurning()
    {
        // 1. AIRBORNE SAFETY
        if (groundedRatio < 0.2f)
        {
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, new Vector3(rb.angularVelocity.x, 0, rb.angularVelocity.z), Time.fixedDeltaTime * airborneDrag);
            return;
        }

        // 2. GROUNDED TURNING
        if (Mathf.Abs(turnInput) > 0.1f)
        {
            float absSpeed = Mathf.Abs(currentSpeed);

            // DEADZONE CHECK:
            // If speed is less than minTurnSpeed (0.5), force factor to 0.
            // We use InverseLerp to create a smooth ramp from 0.5 to 2.5 speed.
            float moveFactor = Mathf.InverseLerp(minTurnSpeed, minTurnSpeed + 3.0f, absSpeed);

            if (moveFactor <= 0.01f)
                return; // Strict exit if stationary

            float directionMult = (currentSpeed < -0.5f) ? -1f : 1f;

            float finalTorque = turnInput * turnTorque * moveFactor * directionMult * groundedRatio;

            rb.AddTorque(transform.up * finalTorque);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.TransformPoint(centerOfMass), 0.2f);
        DrawSide(-1);
        DrawSide(1);
    }

    void DrawSide(int side)
    {
        float xOffset = trackWidth * 0.5f * side;
        float segmentLen = trackLength / (raysPerSide - 1);
        for (int i = 0; i < raysPerSide; i++)
        {
            float zPos = -(trackLength * 0.5f) + (segmentLen * i);
            Vector3 origin = transform.TransformPoint(new Vector3(xOffset, rayOffset, zPos));
            Gizmos.DrawLine(origin, origin - transform.up * suspensionLen);
        }
    }
}