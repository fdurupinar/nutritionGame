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

    private AudioSource audioSource;

    void Awake()
    {
        // Get the AudioSource component attached to this object
        audioSource = GetComponent<AudioSource>();
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
