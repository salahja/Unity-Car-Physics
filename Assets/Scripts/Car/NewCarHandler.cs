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

    [Header("Movement Settings")]
    public float maxMotorTorque = 500f;
    public float maxBrakeTorque = 1000f;
    public float maxSteeringAngle = 30f;
    public float currentSpeed;
    public float maxSpeed = 30f;

    private float inputVertical;
    private float inputHorizontal;
    private bool isBraking;


    [Header("Movement Settings")]
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
[Header("Audio Settings")]
[SerializeField] private float minEnginePitch = 0.5f;
[SerializeField] private float maxEnginePitch = 2.0f;
[SerializeField] private float engineSoundFadeSpeed = 2.0f;
[SerializeField] private float minRPMForSound = 100f;
    [Header("Audio Settings")]
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

    // Emissive property (if carMeshRender is still needed for visual effects)
    [Header("Visual Settings")]
    [SerializeField] private MeshRenderer carMeshRender;
    private int _EmissionColor = Shader.PropertyToID("_EmissionColor");
    private Color emissiveColor = Color.white;
    private float emissiveColorMultiplier = 0f;

    void Start()
    {
          rb = GetComponent<Rigidbody>();
    rb.isKinematic = false; // ← Critical for physics-based movement
 
        if (rb == null)
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
        //GetInput();
        UpdateWheelPoses();
    }

  void FixedUpdate()
    {
        HandleMovement();
        HandleSteering();
        UpdateCarAudio();
    }  
  
public void SetInput(Vector2 input)
{
        if (isCrashed) return;
        input = Vector2.ClampMagnitude(input, 1f);
    inputHorizontal = input.x;
    inputVertical = input.y;
    // braking will be handled separately if needed
}

public void SetBraking(bool braking)
{
    isBraking = braking;
}
/*

    void Update()
    {
        if (isCrashed)
        {
            crashTimer += Time.deltaTime;
            if (crashTimer >= crashDuration)
            {
                RecoverFromCrash();
            }
            return;
        }

        CheckGrounded();
        HandleJumpInput();

        if (carMeshRender != null)
        {
            float desiredCarEmissiveColorMultiplier = 0f;
            if (input.y < 0)
                desiredCarEmissiveColorMultiplier = 4.0f;

            emissiveColorMultiplier = Mathf.Lerp(emissiveColorMultiplier, desiredCarEmissiveColorMultiplier, Time.deltaTime * 4);
            carMeshRender.material.SetColor(_EmissionColor, emissiveColor * emissiveColorMultiplier);
        }
        UpdateCarAudio();
    }

    void FixedUpdate()
    {
        if (isCrashed)
        {
            rb.linearDamping = crashDrag;
            FadeOutCarAudio();
            return;
        }

        HandleMovement();
        HandleSteering();
    }

*/
    void CheckGrounded()
    {
        // Check if any of the wheel colliders are touching the ground
        isGrounded = frontRightCollider.bounds.Intersects(GetGroundBounds()) ||
                     frontLeftCollider.bounds.Intersects(GetGroundBounds()) ||
                     rearRightCollider.bounds.Intersects(GetGroundBounds()) ||
                     rearLeftCollider.bounds.Intersects(GetGroundBounds());

        if (isGrounded && isJumping && rb.linearVelocity.y <= 0)
        {
            isJumping = false;
        }
    }

    private Bounds GetGroundBounds()
    {
        // You might need a more sophisticated way to determine the ground bounds
        // based on your environment. This is a simple approach using a layer mask.
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

    void Reverse()
    {
        float currentReverseSpeed = -Vector3.Dot(rb.linearVelocity, transform.forward);
        if (currentReverseSpeed < reverseSpeed)
        {
            Vector3 reverseDirection = -transform.forward;
            rb.AddForceAtPosition(reverseDirection * reverseAccelerationMultiplier * Mathf.Abs(input.y), (rearRightWheel.position + rearLeftWheel.position) / 2f, ForceMode.Acceleration);
            rb.AddForceAtPosition(reverseDirection * reverseAccelerationMultiplier * Mathf.Abs(input.y), (frontRightWheel.position + frontLeftWheel.position) / 2f, ForceMode.Acceleration);
        }

        if (carReverseAS != null && !carReverseAS.isPlaying)
        {
            carReverseAS.Play();
        }
    }

    void StopBrakeSound()
    {
        if (carBrakeAS != null && carBrakeAS.isPlaying)
        {
            carBrakeAS.Stop();
        }
    }

    void StopReverseSound()
    {
        if (carReverseAS != null && carReverseAS.isPlaying)
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
        if (!brakingEnabled) return;

        Vector3 brakeDirection = -rb.linearVelocity.normalized;
        rb.AddForceAtPosition(brakeDirection * breakMultiplier * Mathf.Abs(input.y), (frontRightWheel.position + frontLeftWheel.position) / 2f, ForceMode.Acceleration);
        rb.AddForceAtPosition(brakeDirection * breakMultiplier * Mathf.Abs(input.y), (rearRightWheel.position + rearLeftWheel.position) / 2f, ForceMode.Acceleration);

        if (carBrakeAS != null && rb.linearVelocity.magnitude > brakeSoundThreshold && !carBrakeAS.isPlaying)
        {
            carBrakeAS.Play();
        }
    }

    // public void SetInput(Vector2 inputVector)
    // {
    //     if (isCrashed) return;
    //     input = Vector2.ClampMagnitude(inputVector, 1f);
    // }

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