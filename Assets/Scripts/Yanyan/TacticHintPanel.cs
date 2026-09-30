using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TacticHintPanel : MonoBehaviour
{
    [Header("Hint Button")]
    public Button hintButton;
    public DailyPostFillBlankManager captionManager;

    [Header("Hint Panel")]
    public GameObject hintPanel;
    public RectTransform hintPanelRect;
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI panelTitle;
    public Image feedbackCat;

    [Header("Optional Close Button")]
    public Button closeButton;

    [Header("Animation")]
    public float openAnimationTime = 0.18f;
    public float closeAnimationTime = 0.14f;
    public Vector3 startScale = new Vector3(0.1f, 0.1f, 0.1f);
    public Vector3 endScale = Vector3.one;

    [Header("Current Selected Card")]
    public Card currentSelectedCard;

    private Coroutine animationRoutine;

    private void Start()
    {
        HideHintPanelInstant();

        // Button stays active and clickable the whole time.
        SetHintButtonAlwaysActive();

        if (hintButton != null)
        {
            hintButton.onClick.RemoveAllListeners();
            hintButton.onClick.AddListener(OpenHintPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseHintPanel);
        }
    }

    public void SetSelectedCard(Card card)
    {
        HideHintPanelInstant();
        currentSelectedCard = card;

        // Do not disable or hide the hint button.
        SetHintButtonAlwaysActive();
    }

    public void ClearSelectedCard()
    {
        currentSelectedCard = null;

        // Do not disable or hide the hint button.
        SetHintButtonAlwaysActive();

        HideHintPanelInstant();
    }

    public void OpenHintPanel()
    {
        if (feedbackCat != null) feedbackCat.gameObject.SetActive(false);
        if (panelTitle != null) panelTitle.text = "Inside the tactic";
        if (hintPanel == null)
            return;

        if (hintText != null)
        {
            hintText.text = TacticExplorer.Describe(currentSelectedCard != null ? currentSelectedCard.TacticData : null);
            if (!string.IsNullOrWhiteSpace(captionManager?.currentCompletedSentence))
                hintText.text = "<b>YOUR POST</b>\n" + TacticExplorer.Escape(captionManager.currentCompletedSentence) + "\n\n" + hintText.text;
        }

        ShowPanel();
    }

    public void ShowFeedback(string title, string message, Sprite expression)
    {
        if (hintPanel == null || hintText == null) return;
        hintText.text = message;
        if (panelTitle != null) panelTitle.text = title;
        if (feedbackCat != null)
        {
            feedbackCat.sprite = expression;
            feedbackCat.gameObject.SetActive(expression != null);
        }
        ShowPanel();
    }

    private void ShowPanel()
    {
        hintPanel.SetActive(true);
        Canvas.ForceUpdateCanvases();
        var reading = hintPanel.GetComponentInChildren<ScrollRect>();
        if (reading != null) { reading.StopMovement(); reading.verticalNormalizedPosition = 1f; }

        if (hintPanelRect == null)
        {
            hintPanelRect = hintPanel.GetComponent<RectTransform>();
        }

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        animationRoutine = StartCoroutine(OpenScaleAnimation());
    }

    public void CloseHintPanel()
    {
        if (hintPanel == null)
            return;

        if (!hintPanel.activeSelf)
            return;

        if (hintPanelRect == null)
        {
            hintPanelRect = hintPanel.GetComponent<RectTransform>();
        }

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        animationRoutine = StartCoroutine(CloseScaleAnimation());
    }

    private IEnumerator OpenScaleAnimation()
    {
        if (hintPanelRect == null)
            yield break;

        float timer = 0f;
        hintPanelRect.localScale = startScale;

        while (timer < openAnimationTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / openAnimationTime);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            hintPanelRect.localScale = Vector3.Lerp(startScale, endScale, smoothT);

            yield return null;
        }

        hintPanelRect.localScale = endScale;
        animationRoutine = null;
    }

    private IEnumerator CloseScaleAnimation()
    {
        if (hintPanelRect == null)
            yield break;

        float timer = 0f;
        hintPanelRect.localScale = endScale;

        while (timer < closeAnimationTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / closeAnimationTime);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            hintPanelRect.localScale = Vector3.Lerp(endScale, startScale, smoothT);

            yield return null;
        }

        hintPanelRect.localScale = startScale;

        if (hintPanel != null)
        {
            hintPanel.SetActive(false);
        }

        animationRoutine = null;
    }

    public void HideHintPanelInstant()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        if (hintPanelRect != null)
        {
            hintPanelRect.localScale = startScale;
        }

        if (hintPanel != null)
        {
            hintPanel.SetActive(false);
        }
    }

    private void SetHintButtonAlwaysActive()
    {
        if (hintButton != null)
        {
            hintButton.gameObject.SetActive(true);
            hintButton.interactable = true;
        }
    }
}
