using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class PauseMenuManager : MonoBehaviour
{

    [SerializeField] private AudioSource audioSource;

    [Header("Audio Clips")]
    [SerializeField] AudioClip HoverSFX;
    [SerializeField] AudioClip PressSFX;

    [SerializeField] private GameObject PauseMenu;

    public bool IsPaused;

    void Start()
    {

    }


    void LateUpdate()
    {
        if (InputManager.Instance.PlayerPause())
        {
            if (IsPaused && PauseMenu.activeSelf)
                ResumeGame();
            
            if(!IsPaused)
                PauseGame();
        }
    }


    public void LockAndHideCursor(bool enable)
    {

        if (enable)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }


    public void PlayButtonHoverSFX()
    {
        audioSource.PlayOneShot(HoverSFX);
    }
    public void PlayButtonDownSFX()
    {
        audioSource.PlayOneShot(PressSFX);
    }


    public void PauseGame()
    {
        Time.timeScale = 0f;

        LockAndHideCursor(false);
        PauseMenu.SetActive(true);
        IsPaused = true;
    }

    public void ResumeGame()
    {


        LockAndHideCursor(true);
        IsPaused = false;

        PauseMenu.SetActive(false);
        Time.timeScale = 1f;
    }


    public void LoadScene(int id)
    {
        Time.timeScale = 1f;
        PauseMenu.SetActive(false);
        LockAndHideCursor(false);
        StartCoroutine(LoadSceneAsync(id));
    }

    IEnumerator LoadSceneAsync(int id)
    {
        var loading = SceneManager.LoadSceneAsync(id);

        while (!loading.isDone)
        {
            yield return null;
        }

    }




}