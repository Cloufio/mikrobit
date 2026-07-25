using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class IntroController : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Drag your TextMeshProUGUI object here in the Unity Inspector")]
    public TextMeshProUGUI introText;

    [Header("Scene Transition Settings")]
    [Tooltip("The name of your main game scene to load after intro")]
    public string mainGameSceneName = "MainScene2";

    [Header("Timing Settings")]
    [Tooltip("How fast each character is revealed (seconds)")]
    public float timeBetweenChars = 0.05f;

    [Tooltip("How long to wait after a paragraph is finished (seconds)")]
    public float timeAfterParagraph = 2.0f;

    [Header("Audio Settings")]
    [Tooltip("Optional AudioSource component for typing sound effect")]
    public AudioSource typingAudioSource;

    private static readonly string[] IntroParagraphs = new string[]
    {
        "In the midst of a bustling megacity that never sleeps, you—the child of a traditional fisherman—receive your father's last will and testament. In the letter, he asks you to continue the family legacy passed down through generations: to protect and care for the ocean wisely.",
        "So you set sail for the remote open sea where your father used to make his living. However, when you arrive, you are faced with a harsh reality: the once-beautiful blue ocean is now choked with floating plastic and marine debris, ready to destroy the ecosystem and endanger all sea life.",
        "You only have 1 minute to sail and collect as much waste as possible. Every piece of trash you scoop up or leave behind will shape the fate of this ocean. Will you act fast to save it, or let these waters perish from pollution?",
        "This ocean is on the brink of its fate. Only you can decide: total destruction... or a new lease on life."
    };

    private void Start()
    {
        if (introText == null)
        {
            Debug.LogError("IntroController: TextMeshProUGUI 'introText' is not assigned in the Inspector!");
            return;
        }

        introText.text = "";
        StartCoroutine(PlayIntroSequence());
    }

    private IEnumerator PlayIntroSequence()
    {
        foreach (string paragraph in IntroParagraphs)
        {
            yield return StartCoroutine(TypeText(paragraph));
            yield return new WaitForSeconds(timeAfterParagraph);
            introText.text = "";
        }

        SceneManager.LoadScene(mainGameSceneName);
    }

    private IEnumerator TypeText(string textToType)
    {
        // Performance Optimization: Set full text once and reveal characters gradually
        // to avoid allocating new strings every frame (Zero Garbage Collection).
        introText.text = textToType;
        introText.maxVisibleCharacters = 0;

        for (int i = 0; i <= textToType.Length; i++)
        {
            introText.maxVisibleCharacters = i;

            if (typingAudioSource != null && i < textToType.Length)
            {
                typingAudioSource.Play();
            }

            yield return new WaitForSeconds(timeBetweenChars);
        }
    }
}