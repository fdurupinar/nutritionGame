using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;
using UnityEngine.Events;

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

    // Auto-scroll state flag
    private bool _isAutoScrolling;

    UserStats _userStats;

    [SerializeField] private float _wordsPerSec = 4f;

    private Coroutine _displayTextRoutine;
    private Coroutine _speakAndCommentRoutine;
    private int _displayRequestId = 0;

    [Header("Daily Post Fill Blank")]
    public DailyPostFillBlankManager dailyPostFillBlankManager;

    [Header("Hint Panel")]
    public TacticHintPanel tacticHintPanel;

    [Header("Metric Formula / Cat Coach")]
    [Tooltip("If enabled, uses the new misinformation metric formula. If disabled, uses the old simple TacticSO scoring.")]
    public bool useMisinformationMetricFormula = true;

    [Tooltip("Drag in MisinformationMetricEngine. It calculates Money, Followers, and Credibility. If empty, the script will try to find one automatically.")]
    public MisinformationMetricEngine metricEngine;

    [Tooltip("Drag in CatCoachManager. The requested flow uses it before tactic selection. Final-publish checking is optional and disabled by default.")]
    public CatCoachManager catCoachManager;

    [Tooltip("Leave OFF for the requested flow. If enabled, the coach can warn again after a tactic is selected and the final Post button is clicked.")]
    public bool enableFinalPublishCoachWarning = false;

    [Header("After Final Post")]
    [Tooltip("Optional. Saved only after the final Post button successfully applies the metric result.")]
    public PublishFlagSaver publishFlagSaver;

    [Tooltip("Optional. Activated only after the final Post button successfully applies the metric result.")]
    public GameObject socialFeedPanel;

    [Tooltip("Optional extra actions invoked after the metric result is applied and the daily post flag is saved.")]
    public UnityEvent onPostCompleted;

    [Header("Post Animation > Metric Animation > Home Button")]
    [Tooltip("帖子文字播放完后，等待多久才开始显示评论。")]
    [SerializeField] private float _commentStartDelay = 2f;

    [Tooltip("全部评论完成后，等待多久再开始更新指标。")]
    [SerializeField] private float _delayBeforeMetricAnimation = 0.20f;

    [Tooltip("指标动画完成后，等待多久再弹出Home按钮。")]
    [SerializeField] private float _delayBeforeHomeButton = 0.15f;

    [Tooltip("开启后，Home按钮会用缩放回弹动画出现。")]
    [SerializeField] private bool _animateHomeButtonPop = true;

    [Tooltip("Home按钮弹出动画时间。")]
    [SerializeField] private float _homeButtonPopDuration = 0.28f;

    [Range(0.01f, 1f)]
    [Tooltip("Home按钮弹出开始时的缩放。")]
    [SerializeField] private float _homeButtonStartScale = 0.15f;

    [Range(1f, 1.5f)]
    [Tooltip("Home按钮回弹时的最大放大倍率。")]
    [SerializeField] private float _homeButtonOvershootScale = 1.12f;

    [Tooltip("开启后，即使Time.timeScale为0，Home按钮动画也能播放。")]
    [SerializeField] private bool _useUnscaledHomeButtonAnimation = true;

    private bool _skipCatCoachWarningOnce;
    private bool _publishSequenceRunning;
    private Coroutine _publishSequenceRoutine;
    private Vector3 _afterSentenceButtonOriginalScale = Vector3.one;

    void Start()
    {
        _allTactics = new List<TacticSO>();
        _currentTactics = new List<TacticSO>();

        _allTactics = Resources.LoadAll<TacticSO>("Content/Tactics").ToList();

        if (_contentBox != null)
        {
            _scrollRect = _contentBox.GetComponentInParent<ScrollRect>();
        }

        _commentManager = FindFirstObjectByType<CommentManager>();

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            _userStats = playerObj.GetComponent<UserStats>();
        }

        if (_userStats == null)
        {
            _userStats = FindFirstObjectByType<UserStats>();
        }

        if (metricEngine == null)
        {
            metricEngine = FindObjectOfType<MisinformationMetricEngine>();
        }

        if (catCoachManager == null)
        {
            catCoachManager = FindObjectOfType<CatCoachManager>();
        }

        if (publishFlagSaver == null)
        {
            publishFlagSaver = FindObjectOfType<PublishFlagSaver>(true);
        }

        if (_afterSentenceButton != null)
        {
            _afterSentenceButtonOriginalScale = _afterSentenceButton.transform.localScale;
        }

        SetAfterSentenceButtonActive(false);

        // Day system logic
        int currentDay = 1;
        if (DayManager.Instance != null)
        {
            currentDay = DayManager.Instance.currentDay;
        }

        _currentTactics.Clear();

        if (dayConfigs != null)
        {
            foreach (DayConfig config in dayConfigs)
            {
                if (config == null || config.tacticsForThisDay == null)
                {
                    continue;
                }

                foreach (TacticSO tactic in config.tacticsForThisDay)
                {
                    if (tactic != null && !_currentTactics.Contains(tactic))
                    {
                        _currentTactics.Add(tactic);
                    }
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

        if (!active)
        {
            _afterSentenceButton.transform.localScale = _afterSentenceButtonOriginalScale;
        }

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
        if (selectedTactic == null)
        {
            yield break;
        }

        // 中文备注：帖子文字已经在前一个阶段完整播放，所以这里仅等待并播放评论。
        if (_commentStartDelay > 0f)
        {
            yield return new WaitForSeconds(_commentStartDelay);
        }

        if (_commentManager != null)
        {
            // 中文备注：直接等待评论协程结束，确保指标动画一定在所有帖子动画完成后才开始。
            yield return StartCoroutine(_commentManager.DisplayCommentsRoutine(selectedTactic.type, 0f));
        }
    }

    private void StopTacticPublishRoutines()
    {
        _displayRequestId++;

        if (_publishSequenceRoutine != null)
        {
            StopCoroutine(_publishSequenceRoutine);
            _publishSequenceRoutine = null;
        }

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

        _publishSequenceRunning = false;
        _isAutoScrolling = false;
        SetAfterSentenceButtonActive(false);
    }

    private IEnumerator DisplayTextCC(string fullText, int requestId)
    {
        if (_contentBox == null) yield break;

        SetAfterSentenceButtonActive(false);

        _contentBox.text = "";
        string[] words = fullText.Split(' ');
        float delay = 1.0f / Mathf.Max(0.01f, _wordsPerSec);

        _isAutoScrolling = true;

        foreach (string word in words)
        {
            if (requestId != _displayRequestId) yield break;

            _contentBox.text += word + " ";

            if (_scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
            }

            yield return new WaitForSeconds(delay);
        }

        // 中文备注：Home按钮不再在文字动画结束时出现。
        // 它会等待评论动画和指标动画全部结束后再弹出。
        yield return new WaitForSeconds(0.5f);
        _isAutoScrolling = false;
    }

    public void OnPublishButtonClicked()
    {
        if (_publishSequenceRunning)
        {
            Debug.LogWarning("TacticManager: A post is already being published.");
            return;
        }

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

        if (_userStats == null)
        {
            Debug.LogError("TacticManager: UserStats was not found. Publish cancelled.");
            return;
        }

        TacticSO selectedTactic = _currentlySelectedCard.TacticData;
        string postTextToShow = selectedTactic.text;

        if (dailyPostFillBlankManager != null &&
            !string.IsNullOrWhiteSpace(dailyPostFillBlankManager.currentCompletedSentence))
        {
            postTextToShow = dailyPostFillBlankManager.currentCompletedSentence;
        }

        if (enableFinalPublishCoachWarning &&
            !_skipCatCoachWarningOnce &&
            useMisinformationMetricFormula &&
            metricEngine != null &&
            catCoachManager != null)
        {
            PostMetricPreview coachPreview = metricEngine.PreviewPost(selectedTactic, dailyPostFillBlankManager, _userStats);

            if (catCoachManager.TryShowEasyModeWarning(coachPreview, this))
            {
                return;
            }
        }

        _skipCatCoachWarningOnce = false;

        // 中文备注：这里只预先计算结果，不更新指标。指标会等帖子和评论动画结束后才开始滚动。
        PostMetricPreview preparedPreview = PrepareMetricPreview(selectedTactic);
        if (preparedPreview == null)
        {
            Debug.LogError("TacticManager: Could not prepare the metric result. Publish cancelled.");
            return;
        }

        StopTacticPublishRoutines();
        _publishSequenceRunning = true;

        if (TacticPanel != null)
        {
            TacticPanel.SetActive(false);
        }

        if (socialFeedPanel != null)
        {
            // 中文备注：先显示Social Feed，让帖子动画可以播放；今日发布状态会在指标成功应用后保存。
            socialFeedPanel.SetActive(true);
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
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        if (_commentManager != null)
        {
            _commentManager.ClearComments();
        }

        Card publishedCard = _currentlySelectedCard;
        publishedCard.Deselect();
        _currentlySelectedCard = null;

        _publishSequenceRoutine = StartCoroutine(PublishSequenceRoutine(
            selectedTactic,
            publishedCard,
            postTextToShow,
            preparedPreview));
    }

    private PostMetricPreview PrepareMetricPreview(TacticSO selectedTactic)
    {
        if (useMisinformationMetricFormula)
        {
            if (metricEngine == null)
            {
                Debug.LogError("TacticManager: MetricEngine is missing.");
                return null;
            }

            return metricEngine.PreviewPost(selectedTactic, dailyPostFillBlankManager, _userStats);
        }

        // 中文备注：旧公式回退模式也先保存结果，等帖子动画结束后再播放指标动画。
        return new PostMetricPreview
        {
            currentMoney = _userStats.Cash,
            currentFollowers = _userStats.FollowerCount,
            currentCredibility = _userStats.Credibility,
            currentLikes = _userStats.Likes,
            moneyDelta = (int)(selectedTactic.engagementBonus * 100),
            followersDelta = (int)(selectedTactic.engagementBonus * 1000),
            likesDelta = (int)(selectedTactic.engagementBonus * 50),
            credibilityDelta = -(int)(selectedTactic.credibilityCost * 100)
        };
    }

    private IEnumerator PublishSequenceRoutine(
        TacticSO selectedTactic,
        Card publishedCard,
        string postTextToShow,
        PostMetricPreview preparedPreview)
    {
        int requestId = _displayRequestId;

        // 1. 帖子文字动画
        _displayTextRoutine = StartCoroutine(DisplayTextCC(postTextToShow, requestId));
        yield return _displayTextRoutine;
        _displayTextRoutine = null;

        // 2. 评论动画
        _speakAndCommentRoutine = StartCoroutine(SpeakAndShowComments(selectedTactic, postTextToShow));
        yield return _speakAndCommentRoutine;
        _speakAndCommentRoutine = null;

        if (_delayBeforeMetricAnimation > 0f)
        {
            yield return new WaitForSeconds(_delayBeforeMetricAnimation);
        }

        // 3. 所有帖子动画结束后，才开始更新指标。
        if (!ApplyPreparedMetricResult(preparedPreview))
        {
            Debug.LogError("TacticManager: Prepared metric result could not be applied after the post animation.");
            RecoverFromFailedPublish(publishedCard);
            yield break;
        }

        // 4. 等待数字滚动、弹跳和重复音效全部结束。
        if (_userStats != null)
        {
            while (_userStats.IsMetricAnimationRunning)
            {
                yield return null;
            }
        }

        // 5. 指标成功后才保存今日发布、消耗战术卡并触发完成事件。
        CompleteSuccessfulPublish();
        _currentTactics.Remove(selectedTactic);
        PopulateGrid();

        if (_delayBeforeHomeButton > 0f)
        {
            yield return WaitForSecondsSmart(_delayBeforeHomeButton);
        }

        // 6. 所有动画完成后，Home按钮最后弹出。
        yield return ShowAfterSentenceButtonPopRoutine();

        // 中文备注：额外完成事件放在最后，避免事件关闭物体后打断Home按钮动画。
        if (onPostCompleted != null)
        {
            onPostCompleted.Invoke();
        }

        _publishSequenceRoutine = null;
        _publishSequenceRunning = false;
    }

    private bool ApplyPreparedMetricResult(PostMetricPreview preparedPreview)
    {
        if (preparedPreview == null || _userStats == null)
        {
            return false;
        }

        if (useMisinformationMetricFormula)
        {
            return metricEngine != null &&
                   metricEngine.ApplyPreparedPostResult(preparedPreview, _userStats, true) != null;
        }

        int targetMoney = preparedPreview.currentMoney + preparedPreview.moneyDelta;
        int targetFollowers = preparedPreview.currentFollowers + preparedPreview.followersDelta;
        int targetCredibility = preparedPreview.currentCredibility + preparedPreview.credibilityDelta;
        int targetLikes = Mathf.Max(0, preparedPreview.currentLikes + preparedPreview.likesDelta);

        _userStats.AnimateMetricsTo(targetMoney, targetFollowers, targetCredibility, targetLikes);

        if (GlobalStatManager.Instance != null)
        {
            GlobalStatManager.Instance.SaveToDisk(targetMoney, targetFollowers, targetCredibility);
        }

        return true;
    }

    private void RecoverFromFailedPublish(Card publishedCard)
    {
        _publishSequenceRoutine = null;
        _publishSequenceRunning = false;

        if (socialFeedPanel != null)
        {
            socialFeedPanel.SetActive(false);
        }

        if (TacticPanel != null)
        {
            TacticPanel.SetActive(true);
        }

        _currentlySelectedCard = publishedCard;
        if (_currentlySelectedCard != null)
        {
            _currentlySelectedCard.Select();
        }
    }

    private IEnumerator ShowAfterSentenceButtonPopRoutine()
    {
        if (_afterSentenceButton == null)
        {
            yield break;
        }

        SetAfterSentenceButtonActive(true);

        if (!_animateHomeButtonPop || _homeButtonPopDuration <= 0f)
        {
            _afterSentenceButton.transform.localScale = _afterSentenceButtonOriginalScale;
            yield break;
        }

        float duration = Mathf.Max(0.01f, _homeButtonPopDuration);
        float elapsed = 0f;
        Vector3 startScale = _afterSentenceButtonOriginalScale * _homeButtonStartScale;
        Vector3 overshootScale = _afterSentenceButtonOriginalScale * _homeButtonOvershootScale;
        Vector3 endScale = _afterSentenceButtonOriginalScale;

        _afterSentenceButton.transform.localScale = startScale;

        while (elapsed < duration)
        {
            float deltaTime = _useUnscaledHomeButtonAnimation ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);

            if (normalized < 0.72f)
            {
                float firstPart = Mathf.Clamp01(normalized / 0.72f);
                float eased = 1f - Mathf.Pow(1f - firstPart, 3f);
                _afterSentenceButton.transform.localScale = Vector3.LerpUnclamped(startScale, overshootScale, eased);
            }
            else
            {
                float secondPart = Mathf.Clamp01((normalized - 0.72f) / 0.28f);
                float eased = 1f - Mathf.Pow(1f - secondPart, 3f);
                _afterSentenceButton.transform.localScale = Vector3.LerpUnclamped(overshootScale, endScale, eased);
            }

            yield return null;
        }

        _afterSentenceButton.transform.localScale = endScale;
    }

    private IEnumerator WaitForSecondsSmart(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += _useUnscaledHomeButtonAnimation ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    private void CompleteSuccessfulPublish()
    {
        if (publishFlagSaver != null)
        {
            publishFlagSaver.SaveThatWePostedToday();
        }
        else
        {
            Debug.LogWarning("TacticManager: PublishFlagSaver is not assigned. The Daily Post button may remain available today.");
        }

        if (socialFeedPanel != null)
        {
            socialFeedPanel.SetActive(true);
        }

    }

    public void ContinuePublishAfterCatCoachWarning()
    {
        _skipCatCoachWarningOnce = true;
        OnPublishButtonClicked();
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

        _isAutoScrolling = false;
        SetAfterSentenceButtonActive(false);
    }

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

        // Set initial position to 0f so the caption starts at the bottom.
        if (_scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        if (_commentManager != null) _commentManager.ClearComments();

        _displayTextRoutine = StartCoroutine(DisplaySentenceOnlyAndShowButtonRoutine(completedSentence, _displayRequestId));
    }

    private IEnumerator DisplaySentenceOnlyAndShowButtonRoutine(string completedSentence, int requestId)
    {
        yield return StartCoroutine(DisplayTextCC(completedSentence, requestId));
        _displayTextRoutine = null;

        if (requestId == _displayRequestId)
        {
            yield return ShowAfterSentenceButtonPopRoutine();
        }
    }

    private bool UpdateScores(TacticSO tactic)
    {
        if (_userStats == null)
        {
            Debug.LogError("TacticManager: UserStats was not found. Make sure the player has UserStats and preferably the Player tag.");
            return false;
        }

        if (useMisinformationMetricFormula)
        {
            if (metricEngine == null)
            {
                Debug.LogError("TacticManager: Use Misinformation Metric Formula is enabled, but MetricEngine is missing.");
                return false;
            }

            return metricEngine.ApplyPostResult(tactic, dailyPostFillBlankManager, _userStats) != null;
        }

        // Old formula is kept only as an intentional fallback when the new formula toggle is disabled.
        _userStats.Cash += (int)(tactic.engagementBonus * 100);
        _userStats.FollowerCount += (int)(tactic.engagementBonus * 1000);
        _userStats.Likes += (int)(tactic.engagementBonus * 50);
        _userStats.Credibility -= (int)(tactic.credibilityCost * 100);
        return true;
    }

    void LateUpdate()
    {
        // Target 1f so the caption auto-scrolls upward smoothly.
        if (_isAutoScrolling && _scrollRect != null)
        {
            _scrollRect.verticalNormalizedPosition = Mathf.Lerp(_scrollRect.verticalNormalizedPosition, 1f, Time.deltaTime * 10f);
        }
    }
}