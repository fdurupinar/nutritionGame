using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq; // <-- Required for ToList()


public class TacticManager : MonoBehaviour
{

    public Animator TacticPanel;
    [Header("Grid Settings")]
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private Transform _gridParent;

    [Header("Grid Settings")]
    [SerializeField] private List<TacticSO> _tacticsToCreate; // Changed to a list of TacticSO

    private Card _currentlySelectedCard;

    [Header("Publish Settings")]
    [SerializeField] private GameObject _contentPanel;

    [SerializeField] private Button _publishButton;

    private CommentManager _commentManager;


    UserStats _userStats;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //// Hide the preview image initially
        //if (_publishedImagePreview != null)        
        //    _publishedImagePreview.gameObject.SetActive(false);

        _tacticsToCreate = new List<TacticSO>();

        _tacticsToCreate = Resources.LoadAll<TacticSO>("Content/Tactics").ToList();


        _commentManager = FindFirstObjectByType<CommentManager>();

        _userStats = GameObject.FindWithTag("Player").GetComponent<UserStats>();
        _userStats.Credibility = 100;

        int numberOfCards = _tacticsToCreate.Count;
        PopulateGrid(numberOfCards);

    }


    public void OpenCardView()
    {
        TacticPanel.SetBool("isHidden", false);

    }

    public void CloseCardView()
    {
        TacticPanel.SetBool("isHidden", true);

        HighlightPublish();

    }

    public void CloseCardViewWithoutSelection()
    {
        TacticPanel.SetBool("isHidden", true);
        _currentlySelectedCard = null;
          UnhighlightPublish();

    }
    public void PopulateGrid(int count)
    {
        // Clear any existing cards in the grid before populating
        foreach (Transform child in _gridParent)
        {
            Destroy(child.gameObject);
        }

        // Loop through the TacticSO list
        foreach (TacticSO tacticData in _tacticsToCreate)
        {
            if (!tacticData.enabledFlag) continue; // Skip disabled tactics

            GameObject newCardObj = Instantiate(_cardPrefab, _gridParent);
            Card cardComponent = newCardObj.GetComponent<Card>();

            // Pass the TacticSO data to the new card
            cardComponent.Setup(tacticData, this);
        }
    }


    public void OnCardSelected(Card card)
    {
        if (_currentlySelectedCard != null)
        {
            _currentlySelectedCard.Deselect();
        }

        if (_currentlySelectedCard == card)
        {
            _currentlySelectedCard.Deselect();
            _currentlySelectedCard = null;
            Debug.Log("Card deselected.");
        }
        else
        {
            _currentlySelectedCard = card;
            _currentlySelectedCard.Select();
            // You can now access the selected tactic's data
            Debug.Log($"Selected Tactic: {_currentlySelectedCard.TacticData.displayName}");
        }
    }


    // This is the public method the Publish Button will call
    public void OnPublishButtonClicked()
    {
        if (_currentlySelectedCard != null)
        {
            // Get the data from the selected card
            TacticSO selectedTactic = _currentlySelectedCard.TacticData;


            _contentPanel.GetComponent<Image>().sprite = selectedTactic.tacticImage;
            _contentPanel.GetComponent<Image>().gameObject.SetActive(true);
            _contentPanel.GetComponentInChildren<TextMeshProUGUI>().text = selectedTactic.text;

            Debug.Log(selectedTactic.text);
            // Update the preview image with the selected card's sprite
            //_publishedImagePreview.sprite = selectedTactic.tacticImage;
            //_publishedImagePreview.gameObject.SetActive(true);

            Debug.Log($"Published Tactic: {selectedTactic.displayName}");

            UpdateScores(selectedTactic);
            StartCoroutine(_commentManager.DisplayCommentsRoutine(2f));
            _currentlySelectedCard.Deselect();
            UnhighlightPublish();
        }
        else
        {

            Debug.LogWarning("No card selected to publish!");
        }
    }

    public void UnhighlightPublish()
    {
        ColorBlock cb = _publishButton.colors;
        cb.normalColor = Color.white;
        _publishButton.colors = cb;

    }
    public void HighlightPublish()
    {
        ColorBlock cb = _publishButton.colors;
        cb.normalColor = Color.yellow;
        _publishButton.colors = cb;

    }
    void UpdateScores(TacticSO tactic)
    {
        _userStats.Cash += (int)(tactic.engagementBonus * 100);
        _userStats.FollowerCount += (int)(tactic.engagementBonus * 1000);

        _userStats.Likes += (int)(tactic.engagementBonus * 50);
        _userStats.Credibility -= (int)(tactic.credibilityCost * 100);

    }
    
}
