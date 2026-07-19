using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;

        if (FindFirstObjectByType<OnboardingTutorial>() == null)
        {
            gameObject.AddComponent<OnboardingTutorial>();
        }
    }

    private void Start()
    {
        // The ambience controller safely does nothing in scenes without the water-detail tilemap.
        if (FindFirstObjectByType<SeaAmbienceController>() == null)
        {
            gameObject.AddComponent<SeaAmbienceController>();
        }
    }

    public GameObject player;
}
