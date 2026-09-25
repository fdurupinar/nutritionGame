using TMPro;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ImproveCaptionReadability
{
    [MenuItem("Tools/Food for Thought/Improve Caption Readability")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        foreach(var path in new[] {"Assets/Scripts/Yanyan/Prefabs/SubTopics.prefab", "Assets/Scripts/Yanyan/Prefabs/Sentences.prefab", "Assets/Scripts/Yanyan/Prefabs/Word choice.prefab"})
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    label.color = GamePalette.OnHighlight;
                    label.enableVertexGradient = false;
                    var original = label.fontSharedMaterial;
                    string assetPath = "Assets/UI/Theme/Raised-" + label.font.name.Replace("/","-") + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                    if(material == null)
                    {
                        material = new Material(original);
                        material.SetColor("_FaceColor",Color.white);
                        material.SetFloat("_OutlineWidth",0);
                        material.DisableKeyword("OUTLINE_ON");
                        material.EnableKeyword("UNDERLAY_ON");
                        material.SetColor("_UnderlayColor",new Color(.12f,.20f,.18f,.32f));
                        material.SetFloat("_UnderlayOffsetX",.3f);
                        material.SetFloat("_UnderlayOffsetY",-.3f);
                        material.SetFloat("_UnderlayDilate",0);
                        material.SetFloat("_UnderlaySoftness",.15f);
                        AssetDatabase.CreateAsset(material,assetPath);
                    }
                    label.fontSharedMaterial = material;
                    label.enableAutoSizing = false;
                    label.fontSize = path.Contains("Word choice") ? 8 : path.Contains("Sentences") ? 10 : 15;
                    label.textWrappingMode = TextWrappingModes.Normal;
                    foreach(var shadow in label.GetComponents<UnityEngine.UI.Shadow>()) shadow.enabled = false;
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Caption and word-choice text now uses dark faces with subtle raised shadows.");
    }
}
