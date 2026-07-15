#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class DailyPostJsonEditorWindow : EditorWindow
{
    private TextAsset jsonFile;
    private DailyPostJsonDatabase database;
    private string jsonAssetPath;
    private Vector2 scrollPosition;
    private bool showTopics = true;
    private readonly Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

    private readonly string[] qualityOptions = new string[]
    {
        "", "Correct", "HalfCorrect", "Neutral", "Wrong", "Nonsense"
    };

    [MenuItem("Tools/Misinformation Game/Daily Post JSON Editor")]
    public static void OpenWindow()
    {
        DailyPostJsonEditorWindow window = GetWindow<DailyPostJsonEditorWindow>("Daily Post JSON");
        window.minSize = new Vector2(880f, 620f);
        window.Show();
    }

    public static void OpenWithFile(TextAsset textAsset)
    {
        DailyPostJsonEditorWindow window = GetWindow<DailyPostJsonEditorWindow>("Daily Post JSON");
        window.minSize = new Vector2(880f, 620f);
        window.jsonFile = textAsset;
        window.Show();

        if (textAsset != null)
        {
            window.LoadJsonFromAsset();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Daily Post JSON Editor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Use this tool to edit DailyPostData.json. New structure: Topic > Subtopic > Caption. Tactic fit belongs only to captions. Blank scoring is ordered, so blank position matters.", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        jsonFile = (TextAsset)EditorGUILayout.ObjectField("JSON TextAsset", jsonFile, typeof(TextAsset), false);

        if (GUILayout.Button("Load", GUILayout.Width(80f)))
        {
            LoadJsonFromAsset();
        }

        using (new EditorGUI.DisabledScope(database == null))
        {
            if (GUILayout.Button("Save", GUILayout.Width(80f)))
            {
                SaveJsonToAsset();
            }
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(jsonAssetPath))
        {
            EditorGUILayout.LabelField("Path", jsonAssetPath);
        }

        if (database == null)
        {
            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Create Empty Database In Memory"))
            {
                database = new DailyPostJsonDatabase();
                NormalizeDatabase(database);
            }
            return;
        }

        NormalizeDatabase(database);

        EditorGUILayout.Space(8f);
        DrawToolbar();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DrawTopicsSection();
        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        if (GUILayout.Button("Normalize All Blank Scoring"))
        {
            NormalizeAllBlankScoring();
            MarkDirty();
        }

        if (GUILayout.Button("Sync All Blanks From [Brackets]"))
        {
            SyncAllBlankWordsFromSentences();
            MarkDirty();
        }

        if (GUILayout.Button("Save JSON"))
        {
            SaveJsonToAsset();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawTopicsSection()
    {
        showTopics = EditorGUILayout.Foldout(showTopics, "Topics / Subtopics / Captions", true);
        if (!showTopics)
        {
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        for (int i = 0; i < database.topics.Count; i++)
        {
            DrawTopicEditor(i);
        }

        if (GUILayout.Button("Add Topic"))
        {
            database.topics.Add(new DailyPostTopicJson
            {
                topicId = "new_topic",
                topicName = "New Topic",
                subTopics = new List<DailyPostSubTopicJson>()
            });
            MarkDirty();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawTopicEditor(int topicIndex)
    {
        DailyPostTopicJson topic = database.topics[topicIndex];
        string topicKey = "topic_" + topicIndex;
        bool open = GetFoldout(topicKey, false);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        open = EditorGUILayout.Foldout(open, "Topic " + (topicIndex + 1) + ": " + Safe(topic.topicId) + " - " + Safe(topic.topicName), true);
        SetFoldout(topicKey, open);

        if (GUILayout.Button("Delete Topic", GUILayout.Width(100f)))
        {
            if (EditorUtility.DisplayDialog("Delete topic?", "Delete this topic, its subtopics, and all captions under it?", "Delete", "Cancel"))
            {
                database.topics.RemoveAt(topicIndex);
                MarkDirty();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
        }
        EditorGUILayout.EndHorizontal();

        if (open)
        {
            EditorGUI.indentLevel++;
            topic.topicId = EditorGUILayout.TextField("Topic ID", topic.topicId);
            topic.topicName = EditorGUILayout.TextField("Topic Name", topic.topicName);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Subtopics", EditorStyles.boldLabel);

            for (int s = 0; s < topic.subTopics.Count; s++)
            {
                DrawSubtopicEditor(topic, s, topicIndex);
            }

            if (GUILayout.Button("Add Subtopic"))
            {
                topic.subTopics.Add(new DailyPostSubTopicJson
                {
                    subTopicId = "new_subtopic",
                    subTopicName = "New Subtopic",
                    sentences = new List<DailyPostSentenceJson>()
                });
                MarkDirty();
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSubtopicEditor(DailyPostTopicJson topic, int subTopicIndex, int topicIndex)
    {
        DailyPostSubTopicJson subTopic = topic.subTopics[subTopicIndex];
        string key = "topic_" + topicIndex + "_sub_" + subTopicIndex;
        bool open = GetFoldout(key, false);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        open = EditorGUILayout.Foldout(open, "Subtopic " + (subTopicIndex + 1) + ": " + Safe(subTopic.subTopicId) + " - " + Safe(subTopic.subTopicName) + "  | Captions: " + subTopic.sentences.Count, true);
        SetFoldout(key, open);

        if (GUILayout.Button("Delete", GUILayout.Width(70f)))
        {
            if (EditorUtility.DisplayDialog("Delete subtopic?", "Delete this subtopic and all captions under it?", "Delete", "Cancel"))
            {
                topic.subTopics.RemoveAt(subTopicIndex);
                MarkDirty();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
        }
        EditorGUILayout.EndHorizontal();

        if (open)
        {
            EditorGUI.indentLevel++;
            subTopic.subTopicId = EditorGUILayout.TextField("Subtopic ID", subTopic.subTopicId);
            subTopic.subTopicName = EditorGUILayout.TextField("Subtopic Name", subTopic.subTopicName);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Captions Under This Subtopic", EditorStyles.boldLabel);

            for (int c = 0; c < subTopic.sentences.Count; c++)
            {
                DrawCaptionEditor(subTopic, c, topicIndex, subTopicIndex);
            }

            if (GUILayout.Button("Add Caption To This Subtopic"))
            {
                subTopic.sentences.Add(CreateNewSentence(topic.topicId, subTopic.subTopicId, subTopic.sentences.Count + 1));
                MarkDirty();
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawCaptionEditor(DailyPostSubTopicJson subTopic, int sentenceIndex, int topicIndex, int subTopicIndex)
    {
        DailyPostSentenceJson sentence = subTopic.sentences[sentenceIndex];
        NormalizeSentence(sentence);

        string foldoutKey = "caption_" + topicIndex + "_" + subTopicIndex + "_" + sentenceIndex;
        string title = "Caption " + (sentenceIndex + 1) + ": " + Shorten(sentence.sentence, 85);
        bool open = GetFoldout(foldoutKey, sentenceIndex == 0);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        open = EditorGUILayout.Foldout(open, title, true);
        SetFoldout(foldoutKey, open);

        if (GUILayout.Button("Up", GUILayout.Width(45f)) && sentenceIndex > 0)
        {
            DailyPostSentenceJson temp = subTopic.sentences[sentenceIndex - 1];
            subTopic.sentences[sentenceIndex - 1] = subTopic.sentences[sentenceIndex];
            subTopic.sentences[sentenceIndex] = temp;
            MarkDirty();
        }

        if (GUILayout.Button("Down", GUILayout.Width(55f)) && sentenceIndex < subTopic.sentences.Count - 1)
        {
            DailyPostSentenceJson temp = subTopic.sentences[sentenceIndex + 1];
            subTopic.sentences[sentenceIndex + 1] = subTopic.sentences[sentenceIndex];
            subTopic.sentences[sentenceIndex] = temp;
            MarkDirty();
        }

        if (GUILayout.Button("Delete", GUILayout.Width(70f)))
        {
            if (EditorUtility.DisplayDialog("Delete caption?", "Delete this caption template?", "Delete", "Cancel"))
            {
                subTopic.sentences.RemoveAt(sentenceIndex);
                MarkDirty();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
        }
        EditorGUILayout.EndHorizontal();

        if (open)
        {
            EditorGUI.indentLevel++;
            sentence.sentenceId = EditorGUILayout.TextField("Sentence ID", sentence.sentenceId);
            sentence.captionTemplateId = EditorGUILayout.TextField("Caption Template ID", sentence.captionTemplateId);
            sentence.captionQuality = DrawQualityPopup("Caption Quality", sentence.captionQuality);

            EditorGUILayout.LabelField("Sentence Template");
            sentence.sentence = EditorGUILayout.TextArea(sentence.sentence, GUILayout.MinHeight(54f));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Sync Blank Words From [Brackets]"))
            {
                SyncBlankWordsFromSentence(sentence);
                MarkDirty();
            }

            if (GUILayout.Button("Normalize Blank Scoring"))
            {
                NormalizeBlankScoring(sentence, true);
                MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            DrawBlankWords(sentence);
            DrawStringList("Word Choices", sentence.wordChoices);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Caption-Level Tactic Fit", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Only captions have tactic fit lists. Topic and subtopic do not. Ideal = best tactic, Neutral = acceptable, Bad = wrong tactic penalty.", MessageType.None);
            DrawStringList("Ideal Tactic Types", sentence.idealTacticTypes);
            DrawStringList("Neutral Tactic Types", sentence.neutralTacticTypes);
            DrawStringList("Bad Tactic Types", sentence.badTacticTypes);

            DrawBlankScoring(sentence, topicIndex, subTopicIndex, sentenceIndex);

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawBlankWords(DailyPostSentenceJson sentence)
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Blank Words / Blank Count", EditorStyles.boldLabel);

        int newCount = Mathf.Max(0, EditorGUILayout.IntField("Blank Count", sentence.blankWords.Count));
        while (sentence.blankWords.Count < newCount)
        {
            sentence.blankWords.Add("blank" + (sentence.blankWords.Count + 1));
        }
        while (sentence.blankWords.Count > newCount)
        {
            sentence.blankWords.RemoveAt(sentence.blankWords.Count - 1);
        }
        NormalizeBlankScoring(sentence, false);

        for (int i = 0; i < sentence.blankWords.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            sentence.blankWords[i] = EditorGUILayout.TextField("Blank " + (i + 1), sentence.blankWords[i]);
            if (GUILayout.Button("Use as Correct", GUILayout.Width(110f)))
            {
                NormalizeBlankScoring(sentence, false);
                sentence.blankScoring[i].correctWords.Clear();
                sentence.blankScoring[i].correctWords.Add(sentence.blankWords[i]);
                MarkDirty();
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawBlankScoring(DailyPostSentenceJson sentence, int topicIndex, int subTopicIndex, int sentenceIndex)
    {
        NormalizeBlankScoring(sentence, false);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Ordered Blank Scoring", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Each block matches the same blank position. This makes order matter. Example: blank 1 correct = eat, blank 2 correct = today. Selecting today first is wrong unless you add it to blank 1 half/neutral.", MessageType.None);

        for (int i = 0; i < sentence.blankScoring.Count; i++)
        {
            DailyPostBlankScoringJson rule = sentence.blankScoring[i];
            string blankName = i < sentence.blankWords.Count ? sentence.blankWords[i] : "Blank " + (i + 1);
            string key = "blank_score_" + topicIndex + "_" + subTopicIndex + "_" + sentenceIndex + "_" + i;
            bool open = GetFoldout(key, false);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            open = EditorGUILayout.Foldout(open, "Blank " + (i + 1) + " Scoring: " + blankName, true);
            SetFoldout(key, open);

            if (open)
            {
                rule.note = EditorGUILayout.TextField("Note", rule.note);
                DrawStringList("Correct Words", rule.correctWords);
                DrawStringList("Half Correct Words", rule.halfCorrectWords);
                DrawStringList("Neutral Words", rule.neutralWords);
                DrawStringList("Wrong Words", rule.wrongWords);
                DrawStringList("Nonsense Words", rule.nonsenseWords);
            }

            EditorGUILayout.EndVertical();
        }
    }

    private void DrawStringList(string label, List<string> list)
    {
        if (list == null)
        {
            EditorGUILayout.HelpBox(label + " is null. Click Normalize All Blank Scoring or reload the JSON.", MessageType.Warning);
            return;
        }

        string key = "list_" + label + "_" + list.GetHashCode();
        bool open = GetFoldout(key, false);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        open = EditorGUILayout.Foldout(open, label + " (" + list.Count + ")", true);
        SetFoldout(key, open);
        if (GUILayout.Button("Add", GUILayout.Width(55f)))
        {
            list.Add("");
            MarkDirty();
        }
        EditorGUILayout.EndHorizontal();

        if (open)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                list[i] = EditorGUILayout.TextField("#" + (i + 1), list[i]);
                if (GUILayout.Button("X", GUILayout.Width(28f)))
                {
                    list.RemoveAt(i);
                    MarkDirty();
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
    }

    private string DrawQualityPopup(string label, string current)
    {
        int index = 0;
        for (int i = 0; i < qualityOptions.Length; i++)
        {
            if (string.Equals(qualityOptions[i], current, StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                break;
            }
        }

        int newIndex = EditorGUILayout.Popup(label, index, qualityOptions);
        return qualityOptions[newIndex];
    }

    private void LoadJsonFromAsset()
    {
        if (jsonFile == null)
        {
            EditorUtility.DisplayDialog("No JSON file", "Drag a DailyPostData.json TextAsset first.", "OK");
            return;
        }

        jsonAssetPath = AssetDatabase.GetAssetPath(jsonFile);
        if (string.IsNullOrEmpty(jsonAssetPath))
        {
            EditorUtility.DisplayDialog("Invalid file", "Could not find the asset path for this JSON file.", "OK");
            return;
        }

        try
        {
            database = JsonUtility.FromJson<DailyPostJsonDatabase>(jsonFile.text);
            if (database == null)
            {
                database = new DailyPostJsonDatabase();
            }
            NormalizeDatabase(database);
        }
        catch (Exception e)
        {
            database = new DailyPostJsonDatabase();
            Debug.LogError("DailyPostJsonEditorWindow: JSON parse failed. " + e.Message);
        }
    }

    private void SaveJsonToAsset()
    {
        if (database == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(jsonAssetPath))
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Daily Post JSON", "DailyPostData", "json", "Choose where to save the JSON file.");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            jsonAssetPath = path;
        }

        NormalizeDatabase(database);
        string json = JsonUtility.ToJson(database, true);
        File.WriteAllText(jsonAssetPath, json);
        AssetDatabase.ImportAsset(jsonAssetPath);
        AssetDatabase.Refresh();
        jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonAssetPath);
        EditorUtility.DisplayDialog("Saved", "Daily post JSON saved.", "OK");
    }

    private void NormalizeDatabase(DailyPostJsonDatabase data)
    {
        if (data.topics == null) data.topics = new List<DailyPostTopicJson>();
        if (data.tactics == null) data.tactics = new List<DailyPostTacticJson>();

        for (int i = 0; i < data.topics.Count; i++)
        {
            DailyPostTopicJson topic = data.topics[i];
            if (topic.subTopics == null) topic.subTopics = new List<DailyPostSubTopicJson>();

            for (int s = 0; s < topic.subTopics.Count; s++)
            {
                DailyPostSubTopicJson subTopic = topic.subTopics[s];
                if (subTopic.sentences == null) subTopic.sentences = new List<DailyPostSentenceJson>();

                for (int c = 0; c < subTopic.sentences.Count; c++)
                {
                    NormalizeSentence(subTopic.sentences[c]);
                }
            }
        }
    }

    private void NormalizeSentence(DailyPostSentenceJson sentence)
    {
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
        NormalizeBlankScoring(sentence, false);
    }

    private void NormalizeBlankScoring(DailyPostSentenceJson sentence, bool resetCorrectFromBlankWords)
    {
        if (sentence.blankScoring == null) sentence.blankScoring = new List<DailyPostBlankScoringJson>();

        while (sentence.blankScoring.Count < sentence.blankWords.Count)
        {
            sentence.blankScoring.Add(new DailyPostBlankScoringJson());
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

            if (rule.correctWords == null) rule.correctWords = new List<string>();
            if (rule.halfCorrectWords == null) rule.halfCorrectWords = new List<string>();
            if (rule.neutralWords == null) rule.neutralWords = new List<string>();
            if (rule.wrongWords == null) rule.wrongWords = new List<string>();
            if (rule.nonsenseWords == null) rule.nonsenseWords = new List<string>();

            if (string.IsNullOrEmpty(rule.note))
            {
                rule.note = "Blank " + (i + 1);
            }

            if (resetCorrectFromBlankWords && i < sentence.blankWords.Count)
            {
                rule.correctWords.Clear();
                if (!string.IsNullOrWhiteSpace(sentence.blankWords[i]))
                {
                    rule.correctWords.Add(sentence.blankWords[i]);
                }
            }
        }
    }

    private void NormalizeAllBlankScoring()
    {
        for (int t = 0; t < database.topics.Count; t++)
        {
            DailyPostTopicJson topic = database.topics[t];
            for (int s = 0; s < topic.subTopics.Count; s++)
            {
                DailyPostSubTopicJson subTopic = topic.subTopics[s];
                for (int c = 0; c < subTopic.sentences.Count; c++)
                {
                    NormalizeBlankScoring(subTopic.sentences[c], true);
                }
            }
        }
    }

    private void SyncAllBlankWordsFromSentences()
    {
        for (int t = 0; t < database.topics.Count; t++)
        {
            DailyPostTopicJson topic = database.topics[t];
            for (int s = 0; s < topic.subTopics.Count; s++)
            {
                DailyPostSubTopicJson subTopic = topic.subTopics[s];
                for (int c = 0; c < subTopic.sentences.Count; c++)
                {
                    SyncBlankWordsFromSentence(subTopic.sentences[c]);
                }
            }
        }
    }

    private void SyncBlankWordsFromSentence(DailyPostSentenceJson sentence)
    {
        List<string> found = ExtractBracketWords(sentence.sentence);
        sentence.blankWords.Clear();
        sentence.blankWords.AddRange(found);
        NormalizeBlankScoring(sentence, true);
    }

    private List<string> ExtractBracketWords(string sentence)
    {
        List<string> result = new List<string>();
        if (string.IsNullOrEmpty(sentence)) return result;

        MatchCollection matches = Regex.Matches(sentence, "\\[(.*?)\\]");
        for (int i = 0; i < matches.Count; i++)
        {
            string value = matches[i].Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(value))
            {
                result.Add(value);
            }
        }

        return result;
    }

    private DailyPostSentenceJson CreateNewSentence(string topicId, string subTopicId, int number)
    {
        DailyPostSentenceJson sentence = new DailyPostSentenceJson();
        string safeTopic = string.IsNullOrWhiteSpace(topicId) ? "topic" : topicId.Trim().ToLowerInvariant();
        string safeSub = string.IsNullOrWhiteSpace(subTopicId) ? "subtopic" : subTopicId.Trim().ToLowerInvariant();
        sentence.sentenceId = safeTopic + "_" + safeSub + "_" + number.ToString("00");
        sentence.captionTemplateId = sentence.sentenceId;
        sentence.captionQuality = "Correct";
        sentence.sentence = "New caption with [blank1] here.";
        sentence.blankWords = new List<string> { "blank1" };
        sentence.wordChoices = new List<string> { "blank1", "half choice", "wrong choice", "nonsense" };
        sentence.idealTacticTypes = new List<string> { "emotion" };
        sentence.neutralTacticTypes = new List<string> { "bandwagon" };
        sentence.badTacticTypes = new List<string> { "authority" };
        sentence.blankScoring = new List<DailyPostBlankScoringJson>
        {
            new DailyPostBlankScoringJson
            {
                note = "Blank 1",
                correctWords = new List<string> { "blank1" },
                halfCorrectWords = new List<string> { "half choice" },
                neutralWords = new List<string>(),
                wrongWords = new List<string> { "wrong choice" },
                nonsenseWords = new List<string> { "nonsense" }
            }
        };
        return sentence;
    }

    private bool GetFoldout(string key, bool defaultValue)
    {
        if (!foldouts.TryGetValue(key, out bool value))
        {
            value = defaultValue;
            foldouts[key] = value;
        }
        return value;
    }

    private void SetFoldout(string key, bool value)
    {
        foldouts[key] = value;
    }

    private string Safe(string value)
    {
        return string.IsNullOrEmpty(value) ? "<empty>" : value;
    }

    private string Shorten(string value, int length)
    {
        if (string.IsNullOrEmpty(value)) return "<empty>";
        if (value.Length <= length) return value;
        return value.Substring(0, length) + "...";
    }

    private void MarkDirty()
    {
        Repaint();
    }
}
#endif
