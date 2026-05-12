using UnityEngine;

public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Default UI Sounds")]
    public AudioClip defaultButtonClickSound;
    public AudioClip lockedButtonSound;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
    }

    public void PlayButtonClick()
    {
        PlaySound(defaultButtonClickSound);
    }

    public void PlayLockedSound()
    {
        PlaySound(lockedButtonSound);
    }

    public void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }
}