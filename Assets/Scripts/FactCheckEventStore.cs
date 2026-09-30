using System;
using UnityEngine;

/// <summary>Persists the caption and matching review when a published post triggers a fact check.</summary>
public static class FactCheckEventStore
{
    public const string SaveKey = "FactCheck_CurrentEvent";
    [Serializable] public class Scenario
    {
        public string subTopicId, title, explanation, correction;
    }
    [Serializable] public class Catalog { public Scenario[] scenarios; }
    [Serializable] public class Event
    {
        public string id, caption, topicId, subTopicId, title, explanation, correction;
        public int day;
    }
    static Catalog catalog;
    public static Event Current
    {
        get
        {
            string json = PlayerPrefs.GetString(SaveKey, "");
            return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<Event>(json);
        }
    }
    public static bool ShouldTrigger(int currentCredibility, int credibilityDelta, int day, int lastDay, int cooldown)
    {
        return currentCredibility > 0 && credibilityDelta < 0 && (long)day - lastDay >= Mathf.Max(4, cooldown);
    }
    public static Scenario Match(string subTopicId)
    {
        if (catalog == null)
        {
            var asset = Resources.Load<TextAsset>("FactChecks/scenarios");
            catalog = asset != null ? JsonUtility.FromJson<Catalog>(asset.text) : new Catalog();
        }
        if (catalog.scenarios != null)
            foreach (var scenario in catalog.scenarios)
                if (string.Equals(scenario.subTopicId, subTopicId, StringComparison.OrdinalIgnoreCase)) return scenario;
        return new Scenario { title = "Unsupported food claim", explanation = "The post makes a claim without enough supporting evidence or context.", correction = "I should have checked the evidence and explained the uncertainty before sharing this claim." };
    }
    public static void Record(PostMetricPreview post, int day)
    {
        var scenario = Match(post.subTopicId);
        var item = new Event {
            id = "post:" + day + ":" + post.captionTemplateKey,
            day = day, caption = post.selectedSentence, topicId = post.topicId, subTopicId = post.subTopicId,
            title = scenario.title, explanation = scenario.explanation, correction = scenario.correction
        };
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(item));
        PlayerPrefs.Save();
    }
    public static string CaptionExcerpt(Event item)
    {
        string caption = item.caption ?? "";
        return caption.Length <= 220 ? caption : caption.Substring(0, 217) + "…";
    }
}
