using UnityEngine;

public class BoatController : MonoBehaviour
{
    [Header("Boat Settings")]
    public float boatSpeed = 5f;

    [Header("References")]
    public GameObject player;
    public MonoBehaviour playerMovementScript;

    [Header("Boarding Setup")]
    public Transform driverSeat;

    [Header("Boarding Prompt")]
    [SerializeField] private bool showBoardingPrompt = false;

    [Header("Animation Setup")]
    public Animator boatAnimator;
    public Animator playerAnimator;

    [Header("Crash Damage")]
    [SerializeField] private int crashDamage = 1;
    [SerializeField] private float crashDamageCooldown = 0.75f;

    [Header("Crash Feedback")]
    [SerializeField] private float cameraShakeDuration = 0.16f;
    [SerializeField] private float cameraShakeStrength = 0.08f;
    [SerializeField] private float recoilDistance = 0.28f;
    [SerializeField] private float boatFlashDuration = 0.12f;

    [Header("Boat Wake")]
    [SerializeField] private float wakeInterval = 0.12f;
    [SerializeField] private float wakeOffset = 1.15f;

    [Header("Render Order")]
    [SerializeField] private int boatSortingOrder = 4;

    private bool canBoard = false;
    private bool isRiding = false;
    private Rigidbody2D rb;
    private Vector2 movement;
    private PlayerHealth playerHealth;
    private float nextCrashDamageTime;
    private CameraShake cameraShake;
    private SpriteRenderer boatRenderer;
    private Coroutine boatFlashCoroutine;
    private float nextWakeTime;
    private BoatBoardingPrompt boardingPrompt;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerHealth = player != null ? player.GetComponent<PlayerHealth>() : null;

        if (playerHealth == null)
        {
            Debug.LogError("BoatController could not find PlayerHealth on the assigned player.", this);
        }

        boatRenderer = GetComponent<SpriteRenderer>();
        if (boatRenderer != null)
        {
            boatRenderer.sortingOrder = boatSortingOrder;
        }

        if (showBoardingPrompt)
        {
            boardingPrompt = GetComponent<BoatBoardingPrompt>();
            if (boardingPrompt == null)
            {
                boardingPrompt = gameObject.AddComponent<BoatBoardingPrompt>();
            }

            RefreshBoardingPrompt();
        }

        if (Camera.main != null)
        {
            cameraShake = Camera.main.GetComponent<CameraShake>();
            if (cameraShake == null)
            {
                cameraShake = Camera.main.gameObject.AddComponent<CameraShake>();
            }
        }
    }

    void Update()
    {
        // 1. Check for boarding input (Press E)
        if (canBoard && Input.GetKeyDown(KeyCode.E))
        {
            if (!isRiding)
            {
                BoardBoat();
            }
            else
            {
                ExitBoat();
            }
        }

        // 2. Handle Movement & Animation if riding
        if (isRiding)
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");

            if (movement != Vector2.zero)
            {
                // We are actively driving (Walking)
                if (boatAnimator != null)
                {
                    boatAnimator.SetBool("isWalking", true);
                    boatAnimator.SetFloat("inputX", movement.x);
                    boatAnimator.SetFloat("inputY", movement.y);

                    // Constantly update our "last" input while driving
                    boatAnimator.SetFloat("lastInputX", movement.x);
                    boatAnimator.SetFloat("lastInputY", movement.y);
                }
                if (playerAnimator != null)
                {
                    playerAnimator.SetBool("isWalking", true);
                    playerAnimator.SetFloat("inputX", movement.x);
                    playerAnimator.SetFloat("inputY", movement.y);

                    playerAnimator.SetFloat("lastInputX", movement.x);
                    playerAnimator.SetFloat("lastInputY", movement.y);
                }
            }
            else
            {
                // We have stopped driving (Idling)
                if (boatAnimator != null)
                {
                    boatAnimator.SetBool("isWalking", false);
                }
                if (playerAnimator != null)
                {
                    playerAnimator.SetBool("isWalking", false);
                }
            }
        }
        else
        {
            // Stop the boat completely if nobody is driving it
            movement = Vector2.zero;
        }
    }

    void FixedUpdate()
    {
        // Apply movement to the boat's Rigidbody
        if (isRiding)
        {
            Vector2 nextPosition = rb.position + movement * boatSpeed * Time.fixedDeltaTime;
            rb.MovePosition(nextPosition);

            if (movement.sqrMagnitude > 0.001f && Time.time >= nextWakeTime)
            {
                Vector2 heading = movement.normalized;
                Vector2 wakePosition = nextPosition - heading * wakeOffset;
                WaterImpactSplash.SpawnWake(wakePosition, heading);
                nextWakeTime = Time.time + wakeInterval;
            }
        }
    }

    private void BoardBoat()
    {
        isRiding = true;
        RefreshBoardingPrompt();

        // Disable the player's normal walking script
        playerMovementScript.enabled = false;

        // Put the player's physics to sleep so they don't fight the boat
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.isKinematic = true;
            playerRb.linearVelocity = Vector2.zero;
        }

        // Snap the player to the animated driver seat!
        player.transform.SetParent(driverSeat);

        // This instantly moves the player to the exact center of the DriverSeat
        player.transform.localPosition = Vector3.zero;

        // Turn off the player's collider so they don't bump into things while on the boat
        player.GetComponent<Collider2D>().enabled = false;
    }

    private void ExitBoat()
    {
        isRiding = false;

        // Re-enable player walking
        playerMovementScript.enabled = true;

        // Un-parent the player so they can walk away
        player.transform.SetParent(null);

        // Wake the player's physics back up
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.isKinematic = false;
        }

        // Move the player slightly to the right so they don't get stuck inside the boat
        player.transform.position += new Vector3(1.5f, 0, 0);

        // Turn the player's collider back on
        player.GetComponent<Collider2D>().enabled = true;
        RefreshBoardingPrompt();
    }

    // Detect when the player walks close to the boat
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == player)
        {
            canBoard = true;
            RefreshBoardingPrompt();
        }
    }

    // Detect when the player walks away from the boat
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject == player)
        {
            canBoard = false;
            RefreshBoardingPrompt();
        }
    }

    private void RefreshBoardingPrompt()
    {
        if (showBoardingPrompt)
        {
            boardingPrompt?.SetVisible(canBoard && !isRiding);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isRiding || playerHealth == null || Time.time < nextCrashDamageTime)
        {
            return;
        }

        CrashableObject crashableObject = collision.collider.GetComponentInParent<CrashableObject>();
        if (crashableObject == null || !crashableObject.DamagesBoat)
        {
            return;
        }

        playerHealth.TakeDamage(crashDamage);
        nextCrashDamageTime = Time.time + crashDamageCooldown;
        PlayCrashFeedback(collision.GetContact(0).point);
    }

    private void PlayCrashFeedback(Vector2 contactPoint)
    {
        Vector2 recoilDirection = rb.position - contactPoint;
        if (recoilDirection.sqrMagnitude < 0.001f)
        {
            recoilDirection = movement.sqrMagnitude > 0.001f ? -movement.normalized : Vector2.left;
        }
        else
        {
            recoilDirection.Normalize();
        }

        rb.position += recoilDirection * recoilDistance;
        cameraShake?.Shake(cameraShakeDuration, cameraShakeStrength);
        WaterImpactSplash.Spawn(contactPoint);

        if (boatRenderer != null)
        {
            if (boatFlashCoroutine != null)
            {
                StopCoroutine(boatFlashCoroutine);
            }

            boatFlashCoroutine = StartCoroutine(FlashBoat());
        }
    }

    private System.Collections.IEnumerator FlashBoat()
    {
        Color originalColor = boatRenderer.color;
        boatRenderer.color = new Color(1f, 0.45f, 0.4f, originalColor.a);
        yield return new WaitForSecondsRealtime(boatFlashDuration);
        boatRenderer.color = originalColor;
        boatFlashCoroutine = null;
    }
}
