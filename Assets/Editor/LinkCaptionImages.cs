using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LinkCaptionImages
{
    [MenuItem("Tools/Food for Thought/Link All Caption Images")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var paths = Directory.GetFiles("Assets/Food for Thought Images/Captions", "*.png", SearchOption.AllDirectories);
        var links = new List<CaptionImageLink>();
        foreach (var rawPath in paths)
        {
            var path = rawPath.Replace('\\', '/');
            var captionId = Path.GetFileNameWithoutExtension(path);
            if (captionId == "dirty_fear_01")
                path = "Assets/Food for Thought Images/Food Safety/Kitchen Hygiene/1/Washing_strawberries_under_running_water.png";
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new System.InvalidOperationException("Could not import " + path);
            links.Add(new CaptionImageLink { captionTemplateId = captionId, image = sprite });
        }
        if (links.Select(l => l.captionTemplateId).Distinct().Count() != links.Count)
            throw new System.InvalidOperationException("Duplicate caption image IDs.");
        EditorSceneManager.SaveOpenScenes();
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/3 Game.unity");
            var manager = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TacticManager>(true)).Single();
            manager.captionImages ??= new List<CaptionImageLink>();
            foreach (var link in links)
            {
                // Keep existing caption art; generated artwork only fills missing slots.
                var existing = manager.captionImages.Find(x => x != null &&
                    x.captionTemplateId == link.captionTemplateId && x.image != null);
                if (existing != null && !AssetDatabase.GetAssetPath(existing.image)
                    .StartsWith("Assets/Food for Thought Images/Captions/"))
                    link.image = existing.image;
                manager.captionImages.RemoveAll(x => x != null && x.captionTemplateId == link.captionTemplateId);
                manager.captionImages.Add(link);
                if (manager.ResolvePostImage(link.captionTemplateId, null) != link.image)
                    throw new System.InvalidOperationException("Image resolution failed for " + link.captionTemplateId);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Linked and verified {links.Count} caption images.");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
}
