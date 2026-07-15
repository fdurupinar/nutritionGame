using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DailyPostFillBlankManager : MonoBehaviour
{
    [Header("Manager References")]
    [Tooltip("Tactic manager opened after the caption passes the Easy Mode coach check.")]
    public TacticManager tacticManager;

    [Tooltip("Cat coach used to warn about wrong or nonsensical blank choices before tactic selection.")]
    public CatCoachManager catCoachManager;

    [Tooltip("Metric engine used to evaluate ordered blank choices for the Easy Mode coach.")]
    public MisinformationMetricEngine metricEngine;

    [Header("Easy Mode Coach Flow")]
    [Tooltip("If enabled, the completed caption is checked by the cat coach before tactic selection.")]
    public bool checkCaptionWithCoachBeforeTacticSelection = true;

    [Tooltip("If enabled, the fill-in-the-blank panel is hidden while the coach warning is open. Keep the coach panel outside the fill-in-the-blank panel hierarchy.")]
    public bool hideFillBlankPanelWhileCoachIsOpen = true;

    [Tooltip("Legacy compatibility only. Leave OFF. Turning this on invokes the old On Next UnityEvent after tactic selection opens.")]
    public bool invokeLegacyOnNextEvent = false;

    [Header("Panels")]
    public GameObject topicPanel;
    public GameObject subTopicPanel;
    public GameObject sentenceSelectionPanel;
    public GameObject fillBlankPanel;

    [Header("JSON Data")]
    public TextAsset dailyPostJsonFile;

    private DailyPostJsonDatabase dailyPostData = new DailyPostJsonDatabase();

    [Header("Selection Flow")]
    [Tooltip("ON = Topic > Subtopic > Caption > Fill Blank. This is the recommended new flow. OFF = Topic > Subtopic > Tactic > Caption, using the old tactic selection flow.")]
    public bool selectCaptionImmediatelyAfterSubTopic = true;

    [Header("Topic Buttons")]
    public List<TopicButtonBinding> topicButtons = new List<TopicButtonBinding>();

    [Header("Sub-Topic UI")]
    public TextMeshProUGUI subTopicTitleText;
    public Transform subTopicButtonParent;
    public Button subTopicButtonPrefab;

    [Header("Sentence Selection UI")]
    public Transform sentenceButtonParent;
    public Button sentenceButtonPrefab;

    [Header("Fill Blank UI")]
    public TextMeshProUGUI sentenceText;

    [Header("Sentence Text Settings")]
    public string emptyBlankText = "_____";
    public bool underlineFilledWords = true;
    public bool useAutoFontSize = true;
    public float sentenceFontSizeMin = 22f;
    public float sentenceFontSizeMax = 34f;
    public TextAlignmentOptions sentenceAlignment = TextAlignmentOptions.TopLeft;

    [Header("Word Choice UI")]
    public Transform wordChoiceParent;
    public Button wordChoiceButtonPrefab;
    public int maxWordChoicesToShow = 12;
    public bool shuffleWordChoices = true;
    public bool hideWordChoiceAfterUse = true;

    [Header("Optional Buttons")]
    public Button undoLastButton;
    public Button resetBlanksButton;

    [Header("Next / Publish")]
    public Button nextButton;
    public UnityEvent onNext;

    [Header("Start Settings")]
    public bool openTopicPanelOnStart = true;

    [Header("Runtime Info")]
    [HideInInspector] public string currentTopicId;
    [HideInInspector] public int currentSubTopicIndex = -1;
    [HideInInspector] public string currentTacticType;

    [HideInInspector] public DailyPostTopicJson currentTopicData;
    [HideInInspector] public DailyPostSubTopicJson currentSubTopicData;
    [HideInInspector] public DailyPostSentenceJson currentSentenceData;

    public string currentCompletedSentence;

    [HideInInspector] public List<string> currentBlankValues = new List<string>();
    [HideInInspector] public List<WordChoiceRuntime> activeWordChoices = new List<WordChoiceRuntime>();

    private void Start()
    {
        LoadJsonData();
        SetupTopicButtons();

        // 中文备注：如果 Inspector 没有手动拖入引用，就自动查找，避免因为漏拖引用导致流程中断。
        if (tacticManager == null)
        {
            tacticManager = FindObjectOfType<TacticManager>();
        }

        if (catCoachManager == null)
        {
            catCoachManager = FindObjectOfType<CatCoachManager>();
        }

        if (metricEngine == null)
        {
            metricEngine = FindObjectOfType<MisinformationMetricEngine>();
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(GoNext);
            nextButton.interactable = false;
        }

        if (undoLastButton != null)
        {
            undoLastButton.onClick.RemoveAllListeners();
            undoLastButton.onClick.AddListener(ClearLastFilledBlank);
        }

        if (resetBlanksButton != null)
        {
            resetBlanksButton.onClick.RemoveAllListeners();
            resetBlanksButton.onClick.AddListener(ResetCurrentBlanks);
        }

        if (openTopicPanelOnStart)
        {
            OpenTopicPanel();
        }
    }

    public void LoadJsonData()
    {
        if (dailyPostJsonFile == null)
        {
            Debug.LogWarning("DailyPostFillBlankManager: No JSON file assigned.");
            dailyPostData = new DailyPostJsonDatabase();
            return;
        }

        try
        {
            dailyPostData = JsonUtility.FromJson<DailyPostJsonDatabase>(dailyPostJsonFile.text);
        }
        catch (Exception e)
        {
            Debug.LogError("DailyPostFillBlankManager: JSON parse failed. " + e.Message);
            dailyPostData = new DailyPostJsonDatabase();
        }

        if (dailyPostData == null)
            dailyPostData = new DailyPostJsonDatabase();

        if (dailyPostData.topics == null)
            dailyPostData.topics = new List<DailyPostTopicJson>();

        if (dailyPostData.tactics == null)
            dailyPostData.tactics = new List<DailyPostTacticJson>();

        NormalizeRuntimeDatabase();
    }

    private void NormalizeRuntimeDatabase()
    {
        if (dailyPostData == null) return;
        if (dailyPostData.topics == null) dailyPostData.topics = new List<DailyPostTopicJson>();

        for (int i = 0; i < dailyPostData.topics.Count; i++)
        {
            DailyPostTopicJson topic = dailyPostData.topics[i];
            if (topic == null) continue;
            if (topic.subTopics == null) topic.subTopics = new List<DailyPostSubTopicJson>();

            for (int s = 0; s < topic.subTopics.Count; s++)
            {
                DailyPostSubTopicJson subTopic = topic.subTopics[s];
                if (subTopic == null) continue;
                if (subTopic.sentences == null) subTopic.sentences = new List<DailyPostSentenceJson>();

                for (int c = 0; c < subTopic.sentences.Count; c++)
                {
                    NormalizeSentence(subTopic.sentences[c]);
                }
            }
        }

        if (dailyPostData.tactics != null)
        {
            for (int t = 0; t < dailyPostData.tactics.Count; t++)
            {
                DailyPostTacticJson tactic = dailyPostData.tactics[t];
                if (tactic == null) continue;
                if (tactic.sentences == null) tactic.sentences = new List<DailyPostSentenceJson>();

                for (int c = 0; c < tactic.sentences.Count; c++)
                {
                    NormalizeSentence(tactic.sentences[c]);
                }
            }
        }
    }

    private void NormalizeSentence(DailyPostSentenceJson sentence)
    {
        if (sentence == null) return;
        if (sentence.blankWords == null) sentence.blankWords = new List<string>();
        if (sentence.wordChoices == null) sentence.wordChoices = new List<string>();
        if (sentence.blankScoring == null) sentence.blankScoring = new List<DailyPostBlankScoringJson>();
        if (sentence.idealTacticTypes == null) sentence.idealTacticTypes = new List<string>();
        if (sentence.neutralTacticTypes == null) sentence.neutralTacticTypes = new List<string>();
        if (sentence.badTacticTypes == null) sentence.badTacticTypes = new List<string>();
        if (sentence.correctWords == null) sentence.correctWords = new List<string>();
        if (sentence.halfCorrectWords == null) sentence.halfCorrectWords = new List<string>();
        if (sentence.neutralWords == null) sentence.neutralWords = new List<string>();
        if (sentence.wrongWords == null) sentence.wrongWords = new List<string>();
        if (sentence.nonsenseWords == null) sentence.nonsenseWords = new List<string>();

        while (sentence.blankScoring.Count < sentence.blankWords.Count)
        {
            int newIndex = sentence.blankScoring.Count;
            sentence.blankScoring.Add(new DailyPostBlankScoringJson
            {
                note = "Blank " + (newIndex + 1),
                correctWords = new List<string> { sentence.blankWords[newIndex] },
                halfCorrectWords = new List<string>(),
                neutralWords = new List<string>(),
                wrongWords = new List<string>(),
                nonsenseWords = new List<string>()
            });
        }

        while (sentence.blankScoring.Count > sentence.blankWords.Count)
        {
            sentence.blankScoring.RemoveAt(sentence.blankScoring.Count - 1);
        }

        for (int i = 0; i < sentence.blankScoring.Count; i++)
        {
            DailyPostBlankScoringJson rule = sentence.blankScoring[i];
            if (rule == null)
            {
                rule = new DailyPostBlankScoringJson();
                sentence.blankScoring[i] = rule;
            }

            if (string.IsNullOrWhiteSpace(rule.note)) rule.note = "Blank " + (i + 1);
            if (rule.correctWords == null) rule.correctWords = new List<string>();
            if (rule.halfCorrectWords == null) rule.halfCorrectWords = new List<string>();
            if (rule.neutralWords == null) rule.neutralWords = new List<string>();
            if (rule.wrongWords == null) rule.wrongWords = new List<string>();
            if (rule.nonsenseWords == null) rule.nonsenseWords = new List<string>();
        }
    }

    public void OpenTopicPanel()
    {
        LoadJsonData();
        SetupTopicButtons();
        ShowOnlyPanel(topicPanel);
    }

    public void SetupTopicButtons()
    {
        int currentDay = 1;

        if (DayManager.Instance != null)
        {
            currentDay = DayManager.Instance.currentDay;
        }

        for (int i = 0; i < topicButtons.Count; i++)
        {
            TopicButtonBinding binding = topicButtons[i];

            if (binding.topicButton == null) continue;

            bool unlocked = currentDay >= binding.unlockDay;

            binding.topicButton.interactable = unlocked;
            binding.topicButton.onClick.RemoveAllListeners();

            if (unlocked)
            {
                string capturedTopicId = binding.topicId;
                binding.topicButton.onClick.AddListener(() => OpenSubTopicPanel(capturedTopicId));
            }
        }
    }

    public void OpenSubTopicPanel(string topicId)
    {
        currentTopicId = topicId;
        currentSubTopicIndex = -1;
        currentSubTopicData = null;
        currentSentenceData = null;
        currentCompletedSentence = "";

        currentTopicData = GetTopicDataById(topicId);

        if (currentTopicData == null)
        {
            Debug.LogWarning("DailyPostFillBlankManager: Cannot find topicId in JSON: " + topicId);
            return;
        }

        ShowOnlyPanel(subTopicPanel);

        if (subTopicTitleText != null)
        {
            subTopicTitleText.text = "Choose a Sub-Topic";
        }

        ClearChildren(subTopicButtonParent);

        if (currentTopicData.subTopics == null)
            currentTopicData.subTopics = new List<DailyPostSubTopicJson>();

        for (int i = 0; i < currentTopicData.subTopics.Count; i++)
        {
            DailyPostSubTopicJson subTopic = currentTopicData.subTopics[i];

            Button button = Instantiate(subTopicButtonPrefab, subTopicButtonParent);

            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = subTopic.subTopicName;
            }

            int capturedSubTopicIndex = i;
            button.onClick.RemoveAllListeners();

            if (selectCaptionImmediatelyAfterSubTopic)
            {
                button.onClick.AddListener(() => OpenSentenceSelectionForSubTopic(capturedSubTopicIndex));
            }
            else
            {
                button.onClick.AddListener(() => GoToTacticSelection(capturedSubTopicIndex));
            }
        }
    }

    public void OpenSentenceSelectionForSubTopic(int subTopicIndex)
    {
        if (currentTopicData == null || currentTopicData.subTopics == null)
        {
            Debug.LogWarning("DailyPostFillBlankManager: No topic selected before opening captions.");
            return;
        }

        if (subTopicIndex < 0 || subTopicIndex >= currentTopicData.subTopics.Count)
        {
            Debug.LogWarning("DailyPostFillBlankManager: Invalid subtopic index: " + subTopicIndex);
            return;
        }

        currentSubTopicIndex = subTopicIndex;
        currentSubTopicData = currentTopicData.subTopics[subTopicIndex];
        currentSentenceData = null;
        currentCompletedSentence = "";

        OpenSentenceSelectionForCurrentSubTopic();
    }

    public void GoToTacticSelection(int subTopicIndex)
    {
        currentSubTopicIndex = subTopicIndex;
        currentSubTopicData = currentTopicData.subTopics[subTopicIndex];

        ShowOnlyPanel(null);

        if (tacticManager != null)
        {
            tacticManager.OpenCardView();
        }
    }

    // Backward compatible method. TacticManager can still call this after a tactic card is confirmed.
    // New JSON stores captions under the currently selected subtopic, so tacticType is saved for scoring but does not decide which captions appear.
    public void OpenSentenceSelectionPanel(string tacticType)
    {
        currentTacticType = string.IsNullOrWhiteSpace(tacticType) ? "" : tacticType.ToLowerInvariant();
        OpenSentenceSelectionForCurrentSubTopic();
    }

    public void OpenSentenceSelectionForCurrentSubTopic()
    {
        if (currentSubTopicData == null)
        {
            Debug.LogWarning("DailyPostFillBlankManager: No subtopic selected before opening caption selection.");
            return;
        }

        List<DailyPostSentenceJson> sentenceList = currentSubTopicData.sentences;

        // Legacy fallback: if this subtopic has no captions yet, use old tactic-based captions.
        if ((sentenceList == null || sentenceList.Count == 0) && !string.IsNullOrWhiteSpace(currentTacticType))
        {
            DailyPostTacticJson tacticData = GetTacticDataById(currentTacticType);
            if (tacticData != null)
            {
                sentenceList = tacticData.sentences;
            }
        }

        if (sentenceList == null || sentenceList.Count == 0)
        {
            Debug.LogWarning("DailyPostFillBlankManager: No captions found for subtopic: " + currentSubTopicData.subTopicId);
            return;
        }

        ShowOnlyPanel(sentenceSelectionPanel);
        ClearChildren(sentenceButtonParent);

        for (int i = 0; i < sentenceList.Count; i++)
        {
            DailyPostSentenceJson sentenceData = sentenceList[i];
            Button button = Instantiate(sentenceButtonPrefab, sentenceButtonParent);

            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = BuildPreviewSentence(sentenceData);
            }

            int capturedIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OpenFillBlankPanel(sentenceList, capturedIndex));
        }
    }

    public void OpenFillBlankPanel(List<DailyPostSentenceJson> sentenceList, int sentenceIndex)
    {
        if (sentenceList == null || sentenceIndex < 0 || sentenceIndex >= sentenceList.Count)
        {
            Debug.LogWarning("DailyPostFillBlankManager: Invalid caption index.");
            return;
        }

        currentSentenceData = sentenceList[sentenceIndex];
        currentCompletedSentence = "";

        ShowOnlyPanel(fillBlankPanel);

        BuildFillBlankSentence();
        BuildWordChoices(currentSentenceData);
        UpdateNextButtonState();
    }

    // Legacy signature kept so old references do not break.
    public void OpenFillBlankPanel(DailyPostTacticJson tacticData, int sentenceIndex)
    {
        if (tacticData == null)
        {
            Debug.LogWarning("DailyPostFillBlankManager: tacticData is null.");
            return;
        }

        OpenFillBlankPanel(tacticData.sentences, sentenceIndex);
    }

    public string BuildPreviewSentence(DailyPostSentenceJson sentenceData)
    {
        if (sentenceData == null || string.IsNullOrEmpty(sentenceData.sentence))
            return "";

        string previewText = sentenceData.sentence;

        if (sentenceData.blankWords != null)
        {
            for (int w = 0; w < sentenceData.blankWords.Count; w++)
            {
                previewText = previewText.Replace("[" + sentenceData.blankWords[w] + "]", emptyBlankText);
            }
        }

        return previewText;
    }

    public DailyPostTopicJson GetTopicDataById(string topicId)
    {
        if (dailyPostData == null || dailyPostData.topics == null) return null;

        for (int i = 0; i < dailyPostData.topics.Count; i++)
        {
            if (string.Equals(dailyPostData.topics[i].topicId, topicId, StringComparison.OrdinalIgnoreCase))
                return dailyPostData.topics[i];
        }
        return null;
    }

    public DailyPostTacticJson GetTacticDataById(string tacticId)
    {
        if (dailyPostData == null || dailyPostData.tactics == null) return null;

        for (int i = 0; i < dailyPostData.tactics.Count; i++)
        {
            if (dailyPostData.tactics[i] != null &&
                string.Equals(dailyPostData.tactics[i].tacticId, tacticId, StringComparison.OrdinalIgnoreCase))
                return dailyPostData.tactics[i];
        }
        return null;
    }

    public void BuildFillBlankSentence()
    {
        currentBlankValues.Clear();

        if (currentSentenceData == null || currentSentenceData.blankWords == null)
            return;

        for (int i = 0; i < currentSentenceData.blankWords.Count; i++)
        {
            currentBlankValues.Add("");
        }

        RefreshSentenceText();
    }

    public void RefreshSentenceText()
    {
        if (sentenceText == null || currentSentenceData == null) return;

        sentenceText.text = BuildDisplaySentence();
        sentenceText.enableWordWrapping = true;
        sentenceText.richText = true;
        sentenceText.raycastTarget = false;
        sentenceText.alignment = sentenceAlignment;

        if (useAutoFontSize)
        {
            sentenceText.enableAutoSizing = true;
            sentenceText.fontSizeMin = sentenceFontSizeMin;
            sentenceText.fontSizeMax = sentenceFontSizeMax;
        }
        else
        {
            sentenceText.enableAutoSizing = false;
            sentenceText.fontSize = sentenceFontSizeMax;
        }

        UpdateNextButtonState();
    }

    public string BuildDisplaySentence()
    {
        if (currentSentenceData == null) return "";

        StringBuilder builder = new StringBuilder();
        string sentence = currentSentenceData.sentence;
        int cursor = 0;

        for (int i = 0; i < currentSentenceData.blankWords.Count; i++)
        {
            string originalBlankWord = currentSentenceData.blankWords[i];

            if (string.IsNullOrWhiteSpace(originalBlankWord)) continue;

            int index = sentence.IndexOf("[" + originalBlankWord + "]", cursor, StringComparison.OrdinalIgnoreCase);

            if (index < 0)
            {
                index = sentence.IndexOf(originalBlankWord, cursor, StringComparison.OrdinalIgnoreCase);
                if (index < 0) continue;
            }

            builder.Append(sentence.Substring(cursor, index - cursor));

            string value = "";
            if (i < currentBlankValues.Count) value = currentBlankValues[i];

            if (string.IsNullOrWhiteSpace(value))
            {
                builder.Append(emptyBlankText);
            }
            else
            {
                string safeValue = EscapeRichText(value);
                if (underlineFilledWords)
                {
                    builder.Append("<u>").Append(safeValue).Append("</u>");
                }
                else
                {
                    builder.Append(safeValue);
                }
            }

            int lengthToSkip = sentence.Substring(index).StartsWith("[") ? originalBlankWord.Length + 2 : originalBlankWord.Length;
            cursor = index + lengthToSkip;
        }

        if (cursor < sentence.Length)
        {
            builder.Append(sentence.Substring(cursor));
        }

        return builder.ToString();
    }

    public string BuildCompletedSentence()
    {
        if (currentSentenceData == null) return "";

        StringBuilder builder = new StringBuilder();
        string sentence = currentSentenceData.sentence;
        int cursor = 0;

        for (int i = 0; i < currentSentenceData.blankWords.Count; i++)
        {
            string originalBlankWord = currentSentenceData.blankWords[i];
            if (string.IsNullOrWhiteSpace(originalBlankWord)) continue;

            int index = sentence.IndexOf("[" + originalBlankWord + "]", cursor, StringComparison.OrdinalIgnoreCase);

            if (index < 0)
            {
                index = sentence.IndexOf(originalBlankWord, cursor, StringComparison.OrdinalIgnoreCase);
                if (index < 0) continue;
            }

            builder.Append(sentence.Substring(cursor, index - cursor));

            if (i < currentBlankValues.Count && !string.IsNullOrWhiteSpace(currentBlankValues[i]))
            {
                builder.Append(currentBlankValues[i]);
            }
            else
            {
                builder.Append(emptyBlankText);
            }

            int lengthToSkip = sentence.Substring(index).StartsWith("[") ? originalBlankWord.Length + 2 : originalBlankWord.Length;
            cursor = index + lengthToSkip;
        }

        if (cursor < sentence.Length)
        {
            builder.Append(sentence.Substring(cursor));
        }

        return builder.ToString();
    }

    public void BuildWordChoices(DailyPostSentenceJson sentenceData)
    {
        activeWordChoices.Clear();
        ClearChildren(wordChoiceParent);

        if (sentenceData == null || sentenceData.wordChoices == null) return;

        List<string> finalChoices = new List<string>(sentenceData.wordChoices);

        if (shuffleWordChoices)
        {
            Shuffle(finalChoices);
        }

        for (int i = 0; i < finalChoices.Count && i < maxWordChoicesToShow; i++)
        {
            Button button = Instantiate(wordChoiceButtonPrefab, wordChoiceParent);
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText != null)
                buttonText.text = finalChoices[i];

            string capturedWord = finalChoices[i];
            Button capturedButton = button;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => UseWordChoice(capturedButton, capturedWord));

            WordChoiceRuntime runtime = new WordChoiceRuntime();
            runtime.word = capturedWord;
            runtime.button = button;
            runtime.used = false;

            activeWordChoices.Add(runtime);
        }
    }

    public void UseWordChoice(Button button, string word)
    {
        int blankIndex = GetFirstEmptyBlankIndex();

        if (blankIndex < 0) return;

        currentBlankValues[blankIndex] = word;
        MarkWordChoiceUsed(button, word);
        RefreshSentenceText();
        UpdateNextButtonState();
    }

    public void MarkWordChoiceUsed(Button button, string word)
    {
        for (int i = 0; i < activeWordChoices.Count; i++)
        {
            WordChoiceRuntime choice = activeWordChoices[i];

            if (choice.button == button && string.Equals(choice.word, word, StringComparison.OrdinalIgnoreCase))
            {
                choice.used = true;

                if (hideWordChoiceAfterUse)
                    choice.button.gameObject.SetActive(false);
                else
                    choice.button.interactable = false;

                return;
            }
        }
    }

    public int GetFirstEmptyBlankIndex()
    {
        for (int i = 0; i < currentBlankValues.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(currentBlankValues[i])) return i;
        }
        return -1;
    }

    public bool AllBlanksFilled()
    {
        if (currentBlankValues.Count == 0) return false;

        for (int i = 0; i < currentBlankValues.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(currentBlankValues[i])) return false;
        }
        return true;
    }

    public void UpdateNextButtonState()
    {
        if (nextButton != null)
            nextButton.interactable = AllBlanksFilled();
    }

    public void ClearLastFilledBlank()
    {
        for (int i = currentBlankValues.Count - 1; i >= 0; i--)
        {
            if (!string.IsNullOrWhiteSpace(currentBlankValues[i]))
            {
                string removedWord = currentBlankValues[i];
                currentBlankValues[i] = "";
                ReactivateWordChoice(removedWord);
                RefreshSentenceText();
                UpdateNextButtonState();
                return;
            }
        }
        UpdateNextButtonState();
    }

    public void ResetCurrentBlanks()
    {
        for (int i = 0; i < currentBlankValues.Count; i++)
        {
            currentBlankValues[i] = "";
        }

        for (int i = 0; i < activeWordChoices.Count; i++)
        {
            if (activeWordChoices[i] != null && activeWordChoices[i].button != null)
            {
                activeWordChoices[i].used = false;
                activeWordChoices[i].button.gameObject.SetActive(true);
                activeWordChoices[i].button.interactable = true;
            }
        }

        RefreshSentenceText();
        UpdateNextButtonState();
    }

    public void ReactivateWordChoice(string word)
    {
        for (int i = 0; i < activeWordChoices.Count; i++)
        {
            WordChoiceRuntime choice = activeWordChoices[i];

            if (choice == null || choice.button == null) continue;

            if (choice.used && string.Equals(choice.word, word, StringComparison.OrdinalIgnoreCase))
            {
                choice.used = false;
                choice.button.gameObject.SetActive(true);
                choice.button.interactable = true;
                return;
            }
        }
    }

    public void GoNext()
    {
        if (!AllBlanksFilled())
        {
            UpdateNextButtonState();
            return;
        }

        currentCompletedSentence = BuildCompletedSentence();

        // 中文备注：这里仅检查字幕/填词，不发布、不保存当天状态，也不计算最终指标。
        // 最终指标必须等玩家选择战术卡并点击最终 Post 按钮后才计算。
        if (checkCaptionWithCoachBeforeTacticSelection &&
            catCoachManager != null &&
            metricEngine != null)
        {
            PostMetricPreview captionPreview = metricEngine.PreviewCaptionChoices(this);

            if (catCoachManager.TryShowCaptionChoiceWarning(captionPreview, this))
            {
                return;
            }
        }

        ContinueToTacticSelection();
    }

    /// <summary>
    /// Called by the Cat Coach Continue Anyway button.
    /// Keeps the selected caption/words and opens tactic selection.
    /// </summary>
    public void ContinueToTacticSelectionAfterCoach()
    {
        ContinueToTacticSelection();
    }

    /// <summary>
    /// Called by the Cat Coach Revise button.
    /// Reopens the exact same caption and clears only the selected blank answers.
    /// </summary>
    public void ReviseCurrentCaptionFromCoach()
    {
        currentCompletedSentence = "";
        ShowOnlyPanel(fillBlankPanel);
        ResetCurrentBlanks();
    }

    /// <summary>
    /// Hides the fill-in-the-blank panel while the coach overlay is open.
    /// The selected topic, subtopic, caption, and word-choice buttons remain in memory.
    /// </summary>
    public void HideFillBlankPanelForCoach()
    {
        if (hideFillBlankPanelWhileCoachIsOpen && fillBlankPanel != null)
        {
            fillBlankPanel.SetActive(false);
        }
    }

    private void ContinueToTacticSelection()
    {
        if (tacticManager == null)
        {
            Debug.LogWarning("DailyPostFillBlankManager: Cannot continue because TacticManager is missing.");
            ShowOnlyPanel(fillBlankPanel);
            return;
        }

        ShowOnlyPanel(null);
        tacticManager.OpenCardView();

        // 中文备注：旧版 On Next 事件默认不再执行，避免过早保存发布状态、计算指标或打开社交动态面板。
        if (invokeLegacyOnNextEvent)
        {
            onNext.Invoke();
        }
    }

    public void ShowOnlyPanel(GameObject panelToShow)
    {
        if (topicPanel != null) topicPanel.SetActive(panelToShow == topicPanel);
        if (subTopicPanel != null) subTopicPanel.SetActive(panelToShow == subTopicPanel);
        if (sentenceSelectionPanel != null) sentenceSelectionPanel.SetActive(panelToShow == sentenceSelectionPanel);
        if (fillBlankPanel != null) fillBlankPanel.SetActive(panelToShow == fillBlankPanel);
    }

    public void ClearChildren(Transform parent)
    {
        if (parent == null) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    public void Shuffle(List<string> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            string temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public string EscapeRichText(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("<", "&lt;").Replace(">", "&gt;");
    }
}

[Serializable]
public class TopicButtonBinding
{
    public int unlockDay = 1;
    public Button topicButton;
    public string topicId;
}

[Serializable]
public class DailyPostJsonDatabase
{
    public List<DailyPostTopicJson> topics = new List<DailyPostTopicJson>();

    [Tooltip("Legacy fallback only. New captions should live under topics > subTopics > sentences.")]
    public List<DailyPostTacticJson> tactics = new List<DailyPostTacticJson>();
}

[Serializable]
public class DailyPostTopicJson
{
    public string topicId;
    public string topicName;
    public List<DailyPostSubTopicJson> subTopics = new List<DailyPostSubTopicJson>();
}

[Serializable]
public class DailyPostSubTopicJson
{
    public string subTopicId;
    public string subTopicName;

    [Header("Captions For This Subtopic")]
    public List<DailyPostSentenceJson> sentences = new List<DailyPostSentenceJson>();
}

[Serializable]
public class DailyPostTacticJson
{
    public string tacticId;
    public List<DailyPostSentenceJson> sentences = new List<DailyPostSentenceJson>();
}

[Serializable]
public class DailyPostSentenceJson
{
    [Header("Caption Template")]
    public string sentenceId;
    public string captionTemplateId;

    [TextArea(2, 5)]
    public string sentence;

    public List<string> blankWords = new List<string>();
    public List<string> wordChoices = new List<string>();

    [Header("Ordered Blank Scoring")]
    [Tooltip("One item per blank, in the same order as blankWords. This makes order matter.")]
    public List<DailyPostBlankScoringJson> blankScoring = new List<DailyPostBlankScoringJson>();

    [Header("Caption Scoring")]
    [Tooltip("Correct, HalfCorrect, Neutral, Wrong, or Nonsense. Empty uses the default from MisinformationMetricFormulaProfile.")]
    public string captionQuality;

    [Header("Caption-Level Tactic Fit")]
    [Tooltip("Best tactic types for this caption.")]
    public List<string> idealTacticTypes = new List<string>();

    [Tooltip("Acceptable but not best tactic types for this caption.")]
    public List<string> neutralTacticTypes = new List<string>();

    [Tooltip("Bad tactic types for this caption.")]
    public List<string> badTacticTypes = new List<string>();

    [Header("Legacy Fallback Scoring Lists")]
    public List<string> correctWords = new List<string>();
    public List<string> halfCorrectWords = new List<string>();
    public List<string> neutralWords = new List<string>();
    public List<string> wrongWords = new List<string>();
    public List<string> nonsenseWords = new List<string>();
}

[Serializable]
public class DailyPostBlankScoringJson
{
    public string note;
    public List<string> correctWords = new List<string>();
    public List<string> halfCorrectWords = new List<string>();
    public List<string> neutralWords = new List<string>();
    public List<string> wrongWords = new List<string>();
    public List<string> nonsenseWords = new List<string>();
}

[Serializable]
public class WordChoiceRuntime
{
    public string word;
    public Button button;
    public bool used;
}
