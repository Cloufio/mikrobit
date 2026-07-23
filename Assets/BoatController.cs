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
    [Tooltip("The fixed front-facing sprite shown while the player is seated. Leave empty to use the player's starting idle sprite.")]
    [SerializeField] private Sprite boardedPlayerSprite;

    [Header("Boarding Prompt")]
    [SerializeField] private bool showBoardingPrompt = true;
    [SerializeField] private BoatBoardingPrompt.Style boardingPromptStyle = new BoatBoardingPrompt.Style();
    [SerializeField, Min(0.1f)] private float boardingPromptRange = 4f;

    [Header("Animation Setup")]
    public Animator boatAnimator;
    public Animator playerAnimator;

    [Header("Crash Timer Penalty")]
    [SerializeField, Min(0f)] private float crashTimePenalty = 5f;
    [SerializeField] private float crashPenaltyCooldown = 0.75f;

    [Header("Crash Feedback")]
    [SerializeField] private float cameraShakeDuration = 0.16f;
    [SerializeField] private float cameraShakeStrength = 0.08f;
    [SerializeField] private float recoilDistance = 0.28f;
    [SerializeField] private float boatFlashDuration = 0.12f;

    [Header("Boat Wake")]
    [SerializeField] private float wakeInterval = 0.12f;
    [SerializeField] private float wakeOffset = 1.15f;

    [Header("Idle Sway")]
    [SerializeField, Min(0f)] private float idleSwayAngle = 1.5f;
    [SerializeField, Min(0f)] private float idleSwaySpeed = 1.5f;

    [Header("Render Order")]
    [SerializeField] private int boatSortingOrder = 4;

    private bool canBoard = false;
    private bool isRiding = false;
    private Rigidbody2D rb;
    private Vector2 movement;
    private float nextCrashPenaltyTime;
    private CameraShake cameraShake;
    private SpriteRenderer boatRenderer;
    private Coroutine boatFlashCoroutine;
    private float nextWakeTime;
    private BoatBoardingPrompt boardingPrompt;
    private SpriteRenderer playerRenderer;
    private float idleBaseRotation;
    private float idleSwayPhase;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        idleBaseRotation = rb != null ? rb.rotation : transform.eulerAngles.z;
        idleSwayPhase = Random.Range(0f, Mathf.PI * 2f);
        playerRenderer = player != null ? player.GetComponent<SpriteRenderer>() : null;

        // The scene starts with the front-facing idle sprite, which is the intended boat pose.
        if (boardedPlayerSprite == null && playerRenderer != null)
        {
            boardedPlayerSprite = playerRenderer.sprite;
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

            boardingPrompt.Configure(boardingPromptStyle);
            boardingPrompt.Initialize();
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
        UpdateBoardingAvailability();

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
            }
            else
            {
                // We have stopped driving (Idling)
                if (boatAnimator != null)
                {
                    boatAnimator.SetBool("isWalking", false);
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
        if (!isRiding)
        {
            ApplyIdleSway();
            return;
        }

        // Apply movement to the boat's Rigidbody.
        if (rb != null)
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
        ResetBoatRotation();
        ScoreManager.Instance?.StartTimer();

        // Disable the player's normal walking script
        playerMovementScript.enabled = false;

        // Always show the same seated passenger pose, regardless of the last walking frame.
        if (playerAnimator != null)
        {
            playerAnimator.enabled = false;
        }
        if (playerRenderer != null && boardedPlayerSprite != null)
        {
            playerRenderer.sprite = boardedPlayerSprite;
        }

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

        // Hand control back to the player animator after leaving the boat.
        if (playerAnimator != null)
        {
            playerAnimator.enabled = true;
            playerAnimator.Rebind();
            playerAnimator.SetFloat("Horizontal", 0f);
            playerAnimator.SetFloat("Vertical", -1f);
            playerAnimator.SetFloat("Speed", 0f);
            playerAnimator.Update(0f);
        }

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

    private void ApplyIdleSway()
    {
        if (rb == null || idleSwayAngle <= 0f)
        {
            return;
        }

        float angle = idleBaseRotation + Mathf.Sin(Time.time * idleSwaySpeed + idleSwayPhase) * idleSwayAngle;
        rb.MoveRotation(angle);
    }

    private void ResetBoatRotation()
    {
        if (rb != null)
        {
            rb.rotation = idleBaseRotation;
        }
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

    private void UpdateBoardingAvailability()
    {
        if (player == null || isRiding)
        {
            return;
        }

        bool playerIsNearby = Vector2.Distance(player.transform.position, transform.position) <= boardingPromptRange;
        if (canBoard == playerIsNearby)
        {
            return;
        }

        canBoard = playerIsNearby;
        RefreshBoardingPrompt();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isRiding || Time.time < nextCrashPenaltyTime)
        {
            return;
        }

        CrashableObject crashableObject = collision.collider.GetComponentInParent<CrashableObject>();
        if (crashableObject == null || !crashableObject.DamagesBoat)
        {
            return;
        }

        ScoreManager.Instance?.SubtractTime(crashTimePenalty);
        nextCrashPenaltyTime = Time.time + crashPenaltyCooldown;
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
