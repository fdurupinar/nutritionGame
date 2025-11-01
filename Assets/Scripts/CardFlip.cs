using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Required for all click detection
using TMPro;

// Implement IPointerClickHandler to get click events
public class CardFlip : MonoBehaviour, IPointerClickHandler
{
    [Header("Card Back")]
    public GameObject cardBack;
    public TextMeshProUGUI debunkingTextComponent;

    [Header("Animation")]
    public float flipDuration = 0.3f;

    // Internal state
    private bool isFlipped = false;
    private bool isAnimating = false;
    private string myDebunkingText = "";
    private List<GameObject> cardFrontVisuals = new List<GameObject>();

    // This is your method that handles selecting the card
    // You can hook this up to your GameManager
    // public System.Action<CardFlip> OnCardSelected;

    void Awake()
    {
        // Find all child UI elements that are NOT the "Card_Back"
        foreach (Transform child in transform)
        {
            if (child.gameObject != cardBack && child.name != "SelectionOutlineImage")
            {
                cardFrontVisuals.Add(child.gameObject);
            }
        }
    }

    /// <summary>
    /// Call this to set the card's debunking text
    /// </summary>
    public void SetupCard(string debunkText)
    {
        myDebunkingText = debunkText;
        debunkingTextComponent.text = myDebunkingText;


    }

    /// <summary>
    /// This single function handles ALL clicks on this object
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Stop if we are already flipping
        if (isAnimating) return;


        // --- LEFT CLICK ---
        // Check if the button pressed was the LEFT mouse button
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Only allow selecting the card if it's face-up
            if (!isFlipped)
            {
                GetComponent<Card>().OnCardClicked();
            }
        }
        // --- RIGHT CLICK ---
        // Check if the button pressed was the RIGHT mouse button
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Flip the card
            StartCoroutine(FlipCardRoutine());
        }
    }

    // /// <summary>
    // /// This is your original function, now called on a left-click
    // /// </summary>
    // private void OnCardClicked()
    // {

    //     // This line calls the action so your GameManager can receive the event
    //     OnCardSelected?.Invoke(this);
    // }

    /// <summary>
    /// This is the flip animation, now called on a right-click
    /// </summary>
    private IEnumerator FlipCardRoutine()
    {
        isAnimating = true;
        Vector3 originalScale = transform.localScale;
        Vector3 halfwayScale = new Vector3(0, originalScale.y, originalScale.z);

        float time = 0;

        // Animate down
        while (time < (flipDuration / 2))
        {
            transform.localScale = Vector3.Lerp(originalScale, halfwayScale, time / (flipDuration / 2));
            time += Time.deltaTime;
            yield return null;
        }
        transform.localScale = halfwayScale;

        // Swap faces
        isFlipped = !isFlipped;
        cardBack.SetActive(isFlipped);
        foreach (GameObject visual in cardFrontVisuals)
        {
            visual.SetActive(!isFlipped);
        }

        // Animate up
        time = 0;
        while (time < (flipDuration / 2))
        {
            transform.localScale = Vector3.Lerp(halfwayScale, originalScale, time / (flipDuration / 2));
            time += Time.deltaTime;
            yield return null;
        }
        transform.localScale = originalScale;

        isAnimating = false;
    }
}