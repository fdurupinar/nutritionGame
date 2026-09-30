using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LinkFoodSafetyCaptionImage
{
    [MenuItem("Tools/Food for Thought/Link Strawberry Caption Image")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        const string path = "Assets/Food for Thought Images/Food Safety/Kitchen Hygiene/1/Washing_strawberries_under_running_water.png";
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new System.InvalidOperationException("Strawberry sprite did not import.");
        EditorSceneManager.SaveOpenScenes();
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/3 Game.unity");
            var manager = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TacticManager>(true)).Single();
            manager.captionImages ??= new System.Collections.Generic.List<CaptionImageLink>();
            manager.captionImages.RemoveAll(x => x != null && x.captionTemplateId == "dirty_fear_01");
            manager.captionImages.Add(new CaptionImageLink { captionTemplateId = "dirty_fear_01", image = sprite });
            var tactic = manager.dayConfigs.SelectMany(d => d.tacticsForThisDay).First(t => t != null);
            if (manager.ResolvePostImage("dirty_fear_01", tactic) != sprite ||
                manager.ResolvePostImage("unmapped_caption", tactic) != tactic.tacticImage ||
                manager.ResolvePostImage(null, tactic) != tactic.tacticImage)
                throw new System.InvalidOperationException("Caption image resolution check failed.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Strawberry caption image linked. Caption override and fallback checks passed.");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
}
