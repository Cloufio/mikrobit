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

    [Header("Animation Setup")]
    public Animator boatAnimator;
    public Animator playerAnimator;

    private bool canBoard = false;
    private bool isRiding = false;
    private Rigidbody2D rb;
    private Vector2 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
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
            rb.MovePosition(rb.position + movement * boatSpeed * Time.fixedDeltaTime);
        }
    }

    private void BoardBoat()
    {
        isRiding = true;

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
    }

    // Detect when the player walks close to the boat
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == player)
        {
            canBoard = true;
        }
    }

    // Detect when the player walks away from the boat
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject == player)
        {
            canBoard = false;
        }
    }
}