using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Make sure to include this for TextMeshPro

public class IntroController : MonoBehaviour
{
    // Drag your TextMeshPro object here in the Unity Inspector
    public TextMeshProUGUI introText;

    // The name of your main game scene
    public string mainGameSceneName = "MainScene";

    // Adjust these values in the Inspector
    [Header("Timing Settings")]
    public float timeBetweenChars = 0.05f; // How fast the text types out
    public float timeAfterParagraph = 2.0f; // How long to wait after a paragraph is finished

    // Optional: Add a sound effect for typing
    [Header("Audio Settings")]
    public AudioSource typingAudioSource; // Assign an AudioSource component

    // The four paragraphs for your intro
    private string[] introParagraphs = new string[]
    {
        "In the midst of a bustling megacity that never sleeps, you—the child of a traditional fisherman—receive your father's last will and testament. In the letter, he asks you to continue the family legacy passed down through generations: to protect and care for the ocean wisely."
        "So you set sail for the remote open sea where your father used to make his living. However, when you arrive, you are faced with a harsh reality: the once-beautiful blue ocean is now choked with floating plastic and marine debris, ready to destroy the ecosystem and endanger all sea life."
        "You only have 1 minute to sail and collect as much waste as possible. Every piece of trash you scoop up or leave behind will shape the fate of this ocean. Will you act fast to save it, or let these waters perish from pollution?"
        "This ocean is on the brink of its fate. Only you can decide: total destruction... or a new lease on life."
    };

    void Start()
    {
        // Ensure the text is empty at the start
        introText.text = "";
        StartCoroutine(PlayIntroSequence());
    }

    IEnumerator PlayIntroSequence()
    {
        // Loop through each paragraph
        foreach (string paragraph in introParagraphs)
        {
            // Call the typing coroutine for the current paragraph
            yield return StartCoroutine(TypeText(paragraph));

            // Wait for a moment before starting the next paragraph
            yield return new WaitForSeconds(timeAfterParagraph);

            // Clear the text for the next paragraph
            introText.text = "";
        }

        // After the last paragraph, load the main game
        SceneManager.LoadScene(mainGameSceneName);
    }

    IEnumerator TypeText(string textToType)
    {
        // 'i' will be our character counter
        int i = 0;

        while (i < textToType.Length)
        {
            // Add one character to the text component
            introText.text += textToType[i];
            i++;

            // Play a typing sound, if one is assigned
            if (typingAudioSource != null)
            {
                typingAudioSource.Play();
            }

            // Wait a short moment before typing the next character
            yield return new WaitForSeconds(timeBetweenChars);
        }
    }
}