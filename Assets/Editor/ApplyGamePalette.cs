using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class ApplyGamePalette
{
    private static Sprite rounded;
    private const string MaterialFolder = "Assets/UI/Theme";

    [MenuItem("Tools/Food for Thought/Apply Sunny Yellow Palette")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before applying the palette.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        // Preserve any in-progress scene edits before changing scenes.
        EditorSceneManager.SaveOpenScenes();
        rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (rounded == null) throw new InvalidOperationException("Unity's neutral UI sprite was not found.");
        try
        {
            foreach (string path in new[] { "Assets/Scenes/1 Main Page.unity", "Assets/Scenes/2 Home Page.unity", "Assets/Scenes/3 Game.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path);
                foreach (var root in scene.GetRootGameObjects()) Style(root);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            string prefabPath = "Assets/Prefabs/Card.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try { Style(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            ImproveCaptionReadability.Apply();
            AssetDatabase.SaveAssets();
            Debug.Log("Sunny Yellow applied to all three scenes and the tactic card prefab.");
            File.WriteAllText("/private/tmp/nutrition-palette-result.txt", "PASS: all three scenes and Card prefab styled and saved.");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    private static string PathOf(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path.ToLowerInvariant();
    }

    private static bool IconButton(Button button)
    {
        string name = button.name.ToLowerInvariant();
        return name.StartsWith("back") || name.StartsWith("exit") || name.StartsWith("close") ||
            name.StartsWith("undo") || name.Contains("mute") || name == "lock" || name == "unlock";
    }

    private static bool Primary(Button button)
    {
        string n = button.name.ToLowerInvariant();
        return n.Contains("postbutton") || n.Contains("publish") || n.Contains("confirm") ||
            n.Contains("next button") || n.Contains("newbutton") || n.Contains("continuebutton") ||
            n.Contains("claimreward") || n.Contains("revise");
    }

    private static void Surface(Image image, Color color, bool round = true)
    {
        image.color = color;
        image.material = null;
        image.sprite = round ? rounded : null;
        image.type = round ? Image.Type.Sliced : Image.Type.Simple;
    }

    private static Material TextMaterial(Material original)
    {
        if (original == null) return null;
        string originalPath = AssetDatabase.GetAssetPath(original);
        if (originalPath.StartsWith(MaterialFolder + "/")) return original;
        // Preserve each font atlas while removing the old face colors/outlines.
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original, out string guid, out long localId);
        string path = MaterialFolder + "/Text-" + guid + "-" + localId + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(original);
            material.name = "Game theme - " + original.name;
            if (material.HasProperty("_FaceColor")) material.SetColor("_FaceColor", Color.white);
            if (material.HasProperty("_OutlineWidth")) material.SetFloat("_OutlineWidth", 0);
            material.DisableKeyword("OUTLINE_ON");
            material.DisableKeyword("UNDERLAY_ON");
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }

    private static Color TextColor(Transform transform, Button button)
    {
        if (button != null && (Primary(button) || IconButton(button))) return GamePalette.OnPrimary;
        string path = PathOf(transform);
        if (path.Contains("mainmenupanel/image/")) return GamePalette.OnHighlight;
        if (path.Contains("cashpanel/") || path.Contains("day panel/")) return GamePalette.OnHighlight;
        return GamePalette.Text;
    }

    private static void Style(GameObject root)
    {
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            string n = image.name.ToLowerInvariant();
            string path = PathOf(image.transform);
            var button = image.GetComponent<Button>();
            if (n == "phoneframe" || n.Contains("persona") || n == "cat" || n.Contains("coinimg") || n.Contains("followersimg") ||
                n.Contains("credibilityimg") || n == "likes" || n == "shares" || n.EndsWith("image") && n != "image" && n != "selectionoutlineimage")
                continue;
            if (n == "selectionoutlineimage") { image.color = new Color(1,1,1,0.12f); continue; }
            if (button != null && button.targetGraphic == image)
            {
                if (IconButton(button)) { image.color = GamePalette.Primary; image.material = null; }
                else Surface(image, Primary(button) ? GamePalette.Primary : GamePalette.Secondary);
                continue;
            }
            if (n == "canvas") { Surface(image, GamePalette.Background, false); continue; }
            if (n == "screenmask" || n == "viewport") continue;
            if (n == "handle" || n == "fill") { Surface(image, GamePalette.Primary); continue; }
            if (n.Contains("scrollbar")) { Surface(image, GamePalette.Secondary); continue; }
            if (n == "cashpanel" || n == "day panel") { Surface(image, GamePalette.Highlight); continue; }
            if (n == "followerpanel" || n == "credibilitypanel") { Surface(image, GamePalette.Secondary); continue; }
            if (n == "card" || n == "cardback") { Surface(image, GamePalette.Secondary); continue; }
            bool builtin = image.sprite == null || AssetDatabase.GetAssetPath(image.sprite).StartsWith("Resources/unity_builtin");
            bool panel = n.Contains("panel") || n.Contains("background") || n == "bg" || n == "comments" || n == "tactictscroll";
            if (panel || (n == "image" && builtin))
            {
                if (image.color.a < 0.6f && panel && n != "pre-ending panel")
                { Surface(image, new Color(GamePalette.Text.r, GamePalette.Text.g, GamePalette.Text.b,0.65f), false); }
                else if (path.Contains("coach") || path.Contains("hint panel")) Surface(image, GamePalette.Coach);
                else if (n == "mainmenupanel" || n == "socialfeedpanel" || n == "tacticpanel" || n == "coachpanel" ||
                    n == "topic panel" || n == "sub topic panel" || n == "sentence selection panel" || n == "fill in blank panel" || n == "loading panel")
                    Surface(image, GamePalette.Background, false);
                else Surface(image, GamePalette.Surface);
            }
        }
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            // Image carries the palette; transition colors are multipliers.
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f,0.90f,1f,1);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.8f,0.73f,0.88f,1);
            colors.disabledColor = new Color(0.65f,0.6f,0.7f,0.75f);
            colors.colorMultiplier = 1;
            button.colors = colors;
        }
        foreach (var label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            var button = label.GetComponentInParent<Button>(true);
            label.color = TextColor(label.transform, button);
            label.enableVertexGradient = false;
            label.fontSharedMaterial = TextMaterial(label.fontSharedMaterial);
        }
        foreach (var label in root.GetComponentsInChildren<Text>(true))
        {
            var button = label.GetComponentInParent<Button>(true);
            label.color = TextColor(label.transform, button);
        }
        foreach (var effect in root.GetComponentsInChildren<Shadow>(true)) effect.enabled = false;
        foreach (var comments in root.GetComponentsInChildren<CommentManager>(true))
        {
            var so = new SerializedObject(comments);
            so.FindProperty("_usernameColor1").colorValue = GamePalette.Primary;
            so.FindProperty("_usernameColor2").colorValue = GamePalette.Text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
