using UnityEngine;
using TMPro;
using System.Collections;

public class Speaking : MonoBehaviour
{
    public static Speaking Instance { get; private set; }

    public AudioSource audioSource;
    public AudioClip audioClip;
    public float volume = 0.5f;
    [Tooltip("How long (in seconds) the clip should keep looping before stopping.")]
    public float loopDuration = 5f;

    private float timeLeft;
    private bool isLooping;
    private float timeBeforeClearing = 1.5f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        audioSource.clip = audioClip;
        audioSource.volume = volume;
    }

    void Update()
    {
        if (!isLooping)
            return;

        timeLeft -= Time.deltaTime;

        if (timeLeft > 0f)
        {
            if (!audioSource.isPlaying)
                audioSource.Play();
        }
        else
        {
            if (audioSource.isPlaying)
                audioSource.Stop();
            isLooping = false;
        }
    }

    private Coroutine typingCoroutine;

    /// <summary>
    /// Start playing & looping the clip for exactly 'duration' seconds,
    /// reveal 'textToDisplay' one character at a time in 'textComponent',
    /// and optionally clear it after timeBeforeClearing.
    /// </summary>
    public void StartSpeaking(
        float duration,
        string textToDisplay,
        TMP_Text textComponent,
        bool clearAfter = true
    )
    {
        loopDuration = duration;
        timeLeft = duration;
        isLooping = true;
        audioSource.Play();

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(
            TypeTextCoroutine(textToDisplay, textComponent, duration, clearAfter)
        );
    }

    private IEnumerator TypeTextCoroutine(
        string fullText,
        TMP_Text textComponent,
        float duration,
        bool clearAfter
    )
    {
        textComponent.text = string.Empty;
        float charInterval = duration / Mathf.Max(1, fullText.Length) * 0.8f;

        foreach (char c in fullText)
        {
            textComponent.text += c;
            yield return new WaitForSeconds(charInterval);
        }

        if (clearAfter)
        {
            yield return new WaitForSeconds(this.timeBeforeClearing);
            textComponent.text = string.Empty;
        }
    }

    private Coroutine responsesCoroutine;

    /// <summary>
    /// Plays the question (with typewriter + audio loop),
    /// then types out two responses in parallel.
    /// You can disable clearing on either.
    /// </summary>
    public void GiveQuestionAndTwoResponse(
        DialougeWithCorrectAnswer interaction,
        TMP_Text questionText,
        TMP_Text leftResponse,
        TMP_Text rightResponse,
        bool clearQuestionAfter = true,
        bool clearResponsesAfter = false
    )
    {
        // question
        Debug.Log($"Asking question: {interaction.question}");
        StartSpeaking(5f, interaction.question, questionText, clearQuestionAfter);

        // two responses
        if (responsesCoroutine != null)
            StopCoroutine(responsesCoroutine);

        Debug.Log($"Correct answer: {interaction.correctAnswer}");
        Debug.Log($"Wrong answer: {interaction.wrongAnswer}");
        responsesCoroutine = StartCoroutine(
            TypeTwoResponsesCoroutine(
                interaction.correctAnswer,
                leftResponse,
                interaction.wrongAnswer,
                rightResponse,
                loopDuration,
                clearResponsesAfter
            )
        );
    }

    private IEnumerator TypeTwoResponsesCoroutine(
        string leftText,
        TMP_Text leftComponent,
        string rightText,
        TMP_Text rightComponent,
        float duration,
        bool clearAfter
    )
    {
        leftComponent.text  = string.Empty;
        rightComponent.text = string.Empty;

        float leftInterval  = duration / Mathf.Max(1, leftText.Length)  * 0.8f;
        float rightInterval = duration / Mathf.Max(1, rightText.Length) * 0.8f;
        int   maxLen        = Mathf.Max(leftText.Length, rightText.Length);

        for (int i = 0; i < maxLen; i++)
        {
            if (i < leftText.Length)
                leftComponent.text += leftText[i];
            if (i < rightText.Length)
                rightComponent.text += rightText[i];

            yield return new WaitForSeconds(Mathf.Min(leftInterval, rightInterval));
        }

        if (clearAfter)
        {
            yield return new WaitForSeconds(this.timeBeforeClearing);
            leftComponent.text  = string.Empty;
            rightComponent.text = string.Empty;
        }
    }
}
