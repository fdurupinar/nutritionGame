using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    // --- Audio Clips ---
    // Assign these in the Inspector
    public AudioClip StandardClick;
    public AudioClip CardSelect;
    public AudioClip EngagementNotification;

    public AudioClip CommentNotification;

    [Header("Post typing")]
    public AudioClip KeyboardTyping;
    [Range(0f, 1f)] public float keyboardTypingVolume = 0.3f;

    private AudioSource audioSource;
    private AudioSource typingSource;
    private int typingTapIndex;

    void Awake()
    {
        // Get the AudioSource component attached to this object
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayTypingTap()
    {
        if (KeyboardTyping == null || !isActiveAndEnabled) return;
        if (typingSource == null)
        {
            typingSource = gameObject.AddComponent<AudioSource>();
            typingSource.playOnAwake = false;
            typingSource.loop = false;
            typingSource.spatialBlend = 0f;
        }
        typingSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
        typingSource.mute = audioSource.mute;
        typingSource.volume = audioSource.volume * keyboardTypingVolume;
        // Vary key tone without changing the random sequence used by gameplay.
        typingSource.pitch = 0.94f + (typingTapIndex++ % 5) * 0.03f;
        typingSource.clip = KeyboardTyping;
        typingSource.Play();
    }

    public void StopTyping()
    {
        if (typingSource != null) typingSource.Stop();
    }

    private void OnDisable()
    {
        StopTyping();
    }

    // --- Public methods to be called by UI Events ---

    public void PlayStandardClick()
    {
        if (StandardClick != null)
        {
            audioSource.PlayOneShot(StandardClick);
        }
    }

    public void PlayCardSelect()
    {
        if (CardSelect != null)
        {
            audioSource.PlayOneShot(CardSelect);
        }
    }


    public void PlayEngagamentNotification()
    {
        if (EngagementNotification != null)
        {
            audioSource.PlayOneShot(EngagementNotification);
        }
    }

    public void PlayCommentNotification()
    {
        if (CommentNotification != null)
        {
            audioSource.PlayOneShot(CommentNotification);
        }
    }

}
