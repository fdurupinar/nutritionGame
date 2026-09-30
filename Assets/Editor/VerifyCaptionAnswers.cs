using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VerifyCaptionAnswers
{
    [MenuItem("Tools/Food for Thought/Verify Caption Answers")]
    public static void Run()
    {
        var data = JsonUtility.FromJson<DailyPostJsonDatabase>(File.ReadAllText("Assets/Scripts/Yanyan/Save/DailyPostData.json"));
        var captions = data.topics.SelectMany(t => t.subTopics).SelectMany(t => t.sentences).ToArray();
        Check(captions.Length == 54, "54 captions retained");
        // Exercise Unity's actual serializer, including explanations edited in the JSON window.
        var roundTrip = JsonUtility.FromJson<DailyPostJsonDatabase>(JsonUtility.ToJson(data));
        Check(roundTrip.topics[0].subTopics[0].sentences[0].blankScoring[0].wordFeedback.Count > 0, "Feedback survives serialization");
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Caption answer verification");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            var engine = root.AddComponent<MisinformationMetricEngine>();
            engine.formulaProfile = AssetDatabase.LoadAssetAtPath<MisinformationMetricFormulaProfile>("Assets/Scripts/Yanyan/Formula/MisinformationMetricFormulaProfile.asset");
            var fill = root.AddComponent<DailyPostFillBlankManager>();
            var coach = root.AddComponent<CatCoachManager>();
            var warning = typeof(CatCoachManager).GetMethod("BuildCaptionWarningReasons", BindingFlags.Instance | BindingFlags.NonPublic);
            // Regressions that motivated the review, independent of the data's own labels.
            foreach (var sample in new[] {
                ("rawmilk_fear_01", 0, "milk", PostChoiceQuality.Correct),
                ("rawmilk_fear_01", 0, "raw milk", PostChoiceQuality.Nonsense),
                ("rawmilk_fear_01", 1, "benefits", PostChoiceQuality.Correct),
                ("rawmilk_fear_01", 1, "nutrition", PostChoiceQuality.Correct),
                ("rawmilk_social_03", 0, "milk", PostChoiceQuality.HalfCorrect),
                ("rawmilk_social_03", 1, "better", PostChoiceQuality.Correct),
                ("diabetes_social_03", 0, "blood sugar", PostChoiceQuality.Correct),
                ("gummies_conspiracy_02", 1, "checkups", PostChoiceQuality.Correct),
                ("fatburn_social_03", 1, "progress", PostChoiceQuality.Correct),
                ("fasting_social_03", 1, "late", PostChoiceQuality.Nonsense)
            })
            {
                fill.currentSentenceData = captions.Single(c => c.sentenceId == sample.Item1);
                fill.currentBlankValues = new List<string>(fill.currentSentenceData.blankWords);
                fill.currentBlankValues[sample.Item2] = sample.Item3;
                Check(engine.PreviewCaptionChoices(fill).wordQualities[sample.Item2] == sample.Item4,
                    "Reviewed regression: " + sample.Item1 + " / " + sample.Item3);
            }
            fill.currentSentenceData = captions.Single(c => c.sentenceId == "negative_social_03");
            var interchangeable = new[] { "ice water", "cold water", "water", "celery", "cucumber", "vegetables" };
            foreach (var first in interchangeable)
            foreach (var second in interchangeable)
            {
                if (first == second) continue; // The word bank uses each choice once.
                fill.currentBlankValues = new List<string> { first, second };
                var preview = engine.PreviewCaptionChoices(fill);
                Check(preview.wordQualities.All(q => q == PostChoiceQuality.Correct), "Interchangeable pair: " + first + " / " + second);
                Check(((List<string>)warning.Invoke(coach, new object[] { preview })).Count == 0, "No revision warning for interchangeable answers");
            }
            int choices = 0;
            foreach (var caption in captions)
            {
                Check(Regex.Matches(caption.sentence, @"\[([^]]+)\]").Cast<Match>().Select(m => m.Groups[1].Value).SequenceEqual(caption.blankWords), caption.sentenceId + " brackets");
                fill.currentSentenceData = caption;
                foreach (var first in caption.blankScoring[0].correctWords)
                foreach (var second in caption.blankScoring[1].correctWords)
                {
                    if (first == second) continue;
                    fill.currentBlankValues = new List<string> { first, second };
                    var pair = engine.PreviewCaptionChoices(fill);
                    Check(pair.wordQualities.All(q => q == PostChoiceQuality.Correct), caption.sentenceId + " full-credit pair");
                    Check(((List<string>)warning.Invoke(coach, new object[] { pair })).Count == 0, "No warning for full-credit pair");
                }
                fill.currentBlankValues = new List<string>(caption.blankWords);
                for (int i = 0; i < caption.blankScoring.Count; i++)
                {
                    var rule = caption.blankScoring[i];
                    var groups = new[] { rule.correctWords, rule.halfCorrectWords, rule.nonsenseWords };
                    var all = groups.SelectMany(g => g).ToArray();
                    Check(all.Distinct(StringComparer.OrdinalIgnoreCase).Count() == all.Length, caption.sentenceId + " overlapping groups");
                    Check(rule.neutralWords.Count == 0 && rule.wrongWords.Count == 0, "Legacy groups migrated");
                    Check(rule.correctWords.Contains(caption.blankWords[i]), "Canonical answer fits");
                    foreach (var word in all) Check(caption.wordChoices.Contains(word), "Answer in word bank: " + word);
                    foreach (var word in caption.wordChoices)
                    {
                        fill.currentBlankValues = new List<string>(caption.blankWords);
                        fill.currentBlankValues[i] = word;
                        var expected = rule.correctWords.Contains(word) ? PostChoiceQuality.Correct :
                            rule.halfCorrectWords.Contains(word) ? PostChoiceQuality.HalfCorrect : PostChoiceQuality.Nonsense;
                        var preview = engine.PreviewCaptionChoices(fill);
                        Check(preview.wordQualities[i] == expected, caption.sentenceId + " blank " + i + ": " + word);
                        var reasons = (List<string>)warning.Invoke(coach, new object[] { preview });
                        Check((reasons.Count > 0) == (expected == PostChoiceQuality.Nonsense), "Only non-fitting words interrupt: " + word);
                        if (expected != PostChoiceQuality.Correct && rule.wordFeedback.Any(f => f.word == word))
                            Check(preview.wordFitFeedback[i].Contains(rule.wordFeedback.First(f => f.word == word).reason), "Specific feedback copied into preview");
                        choices++;
                    }
                }
                fill.currentBlankValues = new List<string>(caption.blankWords);
                var good = engine.PreviewCaptionChoices(fill);
                good.combinedQuality01 = 0; // A poor tactic must not make good words fail the caption check.
                Check(((List<string>)warning.Invoke(coach, new object[] { good })).Count == 0, "Tactic score does not blame caption words");
            }
            Debug.Log("CAPTION_ANSWERS_VERIFICATION_PASSED: " + captions.Length + " captions, " + choices + " word/blank checks.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
