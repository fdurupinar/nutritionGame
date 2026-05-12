using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;

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

    public GameObject TacticPanel;

    [Header("Grid Settings")]
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private Transform _gridParent;

    [Header("Grid Settings")]
    [SerializeField] private List<TacticSO> _allTactics;
    [SerializeField] private List<TacticSO> _currentTactics;
    private Card _currentlySelectedCard;

    [Header("Publish Settings")]
    [SerializeField] private GameObject _contentPanel;
    [SerializeField] TextMeshProUGUI _contentBox;

    [Header("Button After Sentence Animation")]
    [SerializeField] private Button _afterSentenceButton;
    [SerializeField] private bool _hideAfterSentenceButtonObject = true;

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

    [Header("Hint Panel")]
    public TacticHintPanel tacticHintPanel;
    void Start()
    {
        _allTactics = new List<TacticSO>();
        _currentTactics = new List<TacticSO>();

        _allTactics = Resources.LoadAll<TacticSO>("Content/Tactics").ToList();

        _scrollRect = _contentBox.GetComponentInParent<ScrollRect>();
        _commentManager = FindFirstObjectByType<CommentManager>();

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            _userStats = playerObj.GetComponent<UserStats>();
        }

        SetAfterSentenceButtonActive(false);

        // --- Day System Logic ---
        int currentDay = 1;
        if (DayManager.Instance != null)
        {
            currentDay = DayManager.Instance.currentDay;
        }

        _currentTactics.Clear();

        foreach (DayConfig config in dayConfigs)
        {
            foreach (TacticSO tactic in config.tacticsForThisDay)
            {
                if (tactic != null && !_currentTactics.Contains(tactic))
                {
                    _currentTactics.Add(tactic);
                }
            }
        }

        PopulateGrid();
    }

    public bool IsTacticUnlocked(TacticSO tactic)
    {
        if (tactic == null)
            return false;

        if (!tactic.enabledFlag)
            return false;

        int currentDay = 1;
        if (DayManager.Instance != null)
        {
            currentDay = DayManager.Instance.currentDay;
        }

        bool dayUnlocked = false;

        foreach (DayConfig config in dayConfigs)
        {
            if (config == null || config.tacticsForThisDay == null)
                continue;

            if (config.tacticsForThisDay.Contains(tactic) && config.dayNumber <= currentDay)
            {
                dayUnlocked = true;
                break;
            }
        }

        bool levelUnlocked = true;

        if (_userStats != null)
        {
            levelUnlocked = tactic.level <= _userStats.Level;
        }

        return dayUnlocked && levelUnlocked;
    }

    private void SetCardClickable(GameObject cardObject, bool canClick)
    {
        if (cardObject == null)
            return;

        Button button = cardObject.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = canClick;
        }

        CanvasGroup canvasGroup = cardObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = cardObject.AddComponent<CanvasGroup>();
        }

        if (canClick)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else
        {
            canvasGroup.alpha = 0.45f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void SetAfterSentenceButtonActive(bool active)
    {
        if (_afterSentenceButton == null)
            return;

        if (_hideAfterSentenceButtonObject)
        {
            _afterSentenceButton.gameObject.SetActive(active);
        }
        else
        {
            _afterSentenceButton.interactable = active;
        }
    }

    public void OpenCardView()
    {
        if (TacticPanel != null) TacticPanel.SetActive(true);
    }

    public void CloseCardView()
    {
        if (TacticPanel != null) TacticPanel.SetActive(false);
    }

    public void CloseCardViewWithoutSelection()
    {
        if (TacticPanel != null) TacticPanel.SetActive(false);
        _currentlySelectedCard = null;
    }

    public void PopulateGrid()
    {
        foreach (Transform child in _gridParent)
        {
            Destroy(child.gameObject);
        }

        foreach (TacticSO tacticData in _currentTactics)
        {
            if (tacticData == null)
                continue;

            GameObject newCardObj = Instantiate(_cardPrefab, _gridParent);
            Card cardComponent = newCardObj.GetComponent<Card>();
            cardComponent.Setup(tacticData, this);

            bool canClick = IsTacticUnlocked(tacticData);
            SetCardClickable(newCardObj, canClick);
        }
    }

    public void OnCardSelected(Card card)
    {
        if (card == null)
            return;

        if (!IsTacticUnlocked(card.TacticData))
        {
            UnityEngine.Debug.Log("This tactic card is locked and cannot be selected.");
            return;
        }

        if (_currentlySelectedCard != null)
        {
            _currentlySelectedCard.Deselect();
        }

        if (_currentlySelectedCard == card)
        {
            _currentlySelectedCard.Deselect();
            _currentlySelectedCard = null;

            if (tacticHintPanel != null)
            {
                tacticHintPanel.ClearSelectedCard();
            }

            UnityEngine.Debug.Log("Card deselected.");
        }
        else
        {
            _currentlySelectedCard = card;
            _currentlySelectedCard.Select();

            if (tacticHintPanel != null)
            {
                tacticHintPanel.SetSelectedCard(_currentlySelectedCard);
            }
        }
    }

    private IEnumerator SpeakAndShowComments(TacticSO selectedTactic, string postTextToShow)
    {
        if (selectedTactic == null) yield break;

        float delay = postTextToShow.Split(' ').Length / _wordsPerSec;
        yield return new WaitForSeconds(delay);

        UpdateScores(selectedTactic);

        if (_commentManager != null)
        {
            StartCoroutine(_commentManager.DisplayCommentsRoutine(selectedTactic.type, 2f));
        }

        yield return new WaitForSeconds(3f);

        if (GlobalStatManager.Instance != null && _userStats != null)
        {
            GlobalStatManager.Instance.SaveToDisk(
                _userStats.Cash,
                _userStats.FollowerCount,
                _userStats.Credibility
            );
        }
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
        SetAfterSentenceButtonActive(false);
    }

    private IEnumerator DisplayTextCC(string fullText, int requestId)
    {
        if (_contentBox == null) yield break;

        SetAfterSentenceButtonActive(false);

        _contentBox.text = "";
        string[] words = fullText.Split(' ');
        float delay = 1.0f / _wordsPerSec;

        foreach (string word in words)
        {
            if (requestId != _displayRequestId) yield break;

            _contentBox.text += word + " ";

            if (_scrollRect != null)
            {
                _isAtBottom = _scrollRect.verticalNormalizedPosition <= 0.1f;
            }

            yield return new WaitForSeconds(delay);
        }

        if (requestId == _displayRequestId)
        {
            SetAfterSentenceButtonActive(true);
        }
    }

    public void OnPublishButtonClicked()
    {
        if (_currentlySelectedCard == null)
        {
            UnityEngine.Debug.LogWarning("No card selected to publish!");
            return;
        }

        if (!IsTacticUnlocked(_currentlySelectedCard.TacticData))
        {
            UnityEngine.Debug.LogWarning("Selected card is locked and cannot be published.");
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
            TacticPanel.SetActive(false);
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

        if (_contentBox != null) _contentBox.text = "";

        if (_scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 1f;
        }

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
        SetAfterSentenceButtonActive(false);
    }

    // Hook this method up to the "Confirm Tactic" UI Button
    public void ConfirmTacticSelection()
    {
        if (_currentlySelectedCard == null)
        {
            UnityEngine.Debug.LogWarning("No card selected!");
            return;
        }

        if (!IsTacticUnlocked(_currentlySelectedCard.TacticData))
        {
            UnityEngine.Debug.LogWarning("Selected card is locked.");
            return;
        }

        string tacticType = _currentlySelectedCard.TacticData.type;

        if (TacticPanel != null)
        {
            TacticPanel.SetActive(false);
        }

        if (dailyPostFillBlankManager != null)
        {
            dailyPostFillBlankManager.OpenSentenceSelectionPanel(tacticType);
        }
    }

    public void ShowDailyPostCompletedSentenceOnly(string completedSentence)
    {
        if (string.IsNullOrWhiteSpace(completedSentence)) return;

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

        if (_contentBox != null) _contentBox.text = "";

        if (_scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 1f;
        }

        if (_commentManager != null) _commentManager.ClearComments();

        _displayTextRoutine = StartCoroutine(DisplayTextCC(completedSentence, _displayRequestId));
    }

    void UpdateScores(TacticSO tactic)
    {
        if (_userStats == null) return;

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