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
    public TacticManager tacticManager;

    [Header("Panels")]
    public GameObject topicPanel;
    public GameObject subTopicPanel;
    public GameObject sentenceSelectionPanel;
    public GameObject fillBlankPanel;

    [Header("JSON Data")]
    public TextAsset dailyPostJsonFile;

    private DailyPostJsonDatabase dailyPostData = new DailyPostJsonDatabase();

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
            button.onClick.AddListener(() => GoToTacticSelection(capturedSubTopicIndex));
        }
    }

    public void GoToTacticSelection(int subTopicIndex)
    {
        currentSubTopicIndex = subTopicIndex;
        currentSubTopicData = currentTopicData.subTopics[subTopicIndex];

        // Hide daily post panels entirely
        ShowOnlyPanel(null);

        // Turn on the Tactic Panel
        if (tacticManager != null)
        {
            tacticManager.OpenCardView();
        }
    }

    // Called by TacticManager after a card is confirmed
    public void OpenSentenceSelectionPanel(string tacticType)
    {
        currentTacticType = tacticType.ToLower();
        DailyPostTacticJson tacticData = GetTacticDataById(currentTacticType);

        if (tacticData == null || tacticData.sentences == null || tacticData.sentences.Count == 0)
        {
            Debug.LogWarning("DailyPostFillBlankManager: No sentences found for tactic: " + tacticType);
            return;
        }

        ShowOnlyPanel(sentenceSelectionPanel);
        ClearChildren(sentenceButtonParent);

        for (int i = 0; i < tacticData.sentences.Count; i++)
        {
            DailyPostSentenceJson sentenceData = tacticData.sentences[i];
            Button button = Instantiate(sentenceButtonPrefab, sentenceButtonParent);

            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                // Format the preview text to show blanks instead of brackets
                string previewText = sentenceData.sentence;
                if (sentenceData.blankWords != null)
                {
                    for (int w = 0; w < sentenceData.blankWords.Count; w++)
                    {
                        previewText = previewText.Replace($"[{sentenceData.blankWords[w]}]", emptyBlankText);
                    }
                }
                buttonText.text = previewText;
            }

            int capturedIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OpenFillBlankPanel(tacticData, capturedIndex));
        }
    }

    public void OpenFillBlankPanel(DailyPostTacticJson tacticData, int sentenceIndex)
    {
        currentSentenceData = tacticData.sentences[sentenceIndex];
        currentCompletedSentence = "";

        ShowOnlyPanel(fillBlankPanel);

        BuildFillBlankSentence();
        BuildWordChoices(currentSentenceData);
        UpdateNextButtonState();
    }

    public DailyPostTopicJson GetTopicDataById(string topicId)
    {
        if (dailyPostData == null || dailyPostData.topics == null) return null;

        for (int i = 0; i < dailyPostData.topics.Count; i++)
        {
            if (dailyPostData.topics[i].topicId == topicId)
                return dailyPostData.topics[i];
        }
        return null;
    }

    public DailyPostTacticJson GetTacticDataById(string tacticId)
    {
        if (dailyPostData == null || dailyPostData.tactics == null) return null;

        for (int i = 0; i < dailyPostData.tactics.Count; i++)
        {
            if (dailyPostData.tactics[i].tacticId.ToLower() == tacticId.ToLower())
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

            int index = sentence.IndexOf($"[{originalBlankWord}]", cursor, StringComparison.OrdinalIgnoreCase);

            if (index < 0)
            {
                // Fallback if brackets aren't used in the raw string match
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

            // Advance cursor past the blank word (account for brackets if they exist)
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

            int index = sentence.IndexOf($"[{originalBlankWord}]", cursor, StringComparison.OrdinalIgnoreCase);

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

    public void GoNext() // This is called when the Next/publishing button is pressed after filling blanks
    {
        if (!AllBlanksFilled())
        {
            UpdateNextButtonState();
            return;
        }

        currentCompletedSentence = BuildCompletedSentence();

        // Hide all Daily Post panels so the screen is clear for publishing
        ShowOnlyPanel(null);

        onNext.Invoke();
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
    [TextArea(2, 5)]
    public string sentence;
    public List<string> blankWords = new List<string>();
    public List<string> wordChoices = new List<string>();
}

[Serializable]
public class WordChoiceRuntime
{
    public string word;
    public Button button;
    public bool used;
}