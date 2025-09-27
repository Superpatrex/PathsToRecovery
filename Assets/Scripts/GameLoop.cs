using UnityEngine;
using TMPro;
using System.Collections;

public class GameLoop : MonoBehaviour
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
        this.curEnemy = EnemiesUtil.GetRandomEnemyAndRemove();
        ShowInitialOptions();
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
        Speaking.Instance.StartSpeaking(5f, Dialouge.Instance.npcIntroductionLines[curEnemy.GetName()], dialogueText, false);
        Debug.Log("Press 1 to Talk, 2 to Fight");
        currentState = State.ChooseAction;
    }

    void EnterTalk()
    {
        currentState = State.Talking;
        Debug.Log("You start talking... (press Space to finish)");
        Speaking.Instance.GiveQuestionAndTwoResponse(
            Dialouge.Instance.questionsAndAnswersForNPC[curEnemy.GetName()],
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
