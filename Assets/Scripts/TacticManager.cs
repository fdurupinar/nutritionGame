using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq; // <-- Required for ToList()
using System;
using System.Collections;

using System.Diagnostics;

[System.Serializable]
public class DayConfig
{
    public int dayNumber;
    public List<TacticSO> tacticsForThisDay;
}

public class TacticManager : MonoBehaviour
{
    [Header("Day System Configuration")]
    public List<DayConfig> dayConfigs;
    public Animator TacticPanel;
    [Header("Grid Settings")]
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private Transform _gridParent;

    [Header("Grid Settings")]
    [SerializeField] private List<TacticSO> _allTactics; // Changed to a list of TacticSO

    [SerializeField] private List<TacticSO> _currentTactics; // Changed to a list of TacticSO
    private Card _currentlySelectedCard;

    [Header("Publish Settings")]
    [SerializeField] private GameObject _contentPanel;

    [SerializeField] private Button _selectCardButton;

    [SerializeField] TextMeshProUGUI _contentBox;
    private ScrollRect _scrollRect;

    private CommentManager _commentManager;

    bool _isAtBottom;
    UserStats _userStats;

    [SerializeField] private float _wordsPerSec = 4f;

    private Coroutine _displayTextRoutine;
    private Coroutine _speakAndCommentRoutine;
    private int _displayRequestId = 0;

    [Header("Daily Post Fill Blank")]
    public DailyPostFillBlankManager dailyPostFillBlankManager;

  

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //// Hide the preview image initially
        //if (_publishedImagePreview != null)        
        //    _publishedImagePreview.gameObject.SetActive(false);

        _allTactics = new List<TacticSO>();
        _currentTactics = new List<TacticSO>();

        _allTactics = Resources.LoadAll<TacticSO>("Content/Tactics").ToList();



        _scrollRect = _contentBox.GetComponentInParent<ScrollRect>();

        _commentManager = FindFirstObjectByType<CommentManager>();

        _userStats = GameObject.FindWithTag("Player").GetComponent<UserStats>();
       

        // _contentBox = _contentPanel.GetComponentInChildren<TextMeshProUGUI>();


        // --- Day System Logic ---
        int currentDay = 1;
        if (DayManager.Instance != null)
        {
            currentDay = DayManager.Instance.currentDay;
        }

        // Clear current tactics to ensure a fresh start for the day
        _currentTactics.Clear();

        // Loop through ALL configs to add tactics for the current day AND previous days
        foreach (DayConfig config in dayConfigs)
        {
            if (config.dayNumber <= currentDay)
            {
                foreach (TacticSO tactic in config.tacticsForThisDay)
                {
                    // Only add if it matches the player level AND isn't already in the list
                    if (tactic.level == _userStats.Level && !_currentTactics.Contains(tactic))
                    {
                        _currentTactics.Add(tactic);
                    }
                }
            }
        }
        PopulateGrid();

        // HidePublish();

    }


    public void OpenCardView()
    {
        TacticPanel.SetBool("isHidden", false);

    }

    public void CloseCardView()
    {
        TacticPanel.SetBool("isHidden", true);


        //  ShowPublish();

    }

    public void CloseCardViewWithoutSelection()
    {
        TacticPanel.SetBool("isHidden", true);
        _currentlySelectedCard = null;
        // HidePublish();

    }
    public void PopulateGrid()
    {
        // Clear any existing cards in the grid before populating
        foreach (Transform child in _gridParent)
        {
            Destroy(child.gameObject);
        }

        // Loop through the TacticSO list
        foreach (TacticSO tacticData in _currentTactics)
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

        }
    }






    private IEnumerator SpeakAndShowComments(TacticSO selectedTactic, string postTextToShow)
    {
        if (selectedTactic == null)
            yield break;

        float delay = postTextToShow.Split(' ').Length / _wordsPerSec;

        yield return new WaitForSeconds(delay);

        UpdateScores(selectedTactic);

        if (_commentManager != null)
        {
            StartCoroutine(_commentManager.DisplayCommentsRoutine(selectedTactic.type, 2f));
        }

        yield return new WaitForSeconds(3f);

        if (GlobalStatManager.Instance != null)
        {
            GlobalStatManager.Instance.SaveToDisk(
                _userStats.Cash,
                _userStats.FollowerCount,
                _userStats.Credibility
            );
        }

        yield return new WaitForSeconds(2f);

        ShowSelectCardButton();
    }


    private void StopTacticPublishRoutines()
    {
        _displayRequestId++;

        if (_displayTextRoutine != null)
        {
            StopCoroutine(_displayTextRoutine);
            _displayTextRoutine = null;
        }

        if (_speakAndCommentRoutine != null)
        {
            StopCoroutine(_speakAndCommentRoutine);
            _speakAndCommentRoutine = null;
        }

        _isAtBottom = false;
    }




    private IEnumerator DisplayTextCC(string fullText, int requestId)
    {
        if (_contentBox == null)
            yield break;

        _contentBox.text = "";

        string[] words = fullText.Split(' ');
        float delay = 1.0f / _wordsPerSec;

        foreach (string word in words)
        {
            if (requestId != _displayRequestId)
                yield break;

            _contentBox.text += word + " ";

            if (_scrollRect != null)
            {
                _isAtBottom = _scrollRect.verticalNormalizedPosition <= 0.1f;
            }

            yield return new WaitForSeconds(delay);
        }
    }


    // This is the public method the Publish Button will call
    public void OnPublishButtonClicked()
    {
        if (_currentlySelectedCard == null)
        {
            UnityEngine.Debug.LogWarning("No card selected to publish!");
            return;
        }

        StopTacticPublishRoutines();

        TacticSO selectedTactic = _currentlySelectedCard.TacticData;

        string postTextToShow = selectedTactic.text;

        if (dailyPostFillBlankManager != null &&
            !string.IsNullOrWhiteSpace(dailyPostFillBlankManager.currentCompletedSentence))
        {
            postTextToShow = dailyPostFillBlankManager.currentCompletedSentence;
        }

        if (TacticPanel != null)
        {
            TacticPanel.SetBool("isHidden", false);
        }

        if (_contentPanel != null)
        {
            Image contentImage = _contentPanel.GetComponent<Image>();

            if (contentImage != null)
            {
                contentImage.sprite = selectedTactic.tacticImage;
                contentImage.gameObject.SetActive(true);
            }
        }

        if (_contentBox != null)
        {
            _contentBox.text = "";
        }

        if (_scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 1f;
        }

        HideSelectCardButton();

        _displayTextRoutine = StartCoroutine(DisplayTextCC(postTextToShow, _displayRequestId));
        _speakAndCommentRoutine = StartCoroutine(SpeakAndShowComments(selectedTactic, postTextToShow));

        _currentlySelectedCard.Deselect();
        _currentlySelectedCard = null;

        _currentTactics.Remove(selectedTactic);
        PopulateGrid();
    }

    private void StopTacticScrollRoutines()
    {
        _displayRequestId++;

        if (_displayTextRoutine != null)
        {
            StopCoroutine(_displayTextRoutine);
            _displayTextRoutine = null;
        }

        if (_speakAndCommentRoutine != null)
        {
            StopCoroutine(_speakAndCommentRoutine);
            _speakAndCommentRoutine = null;
        }

        _isAtBottom = false;
    }
    public void ShowDailyPostCompletedSentenceOnly(string completedSentence)
    {
        if (string.IsNullOrWhiteSpace(completedSentence))
        {
            UnityEngine.Debug.LogWarning("TacticManager: Daily Post completed sentence is empty.");
            return;
        }

        StopTacticScrollRoutines();

        if (_contentPanel != null)
        {
            _contentPanel.SetActive(true);

            Image contentImage = _contentPanel.GetComponent<Image>();
            if (contentImage != null)
            {
                contentImage.sprite = null;
                contentImage.color = Color.white;
                contentImage.enabled = true;
            }
        }

        if (_contentBox != null)
        {
            _contentBox.text = "";
        }

        if (_scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 1f;
        }

        if (_commentManager != null)
        {
            _commentManager.ClearComments();
        }

        HideSelectCardButton();

        _displayTextRoutine = StartCoroutine(DisplayTextCC(completedSentence, _displayRequestId));
    }

    public void HideSelectCardButton()
    {
        _selectCardButton.gameObject.SetActive(false);

    }
    public void ShowSelectCardButton()
    {
        _selectCardButton.gameObject.SetActive(true);

    }
    void UpdateScores(TacticSO tactic)
    {
        _userStats.Cash += (int)(tactic.engagementBonus * 100);
        _userStats.FollowerCount += (int)(tactic.engagementBonus * 1000);

        _userStats.Likes += (int)(tactic.engagementBonus * 50);
        _userStats.Credibility -= (int)(tactic.credibilityCost * 100);

    }

    void LateUpdate()
    {

        if (_isAtBottom)
        {
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
            _isAtBottom = false;
        }
    }
}
