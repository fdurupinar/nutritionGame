using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class StyleJobPosts
{
    static Sprite rounded;
    static TMP_Text template;

    [MenuItem("Tools/Food for Thought/Style Job Posts")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/2 Home Page.unity");
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        var manager = all.Select(t => t.GetComponent<JobPostManager>()).First(m => m != null);
        template = all.Select(t => t.GetComponent<TextMeshProUGUI>()).First(t => t != null && t.text.Trim() == "Daily Post");
        rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        var panel = manager.jobPanel.transform;
        Place(panel, 0, 0, 1, 1);
        Paint(panel, GamePalette.Background, true);
        var content = manager.jobPanelRect;
        Place(content, .08f, .12f, .92f, .69f);
        content.GetComponent<Image>().enabled = false;
        Label(panel, "JobEyebrow", "PAID COLLABS", .08f, .87f, .72f, .92f, 23, true);
        Label(panel, "JobHeading", "Choose your next gig", .08f, .78f, .92f, .86f, 40, true);
        Label(panel, "JobIntro", "Pick one job. Collect your reward after the listed game days.", .08f, .70f, .92f, .77f, 24, false);
        Label(panel, "JobFooter", "One active job at a time. Unfinished offers stay available.", .08f, .035f, .92f, .10f, 22, false);
        for (int i = 0; i < manager.jobOfferSlots.Count; i++)
        {
            var slot = manager.jobOfferSlots[i];
            var card = slot.rootObject.transform;
            Place(card, 0, i == 0 ? .53f : 0, 1, i == 0 ? 1 : .47f);
            Paint(card, GamePalette.Secondary, false);
            var oldArt = card.Find("Image");
            if (oldArt != null) oldArt.gameObject.SetActive(false);
            var accent = card.Find("JobAccent");
            if (accent == null) { accent = new GameObject("JobAccent", typeof(RectTransform), typeof(Image)).transform; accent.SetParent(card, false); }
            Place(accent, .07f, .96f, .23f, .975f); Paint(accent, GamePalette.Primary, false);
            Text(slot.jobText, .07f, .68f, .93f, .92f, 30, true);
            Text(slot.rewardText, .07f, .48f, .93f, .63f, 25, true);
            Text(slot.dayText, .07f, .29f, .93f, .45f, 22, false);
            Place(slot.acceptButton.transform, .07f, .06f, .93f, .23f);
            Paint(slot.acceptButton.transform, GamePalette.Primary, true);
            var buttonLabel = slot.acceptButton.GetComponentInChildren<TextMeshProUGUI>(true);
            buttonLabel.text = "Accept job";
            Text(buttonLabel, .04f, .05f, .96f, .95f, 25, true);
            buttonLabel.alignment = TextAlignmentOptions.Center;
            // Keep the authored preview representative; gameplay replaces these values.
            var job = manager.jobs[i];
            slot.jobText.text = job.jobText;
            slot.rewardText.text = "Reward: $" + job.rewardMoney;
            slot.dayText.text = "Collect reward " + job.workDays + " game days after accepting";
        }
        var back = panel.Find("Exit Button");
        Place(back, .80f, .89f, .92f, .96f); Paint(back, GamePalette.Secondary, true);
        foreach (var child in back.GetComponentsInChildren<Image>(true))
            if (child.transform != back) child.gameObject.SetActive(false);
        var backLabel = Label(back, "CloseLabel", "Back", .04f, .06f, .96f, .94f, 22, true);
        backLabel.alignment = TextAlignmentOptions.Center;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("JOB_POST_STYLE_APPLIED");
    }

    static void Place(Transform t, float x, float y, float r, float top)
    {
        var rect = (RectTransform)t;
        rect.localScale = Vector3.one;
        rect.anchorMin = new Vector2(x, y); rect.anchorMax = new Vector2(r, top);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    static void Paint(Transform t, Color color, bool blocks)
    {
        var image = t.GetComponent<Image>() ?? t.gameObject.AddComponent<Image>();
        image.enabled = true; image.sprite = rounded; image.type = Image.Type.Sliced;
        image.color = color; image.material = null; image.raycastTarget = blocks;
        var button = t.GetComponent<Button>();
        if (button == null) return;
        button.targetGraphic = image; button.transition = Selectable.Transition.ColorTint;
        var colors = ColorBlock.defaultColorBlock;
        colors.highlightedColor = new Color(1f, .97f, .88f);
        colors.pressedColor = new Color(.88f, .84f, .75f);
        button.colors = colors;
    }
    static TMP_Text Label(Transform parent, string name, string value, float x, float y, float r, float top, float size, bool bold)
    {
        var existing = parent.Find(name);
        var text = existing != null ? existing.GetComponent<TMP_Text>() : Object.Instantiate(template, parent);
        text.name = name; text.text = value; text.gameObject.SetActive(true);
        Text(text, x, y, r, top, size, bold);
        return text;
    }
    static void Text(TMP_Text text, float x, float y, float r, float top, float size, bool bold)
    {
        text.font = template.font; text.fontSharedMaterial = template.fontSharedMaterial;
        text.color = GamePalette.Text; text.enableVertexGradient = false; text.raycastTarget = false;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.enableAutoSizing = true; text.fontSizeMin = size - 3; text.fontSizeMax = size;
        text.margin = Vector4.zero; text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        Place(text.transform, x, y, r, top);
    }
}
