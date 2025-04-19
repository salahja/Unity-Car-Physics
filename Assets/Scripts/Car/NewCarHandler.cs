using UnityEngine;

public class NewCarHandler : MonoBehaviour
{
    [Header("Car Parts")]
    [SerializeField] private Transform body;
    [SerializeField] private Transform spoiler;
    [SerializeField] private Transform frontRightWheel;
    [SerializeField] private Transform frontLeftWheel;
    [SerializeField] private Transform rearRightWheel;
    [SerializeField] private Transform rearLeftWheel;

    [Header("Wheel Colliders")]
    [SerializeField] private WheelCollider frontRightCollider;
    [SerializeField] private WheelCollider frontLeftCollider;
    [SerializeField] private WheelCollider rearRightCollider;
    [SerializeField] private WheelCollider rearLeftCollider;

    [Header("Movement Settings - Wheel Collider Based")]
    public float maxMotorTorque = 500f;
    public float maxBrakeTorque = 1000f;
    public float maxSteeringAngle = 30f;
    public float currentSpeed;
    public float maxSpeed = 30f;

    private float inputVertical;
    private float inputHorizontal;
    private bool isBraking;

    [Header("Movement Settings - Force Based (Redundant with Wheel Colliders)")]
    public float moveSpeed = 10f;
    public float reverseSpeed = 5f;
    public float accelerationMultiplier = 3f;
    public float reverseAccelerationMultiplier = 2f;
    public float breakMultiplier = 15f;
    public float maxForwardVelocity = 30f;
    [Range(0f, 1f)] public float driftFactor = 0.2f;

    [Header("Steering Settings")]
    public float steeringMultiplier = 2f;
    public float minSteeringSpeed = 5f;
    public float maxSteeringSpeed = 30f;
    [SerializeField] float steeringLerpSpeed = 5f;
    public Transform[] steeringWheels;
    public float wheelTurnRatio = 0.5f;
    private float currentSteerAngle;

    [Header("Jump Settings")]
    public float jumpForce = 10f;
    public float groundCheckDistance = 0.5f;
    public LayerMask groundLayer;
    private bool isGrounded;
    private bool isJumping;

    [Header("Crash Settings")]
    public float crashBounceForce = 5f;
    public float crashTorqueForce = 10f;
    public float crashRecoveryTime = 3f;
    public float crashDrag = 2f;
    private bool isCrashed = false;
    private float crashTimer = 0f;
    private float crashDuration = 3f;

    [Header("Audio Settings - Engine")]
    [SerializeField] private float minEnginePitch = 0.5f;
    [SerializeField] private float maxEnginePitch = 2.0f;
    [SerializeField] private float engineSoundFadeSpeed = 2.0f;
    [SerializeField] private float minRPMForSound = 100f;

    [Header("Audio Settings - Other")]
    [SerializeField] AudioSource carEngineAS;
    [SerializeField] AudioSource carReverseAS;
    [SerializeField] AudioSource carSkidAS;
    [SerializeField] AudioSource carCrashAS;
    [SerializeField] AudioSource carBrakeAS;
    [SerializeField] AnimationCurve carPitchAnimationCurve;
    [SerializeField] float brakeSoundThreshold = 5f;
    [SerializeField] float crashSoundThreshold = 5f;
    private float originalEngineVolume;
    private bool brakingEnabled = true;

    private Rigidbody rb;
    private Vector2 input = Vector2.zero;
    private bool isPlayer = true;

    [Header("Visual Settings")]
    [SerializeField] private MeshRenderer carMeshRender;
    private int _EmissionColor = Shader.PropertyToID("_EmissionColor");
    private Color emissiveColor = Color.white;
    private float emissiveColorMultiplier = 0f;

    [Header("Coin Detection")]
    [SerializeField] private BoxCollider coinTriggerCollider;

    void Start()
    {
        if (coinTriggerCollider != null)
        {
            coinTriggerCollider.isTrigger = true;
        }
        else
        {
            Debug.LogError("Assign the Body's BoxCollider as Coin Trigger in Inspector!");
        }

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false; // Ensure Rigidbody is not kinematic for physics-based movement
        }
        else
        {
            Debug.LogError("Rigidbody component not found on this GameObject.");
            enabled = false;
            return;
        }

        isPlayer = CompareTag("Player");
        if (isPlayer)
        {
            originalEngineVolume = carEngineAS.volume;
            if (carEngineAS != null) carEngineAS.Play();
            if (carSkidAS != null) carSkidAS.loop = true;
            if (carBrakeAS != null) carBrakeAS.loop = false;
            if (carCrashAS != null) carCrashAS.loop = false;
        }
    }

    void Update()
    {
        UpdateWheelPoses();
        CheckGrounded();
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleSteering();
        UpdateCarAudio();
    }

    public void SetInput(Vector2 inputVector)
    {
        if (isCrashed) return;
        input = Vector2.ClampMagnitude(inputVector, 1f);
        inputHorizontal = input.x;
        inputVertical = input.y;
        // Braking will be handled separately if needed
    }

    public void SetBraking(bool braking)
    {
        isBraking = braking;
    }

    void CheckGrounded()
    {
        // Check if any of the wheel colliders are touching the ground
        isGrounded = frontRightCollider.isGrounded ||
                     frontLeftCollider.isGrounded ||
                     rearRightCollider.isGrounded ||
                     rearLeftCollider.isGrounded;

        if (isGrounded && isJumping && rb.linearVelocity.y <= 0) // Use rb.velocity for Rigidbody
        {
            isJumping = false;
        }
    }

    private Bounds GetGroundBounds()
    {
        // This function is not used when checking WheelCollider.isGrounded
        // Consider removing it or using WheelCollider.GetGroundHit() for more info
        Collider[] groundColliders = Physics.OverlapSphere(transform.position - Vector3.up * groundCheckDistance, 0.1f, groundLayer);
        if (groundColliders.Length > 0)
        {
            Bounds combinedBounds = groundColliders[0].bounds;
            for (int i = 1; i < groundColliders.Length; i++)
            {
                combinedBounds.Encapsulate(groundColliders[i].bounds);
            }
            return combinedBounds;
        }
        return new Bounds(transform.position - Vector3.up * groundCheckDistance, Vector3.zero);
    }

    void HandleJumpInput()
    {
        if (isGrounded && (Input.GetKeyDown(KeyCode.UpArrow) || SwipeManager.swipeUp))
        {
            Jump();
        }
    }

    void Jump()
    {
        if (!isGrounded || isJumping) return;

        AudioManager audioManager = FindObjectOfType<AudioManager>();
        if (audioManager != null)
        {
            audioManager.PlaySound("Jump");
        }

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isJumping = true;
    }

    void HandleMovement()
    {
        currentSpeed = rb.linearVelocity.magnitude * 3.6f; // Convert to km/h

        if (currentSpeed < maxSpeed)
        {
            // Apply motor torque to rear wheels (for RWD car)
            rearRightCollider.motorTorque = inputVertical * maxMotorTorque;
            rearLeftCollider.motorTorque = inputVertical * maxMotorTorque;
        }
        else
        {
            rearRightCollider.motorTorque = 0;
            rearLeftCollider.motorTorque = 0;
        }

        // Apply brakes
        float brakeTorque = isBraking ? maxBrakeTorque : 0f;
        frontRightCollider.brakeTorque = brakeTorque;
        frontLeftCollider.brakeTorque = brakeTorque;
        rearRightCollider.brakeTorque = brakeTorque;
        rearLeftCollider.brakeTorque = brakeTorque;
    }

    void HandleSteering()
    {
        float steeringAngle = maxSteeringAngle * inputHorizontal;
        frontRightCollider.steerAngle = steeringAngle;
        frontLeftCollider.steerAngle = steeringAngle;
    }

    void UpdateWheelPoses()
    {
        UpdateWheelPose(frontRightCollider, frontRightWheel);
        UpdateWheelPose(frontLeftCollider, frontLeftWheel);
        UpdateWheelPose(rearRightCollider, rearRightWheel);
        UpdateWheelPose(rearLeftCollider, rearLeftWheel);
    }

    void UpdateWheelPose(WheelCollider collider, Transform wheelTransform)
    {
        collider.GetWorldPose(out Vector3 position, out Quaternion rotation);
        wheelTransform.position = position;
        wheelTransform.rotation = rotation;
    }

    // The following methods (Reverse, StopBrakeSound, StopReverseSound, RecoverFromCrash, Brake)
    // seem to be remnants from a non-WheelCollider based implementation.
    // With WheelColliders, braking and reverse are typically handled through
    // brakeTorque and negative motorTorque. Consider if you need these.

    void Reverse()
    {
        // Consider using negative motorTorque with WheelColliders instead
        float currentReverseSpeed = -Vector3.Dot(rb.linearVelocity, transform.forward);
        if (currentReverseSpeed < reverseSpeed)
        {
            rearRightCollider.motorTorque = -Mathf.Abs(inputVertical) * maxMotorTorque * reverseAccelerationMultiplier;
            rearLeftCollider.motorTorque = -Mathf.Abs(inputVertical) * maxMotorTorque * reverseAccelerationMultiplier;
        }

        if (carReverseAS != null && !carReverseAS.isPlaying && inputVertical < 0 && currentSpeed < 5f) // Play when trying to reverse at low speed
        {
            carReverseAS.Play();
        }
        else if (carReverseAS != null && carReverseAS.isPlaying && inputVertical >= 0)
        {
            carReverseAS.Stop();
        }
    }

    void StopBrakeSound()
    {
        if (carBrakeAS != null && carBrakeAS.isPlaying && !isBraking)
        {
            carBrakeAS.Stop();
        }
    }

    void StopReverseSound()
    {
        if (carReverseAS != null && carReverseAS.isPlaying && inputVertical >= 0)
        {
            carReverseAS.Stop();
        }
    }

    void RecoverFromCrash()
    {
        isCrashed = false;
        brakingEnabled = true;
        crashTimer = 0f;
        rb.linearDamping = 0f;
        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

        if (carEngineAS != null)
        {
            carEngineAS.volume = originalEngineVolume;
        }
    }

    void Brake()
    {
        // Braking is now handled in HandleMovement using brakeTorque on WheelColliders
        if (!brakingEnabled) return;

        float brakeTorque = maxBrakeTorque * Mathf.Abs(inputVertical);
        frontRightCollider.brakeTorque = brakeTorque;
        frontLeftCollider.brakeTorque = brakeTorque;
        rearRightCollider.brakeTorque = brakeTorque;
        rearLeftCollider.brakeTorque = brakeTorque;

        if (carBrakeAS != null && rb.linearVelocity.magnitude > brakeSoundThreshold && !carBrakeAS.isPlaying && isBraking)
        {
            carBrakeAS.Play();
        }
        else if (carBrakeAS != null && carBrakeAS.isPlaying && !isBraking)
        {
            carBrakeAS.Stop();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isCrashed) return;

        float impactForce = collision.impulse.magnitude / Time.fixedDeltaTime; // Approximate impact force
        if (impactForce > 5f)
        {
            HandleCrash(collision);

            if (impactForce > crashSoundThreshold && carCrashAS != null)
            {
                carCrashAS.Play();
            }
        }
    }

    void HandleCrash(Collision collision)
    {
        isCrashed = true;
        brakingEnabled = false;
        crashTimer = 0f;

        if (!isPlayer && (collision.transform.root.CompareTag("Untagged") || collision.transform.root.CompareTag("CarAI")))
        {
            return;
        }

        Vector3 bounceDirection = Vector3.Reflect(rb.linearVelocity.normalized, collision.contacts[0].normal);
        rb.AddForce(bounceDirection * crashBounceForce, ForceMode.VelocityChange);

        Vector3 torqueDirection = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(0.5f, 1f),
            Random.Range(-1f, 1f)
        );
        rb.AddTorque(torqueDirection * crashTorqueForce, ForceMode.VelocityChange);

        input = Vector2.zero;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position - Vector3.up * groundCheckDistance);
    }

    void UpdateCarAudio()
    {
        if (!isPlayer || carEngineAS == null) return;

        // Get average RPM from all powered wheels
        float averageRPM = (frontLeftCollider.rpm + frontRightCollider.rpm +
                           rearLeftCollider.rpm + rearRightCollider.rpm) / 4f;

        // Only play engine sound if wheels are moving significantly
        if (Mathf.Abs(averageRPM) > minRPMForSound)
        {
            if (!carEngineAS.isPlaying)
            {
                carEngineAS.Play();
            }

            // Calculate pitch based on RPM (absolute value)
            float rpmRatio = Mathf.Clamp01(Mathf.Abs(averageRPM) / 5000f);
            float targetPitch = Mathf.Lerp(minEnginePitch, maxEnginePitch, rpmRatio);
            carEngineAS.pitch = Mathf.Lerp(carEngineAS.pitch, targetPitch, Time.deltaTime * engineSoundFadeSpeed);

            // Fade in volume
            carEngineAS.volume = Mathf.Lerp(carEngineAS.volume, originalEngineVolume, Time.deltaTime * engineSoundFadeSpeed);
        }
        else
        {
            // Fade out volume when not moving
            carEngineAS.volume = Mathf.Lerp(carEngineAS.volume, 0, Time.deltaTime * engineSoundFadeSpeed);

            // Stop completely when volume is very low
            if (carEngineAS.volume < 0.05f)
            {
                carEngineAS.Stop();
            }
        }

        // Handle reverse sound
        if (carReverseAS != null)
        {
            if (inputVertical < 0 && currentSpeed < 5f && Mathf.Abs(averageRPM) > minRPMForSound)
            {
                if (!carReverseAS.isPlaying)
                {
                    carReverseAS.Play();
                }
            }
            else if (carReverseAS != null && carReverseAS.isPlaying && inputVertical >= 0)
            {
                carReverseAS.Stop();
            }
        }

        // Handle brake sound
        if (carBrakeAS != null)
        {
            if (isBraking && rb.linearVelocity.magnitude > brakeSoundThreshold && !carBrakeAS.isPlaying)
            {
                carBrakeAS.Play();
            }
            else if (carBrakeAS != null && carBrakeAS.isPlaying && !isBraking)
            {
                carBrakeAS.Stop();
            }
        }
    }

    void FadeOutCarAudio()
    {
        if (!isPlayer)
            return;

        if (carEngineAS != null)
        {
            carEngineAS.volume = Mathf.Lerp(carEngineAS.volume, 0, Time.deltaTime * 10);
        }
        if (carSkidAS != null)
        {
            carSkidAS.volume = Mathf.Lerp(carSkidAS.volume, 0, Time.deltaTime * 10);
        }
        if (carBrakeAS != null)
        {
            carBrakeAS.volume = Mathf.Lerp(carBrakeAS.volume, 0, Time.deltaTime * 10);
        }
    }
} 
