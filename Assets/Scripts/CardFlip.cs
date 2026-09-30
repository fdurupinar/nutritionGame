using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

// Right-click opens caption-based tactic feedback. Left-click selects the card.
public class CardFlip : MonoBehaviour, IPointerClickHandler
{
    // Retain serialized references so existing card prefabs remain compatible.
    public GameObject cardBack;
    public TextMeshProUGUI debunkingTextComponent;
    public float flipDuration = 0.3f;

    public void SetupCard(string debunkText)
    {
        if (cardBack != null) cardBack.SetActive(false);
        if (debunkingTextComponent != null) debunkingTextComponent.text = debunkText;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        var card = GetComponent<Card>();
        if (card == null || !card.CanSelect()) return;
        if (eventData.button == PointerEventData.InputButton.Left)
            card.OnCardClicked();
        else if (eventData.button == PointerEventData.InputButton.Right)
            card.ShowHint();
    }
}
