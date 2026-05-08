using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Diagnostics;

public class Card : MonoBehaviour
{
    [Header("Tactic Data")]
    [SerializeField] private TacticSO _tacticData;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _subtitleText;
    [SerializeField] private TextMeshProUGUI _bonusText; // New UI element for the bonus
    [SerializeField] private TextMeshProUGUI _costText;  // New UI element for the cost
    [SerializeField] private Image _tacticMainImage; // Add this reference


    [SerializeField] private GameObject _selectionOutline;

    CardFlip _flipper;
    private Button _button;
    private TacticManager _controller;
    AudioManager _audioManager;

    // The Setup method now accepts a TacticSO
    public void Setup(TacticSO data, TacticManager controller)
    {
        _tacticData = data;
        _controller = controller;

        // Populate the card's UI with data from the TacticSO
        _nameText.text = _tacticData.displayName; // Puts "Appeal to Emotion" in the big text slot
        _subtitleText.text = ""; // Leaves the small text slot empty


        _audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();

        // Format and display the bonus and cost
        _bonusText.text = _tacticData.engagementBonus.ToString("+0.#%;-0.#%;0");
        _costText.text = (_tacticData.credibilityCost * -100).ToString("F0");

        _button = GetComponent<Button>();
        // _button.onClick.RemoveAllListeners(); // Clear previous listeners
        // _button.onClick.AddListener(OnCardClicked);


        _flipper = GetComponent<CardFlip>();
        _flipper.SetupCard(_tacticData.debunkingText);

        GetComponent<Image>().color = GetColorForTactic();

        transform.Find("CardBack").GetComponent<Image>().color = GetColorForTactic(); // Background image



        // Open this to change the card face
        //_tacticMainImage = GetComponent<Image>(); 
        //_tacticMainImage.sprite = _tacticData.tacticImage;


    }


    public Color GetColorForTactic()
    {
        string type = _tacticData.type;
        switch (type.ToLower())
        {
            case "emotion":
                // Soft Coral (#FFC4B4)
                return new Color32(242, 195, 185, 255);

            case "attack":
                return new Color32(210, 224, 211, 255);

            case "conspiracy":

                return new Color32(240, 221, 214, 255);


            case "financialgain":
                // Sandy Gold (#F5E8C7)
                return new Color32(245, 232, 199, 255);

            case "twistedevidence":

                return new Color32(151, 179, 174, 255);

            default:
                // A default color in case a new type is added
                return Color.white;
        }
    }
    public void OnCardClicked()
    {
        UnityEngine.Debug.Log("clicked");
        _controller.OnCardSelected(this);
        _audioManager.PlayCardSelect();
    }

    public void Select()
    {

        _selectionOutline.SetActive(true);
    }

    public void Deselect()
    {
        _selectionOutline.SetActive(false);
    }

    // A helper property to let the controller access the card's data
    public TacticSO TacticData => _tacticData;
}