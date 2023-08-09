using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DemoScene : MonoBehaviour
{

    string _playSceneName = "";
    public GameObject EventSystem;
    public GameObject RtcButton;
    public GameObject RtmButton;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnBackButton()
    {
        StartCoroutine(this.UnloadSceneAsync());
    }

    public IEnumerator UnloadSceneAsync()
    {
        if (this._playSceneName != "")
        {
            AsyncOperation async = SceneManager.UnloadSceneAsync(_playSceneName);
            yield return async;
            EventSystem.SetActive(true);
            RtcButton.SetActive(true);
            RtmButton.SetActive(true);
        }
    }

    public void OnRtcDemo()
    {
        this._playSceneName = "HomeScene";
        SceneManager.LoadScene(_playSceneName, LoadSceneMode.Additive);
        EventSystem.SetActive(false);
        RtcButton.SetActive(false);
        RtmButton.SetActive(false);
    }

    public void OnRtmDemo()
    {
        this._playSceneName = "RtmHomeScene";
        SceneManager.LoadScene(_playSceneName, LoadSceneMode.Additive);
        EventSystem.SetActive(false);
        RtcButton.SetActive(false);
        RtmButton.SetActive(false);
    }



}
