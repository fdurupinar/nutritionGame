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
    private bool advanced;
    private int topicSelection, subtopicSelection, captionSelection;

    private void OnEnable()
    {
        if (database != null) return;
        jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Scripts/Yanyan/Save/DailyPostData.json");
        if (jsonFile != null) LoadJsonFromAsset();
    }

    public override void SaveChanges() { if (SaveJsonToAsset()) base.SaveChanges(); }

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
        EditorGUILayout.LabelField("Caption & Answer Editor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Choose a topic, subtopic and caption. Add one word or phrase per answer row. Full credit means it fits this blank in the game, not that the health claim is true. Click Save when finished.", MessageType.Info);

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
        advanced = EditorGUILayout.ToggleLeft("Advanced: manage topics, captions and tactic settings", advanced);
        EditorGUI.BeginChangeCheck();
        if (advanced) DrawToolbar();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        if (advanced) DrawTopicsSection(); else DrawCaptionPicker();
        EditorGUILayout.EndScrollView();
        if (EditorGUI.EndChangeCheck()) MarkDirty();
    }

    private void DrawCaptionPicker()
    {
        if (database.topics.Count == 0) { EditorGUILayout.HelpBox("Add a topic in Advanced to get started.", MessageType.Info); return; }
        bool previousChanged = GUI.changed;
        topicSelection = Mathf.Clamp(topicSelection, 0, database.topics.Count - 1);
        int nextTopic = EditorGUILayout.Popup("1. Topic", topicSelection, database.topics.ConvertAll(t => t.topicName).ToArray());
        if (nextTopic != topicSelection) { topicSelection = nextTopic; subtopicSelection = captionSelection = 0; }
        var topic = database.topics[topicSelection];
        if (topic.subTopics.Count == 0) return;
        subtopicSelection = Mathf.Clamp(subtopicSelection, 0, topic.subTopics.Count - 1);
        int nextSub = EditorGUILayout.Popup("2. Subtopic", subtopicSelection, topic.subTopics.ConvertAll(t => t.subTopicName).ToArray());
        if (nextSub != subtopicSelection) { subtopicSelection = nextSub; captionSelection = 0; }
        var sub = topic.subTopics[subtopicSelection];
        if (sub.sentences.Count == 0) return;
        captionSelection = Mathf.Clamp(captionSelection, 0, sub.sentences.Count - 1);
        captionSelection = EditorGUILayout.Popup("3. Caption", captionSelection, sub.sentences.ConvertAll(c => Shorten(c.sentence, 100)).ToArray());
        GUI.changed = previousChanged;
        var sentence = sub.sentences[captionSelection];
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField(sentence.sentence, EditorStyles.wordWrappedLabel);
        DrawBlankScoring(sentence, topicSelection, subtopicSelection, captionSelection);
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("Each blank can accept several answers. For example, Blank 2 can give full credit for both power and benefits. Moving a word into a group removes it from the other groups for this blank only.", MessageType.Info);
        DrawStringList("Words players can select (including distractors)", sentence.wordChoices);
        if (sentence.wordChoices.Count > 12)
            EditorGUILayout.HelpBox("This caption has more than 12 word choices. The game's default display limit is 12, so some may be hidden. Remove unused distractors or raise Max Word Choices To Show on the caption manager.", MessageType.Warning);
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        if (GUILayout.Button("Fill Missing Answer Groups"))
        {
            NormalizeAllBlankScoring();
            MarkDirty();
        }

        if (GUILayout.Button("Update Blanks From Captions"))
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
                NormalizeBlankScoring(sentence, false);
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
            if (GUILayout.Button("Mark as Fits", GUILayout.Width(110f)))
            {
                NormalizeBlankScoring(sentence, false);
                SetAnswer(sentence, sentence.blankScoring[i], sentence.blankScoring[i].correctWords, -1, sentence.blankWords[i]);
                MarkDirty();
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawBlankScoring(DailyPostSentenceJson sentence, int topicIndex, int subTopicIndex, int sentenceIndex)
    {
        NormalizeBlankScoring(sentence, false);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Answers for each blank", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Grade how words fit this caption, not whether the health claim is true. Put equivalent answers together. Use partial credit for readable but broader or different claims.", MessageType.Info);
        for (int i = 0; i < sentence.blankScoring.Count; i++)
        {
            var rule = sentence.blankScoring[i];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Blank " + (i + 1) + " — [" + sentence.blankWords[i] + "]", EditorStyles.boldLabel);
            DrawAnswerGroup(sentence, rule, "Full credit — fits the caption", rule.correctWords);
            DrawAnswerGroup(sentence, rule, "Partial credit — partly fits", rule.halfCorrectWords);
            var key = "other_" + rule.GetHashCode();
            bool other = EditorGUILayout.Foldout(GetFoldout(key, false), "Other answers (no credit)", true);
            SetFoldout(key, other);
            if (other)
            {
                DrawAnswerGroup(sentence, rule, "Legacy neutral answers (no credit)", rule.neutralWords);
                DrawAnswerGroup(sentence, rule, "Legacy wrong answers (no credit)", rule.wrongWords);
                DrawAnswerGroup(sentence, rule, "Does not fit", rule.nonsenseWords);
            }
            DrawWordFeedback(rule);
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawWordFeedback(DailyPostBlankScoringJson rule)
    {
        if (rule.wordFeedback == null) rule.wordFeedback = new List<DailyPostWordFeedbackJson>();
        var key = "feedback_" + rule.GetHashCode();
        bool open = EditorGUILayout.Foldout(GetFoldout(key, false), "Explain why an answer only partly fits or does not fit", true);
        SetFoldout(key, open);
        if (!open) return;
        EditorGUILayout.HelpBox("Match the answer spelling to the word bank. Explain the specific issue: vague wording, a different claim, grammar, or a contradiction. Partial fits do not interrupt the player by default.", MessageType.Info);
        for (int i = 0; i < rule.wordFeedback.Count; i++)
        {
            var feedback = rule.wordFeedback[i];
            EditorGUI.BeginChangeCheck();
            feedback.word = EditorGUILayout.TextField("Answer", feedback.word);
            feedback.reason = EditorGUILayout.TextField("Explanation", feedback.reason);
            if (EditorGUI.EndChangeCheck()) MarkDirty();
            if (GUILayout.Button("Remove explanation")) { rule.wordFeedback.RemoveAt(i); MarkDirty(); break; }
        }
        if (GUILayout.Button("Add explanation")) { rule.wordFeedback.Add(new DailyPostWordFeedbackJson()); MarkDirty(); }
    }

    private void DrawAnswerGroup(DailyPostSentenceJson sentence, DailyPostBlankScoringJson rule, string label, List<string> words)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        if (words.Count == 0) EditorGUILayout.LabelField("No answers yet", EditorStyles.miniLabel);
        for (int i = 0; i < words.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            string value = EditorGUILayout.DelayedTextField(words[i]);
            if (value != words[i]) { SetAnswer(sentence, rule, words, i, value); MarkDirty(); }
            bool remove = GUILayout.Button("Remove", GUILayout.Width(75));
            EditorGUILayout.EndHorizontal();
            if (remove) { words.RemoveAt(i); MarkDirty(); break; }
        }
        if (GUILayout.Button("+ Add answer", GUILayout.Width(130))) { words.Add(""); MarkDirty(); }
        EditorGUILayout.Space(5);
    }

    public static void SetAnswer(DailyPostSentenceJson sentence, DailyPostBlankScoringJson rule, List<string> target, int index, string value)
    {
        value = (value ?? "").Trim();
        if (index < 0) target.Add(value); else target[index] = value;
        if (value.Length == 0) return;
        foreach (var group in new[] { rule.correctWords, rule.halfCorrectWords, rule.neutralWords, rule.wrongWords, rule.nonsenseWords })
            if (group != target) group.RemoveAll(w => string.Equals(w?.Trim(), value, StringComparison.OrdinalIgnoreCase));
        if (!sentence.wordChoices.Exists(w => string.Equals(w?.Trim(), value, StringComparison.OrdinalIgnoreCase))) sentence.wordChoices.Add(value);
    }

    private void DrawStringList(string label, List<string> list)
    {
        if (list == null)
        {
            EditorGUILayout.HelpBox(label + " is null. Click Fill Missing Answer Groups or reload the JSON.", MessageType.Warning);
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
            SetFoldout(key, true);
            open = true;
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
        if (hasUnsavedChanges && !EditorUtility.DisplayDialog("Reload caption data?", "Reloading discards your unsaved edits.", "Reload", "Cancel")) return;
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
            hasUnsavedChanges = false;
        }
        catch (Exception e)
        {
            database = null;
            Debug.LogError("DailyPostJsonEditorWindow: JSON parse failed. " + e.Message);
        }
    }

    private bool SaveJsonToAsset()
    {
        if (database == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(jsonAssetPath))
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Daily Post JSON", "DailyPostData", "json", "Choose where to save the JSON file.");
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }
            jsonAssetPath = path;
        }

        NormalizeDatabase(database);
        foreach (var topic in database.topics)
        foreach (var sub in topic.subTopics)
        foreach (var caption in sub.sentences)
        foreach (var rule in caption.blankScoring)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in new[] { rule.correctWords, rule.halfCorrectWords, rule.neutralWords, rule.wrongWords, rule.nonsenseWords })
            {
                group.RemoveAll(string.IsNullOrWhiteSpace);
                for (int i = 0; i < group.Count; i++)
                {
                    group[i] = group[i].Trim();
                    if (!seen.Add(group[i]))
                    {
                        EditorUtility.DisplayDialog("Answer appears more than once", caption.sentence + "\n\n" + rule.note + ": " + group[i] + "\nKeep this answer in only one group for this blank.", "OK");
                        return false;
                    }
                }
            }
            foreach (var word in rule.correctWords)
                if (!caption.wordChoices.Exists(w => string.Equals(w, word, StringComparison.OrdinalIgnoreCase))) caption.wordChoices.Add(word);
            foreach (var word in rule.halfCorrectWords)
                if (!caption.wordChoices.Exists(w => string.Equals(w, word, StringComparison.OrdinalIgnoreCase))) caption.wordChoices.Add(word);
        }
        string json = JsonUtility.ToJson(database, true);
        File.WriteAllText(jsonAssetPath, json);
        AssetDatabase.ImportAsset(jsonAssetPath);
        AssetDatabase.Refresh();
        jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonAssetPath);
        hasUnsavedChanges = false;
        ShowNotification(new GUIContent("Caption answers saved"));
        return true;
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
                    NormalizeBlankScoring(subTopic.sentences[c], false);
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
        NormalizeBlankScoring(sentence, false);
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
        hasUnsavedChanges = true;
        saveChangesMessage = "Save your caption and answer changes?";
        Repaint();
    }
}
#endif
