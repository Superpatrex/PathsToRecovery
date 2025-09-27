using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Cutscene : MonoBehaviour
{
    // track “good” vs “bad” endings
    public int goodEnding = 0;
    public int endingNum = 0;

    private bool isEnemyGood = true;
    private Enemy curEnemy;
    public GameObject leftPanel;
    public GameObject rightPanel;
    public TMP_Text dialogueText;
    public TMP_Text leftOption;
    public TMP_Text rightOption;

    private enum State
    {
        ChooseAction,
        Talking,
        FightChoose
    }
    private State currentState = State.ChooseAction;

    void Start()
    {
        StartCoroutine(Exposit(6, Dialouge.Instance.openingCutscene, 5f));
        //this.curEnemy = EnemiesUtil.GetRandomEnemyAndRemove();
        //ShowInitialOptions();
    }

    void Update()
    {
        switch (currentState)
        {
            case State.ChooseAction:
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    MakeLeftAndRightOptionsVisible();
                    EnterTalk();
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    EnterFight();
                }

                break;

            case State.Talking:
                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    Debug.Log("You pick left");
                    ExitTalk();
                    ClearLeftAndRightOptionsAndHide();
                }
                if (Input.GetKeyDown(KeyCode.Alpha4))
                {
                    Debug.Log("You pick right");
                    ExitTalk();
                    ClearLeftAndRightOptionsAndHide();
                }

                break;
        }
    }

    void ShowInitialOptions()
    {
        Debug.Log("Press 1 to Talk, 2 to Fight");
        currentState = State.ChooseAction;
    }

    void EnterTalk()
    {
        currentState = State.Talking;
        Debug.Log("You start talking... (press Space to finish)");
        Speaking.Instance.GiveQuestionAndTwoResponse(
            Dialouge.Instance.getRandomQuestionAndAnswer(curEnemy.GetName()),
            dialogueText,
            leftOption,
            rightOption,
            false
        );
    }

    void ExitTalk()
    {
        Debug.Log("You finish talking.");

        if (isEnemyGood)
        {
            if (CheckAnswer())
            {
                Debug.Log("Your answer was spot on! (+1 good ending)");
                goodEnding++;
            }
            else
            {
                Debug.Log("You fumbled the conversation... (-1 endingNum)");
                endingNum--;
            }
        }
        else
        {
            Debug.Log("The enemy did not like your small talk. (-1 good ending)");
            goodEnding--;
        }

        this.curEnemy = EnemiesUtil.GetRandomEnemyAndRemove();
        ShowInitialOptions();
    }

    void EnterFight()
    {
        StartCoroutine(EnterFightRoutine());
    }

    IEnumerator EnterFightRoutine()
    {
        Speaking.Instance.StartSpeaking(3f, "The King looks at you disappointingly.", dialogueText, true);
        yield return new WaitForSeconds(5f);
        currentState = State.ChooseAction;
        this.curEnemy = EnemiesUtil.GetRandomEnemyAndRemove();
        ShowInitialOptions();
    }

    void DoAttack()
    {
        Debug.Log($"You attack!");

        if (!isEnemyGood)
        {
            Debug.Log("Nice! You took down the bad guy. (+1 good ending)");
            goodEnding++;
        }
    }

    IEnumerator Wait(float delayTime)
    {
        //Wait for the specified delay time before continuing.
        yield return new WaitForSeconds(delayTime);

        //Do the action after the delay time has finished.
    }

    IEnumerator Exposit(int lines, List<string> source, float delayTime)
    {
        for (int i = 0; i < lines; i++)
        {
            Speaking.Instance.StartSpeaking(5f, source[i], dialogueText, false);
            yield return new WaitForSeconds(delayTime+5f);
        }
    }

    void DoBlock()
    {
        Debug.Log("You block the next attack.");
    }

    bool CheckAnswer()
    {
        return true;
    }

    void ClearLeftAndRightOptionsAndHide()
    {
        leftOption.text = string.Empty;
        rightOption.text = string.Empty;
        leftPanel.SetActive(false);
        rightPanel.SetActive(false);
    }

    void MakeLeftAndRightOptionsVisible()
    {
        leftPanel.SetActive(true);
        rightPanel.SetActive(true);
    }
}
