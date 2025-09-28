using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DemonCutscene : MonoBehaviour
{
    // track “good” vs “bad” endings
    public int goodEnding = 0;
    public int endingNum = 0;

    private bool isEnemyGood = true;
    private Enemy curEnemy;
    public TMP_Text dialogueText;

    // now a list of Sprites, not Image components
    public List<Sprite> cutsceneSprites;
    // this Image will display whichever sprite is next
    public Image background;

    // the name of the scene to load when the cutscene finishes
    private string nextSceneName = Constants.MAIN_SCENE;
    public MusicPlayer musicPlayer;
    private bool hasMusicStarted = false;

    private enum State
    {
        ChooseAction,
        Talking,
        FightChoose
    }
    private State currentState = State.ChooseAction;

    void Start()
    {
        Debug.Log(nextSceneName);
        musicPlayer.StartMusic();
        StartCoroutine(Exposit(6, Dialouge.Instance.demonKingEnding, 5f));
    }

    void Update()
    {
        if (!hasMusicStarted)
        {
            musicPlayer.StartMusic();
            hasMusicStarted = true;
        }
    }

    IEnumerator FadeImage(Image img, float fromAlpha, float toAlpha, float duration)
    {
        Color col = img.color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
            img.color = new Color(col.r, col.g, col.b, a);
            yield return null;
        }
        img.color = new Color(col.r, col.g, col.b, toAlpha);
    }

    IEnumerator Exposit(int lines, List<string> source, float delayTime)
    {
        for (int i = 0; i < lines; i++)
        {
            if (i < cutsceneSprites.Count)
            {
                // swap sprite and prep it for fading
                background.sprite = cutsceneSprites[i];
                background.color = new Color(background.color.r,
                                             background.color.g,
                                             background.color.b,
                                             0f);
                yield return StartCoroutine(FadeImage(background, 0f, 1f, 1f));
            }

            Speaking.Instance.StartSpeaking(5f, source[i], dialogueText, false, true);
            yield return new WaitForSeconds(delayTime + 3f);

            if (i < cutsceneSprites.Count)
            {
                yield return StartCoroutine(FadeImage(background, 1f, 0f, 1f));
            }
        }

        // Once all lines and fades are done, load the next scene
        SceneManager.LoadScene(nextSceneName);
    }
}
