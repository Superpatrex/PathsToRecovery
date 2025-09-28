using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class GameLoop : MonoBehaviour
{
    public int goodEnding = 0;
    public int endingNum = 0;

    private bool isEnemyGood = true;
    private Enemy curEnemy;
    public GameObject leftPanel;
    public GameObject rightPanel;
    public TMP_Text dialogueText;
    public TMP_Text leftOption;
    public TMP_Text rightOption;
    public SpriteRenderer enemySpriteRenderer;
    public ImageHolder imageHolder;
    public Image cursorImage;
    public TCPClient tcpClient;
    private DialougeWithCorrectAnswer speech;

    public Image fightImage;
    public Image talkImage;

    private enum State
    {
        ChooseAction,
        Talking,
        FightChoose
    }

    private State currentState = State.ChooseAction;
    private float lastGestureTime = -1;

    void Start()
    {
        this.curEnemy = EnemiesUtil.GetRandomEnemyAndRemove();
        enemySpriteRenderer.sprite = imageHolder.GetSprite(curEnemy.GetName(), ImageHolder.State.NEUTRAL);
        ShowInitialOptions();
    }

    void Update()
    {
        if (lastGestureTime == -1)
        {
            lastGestureTime = Time.time;
        }
        
        // Add delta to see if the TPC Client open or close has been going on for 2 seconds straight
        float delta = Time.time - lastGestureTime;
        
        //Debug.Log($"Delta time since last gesture: {delta} " + cursorImage.rectTransform.anchoredPosition.ToString() + " " + tcpClient.IsGestureClosed() + " " + tcpClient.IsGestureOpen());

        if (tcpClient.IsGestureOpen() || tcpClient.IsGestureClosed())
        {
            //Debug.Log("Mouse click detected via gesture!");

            Vector2 cursorPos = cursorImage.rectTransform.anchoredPosition;


            if (currentState == State.ChooseAction && delta > 2f && cursorPos.x < -70f && cursorPos.x > -320f && cursorPos.y < 50f && cursorPos.y > -70f)
            {
                //Debug.Log("Mouse clicked on TALK area");

                enemySpriteRenderer.sprite = imageHolder.GetSprite(curEnemy.GetName(), ImageHolder.State.BLOCK);
                currentState = State.Talking;
                hideTalkFightOptions();
                MakeLeftAndRightOptionsVisible();
                EnterTalk();
                lastGestureTime = Time.time;
            }
            else if (currentState == State.ChooseAction && delta > 2f && cursorPos.x > 75f && cursorPos.x < 320f && cursorPos.y < 50f && cursorPos.y > -70f)
            {
                Debug.Log("Mouse clicked on FIGHT area");

                enemySpriteRenderer.sprite = imageHolder.GetSprite(curEnemy.GetName(), ImageHolder.State.ATTACK);
                currentState = State.FightChoose;
                hideTalkFightOptions();
                EnterFight();
                lastGestureTime = Time.time;
            }
            else if (currentState == State.Talking && delta > 2f)
            {
                //Debug.Log(cursorPos.x + " " + cursorPos.y);
                if (cursorPos.x > -450f && cursorPos.x < 0 && cursorPos.y < 300f && cursorPos.y > -300f)
                {
                    Debug.Log("Mouse clicked on LEFT option");
                    ExitTalk(leftOption);
                    ClearLeftAndRightOptionsAndHide();
                    lastGestureTime = Time.time;
                }
                else if (cursorPos.x > 0 && cursorPos.x < 450f && cursorPos.y < 300f && cursorPos.y > -300f)
                {
                    Debug.Log("Mouse clicked on RIGHT option");
                    ExitTalk(rightOption);
                    ClearLeftAndRightOptionsAndHide();
                    lastGestureTime = Time.time;
                }
            }
        }
        else
        {
            lastGestureTime = Time.time;
        }
    }

    void hideTalkFightOptions()
    {
        this.talkImage.gameObject.SetActive(false);
        this.fightImage.gameObject.SetActive(false);
    }

    void ShowTalkFightOptions()
    {
        this.talkImage.gameObject.SetActive(true);
        this.fightImage.gameObject.SetActive(true);
    }

    void ShowInitialOptions()
    {
        Speaking.Instance.StartSpeaking(5f, Dialouge.Instance.getRandomIntroductionLine(curEnemy.GetName()), dialogueText, false);
        Debug.Log("Press 1 to Talk, 2 to Fight");
        currentState = State.ChooseAction;
        ShowTalkFightOptions();
    }

    void EnterTalk()
    {
        currentState = State.Talking;
        Debug.Log("You start talking... (press Space to finish)");
        this.speech = Dialouge.Instance.getRandomQuestionAndAnswer(curEnemy.GetName());
        Speaking.Instance.GiveQuestionAndTwoResponse(
            speech,
            dialogueText,
            leftOption,
            rightOption,
            false
        );
    }

    void ExitTalk(TMP_Text option)
    {
        Debug.Log("You finish talking.");

        if (isEnemyGood)
        {
            if (CheckAnswer(option.text))
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
        enemySpriteRenderer.sprite = imageHolder.GetSprite(curEnemy.GetName(), ImageHolder.State.NEUTRAL);
        ShowInitialOptions();
    }

    void EnterFight()
    {
        StartCoroutine(EnterFightRoutine());
    }

    IEnumerator EnterFightRoutine()
    {
        Speaking.Instance.StartSpeaking(3f, Dialouge.Instance.getRandomIntroductionLine(this.curEnemy.GetName()), dialogueText, true);
        yield return new WaitForSeconds(5f);
        currentState = State.ChooseAction;

        if (this.curEnemy.GetName() == "The King")
        {
            TriggerBadEnding();
            yield break;
        }

        this.curEnemy = EnemiesUtil.GetRandomEnemyAndRemove();
        enemySpriteRenderer.sprite = imageHolder.GetSprite(curEnemy.GetName(), ImageHolder.State.NEUTRAL);
        ShowInitialOptions();
    }

    void TriggerBadEnding()
    {
        Debug.Log("You attacked the King! This is a bad ending.");
    }

    void TriggerGoodEnding()
    {
        Debug.Log("You have defeated all enemies and reached a good ending!");
    }

    void TriggerNeutralEnding()
    {
        Debug.Log("You have reached a neutral ending.");
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

    bool CheckAnswer(string text)
    {
       return speech.IsCorrect(text);
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
