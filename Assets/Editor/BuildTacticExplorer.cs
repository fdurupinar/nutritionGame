using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BuildTacticExplorer
{
    const string ScenePath = "Assets/Scenes/3 Game.unity";
    static TextMeshProUGUI template;

    [MenuItem("Tools/Food for Thought/Build Tactic Explorer")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorSceneManager.SaveOpenScenes();
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var manager = Object.FindFirstObjectByType<TacticManager>();
        var fill = manager.dailyPostFillBlankManager;
        var cat = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Image>(true))
            .First(i => i.sprite != null && i.sprite.name.StartsWith("catface") &&
                i.GetComponentsInParent<Transform>(true).Any(t => t.name == "Fill in Blank Panel"));
        fill.captionCat = cat;
        fill.captionCatNeutral = CatSprite("catfacesideeye3");
        fill.captionCatSmile = CatSprite("catface");
        fill.captionCatSideEye = CatSprite("catfacesideeye");
        fill.captionCatUnsure = CatSprite("catfacesideeye2");
        cat.preserveAspect = true;
        cat.sprite = fill.captionCatNeutral;
        VerifyCaptionCat(fill);
        var panel = manager.TacticPanel.transform;
        template = panel.Find("SetenceTitle").GetComponent<TextMeshProUGUI>();
        template.text = "Explore the tactics";
        Rect(template.rectTransform, new Vector2(.18f,.90f), new Vector2(.92f,.96f));
        template.fontSize = 38;
        template.enableAutoSizing = false;
        template.fontSizeMin = 26;
        template.fontSizeMax = 38;
        var intro = Label(panel, "ExplorerIntro", "Choose the tactic that best matches your caption. Right-click a card for a hint.", .08f,.79f,.92f,.88f,28);
        var caption = Label(panel, "ExplorerCaption", "<b>YOUR POST</b>\nYour completed caption will appear here.", .08f,.63f,.92f,.79f,26);
        caption.overflowMode = TextOverflowModes.Overflow;
        caption.margin = new Vector4(20, 14, 20, 14);
        var captionSurface = panel.Find("CaptionSurface")?.GetComponent<Image>();
        if (captionSurface == null) captionSurface = new GameObject("CaptionSurface", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        captionSurface.transform.SetParent(panel, false);
        Rect(captionSurface.rectTransform, new Vector2(.07f,.64f), new Vector2(.93f,.79f));
        captionSurface.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        captionSurface.type = Image.Type.Sliced; captionSurface.color = GamePalette.Surface; captionSurface.raycastTarget = false;
        // Remove it from the ordering first so rebuilding cannot move it over the caption.
        captionSurface.transform.SetAsFirstSibling();
        captionSurface.transform.SetSiblingIndex(caption.transform.GetSiblingIndex() - 1);
        Rect(caption.rectTransform, new Vector2(.07f,.64f), new Vector2(.93f,.79f));
        var status = Label(panel, "ExplorerSelection", "Select a tactic to explore its meaning.", .08f,.105f,.92f,.18f,25);
        Rect((RectTransform)panel.Find("Viewport"),new Vector2(.06f,.19f),new Vector2(.92f,.62f));
        Rect((RectTransform)panel.Find("Scrollbar Vertical"),new Vector2(.93f,.19f),new Vector2(.95f,.62f));
        var content = panel.Find("Viewport/Content").GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0,1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f,1); content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
        var grid = content.GetComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3; grid.cellSize = new Vector2(184,322);
        grid.spacing = new Vector2(18,18); grid.padding = new RectOffset(8,8,8,8); grid.childAlignment = TextAnchor.UpperCenter;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = panel.GetComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        var publish = panel.Find("Confirm Tactic Button").GetComponent<Button>();
        Rect((RectTransform)publish.transform,new Vector2(.28f,.035f),new Vector2(.72f,.09f));
        publish.GetComponentInChildren<TextMeshProUGUI>().text = "Publish post";
        var hint = manager.tacticHintPanel;
        hint.captionManager = manager.dailyPostFillBlankManager;
        var oldExploreButton = panel.Find("Hint Button");
        if (oldExploreButton != null) Object.DestroyImmediate(oldExploreButton.gameObject);
        hint.hintButton = null;
        var explorer = panel.GetComponent<TacticExplorer>() ?? panel.gameObject.AddComponent<TacticExplorer>();
        explorer.manager = manager; explorer.captionText = caption; explorer.selectionText = status; explorer.publishButton = publish;
        var checkTransform = panel.Find("CheckChoiceButton");
        if (checkTransform != null) Object.DestroyImmediate(checkTransform.gameObject);
        var buttonLabel = publish.GetComponentInChildren<TextMeshProUGUI>();
        buttonLabel.fontSize = 23; buttonLabel.enableAutoSizing = false;
        buttonLabel.textWrappingMode = TextWrappingModes.Normal; buttonLabel.margin = new Vector4(6,0,6,0);
        explorer.Refresh();
        VerifyTacticFeedback(fill);
        // The explanation scrolls so long authored descriptions stay readable.
        Rect(hint.hintPanelRect,new Vector2(.05f,.12f),new Vector2(.95f,.9f));
        hint.hintPanel.GetComponent<Image>().color = GamePalette.Surface;
        hint.hintPanel.GetComponent<Image>().raycastTarget = true;
        // The panel itself supplies the background; the old fixed-size inset is redundant.
        var oldBackground = hint.hintPanel.transform.Find("Panel background");
        if (oldBackground != null) oldBackground.gameObject.SetActive(false);
        var title = hint.hintPanel.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t=>t.name == "HintTitle");
        if(title != null) { title.transform.SetParent(hint.hintPanel.transform,false); title.transform.SetAsLastSibling(); title.text = "Inside the tactic"; title.fontSize = 32; title.enableAutoSizing = false; title.alignment = TextAlignmentOptions.TopLeft; Rect(title.rectTransform,new Vector2(.08f,.89f),new Vector2(.68f,.97f)); }
        hint.panelTitle = title;
        var catTransform = hint.hintPanel.transform.Find("FeedbackCat");
        var feedbackCat = catTransform != null ? catTransform.GetComponent<Image>() :
            new GameObject("FeedbackCat", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        feedbackCat.transform.SetParent(hint.hintPanel.transform, false);
        Rect(feedbackCat.rectTransform, new Vector2(.69f,.89f), new Vector2(.84f,.98f));
        feedbackCat.preserveAspect = true; feedbackCat.raycastTarget = false;
        feedbackCat.sprite = fill.captionCatNeutral; feedbackCat.gameObject.SetActive(false);
        hint.feedbackCat = feedbackCat;
        hint.closeButton.transform.SetParent(hint.hintPanel.transform,false);
        hint.closeButton.transform.SetAsLastSibling();
        Rect((RectTransform)hint.closeButton.transform,new Vector2(.85f,.91f),new Vector2(.97f,.98f));
        var viewport = hint.hintPanel.transform.Find("ExplorerReadingArea") as RectTransform;
        if(viewport == null) viewport = new GameObject("ExplorerReadingArea",typeof(RectTransform),typeof(RectMask2D),typeof(Image),typeof(ScrollRect)).GetComponent<RectTransform>();
        viewport.SetParent(hint.hintPanel.transform,false);
        Rect(viewport,new Vector2(.07f,.06f),new Vector2(.93f,.87f));
        viewport.GetComponent<Image>().color = new Color(1,1,1,.01f);
        hint.hintText.transform.SetParent(viewport,false);
        var textRect = hint.hintText.rectTransform;
        textRect.anchorMin = new Vector2(0,1); textRect.anchorMax = Vector2.one; textRect.pivot = new Vector2(.5f,1);
        textRect.anchoredPosition = Vector2.zero; textRect.sizeDelta = Vector2.zero;
        hint.hintText.fontSize = 27; hint.hintText.enableAutoSizing = false;
        hint.hintText.alignment = TextAlignmentOptions.TopLeft;
        hint.hintText.textWrappingMode = TextWrappingModes.Normal;
        var fitter = hint.hintText.GetComponent<ContentSizeFitter>() ?? hint.hintText.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var reading = viewport.GetComponent<ScrollRect>(); reading.viewport = viewport; reading.content = textRect;
        reading.horizontal = false; reading.vertical = true; reading.movementType = ScrollRect.MovementType.Clamped;
        hint.hintPanel.SetActive(false);
        var prefab = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Card.prefab");
        try
        {
            ((RectTransform)prefab.transform).sizeDelta = new Vector2(184,322);
            var subtitle = prefab.transform.Find("SubtitleText").GetComponent<TextMeshProUGUI>();
            subtitle.fontSize = 30; subtitle.enableAutoSizing = false;
            subtitle.fontStyle = FontStyles.Bold; subtitle.alignment = TextAlignmentOptions.MidlineLeft;
            subtitle.textWrappingMode = TextWrappingModes.Normal;
            subtitle.overflowMode = TextOverflowModes.Overflow;
            subtitle.margin = Vector4.zero;
            Rect(subtitle.rectTransform, new Vector2(.08f,.35f), new Vector2(.92f,.86f));
            subtitle.color = GamePalette.Text;
            var category = prefab.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
            category.gameObject.SetActive(true); category.fontSize = 18; category.enableAutoSizing = false;
            category.fontStyle = FontStyles.Normal; category.alignment = TextAlignmentOptions.TopLeft;
            category.margin = Vector4.zero;
            Rect(category.rectTransform, new Vector2(.08f,.83f), new Vector2(.92f,.97f));
            category.color = GamePalette.Text;
            prefab.GetComponent<Image>().color = GamePalette.Secondary;
            // Verify every authored title fits the fixed typography and portrait title area.
            foreach (var guid in AssetDatabase.FindAssets("t:TacticSO", new[] { "Assets/Resources/Content" }))
            {
                var data = AssetDatabase.LoadAssetAtPath<TacticSO>(AssetDatabase.GUIDToAssetPath(guid));
                string name = string.IsNullOrWhiteSpace(data.displayName) ? data.type : data.displayName;
                if (subtitle.GetPreferredValues(name, 154.56f, 1000).y > 164.22f)
                    throw new System.InvalidOperationException("Tactic title needs more space: " + name);
            }
            PrefabUtility.SaveAsPrefabAsset(prefab,"Assets/Prefabs/Card.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("Tactic Explorer built and saved.");
    }

    static void VerifyTacticFeedback(DailyPostFillBlankManager fill)
    {
        var originalSentence = fill.currentSentenceData;
        var originalCaption = fill.currentCompletedSentence;
        var originalPreview = fill.metricEngine.lastPreview;
        var sample = ScriptableObject.CreateInstance<TacticSO>();
        try
        {
            var data = JsonUtility.FromJson<DailyPostJsonDatabase>(fill.dailyPostJsonFile.text);
            fill.currentSentenceData = data.topics[0].subTopics[0].sentences[0];
            fill.currentCompletedSentence = "Caption feedback verification";
            var groups = new[] { fill.currentSentenceData.idealTacticTypes, fill.currentSentenceData.neutralTacticTypes, fill.currentSentenceData.badTacticTypes };
            var expected = new[] { PostChoiceQuality.Correct, PostChoiceQuality.HalfCorrect, PostChoiceQuality.Nonsense };
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] == null || groups[i].Count == 0) throw new System.InvalidOperationException("Missing feedback test fixture.");
                sample.type = sample.tacticId = groups[i][0]; sample.displayName = "Sample tactic";
                var quality = fill.metricEngine.PreviewTacticFit(sample, fill);
                if (quality != expected[i]) throw new System.InvalidOperationException("Tactic feedback does not match scoring.");
                var message = TacticExplorer.BuildFeedback(sample, fill, quality);
                if (!message.Contains(fill.currentCompletedSentence) || !message.Contains("doesn't mean the health claim is true"))
                    throw new System.InvalidOperationException("Feedback missing caption or learning context.");
            }
            if (fill.metricEngine.PreviewTacticFit(null, fill).HasValue || fill.metricEngine.lastPreview != originalPreview)
                throw new System.InvalidOperationException("Read-only tactic check changed preview state.");
            Debug.Log("Tactic feedback verified: strong, partial, weak, no selection, and read-only preview.");
        }
        finally
        {
            fill.currentSentenceData = originalSentence; fill.currentCompletedSentence = originalCaption;
            Object.DestroyImmediate(sample);
        }
    }

    static void VerifyCaptionCat(DailyPostFillBlankManager fill)
    {
        var originalSentence = fill.currentSentenceData;
        var originalValues = fill.currentBlankValues;
        var originalPreview = fill.metricEngine.lastPreview;
        try
        {
            var data = JsonUtility.FromJson<DailyPostJsonDatabase>(fill.dailyPostJsonFile.text);
            var sentence = data.topics[0].subTopics[0].sentences[0];
            fill.currentSentenceData = sentence;
            fill.currentBlankValues = new System.Collections.Generic.List<string> { "", "" };
            CheckCat(fill, fill.captionCatNeutral);
            fill.currentBlankValues[0] = sentence.blankScoring[0].correctWords[0];
            CheckCat(fill, fill.captionCatSmile);
            fill.currentBlankValues[1] = sentence.blankScoring[1].halfCorrectWords[0];
            CheckCat(fill, fill.captionCatUnsure);
            fill.currentBlankValues[1] = sentence.blankScoring[1].nonsenseWords[0];
            CheckCat(fill, fill.captionCatSideEye);
            fill.currentBlankValues[1] = sentence.blankScoring[1].correctWords[0];
            CheckCat(fill, fill.captionCatSmile);
            fill.currentBlankValues[0] = fill.currentBlankValues[1] = "";
            CheckCat(fill, fill.captionCatNeutral);
            Debug.Log("Caption cat verified: empty, correct, partial, nonsense, corrected and reset choices.");
        }
        finally
        {
            fill.currentSentenceData = originalSentence;
            fill.currentBlankValues = originalValues;
            fill.metricEngine.lastPreview = originalPreview;
            fill.captionCat.sprite = fill.captionCatNeutral;
        }
    }

    static void CheckCat(DailyPostFillBlankManager fill, Sprite expected)
    {
        fill.RefreshCaptionCat();
        if (fill.captionCat.sprite != expected) throw new System.InvalidOperationException("Caption cat feedback mismatch.");
    }

    static Sprite CatSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Food for Thought Images/Cat/" + name + ".png");

    static void Rect(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min; rect.anchorMax = max; rect.pivot = new Vector2(.5f,.5f);
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }
    static TextMeshProUGUI Label(Transform parent,string name,string text,float x,float y,float right,float top,float size)
    {
        var child = parent.Find(name);
        var label = child != null ? child.GetComponent<TextMeshProUGUI>() : Object.Instantiate(template,parent);
        label.name = name; label.text = text; label.fontSize = size; label.enableAutoSizing = false;
        label.color = GamePalette.Text; label.alignment = TextAlignmentOptions.TopLeft;
        label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.Normal;
        Rect(label.rectTransform,new Vector2(x,y),new Vector2(right,top));
        return label;
    }
}
