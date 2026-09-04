using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class MainMenuManager : MonoBehaviour
{

    [SerializeField] AudioSource audioSource;

    [Header("Audio Clips")]
    [SerializeField] AudioClip HoverSFX;
    [SerializeField] AudioClip PressSFX;

    public void PlayButtonHoverSFX()
    {
        audioSource.PlayOneShot(HoverSFX);
    }
    public void PlayButtonDownSFX()
    {
        audioSource.PlayOneShot(PressSFX);
    }

    public void LoadScene(int id)
    {
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

 
    public void ShowImage()
    {
       
    }

    public void HideImage()
    {
       
    }

    public void QuitApplication()
    {
        Application.Quit();
    }
}
