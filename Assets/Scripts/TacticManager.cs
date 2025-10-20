using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq; // <-- Required for ToList()
using System;
using System.Collections;

using System.Diagnostics;

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

    [SerializeField] private float _wordsPerSec = 3f;

    TextMeshProUGUI _contentBox;
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

        _contentBox = _contentPanel.GetComponentInChildren<TextMeshProUGUI>();

        int numberOfCards = _tacticsToCreate.Count;
        PopulateGrid(numberOfCards);

        HidePublish();

    }


    public void OpenCardView()
    {
        TacticPanel.SetBool("isHidden", false);

    }

    public void CloseCardView()
    {
        TacticPanel.SetBool("isHidden", true);

        ShowPublish();

    }

    public void CloseCardViewWithoutSelection()
    {
        TacticPanel.SetBool("isHidden", true);
        _currentlySelectedCard = null;
        HidePublish();

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
            UnityEngine.Debug.Log("Card deselected.");
        }
        else
        {
            _currentlySelectedCard = card;
            _currentlySelectedCard.Select();
            // You can now access the selected tactic's data
            UnityEngine.Debug.Log($"Selected Tactic: {_currentlySelectedCard.TacticData.displayName}");
        }
    }



    private IEnumerator SpeakAndShowComments(string voice, string text)
    {

        string cmdArgs = string.Format(" -v {0} -r {1} \"{2}\"", voice, _wordsPerSec * 60, text.Replace("\"", ","));

        Process speechProcess = Process.Start("/usr/bin/say", cmdArgs);

        float delay = text.Split(' ').Length / _wordsPerSec;
        print(delay);
        yield return new WaitForSeconds(delay);
        UpdateScores(_currentlySelectedCard.TacticData);
        StartCoroutine(_commentManager.DisplayCommentsRoutine(2f));

    }




    private IEnumerator DisplayTextCC(string fullText)
    {
        // 1. Clear the text box
        _contentBox.text = "";

        // 2. Split the full text into an array of words
        string[] words = fullText.Split(' ');


        // 3. Calculate the time to wait between words
        float delay = 1.0f / _wordsPerSec;

        // 4. Loop through each word in the array
        foreach (string word in words)
        {

            _contentBox.text += word + " ";

            yield return new WaitForSeconds(delay);
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

            StartCoroutine(DisplayTextCC(selectedTactic.text));


            StartCoroutine(SpeakAndShowComments("Samantha", selectedTactic.text));

            _currentlySelectedCard.Deselect();

            HidePublish();
        }
        else
        {

            UnityEngine.Debug.LogWarning("No card selected to publish!");
        }
    }

    public void HidePublish()
    {
        _publishButton.gameObject.SetActive(false);

    }
    public void ShowPublish()
    {
        _publishButton.gameObject.SetActive(true);

    }
    void UpdateScores(TacticSO tactic)
    {
        _userStats.Cash += (int)(tactic.engagementBonus * 100);
        _userStats.FollowerCount += (int)(tactic.engagementBonus * 1000);

        _userStats.Likes += (int)(tactic.engagementBonus * 50);
        _userStats.Credibility -= (int)(tactic.credibilityCost * 100);

    }

}
