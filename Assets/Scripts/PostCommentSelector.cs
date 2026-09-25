using System;
using System.Collections.Generic;
using System.Linq;

public sealed class PostCommentContext
{
    public string tacticType;
    public string tacticId;
    public string tacticName;
    public string topicId;
    public string topicName;
    public string subTopicId;
    public string subTopicName;
    public string captionTemplateId;
    public string completedCaption;
    public PostMetricPreview metrics;
}

public static class PostCommentSelector
{
    public static List<CommentLineSO> Select(IEnumerable<CommentLineSO> pool, PostCommentContext context, int limit)
    {
        if (context == null || pool == null || limit <= 0)
            return new List<CommentLineSO>();

        // Shuffle ties, then prefer caption/subtopic/topic-specific authored reactions.
        var candidates = pool.Where(c => Matches(c, context)).Distinct()
            .GroupBy(c => string.IsNullOrWhiteSpace(c.id) ? (object)c : c.id)
            .Select(g => g.First()).ToList();
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var swap = candidates[i];
            candidates[i] = candidates[j];
            candidates[j] = swap;
        }
        candidates = candidates.OrderByDescending(Specificity).ToList();
        var result = new List<CommentLineSO>();

        // Ensure the audience acknowledges the assessed outcome, even when many
        // caption-specific comments exist. This does not alter scoring or saves.
        var reaction = candidates.FirstOrDefault(c => c.reaction != CommentReaction.Any);
        if (reaction != null)
        {
            result.Add(reaction);
            candidates.Remove(reaction);
        }
        result.AddRange(candidates.Take(limit - result.Count));
        return result;
    }

    private static bool Matches(CommentLineSO comment, PostCommentContext context)
    {
        if (comment == null || string.IsNullOrWhiteSpace(comment.text)) return false;
        if (!string.IsNullOrWhiteSpace(comment.type) &&
            !Same(comment.type, context.tacticType) && !Same(comment.type, context.tacticId)) return false;
        if (!OptionalMatch(comment.topicId, context.topicId) ||
            !OptionalMatch(comment.subTopicId, context.subTopicId) ||
            !OptionalMatch(comment.captionTemplateId, context.captionTemplateId)) return false;

        if (comment.reaction == CommentReaction.Any) return true;
        if (context.metrics == null) return false;
        switch (comment.reaction)
        {
            case CommentReaction.ClearCaption:
                return !context.metrics.wrongWordChoice && !context.metrics.wrongTactic;
            case CommentReaction.ConfusingWords:
                return context.metrics.wrongWordChoice;
            case CommentReaction.PoorTacticFit:
                return context.metrics.wrongTactic;
            default:
                return false;
        }
    }

    private static int Specificity(CommentLineSO comment)
    {
        return (!string.IsNullOrWhiteSpace(comment.captionTemplateId) ? 16 : 0)
            + (!string.IsNullOrWhiteSpace(comment.subTopicId) ? 8 : 0)
            + (!string.IsNullOrWhiteSpace(comment.topicId) ? 4 : 0)
            + (comment.reaction != CommentReaction.Any ? 2 : 0)
            + (!string.IsNullOrWhiteSpace(comment.type) ? 1 : 0);
    }

    private static bool OptionalMatch(string filter, string value)
    {
        return string.IsNullOrWhiteSpace(filter) || Same(filter, value);
    }

    private static bool Same(string a, string b)
    {
        return string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static string RenderText(CommentLineSO comment, PostCommentContext context)
    {
        // Replace only placeholders in the authored template, not tokens inside
        // substituted caption text. Escape markup so captions cannot change UI styling.
        return System.Text.RegularExpressions.Regex.Replace(comment.text,
            @"\{(topic|subtopic|caption|tactic)\}", match =>
            {
                string value;
                switch (match.Groups[1].Value)
                {
                    case "topic": value = context.topicName; break;
                    case "subtopic": value = context.subTopicName; break;
                    case "caption": value = context.completedCaption; break;
                    default: value = context.tacticName; break;
                }
                return (value ?? "").Replace("<", "&lt;").Replace(">", "&gt;");
            });
    }
}
