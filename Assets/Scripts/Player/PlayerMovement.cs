using System.Collections;
using System.Diagnostics.Contracts;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.Searcher.SearcherWindow.Alignment;
using static UnityEngine.GraphicsBuffer;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody playerRb; // Reference to the player's rigidbody

    [Header("Movement")]
    [SerializeField] private float maxSpeed = 5f; // Maximum horizontal movement speed
    // How quickly the player reaches max speed
    // Higher = snappy, Lower = sluggish
    [SerializeField] private float acceleration = 20f;

    // Stores the current movement input from the Input System
    // X = left/right, Y = forward/back
    private Vector2 moveInput;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 10f; // Upward velocity applied when jumping.
    private bool isGrounded;
    public LayerMask groundLayer;

    [Header("Dashing")]
    private bool isDashing = false;
    [SerializeField] private float dashForce = 15f;

    [Header("Drifting")]
    private bool isDrifting = false;
    [SerializeField] private float driftForce = 20f;
    [SerializeField] private float chargeTime = 3f;
    [SerializeField] private float deceleration = 10f;
    public GameObject forwardPoint;

    public Material material;
    public Color originalColor;

    public MeshRenderer meshRenderer;

    // The amount of launch force currently charged
    // Builds up while the player is drifting
    private float currentCharge = 0f;
    private Vector3 launchDirection; // The direction the player will launch when they release the drift button

    public int points = 0;
    public TextMeshProUGUI pointText;

    private void Awake()
    {
        playerRb = GetComponent<Rigidbody>(); // Get the Rigidbody attached to this GameObject
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        material = meshRenderer.material;
        originalColor = material.color;

        pointText = GameObject.FindWithTag("PointText").GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void LateUpdate()
    {
        // Update the launch direction every frame based on the player's movement input
        // Allows the player to change the direction they'll launch while charging
        launchDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
    }

    private void FixedUpdate()
    {
        if (isDrifting)
        {
            // Get only the player's horizontal velocity
            Vector3 horizontal = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);

            // Gradually slow the player's horizontal movement toward zero
            horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, deceleration * Time.fixedDeltaTime);

            // Apply the slowed horizontal velocity while keeping the current vertical velocity
            playerRb.linearVelocity = new Vector3(horizontal.x, playerRb.linearVelocity.y, horizontal.z);

            // Increase the amount of stored launch force over time.
            // The player reaches the maximum drift force after 'chargeTime' seconds

            // Increase the charge at a constant rate.
            //
            // Example:
            // driftForce = 20
            // chargeTime = 4
            //
            // Charge increases by 5 every second,
            // reaching the maximum of 20 after 4 seconds
            currentCharge += (driftForce / chargeTime) * Time.fixedDeltaTime;

            // Prevent the charge from exceeding the maximum drift force
            currentCharge = Mathf.Clamp(currentCharge, 0f, driftForce);

            // Skip the normal movement code while drifting
            return;
        }

        // The velocity the player wants to reach
        Vector3 targetVelocity = new Vector3(moveInput.x * maxSpeed, playerRb.linearVelocity.y, moveInput.y * maxSpeed);

        // Store the player's current velocity
        Vector3 currentVelocity = playerRb.linearVelocity;

        // Extract only the horizontal movement
        // Ignore Y because gravity and jumping control vertical movement
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        // Extract only the horizontal target velocity.
        Vector3 targetHorizontal = new Vector3(targetVelocity.x, 0f, targetVelocity.z);

        // Smoothly move the current horizontal velocity toward the target velocity
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetHorizontal, acceleration * Time.fixedDeltaTime);

        // If the player is not drifting
        if (!isDrifting)
        {
            // Combine the new horizontal velocity with the existing vertical velocity
            playerRb.linearVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z);
        }
    }

    // Called whenever the Move input action changes
    public void Move(InputAction.CallbackContext context)
    {
        // Store the value received from the input
        moveInput = context.ReadValue<Vector2>();
    }

    // Called when the Jump input action is performed
    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded && !isDrifting)
        {
            // Apply the jump velocity to the y velocity of the player
            playerRb.linearVelocity = new Vector3(playerRb.linearVelocity.x, jumpForce, playerRb.linearVelocity.z);
        }
    }

    public void Dash(InputAction.CallbackContext context)
    {
        if (!context.performed || isDrifting)
        {
            return;
        }

        Vector3 dashDirection;

        if (moveInput == Vector2.zero)
        {
            dashDirection = forwardPoint.transform.forward;
        }

        else
        {
            dashDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        }

        playerRb.linearVelocity += dashDirection * dashForce;
    }

    public void Drift(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Drifting");
            material.color = Color.red;
            isDrifting = true; // Put the player into the drifting state
            currentCharge = 0f; // Reset the stored launch force so each drift starts from zero

            launchDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized; // Store the current movement direction

        }

        else if (context.canceled)
        {
            Debug.Log("Not Drifting");
            material.color = originalColor;

            isDrifting = false; // Exit the drifting state so normal movement resumes

            // If no movement is held, launch forward
            if (launchDirection == Vector3.zero)
            {
                launchDirection = forwardPoint.transform.forward;
            }

            // Calculate the launch velocity using the stored direction
            // and the amount of charge built up while drifting
            Vector3 velocity = launchDirection * currentCharge;

            // Preserve the player's current vertical velocity so
            // releasing a drift doesn't cancel jumps or falling
            velocity.y = playerRb.linearVelocity.y;

            // Apply the launch velocity to the player
            playerRb.linearVelocity = velocity;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }

        else if (collision.gameObject.CompareTag("Pin"))
        {
            Pin pin = collision.gameObject.GetComponent<Pin>();

            if (pin != null)
            {
                if (pin.active)
                {
                    points += pin.pointValue;
                    pointText.text = points.ToString();
                    pin.active = false;
                }
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}
