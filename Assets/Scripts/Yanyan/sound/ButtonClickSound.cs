using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonClickSound : MonoBehaviour, IPointerClickHandler
{
    [Header("Optional Custom Sound")]
    public AudioClip customClickSound;

    [Header("Locked Button Sound")]
    public bool playLockedSoundWhenNotInteractable = true;
    public AudioClip customLockedSound;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (UISoundManager.Instance == null)
        {
            Debug.LogWarning("ButtonClickSound: No UISoundManager found in scene.");
            return;
        }

        if (button != null && !button.interactable)
        {
            if (playLockedSoundWhenNotInteractable)
            {
                if (customLockedSound != null)
                    UISoundManager.Instance.PlaySound(customLockedSound);
                else
                    UISoundManager.Instance.PlayLockedSound();
            }

            return;
        }

        if (customClickSound != null)
            UISoundManager.Instance.PlaySound(customClickSound);
        else
            UISoundManager.Instance.PlayButtonClick();
    }
}