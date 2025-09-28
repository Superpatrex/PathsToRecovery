using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AdjustPercentage : MonoBehaviour
{
    public TMP_Text percentageText;
    public float currentPercentage = 0f;
    public float targetPercentage = 100f;
    public float duration = 2f;
    public TCPClient tcpClient;

    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(AdjustPercentageCoroutine());
    }

    void Update()
    {
        percentageText.text = tcpClient.GetLastGesture();
    }

    private Coroutine percentageCoroutine;

    // Call this public method to start adjusting the percentage
    public void AdjustToPercentage(float newTargetPercentage)
    {
        // Stop any running coroutine so they don't overlap
        if (percentageCoroutine != null)
            StopCoroutine(percentageCoroutine);

        // Update the target and kick off the coroutine
        targetPercentage = newTargetPercentage;
        percentageCoroutine = StartCoroutine(AdjustPercentageCoroutine());
    }

    private IEnumerator AdjustPercentageCoroutine()
    {
        float startPercentage = currentPercentage;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            currentPercentage = Mathf.Lerp(startPercentage, targetPercentage, elapsedTime / duration);
            percentageText.text = $"{currentPercentage:F0}%";
            yield return null;
        }

        currentPercentage = targetPercentage;
        percentageText.text = $"{currentPercentage:F0}%";
    }
}