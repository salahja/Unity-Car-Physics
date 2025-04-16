using UnityEngine;

public class CarHandler : MonoBehaviour
{
   
    [Header("Movement Settings")]
    public float reverseSpeed = 5f; // Speed when moving backwards
    public float reverseAccelerationMultiplier = 2f; // Acceleration multiplier for reverse

    [SerializeField] Transform gameModel;
    [SerializeField] MeshRenderer carMeshRender;
    [SerializeField] private Rigidbody rb;
    public float moveSpeed = 10f;
    public float turnSpeed = 50f;
    bool isCrashed = false;
    float crashTimer = 0f;
    float crashDuration = 3f;

    [Header("Steering Settings")]
    public float steeringMultiplier = 2f;
    public float maxSteeringAngle = 25f;
    public float minSteeringSpeed = 5f;
    public float maxSteeringSpeed = 30f;
    [Range(0f, 1f)] public float driftFactor = 0.2f;

    Vector2 input = Vector2.zero;
    float accelerationMultiplier = 3f;
    float breakMultiplier = 15f;

    //emissive property
    int _EmissionColor = Shader.PropertyToID("_EmissionColor");
    Color emissiveColor = Color.white;
    float emissiveColorMultiplier = 0f;

    [Header("Crash Settings")]
    public float crashBounceForce = 5f;
    public float crashTorqueForce = 10f;
    public float crashRecoveryTime = 3f;
    public float crashDrag = 2f;
    float maxForwardVelocity = 30;
    [Header("Wheel Settings")]
    public Transform[] steeringWheels;
    public float wheelTurnRatio = 0.5f;
    [SerializeField] float steeringLerpSpeed = 5f;
    private float currentSteerAngle;



    [Header("Jump Settings")]
    public float jumpForce = 10f;
    public float groundCheckDistance = 0.5f;
    public LayerMask groundLayer;
    private bool isGrounded;
    private bool isJumping;
    bool isPlayer = true;

    [Header("Audio Settings")]
    [SerializeField] AudioSource carEngineAS;
    [SerializeField] AudioSource carReverseAS;
    [SerializeField] AudioSource carSkidAS;
    [SerializeField] AudioSource carCrashAS; // New audio source for crash sounds
    [SerializeField] AudioSource carBrakeAS; // New audio source for brake sounds
    [SerializeField] AnimationCurve carPitchAnimationCurve;
    [SerializeField] float brakeSoundThreshold = 5f; // Minimum speed to play brake sound
    [SerializeField] float crashSoundThreshold = 5f; // Minimum impact force to play crash sound
    // [Previous variables remain the same...]
    private float originalEngineVolume; // Store original engine volume
    private bool brakingEnabled = true; // New flag to control braking
    void Start()
    {
        isPlayer = CompareTag("Player");
        if (isPlayer)
        {
            // Store original engine volume
            originalEngineVolume = carEngineAS.volume;

            carEngineAS.Play();
            // Initialize other audio sources if they exist
            if (carSkidAS != null)
            {
                carSkidAS.loop = true;
            }
            if (carBrakeAS != null)
            {
                carBrakeAS.loop = false;
            }
            if (carCrashAS != null)
            {
                carCrashAS.loop = false;
            }
        }
    }

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

        gameModel.transform.rotation = Quaternion.Euler(0, rb.linearVelocity.x * 5, 0);

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

    void CheckGrounded()
    {
        // Raycast downward to check if car is grounded
        isGrounded = Physics.Raycast(transform.position, -Vector3.up, groundCheckDistance, groundLayer);

        // Reset jumping flag when grounded
        if (isGrounded && isJumping && rb.linearVelocity.y <= 0)
        {
            isJumping = false;
        }
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

        // Play jump sound if available
        AudioManager audioManager = FindObjectOfType<AudioManager>();
        if (audioManager != null)
        {
            audioManager.PlaySound("Jump");
        }

        // Apply jump force
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isJumping = true;
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
        HandleAdvancedSteering();
    }

    void HandleAdvancedSteering()
    {
        float currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float speedFactor = Mathf.Clamp01(
            (Mathf.Abs(currentSpeed) - minSteeringSpeed) /
            (maxSteeringSpeed - minSteeringSpeed)
        );

        float targetAngle = maxSteeringAngle * input.x * (1f - speedFactor);
        currentSteerAngle = Mathf.Lerp(
            currentSteerAngle,
            targetAngle,
            steeringLerpSpeed * Time.fixedDeltaTime
        );
        // Reverse steering effect when moving backward

        if (currentSpeed < 0)
        {
            targetAngle *= -1; // Invert steering when reversing
        }
        foreach (var wheel in steeringWheels)
        {
            wheel.localRotation = Quaternion.Euler(0f, currentSteerAngle, 0f);
        }

        rb.AddTorque(
            transform.up * currentSteerAngle * 0.05f * currentSpeed,
            ForceMode.VelocityChange
        );

        if (Mathf.Abs(currentSteerAngle) > 5f)
        {
            rb.AddForce(
                -transform.right * currentSteerAngle * 0.1f,
                ForceMode.Acceleration
            );
        }
    } 


void HandleMovement()
{
    if (isCrashed) return;

    if (input.y > 0)
    {
        // Forward acceleration
        Accelerate();
        StopReverseSound(); // Ensure reverse sound stops when accelerating forward
    }
    else if (input.y < 0)
    {
        // Check current movement direction
        float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        
        if (currentForwardSpeed > 0.1f)
        {
            // If moving forward, apply brakes
            Brake();
            StopReverseSound(); // Ensure reverse sound stops when braking
        }
        else
        {
            // If stopped or moving backward, apply reverse
            Reverse();
            StopBrakeSound(); // Ensure brake sound stops when reversing
        }
    }
    else
    {
        // No input - apply some damping
        rb.linearDamping = 0.5f;
        StopBrakeSound();
        StopReverseSound();
    }
}

void Reverse()
{
    // Limit reverse speed
    float currentReverseSpeed = -Vector3.Dot(rb.linearVelocity, transform.forward);
    if (currentReverseSpeed < reverseSpeed)
    {
        Vector3 reverseDirection = -transform.forward;
        rb.AddForce(reverseDirection * reverseAccelerationMultiplier * Mathf.Abs(input.y),
                   ForceMode.Acceleration);
    }

    // Play reverse sound effect if needed
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
        brakingEnabled = true; // Re-enable braking after recovery
        crashTimer = 0f;
        rb.linearDamping = 0f;
        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

        // Restore engine audio
        if (carEngineAS != null)
        {
            carEngineAS.volume = originalEngineVolume;
        }
    }

    void Brake()
    {
        if (!brakingEnabled) return; // Skip braking if disabled

        Vector3 brakeDirection = -rb.linearVelocity.normalized;
        rb.AddForce(brakeDirection * breakMultiplier * Mathf.Abs(input.y),
                   ForceMode.Acceleration);

        // Play brake sound if moving fast enough and audio source exists
        if (carBrakeAS != null && rb.linearVelocity.magnitude > brakeSoundThreshold && !carBrakeAS.isPlaying)
        {
            carBrakeAS.Play();
        }
    }
    void Accelerate()
    {
        rb.linearDamping = 0;
        Vector3 forwardDirection = transform.forward;
        rb.AddForce(forwardDirection * accelerationMultiplier * input.y,
                   ForceMode.Acceleration);
    }

    public void SetInput(Vector2 inputVector)
    {
        if (isCrashed) return;
        input = Vector2.ClampMagnitude(inputVector, 1f);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (isCrashed) return;

        float impactForce = collision.impulse.magnitude;
        if (impactForce > 5f)
        {
            HandleCrash(collision);

            // Play crash sound if impact is strong enough and audio source exists
            if (impactForce > crashSoundThreshold && carCrashAS != null)
            {
                carCrashAS.Play();
            }
        }
    }


    void HandleCrash(Collision collision)
    {
        isCrashed = true;
        brakingEnabled = false; // Disable braking during crash
        crashTimer = 0f;

        if (!isPlayer)
        {
            if (collision.transform.root.CompareTag("Untagged") ||
                collision.transform.root.CompareTag("CarAI"))
            {
                return;
            }
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
    void HandleCrashs(Collision collision)
    {
        isCrashed = true;
        crashTimer = 0f;
        if (!isPlayer)
        {

            if (collision.transform.root.CompareTag("Untagged"))
            {

                return;
            }
            if (collision.transform.root.CompareTag("CarAI"))
            {

                return;
            }


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

    // Visualize ground check in editor
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position - Vector3.up * groundCheckDistance);
    }
    void UpdateCarAudio()
    {
        if (!isPlayer)
            return;

        float carmaxSpeedPercentage = rb.linearVelocity.z / maxForwardVelocity;
        if (carEngineAS != null)
        {
            carEngineAS.pitch = carPitchAnimationCurve.Evaluate(carmaxSpeedPercentage);
        }

        // Handle skid sound if audio source exists
        if (carSkidAS != null)
        {
            if (input.y < 0 && carmaxSpeedPercentage > 0.92f)
            {
                if (!carSkidAS.isPlaying)
                    carSkidAS.Play();

                carSkidAS.volume = Mathf.Lerp(carSkidAS.volume, 1.0f, Time.deltaTime * 10);
            }
            else
            {
                carSkidAS.volume = Mathf.Lerp(carSkidAS.volume, 0, Time.deltaTime * 30);
            }
        }

        // Handle brake sound fade out when not braking
        if (input.y >= 0 && carBrakeAS != null && carBrakeAS.isPlaying)
        {
            carBrakeAS.Stop();
        }

        float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        if (input.y < 0 && currentForwardSpeed < -0.1f)
        {
            // Play reverse sound
            if (carReverseAS != null && !carReverseAS.isPlaying)
            {
                carReverseAS.Play();
            }
        }
        else if (carReverseAS != null && carReverseAS.isPlaying)
        {
            carReverseAS.Stop();
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



/*
using UnityEngine;

public class CarHandler : MonoBehaviour
{
    [SerializeField] Transform gameModel;
    [SerializeField] MeshRenderer carMeshRender;
    [SerializeField] private Rigidbody rb;
    public float moveSpeed = 10f;
    public float turnSpeed = 50f;
    bool isCrashed = false;
    float crashTimer = 0f;
    float crashDuration = 3f; // Time to recover from crash

    [Header("Steering Settings")]
    public float steeringMultiplier = 2f;
    public float maxSteeringAngle = 25f;
    public float minSteeringSpeed = 5f;
    public float maxSteeringSpeed = 30f;
    [Range(0f, 1f)] public float driftFactor = 0.2f;

    Vector2 input = Vector2.zero;
    float accelerationMultiplier = 3f;
    float breakMultiplier = 15f;
    
    //emissive property
    int _EmissionColor = Shader.PropertyToID("_EmissionColor");
    Color emissiveColor = Color.white;
    float emissiveColorMultiplier = 0f;

    [Header("Crash Settings")]
    public float crashBounceForce = 5f;
    public float crashTorqueForce = 10f;
    public float crashRecoveryTime = 3f;
    public float crashDrag = 2f;

    [Header("Wheel Settings")]
    public Transform[] steeringWheels;
    public float wheelTurnRatio = 0.5f;
    [SerializeField] float steeringLerpSpeed = 5f;
    private float currentSteerAngle;

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

        gameModel.transform.rotation = Quaternion.Euler(0, rb.linearVelocity.x * 5, 0);

        if (carMeshRender != null)
        {
            float desiredCarEmissiveColorMultiplier = 0f;
            if (input.y < 0)
                desiredCarEmissiveColorMultiplier = 4.0f;

            emissiveColorMultiplier = Mathf.Lerp(emissiveColorMultiplier, desiredCarEmissiveColorMultiplier, Time.deltaTime * 4);
            carMeshRender.material.SetColor(_EmissionColor, emissiveColor * emissiveColorMultiplier);
        }
    }

    void FixedUpdate()
    {
        if (isCrashed)
        {
            // Apply additional drag while crashed
            rb.linearDamping = crashDrag;
            return;
        }
        
        HandleMovement();
        HandleAdvancedSteering();
    }

    void HandleAdvancedSteering()
    {
        float currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float speedFactor = Mathf.Clamp01(
            (Mathf.Abs(currentSpeed) - minSteeringSpeed) /
            (maxSteeringSpeed - minSteeringSpeed)
        );

        float targetAngle = maxSteeringAngle * input.x * (1f - speedFactor);
        currentSteerAngle = Mathf.Lerp(
            currentSteerAngle,
            targetAngle,
            steeringLerpSpeed * Time.fixedDeltaTime
        );

        foreach (var wheel in steeringWheels)
        {
            wheel.localRotation = Quaternion.Euler(0f, currentSteerAngle, 0f);
        }

        rb.AddTorque(
            transform.up * currentSteerAngle * 0.05f * currentSpeed,
            ForceMode.VelocityChange
        );

        if (Mathf.Abs(currentSteerAngle) > 5f)
        {
            rb.AddForce(
                -transform.right * currentSteerAngle * 0.1f,
                ForceMode.Acceleration
            );
        }
    }

    void HandleMovement()
    {
        if (isCrashed) return;

        if (input.y > 0)
        {
            Accelerate();
        }
        else if (input.y < 0)
        {
            Brake();
        }
        else
        {
            rb.linearDamping = 0.5f;
        }
    }

    void Brake()
    {
        Vector3 brakeDirection = -rb.linearVelocity.normalized;
        rb.AddForce(brakeDirection * breakMultiplier * Mathf.Abs(input.y),
                   ForceMode.Acceleration);
    }

    void Accelerate()
    {
        rb.linearDamping = 0;
        Vector3 forwardDirection = transform.forward;
        rb.AddForce(forwardDirection * accelerationMultiplier * input.y,
                   ForceMode.Acceleration);
    }

    public void SetInput(Vector2 inputVector)
    {
        if (isCrashed) return;
        input = Vector2.ClampMagnitude(inputVector, 1f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isCrashed) return;
        
        // Calculate crash force based on collision impact
        float impactForce = collision.impulse.magnitude;
        
        // Only trigger crash if impact is significant
        if (impactForce > 5f)
        {
            HandleCrash(collision);
        }
    }

    void HandleCrash(Collision collision)
    {
        isCrashed = true;
        crashTimer = 0f;
        
        // Calculate bounce direction (reflect off the collision surface)
        Vector3 bounceDirection = Vector3.Reflect(rb.linearVelocity.normalized, collision.contacts[0].normal);
        
        // Apply bounce force
        rb.AddForce(bounceDirection * crashBounceForce, ForceMode.VelocityChange);
        
        // Apply random torque to make the car flip/roll
        Vector3 torqueDirection = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(0.5f, 1f),
            Random.Range(-1f, 1f)
        );
        rb.AddTorque(torqueDirection * crashTorqueForce, ForceMode.VelocityChange);
        
        // Disable input temporarily
        input = Vector2.zero;
    }

    void RecoverFromCrash()
    {
        isCrashed = false;
        crashTimer = 0f;
        
        // Reset drag
        rb.linearDamping = 0f;
        
        // Try to upright the car
        Quaternion targetRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        transform.rotation = targetRotation;
    }
}

*/
