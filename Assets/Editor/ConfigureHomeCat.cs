using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ConfigureHomeCat
{
    [MenuItem("Tools/Food for Thought/Animate Home Cat")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorSceneManager.SaveOpenScenes();
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/2 Home Page.unity");
            var image = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Image>(true))
                .Single(i => i.name == "Cat");
            var idle = image.GetComponent<CatIdleExpressions>() ?? image.gameObject.AddComponent<CatIdleExpressions>();
            idle.awakeBody = Load("cat");
            idle.sleepingBody = Load("catsleep");
            idle.faces = new[] { Load("catface"), Load("catfacesideeye"), Load("catfacesideeye2"), Load("catfacesideeye3") };
            if (idle.faceOverlay == null)
            {
                var face = new GameObject("Idle Face", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                face.transform.SetParent(image.transform, false);
                idle.faceOverlay = face.GetComponent<Image>();
                idle.faceOverlay.raycastTarget = false;
                idle.faceOverlay.preserveAspect = true;
            }
            for (int i = 0; i <= idle.faces.Length; i++)
            {
                idle.ShowExpression(i);
                bool sleeping = i == idle.faces.Length;
                if (image.sprite != (sleeping ? idle.sleepingBody : idle.awakeBody) ||
                    idle.faceOverlay.gameObject.activeSelf == sleeping)
                    throw new System.InvalidOperationException("Cat pose failed: " + i);
            }
            idle.ShowExpression(0);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Home cat configured: four face expressions and sleeping pose verified.");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    private static Sprite Load(string name)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Food for Thought Images/Cat/" + name + ".png");
        if (sprite == null) throw new System.InvalidOperationException("Missing cat sprite: " + name);
        return sprite;
    }
}
