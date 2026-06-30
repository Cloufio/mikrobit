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
        "In the midst of a bustling city that never sleeps, you—the son of a traditional woodcutter—receive your father's last will and testament. In the letter, he asks you to continue the family business that has been passed down through generations to protect and utilize the forest wisely.",
        "So you set off for a remote forest, where your father used to live and work. However, when you arrive there, you are faced with a harsh reality: ancient trees towering high, ready to be cut down for great profit—but each tree has its own impact on the ecosystem.",
        "You only have 1 minute to cut it down. But every choice you make will shape the fate of this forest. Will you choose quick profit, or maintain the fragile balance of nature?",
        "This forest is on the brink of its fate. Only you can decide: destruction... or life."
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