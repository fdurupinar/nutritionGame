using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Card : MonoBehaviour {
    [Header("Tactic Data")]
    [SerializeField] private TacticSO _tacticData;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _bonusText; // New UI element for the bonus
    [SerializeField] private TextMeshProUGUI _costText;  // New UI element for the cost
    [SerializeField] private Image _tacticMainImage; // Add this reference


    [SerializeField] private GameObject _selectionOutline;

    private Button _button;
    private TacticManager _controller;

    // The Setup method now accepts a TacticSO
    public void Setup(TacticSO data, TacticManager controller) {
        _tacticData = data;
        _controller = controller;

        // Populate the card's UI with data from the TacticSO
        _nameText.text = _tacticData.displayName;

        

        // Format and display the bonus and cost
        _bonusText.text = _tacticData.engagementBonus.ToString("+0.#%;-0.#%;0");
        _costText.text = (_tacticData.credibilityCost * -100).ToString("F0");

        _button = GetComponent<Button>();
        _button.onClick.RemoveAllListeners(); // Clear previous listeners
        _button.onClick.AddListener(OnCardClicked);

        GetComponent<Image>().color = GetColorForTactic(_tacticData.type);

        // Open this to change the card face
        //_tacticMainImage = GetComponent<Image>(); 
        //_tacticMainImage.sprite = _tacticData.tacticImage;


    }

    public Color GetColorForTactic(TacticType type)
    {
        switch (type)
        {
            case TacticType.Emotion:
                // Soft Coral (#FFC4B4)
                return new Color32(255, 196, 180, 255);

            case TacticType.Pseudoscience:
                // Mint Green (#BDEFD2)
                return new Color32(189, 239, 210, 255);

            case TacticType.Conspiracy:
                // Muted Lavender (#D2C4E4)
                return new Color32(210, 196, 228, 255);

            case TacticType.LogicalFallacy:
                // Light Yellow (#FFFACD)
                return new Color32(255, 250, 205, 255);

            case TacticType.FinancialGain:
                // Sandy Gold (#F5E8C7)
                return new Color32(245, 232, 199, 255);

            case TacticType.TwistedEvidence:
                // Slate Blue (#BCCDE4)
                return new Color32(188, 205, 228, 255);

            default:
                // A default color in case a new type is added
                return Color.white;
        }
    }
    private void OnCardClicked() {
        _controller.OnCardSelected(this);
    }

    public void Select() {
        _selectionOutline.SetActive(true);
    }

    public void Deselect() {
        _selectionOutline.SetActive(false);
    }

    // A helper property to let the controller access the card's data
    public TacticSO TacticData => _tacticData;
}