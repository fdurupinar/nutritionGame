using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Card : MonoBehaviour
{
    [Header("Tactic Data")]
    [SerializeField] private TacticSO _tacticData;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _subtitleText;
    [SerializeField] private TextMeshProUGUI _bonusText;
    [SerializeField] private TextMeshProUGUI _costText;
    [SerializeField] private Image _tacticMainImage;
    [SerializeField] private GameObject _selectionOutline;

    [Header("Locked Visual")]
    [SerializeField] private float _lockedAlpha = 0.45f;

    private CardFlip _flipper;
    private Button _button;
    private TacticManager _controller;
    private AudioManager _audioManager;
    private CanvasGroup _canvasGroup;
    private Outline _cardBorder;
    private Image _cardFrontArtwork;
    private bool _canSelect = true;

    public void Setup(TacticSO data, TacticManager controller)
    {
        _tacticData = data;
        _controller = controller;

        _button = GetComponent<Button>();
        _flipper = GetComponent<CardFlip>();

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        GameObject audioObj = GameObject.Find("AudioManager");
        if (audioObj != null)
        {
            _audioManager = audioObj.GetComponent<AudioManager>();
        }

        if (_tacticData == null)
            return;

        if (_nameText != null)
        {
            _nameText.text = string.IsNullOrWhiteSpace(_tacticData.displayName) ? "TACTIC" :
                System.Text.RegularExpressions.Regex.Replace(_tacticData.type ?? "TACTIC", "([a-z])([A-Z])", "$1 $2").ToUpperInvariant();
        }

        if (_subtitleText != null)
        {
            _subtitleText.text = string.IsNullOrWhiteSpace(_tacticData.displayName) ? _tacticData.type : _tacticData.displayName;
        }

        if (_bonusText != null)
        {
            _bonusText.text = _tacticData.engagementBonus.ToString("+0.#%;-0.#%;0");
        }

        if (_costText != null)
        {
            _costText.text = (_tacticData.credibilityCost * -100).ToString("F0");
        }

        if (_tacticMainImage != null)
        {
            _tacticMainImage.sprite = _tacticData.tacticImage;
        }

        if (_flipper != null)
        {
            _flipper.SetupCard(_tacticData.debunkingText);
        }

        Image frontImage = GetComponent<Image>();
        if (frontImage != null)
        {
            frontImage.color = GetColorForTactic();
        }

        Transform cardBack = transform.Find("CardBack");
        if (cardBack != null)
        {
            Image backImage = cardBack.GetComponent<Image>();
            if (backImage != null)
            {
                backImage.color = GetColorForTactic();
            }
        }

        ApplyCardFrontArtwork();
        Deselect();
    }

    private void ApplyCardFrontArtwork()
    {
        bool hasArtwork = _tacticData.cardFrontArtwork != null;
        if (hasArtwork && _cardFrontArtwork == null)
        {
            var artworkObject = new GameObject("Card Front Artwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            artworkObject.transform.SetParent(transform, false);
            _cardFrontArtwork = artworkObject.GetComponent<Image>();
            var rect = _cardFrontArtwork.rectTransform;
            // Proportional inset preserves the portrait ratio and leaves a selection edge.
            rect.anchorMin = new Vector2(0.02f, 0.02f);
            rect.anchorMax = new Vector2(0.98f, 0.98f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _cardFrontArtwork.preserveAspect = true;
            _cardFrontArtwork.raycastTarget = false;
            _cardFrontArtwork.color = Color.white;
        }

        if (_cardFrontArtwork != null)
        {
            _cardFrontArtwork.sprite = _tacticData.cardFrontArtwork;
            _cardFrontArtwork.gameObject.SetActive(hasArtwork);
        }

        // Complete card artwork already includes its heading; avoid printing text over it.
        if (_nameText != null) _nameText.gameObject.SetActive(!hasArtwork);
        if (_subtitleText != null) _subtitleText.gameObject.SetActive(!hasArtwork);
    }

    public void SetCardInteractable(bool canSelect)
    {
        _canSelect = canSelect;

        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        if (_button != null)
        {
            _button.interactable = canSelect;
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (canSelect)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }
        else
        {
            _canvasGroup.alpha = _lockedAlpha;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            Deselect();
        }
    }

    public bool CanSelect()
    {
        return _canSelect;
    }

    public Color GetColorForTactic()
    {
        return GamePalette.Secondary;
    }

    private void ApplySelectionColors(bool selected)
    {
        Color surface = selected ? GamePalette.Primary : GetColorForTactic();
        var front = GetComponent<Image>();
        if (front != null)
        {
            front.color = surface;
            // Outline the actual card shape, without a rectangular overlay over its content.
            if (_cardBorder == null)
                _cardBorder = GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
            Color borderColor = GamePalette.Text;
            borderColor.a = selected ? 1f : 0.2f;
            _cardBorder.effectColor = borderColor;
            _cardBorder.effectDistance = selected ? new Vector2(3f, -3f) : new Vector2(1f, -1f);
            _cardBorder.useGraphicAlpha = true;
        }
        var back = transform.Find("CardBack");
        if (back != null && back.TryGetComponent<Image>(out var backImage)) backImage.color = surface;
        foreach (var label in GetComponentsInChildren<TextMeshProUGUI>(true))
            label.color = selected ? GamePalette.OnPrimary : GamePalette.Text;
    }

    public void OnCardClicked()
    {
        if (!_canSelect)
        {
            Debug.Log("This card is locked.");
            return;
        }

        if (_controller != null)
        {
            _controller.OnCardSelected(this);
        }

        if (_audioManager != null)
        {
            _audioManager.PlayCardSelect();
        }
    }

    public void ShowHint()
    {
        if (!_canSelect || _controller == null || !_controller.IsTacticUnlocked(_tacticData)) return;
        // Do not toggle off a card that is already selected.
        if (_controller.SelectedTactic != _tacticData)
            _controller.OnCardSelected(this);
        var explorer = _controller.TacticPanel != null
            ? _controller.TacticPanel.GetComponent<TacticExplorer>() : null;
        if (explorer != null) explorer.CheckMyChoice();
    }

    public void Select()
    {
        if (!_canSelect)
            return;

        ApplySelectionColors(true);
        if (_selectionOutline != null)
        {
            _selectionOutline.SetActive(true);
        }
    }

    public void Deselect()
    {
        ApplySelectionColors(false);
        if (_selectionOutline != null)
        {
            _selectionOutline.SetActive(false);
        }
    }

    public TacticSO TacticData => _tacticData;
}
