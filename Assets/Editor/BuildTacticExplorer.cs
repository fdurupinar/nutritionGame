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
        var panel = manager.TacticPanel.transform;
        template = panel.Find("SetenceTitle").GetComponent<TextMeshProUGUI>();
        template.text = "Explore the tactics";
        Rect(template.rectTransform, new Vector2(.18f,.90f), new Vector2(.92f,.96f));
        template.fontSize = 38;
        template.enableAutoSizing = true;
        template.fontSizeMin = 26;
        template.fontSizeMax = 38;
        var intro = Label(panel, "ExplorerIntro", "How does your post persuade? Compare tactics, then choose the one you see in your caption.", .08f,.79f,.92f,.90f,28);
        var caption = Label(panel, "ExplorerCaption", "<b>YOUR POST</b>\nYour completed caption will appear here.", .08f,.63f,.92f,.79f,26);
        caption.overflowMode = TextOverflowModes.Ellipsis;
        var status = Label(panel, "ExplorerSelection", "Tap a card, then explore how it works.", .08f,.105f,.92f,.18f,25);
        Rect((RectTransform)panel.Find("Viewport"),new Vector2(.06f,.19f),new Vector2(.92f,.62f));
        Rect((RectTransform)panel.Find("Scrollbar Vertical"),new Vector2(.93f,.19f),new Vector2(.95f,.62f));
        var content = panel.Find("Viewport/Content").GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0,1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f,1); content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
        var grid = content.GetComponent<GridLayoutGroup>();
        grid.constraintCount = 3; grid.cellSize = new Vector2(190,250);
        grid.spacing = new Vector2(18,18); grid.padding = new RectOffset(8,8,8,8); grid.childAlignment = TextAnchor.UpperCenter;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = panel.GetComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        var publish = panel.Find("Confirm Tactic Button").GetComponent<Button>();
        Rect((RectTransform)publish.transform,new Vector2(.52f,.035f),new Vector2(.92f,.09f));
        publish.GetComponentInChildren<TextMeshProUGUI>().text = "Publish post";
        var hint = manager.tacticHintPanel;
        hint.captionManager = manager.dailyPostFillBlankManager;
        Rect((RectTransform)hint.hintButton.transform,new Vector2(.08f,.035f),new Vector2(.48f,.09f));
        var image = hint.hintButton.GetComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced; image.color = GamePalette.Secondary;
        foreach(var childImage in hint.hintButton.GetComponentsInChildren<Image>(true))
            if(childImage != image) childImage.enabled = false;
        var hintLabel = hint.hintButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (hintLabel == null) hintLabel = Label(hint.hintButton.transform,"ExploreLabel","Explore tactic",0,0,1,1,26);
        hintLabel.text = "Explore tactic"; hintLabel.color = GamePalette.Text;
        hintLabel.alignment = TextAlignmentOptions.Center;
        Rect(hintLabel.rectTransform,Vector2.zero,Vector2.one);
        var explorer = panel.GetComponent<TacticExplorer>() ?? panel.gameObject.AddComponent<TacticExplorer>();
        explorer.manager = manager; explorer.captionText = caption; explorer.selectionText = status; explorer.publishButton = publish;
        explorer.Refresh();
        // The explanation scrolls so long authored descriptions stay readable.
        Rect(hint.hintPanelRect,new Vector2(.05f,.12f),new Vector2(.95f,.9f));
        hint.hintPanel.GetComponent<Image>().color = GamePalette.Coach;
        hint.hintPanel.GetComponent<Image>().raycastTarget = true;
        var title = hint.hintPanel.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t=>t.name == "HintTitle");
        if(title != null) { title.transform.SetParent(hint.hintPanel.transform,false); title.transform.SetAsLastSibling(); title.text = "Inside the tactic"; title.fontSize = 32; title.enableAutoSizing = false; title.alignment = TextAlignmentOptions.TopLeft; Rect(title.rectTransform,new Vector2(.08f,.89f),new Vector2(.84f,.97f)); }
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
            var subtitle = prefab.transform.Find("SubtitleText").GetComponent<TextMeshProUGUI>();
            subtitle.fontSize = 24; subtitle.enableAutoSizing = true; subtitle.fontSizeMin = 16; subtitle.fontSizeMax = 24;
            PrefabUtility.SaveAsPrefabAsset(prefab,"Assets/Prefabs/Card.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("Tactic Explorer built and saved.");
    }

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
