using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VerifyCoachLayout
{
    [MenuItem("Tools/Food for Thought/Verify Coach Layout")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/3 Game.unity");
        try
        {
            var coach = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CatCoachManager>(true)).Single();
            var body = coach.coachBodyText;
            typeof(CatCoachManager).GetMethod("ShowCoachPanel", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(coach,
                new object[] { new List<string> {
                    "Blank 1: raw milk — Does not fit. Raw milk means unpasteurized, so it contradicts Pasteurized before this blank. " + new string('x', 160),
                    "This tactic does not match the caption’s main persuasion tactic. Try comparing it with another card.",
                    "Repeated patterns have less effect.", "A fact check is expected."
                } });
            Canvas.ForceUpdateCanvases();
            body.rectTransform.ForceUpdateRectTransforms();
            var parent = (RectTransform)body.transform.parent;
            float bottom = body.rectTransform.anchorMin.y * parent.rect.height + body.rectTransform.offsetMin.y;
            foreach (var button in new[] { coach.reviseButton, coach.continueAnywayButton })
            {
                var rect = (RectTransform)button.transform;
                float top = rect.anchorMax.y * parent.rect.height + rect.offsetMax.y;
                if (bottom <= top + 16) throw new Exception("Coach text intersects button row");
            }
            if (body.text.Contains("Repeated patterns")) throw new Exception("Coach includes too many reasons");
            float max = body.fontSizeMax;
            body.enableAutoSizing = false;
            body.fontSize = body.fontSizeMin;
            var required = body.GetPreferredValues(body.text, body.rectTransform.rect.width, 10000);
            if (required.y > body.rectTransform.rect.height) throw new Exception("Coach message does not fit at minimum readable font size");
            body.fontSizeMax = max;
            Debug.Log("COACH_LAYOUT_VERIFICATION_PASSED: text fits above separate button row.");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
