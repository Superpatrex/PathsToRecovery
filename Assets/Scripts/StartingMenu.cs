using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartingMenu : MonoBehaviour
{
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
    }
    void Update()
    {
        if (!hasMusicStarted)
        {
            musicPlayer.StartMusic();
            hasMusicStarted = true;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("Mouse clicked on START area");
            SceneManager.LoadScene(Constants.INTRO_CUTSCENE);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Exiting application");
    #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
    #else
            Application.Quit();
    #endif
        }
    }
}
