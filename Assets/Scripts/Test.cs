// cspell:ignore MonoBehaviour
using UnityEngine;
using TMPro;
using System.Collections;

public class Test : MonoBehaviour
{
    public TMP_Text textComponent;
    public TMP_Text leftOption;
    public TMP_Text rightOption;
    public string textToDisplay = "When she shelling on my shell, until I hack";
    public AdjustPercentage adjustPercentage;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Speaking.Instance.StartSpeaking(5f, textToDisplay, textComponent);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            adjustPercentage.AdjustToPercentage(Random.Range(0f, 100f));
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                Speaking.Instance.GiveQuestionAndTwoResponse(Dialouge.Instance.questionsAndAnswersForNPC["The King"],
                    leftOption, rightOption, textComponent, false);
            }
        }
    }
    

}
