using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingBarController : MonoBehaviour
{
    [Header("Loading UI")]
    public GameObject loadingPanel;
    public Slider loadingBar;
    public TextMeshProUGUI loadingText;

    [Header("Loading Text")]
    public string loadingMessage = "Next Day...";
    public bool loopTypingText = true;
    public float letterDelay = 0.08f;

    [Header("Loading Time")]
    public float loadingDuration = 1.5f;

    [Header("Sound Effect")]
    public AudioSource audioSource;
    public AudioClip loadingStartSound;
    public AudioClip loadingFinishSound;
    public bool loopLoadingSound = false;

    [Header("Optional Scene Loading")]
    public bool loadSceneAfterLoading = false;
    public string sceneNameToLoad;

    [Header("After Loading Event")]
    public UnityEvent onLoadingFinished;

    private bool isLoading = false;
    private Coroutine typingRoutine;

    private void Start()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        if (loadingBar != null)
        {
            loadingBar.value = 0f;
        }

        if (loadingText != null)
        {
            loadingText.text = "";
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void StartLoading()
    {
        if (isLoading)
            return;

        StartCoroutine(LoadingRoutine());
    }

    private IEnumerator LoadingRoutine()
    {
        isLoading = true;

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        if (loadingBar != null)
        {
            loadingBar.value = 0f;
        }

        PlayLoadingStartSound();

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
        }

        typingRoutine = StartCoroutine(TypeLoadingText());

        float timer = 0f;

        while (timer < loadingDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / loadingDuration);

            if (loadingBar != null)
            {
                loadingBar.value = progress;
            }

            yield return null;
        }

        if (loadingBar != null)
        {
            loadingBar.value = 1f;
        }

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        if (loadingText != null)
        {
            loadingText.text = loadingMessage;
        }

        StopLoopingLoadingSound();
        PlayLoadingFinishSound();

        yield return new WaitForSeconds(0.15f);

        onLoadingFinished.Invoke();

        if (loadSceneAfterLoading && !string.IsNullOrWhiteSpace(sceneNameToLoad))
        {
            SceneManager.LoadScene(sceneNameToLoad);
        }
        else
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }

            isLoading = false;
        }
    }

    private IEnumerator TypeLoadingText()
    {
        if (loadingText == null)
            yield break;

        while (isLoading)
        {
            loadingText.text = "";

            for (int i = 0; i < loadingMessage.Length; i++)
            {
                loadingText.text += loadingMessage[i];
                yield return new WaitForSeconds(letterDelay);
            }

            if (!loopTypingText)
                yield break;

            yield return new WaitForSeconds(0.25f);
        }
    }

    private void PlayLoadingStartSound()
    {
        if (audioSource == null || loadingStartSound == null)
            return;

        if (loopLoadingSound)
        {
            audioSource.clip = loadingStartSound;
            audioSource.loop = true;
            audioSource.Play();
        }
        else
        {
            audioSource.PlayOneShot(loadingStartSound);
        }
    }

    private void StopLoopingLoadingSound()
    {
        if (audioSource == null)
            return;

        if (loopLoadingSound)
        {
            audioSource.Stop();
            audioSource.loop = false;
            audioSource.clip = null;
        }
    }

    private void PlayLoadingFinishSound()
    {
        if (audioSource == null || loadingFinishSound == null)
            return;

        audioSource.PlayOneShot(loadingFinishSound);
    }
}