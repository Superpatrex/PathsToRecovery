using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameLoop : MonoBehaviour
{
    private int goodEnding = 4;

    private bool isEnemyGood = true;
    private Enemy curEnemy;
    public GameObject leftPanel;
    public GameObject rightPanel;
    public TMP_Text dialogueText;
    public TMP_Text leftOption;
    public TMP_Text rightOption;
    public TMP_Text healthText;
    public SpriteRenderer enemySpriteRenderer;
    public ImageHolder imageHolder;
    public Image cursorImage;
    public TCPClient tcpClient;
    private DialougeWithCorrectAnswer speech;

    public Image fightImage;
    public Image talkImage;
    public Image blockImage;
    public Image attackImage;
    private float health = 100f;
    private bool isEnemyGoingToAttack = false;
    public MusicPlayer musicPlayer;

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
        this.healthText.text = $"{health} HP";
    }

    void Update()
    {
        // If we press the h key, we add 50 health to the player
        if (Input.GetKeyDown(KeyCode.H))
        {
            health += 50;
            Debug.Log($"Health increased to {health}");
        }

        if (Input.GetKeyDown(KeyCode.Minus))
        {
            goodEnding--;
            Debug.Log($"Good ending decreased to {goodEnding}");
        }

        if (Input.GetKeyDown(KeyCode.Equals))
        {
            goodEnding++;
            Debug.Log($"Good ending increased to {goodEnding}");
        }

        if (lastGestureTime == -1)
        {
            lastGestureTime = Time.time;
        }

        if (curEnemy.IsDefeated() || Input.GetKeyDown(KeyCode.S))
        {
            musicPlayer.FadeOut(3f);
            Debug.Log($"You have defeated {curEnemy.GetName()}!");

            if (curEnemy.IsGood())
            {
                goodEnding++;
                Debug.Log($"Good ending increased to {goodEnding}");
            }
            else
            {
                goodEnding--;
                Debug.Log($"Good ending decreased to {goodEnding}");
            }

            if (curEnemy.IsTheKing())
                {
                    TriggerDeadKindEnding();
                    return;
                }

            if (EnemiesUtil.IsEnemyListEmpty())
            {
                if (goodEnding >= 8)
                {
                    TriggerGoodEnding();
                }
                else if (goodEnding >= 0 && goodEnding < 8)
                {
                    TriggerNeutralEnding();
                }
                else if (goodEnding < 0)
                {
                    TriggerBadEnding();
                }

                return;
            }

            this.curEnemy = EnemiesUtil.GetRandomEnemyAndRemove();
            enemySpriteRenderer.sprite = imageHolder.GetSprite(curEnemy.GetName(), ImageHolder.State.NEUTRAL);
        }
        
        // Add delta to see if the TPC Client open or close has been going on for 2 seconds straight
        float delta = Time.time - lastGestureTime;

        //Debug.Log($"Delta time since last gesture: {delta} " + cursorImage.rectTransform.anchoredPosition.ToString() + " " + tcpClient.IsGestureClosed() + " " + tcpClient.IsGestureOpen());

        if (tcpClient.IsGestureOpen() || tcpClient.IsGestureClosed() || Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Alpha6))
        {
            //Debug.Log("Mouse click detected via gesture!");

            Vector2 cursorPos = cursorImage.rectTransform.anchoredPosition;

            if (Input.GetKeyDown(KeyCode.Alpha1) || currentState == State.ChooseAction && delta > 2f && cursorPos.x < -120f && cursorPos.x > -650f && cursorPos.y < 50f && cursorPos.y > -120f)
            {
                //Debug.Log("Mouse clicked on TALK area");

                enemySpriteRenderer.sprite = imageHolder.GetSprite(curEnemy.GetName(), ImageHolder.State.BLOCK);
                currentState = State.Talking;
                hideTalkFightOptions();
                MakeLeftAndRightOptionsVisible();
                EnterTalk();
                lastGestureTime = Time.time;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || currentState == State.ChooseAction && delta > 2f && cursorPos.x > 200f && cursorPos.x < 820f && cursorPos.y < 50f && cursorPos.y > -120f)
            {
                Debug.Log("Mouse clicked on FIGHT area");

                musicPlayer.StartMusic();
                enemySpriteRenderer.sprite = imageHolder.GetSprite(curEnemy.GetName(), ImageHolder.State.ATTACK);
                currentState = State.FightChoose;
                hideTalkFightOptions();
                EnterFight();
                lastGestureTime = Time.time;
                isEnemyGoingToAttack = Random.value > 0.5f;

                if (isEnemyGoingToAttack)
                {
                    Debug.Log("Enemy is going to attack!");
                    enemySpriteRenderer.sprite = imageHolder.GetSprite(curEnemy.GetName(), ImageHolder.State.ATTACK);
                }
                else
                {
                    Debug.Log("Enemy is loafing around!");
                    enemySpriteRenderer.sprite = imageHolder.GetSprite(curEnemy.GetName(), ImageHolder.State.NEUTRAL);
                }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Alpha4) || currentState == State.Talking && delta > 2f)
            {
                if (Input.GetKeyDown(KeyCode.Alpha3) || cursorPos.x > -1350f && cursorPos.x < -420f && cursorPos.y < 280f && cursorPos.y > -200f)
                {
                    Debug.Log("Mouse clicked on LEFT option");
                    ExitTalk(leftOption);
                    ClearLeftAndRightOptionsAndHide();
                    lastGestureTime = Time.time;
                }
                else if (Input.GetKeyDown(KeyCode.Alpha4) || cursorPos.x > 440 && cursorPos.x < 1370f && cursorPos.y < 280f && cursorPos.y > -200f)
                {
                    Debug.Log("Mouse clicked on RIGHT option");
                    ExitTalk(rightOption);
                    ClearLeftAndRightOptionsAndHide();
                    lastGestureTime = Time.time;
                }

                musicPlayer.FadeOut(3f);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Alpha6) || currentState == State.FightChoose && delta > 2f)
            {
                if (Input.GetKeyDown(KeyCode.Alpha5) || cursorPos.x < -120f && cursorPos.x > -650f && cursorPos.y < 50f && cursorPos.y > -120f)
                {
                    // Block
                    Debug.Log("Mouse clicked on BLOCK area");
                    if (isEnemyGoingToAttack)
                    {
                        Debug.Log("You successfully blocked the attack!");
                        DoBlock();
                        hideAttackAndBlockOptions();
                        ShowInitialOptions();
                    }
                    else
                    {
                        Debug.Log("You blocked at nothing. Kinda embarrassing.");
                        hideAttackAndBlockOptions();
                        ShowInitialOptions();
                    }

                    lastGestureTime = Time.time;
                }
                else if (Input.GetKeyDown(KeyCode.Alpha6) || cursorPos.x > 200f && cursorPos.x < 820f && cursorPos.y < 50f && cursorPos.y > -120f)
                {
                    // Attack
                    Debug.Log("Mouse clicked on ATTACK area");
                    if (isEnemyGoingToAttack)
                    {
                        Debug.Log("You tried to attack while the enemy was attacking you. They were faster than you and got hit!");
                        health -= 20;
                        this.healthText.text = $"{health} HP";
                        Debug.Log($"Your health is now {health}");

                        if (health <= 0)
                        {
                            Debug.Log("You have been defeated due to loss of health. This is a bad ending.");
                            SceneManager.LoadScene(Constants.DEAD_CUTSCENE);
                            return;
                        }

                        hideAttackAndBlockOptions();
                        ShowInitialOptions();
                    }
                    else
                    {
                        Debug.Log("You successfully attacked the enemy!");
                        hideAttackAndBlockOptions();
                        ShowInitialOptions();
                    }

                    lastGestureTime = Time.time;
                    DoAttack();
                }
            }
        }
        else
        {
            lastGestureTime = Time.time;
        }
    }

    void hideAttackAndBlockOptions()
    {
        this.attackImage.gameObject.SetActive(false);
        this.blockImage.gameObject.SetActive(false);
    }

    void showAttackAndBlockOptions()
    {
        this.attackImage.gameObject.SetActive(true);
        this.blockImage.gameObject.SetActive(true);
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
        enemySpriteRenderer.sprite = imageHolder.GetSprite(curEnemy.GetName(), ImageHolder.State.NEUTRAL);
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

        if (curEnemy.IsDemonKing())
        {
            TriggerTalkToDemonKing();
            return;
        }

        if (isEnemyGood)
        {
            if (CheckAnswer(option.text))
            {
                Debug.Log("Your answer was spot on! (+1 good ending)");
                goodEnding++;
            }
            else
            {
                Debug.Log("You fumbled the conversation... (-1 good ending)");
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
        showAttackAndBlockOptions();
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

        ShowInitialOptions();
    }

    void TriggerDeadKindEnding()
    {
        Debug.Log("You have defeated the King! This is a bad ending.");
        SceneManager.LoadScene(Constants.DEAD_KING_CUTSCENE);
    }

    void TriggerTalkToDemonKing()
    {
        Debug.Log("You have talked to the Demon King. This is a bad ending.");
        SceneManager.LoadScene(Constants.DEMON_CUTSCENE);
    }

    void TriggerTalkToDeadEnding()
    {
        Debug.Log("You have talked to the Dead King. This is a bad ending.");
        SceneManager.LoadScene(Constants.DEAD_CUTSCENE);
    }

    void TriggerBadEnding()
    {
        Debug.Log("You attacked the King! This is a bad ending.");
        SceneManager.LoadScene(Constants.BAD_CUTSCENE);
    }

    void TriggerGoodEnding()
    {
        Debug.Log("You have defeated all enemies and reached a good ending!");
        SceneManager.LoadScene(Constants.GOOD_CUTSCENE);
    }

    void TriggerNeutralEnding()
    {
        Debug.Log("You have reached a neutral ending.");
        SceneManager.LoadScene(Constants.NEUTRAL_CUTSCENE);
    }

    void DoAttack()
    {
        curEnemy.TakeDamage(50);
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
