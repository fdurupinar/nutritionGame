// Assets/Editor/NutriGameCsvImporter.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class NutriGameCsvImporter {
    // ---- Config ----
    private const string CsvFolder = "Assets/Content/CSV"; // put your CSVs here
    private const string OutFolder = "Assets/Resources/Content";
    private static readonly CultureInfo CI = CultureInfo.InvariantCulture;

    [MenuItem("FFT/Import All CSVs")]
    public static void ImportAll() {
        EnsureDirs();

        ImportAudiences(Path.Combine(CsvFolder, "audiences.csv"));
        ImportMyths(Path.Combine(CsvFolder, "myths.csv"));
        ImportTactics(Path.Combine(CsvFolder, "tactics.csv"));
        ImportFormats(Path.Combine(CsvFolder, "formats.csv"));
        ImportAvatars(Path.Combine(CsvFolder, "avatars.csv"));
        ImportComments(Path.Combine(CsvFolder, "comments.csv"));
        ImportQuests(Path.Combine(CsvFolder, "quests.csv"));
        ImportDebrief(Path.Combine(CsvFolder, "debrief_cards.csv"));
        ImportLocalization(Path.Combine(CsvFolder, "localization_en.csv"));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CSV import complete.");
    }

    // ---------- Importers ----------
    private static void ImportMyths(string file) {
        if(!Check(file)) return;
        var rows = Csv.Read(file);
        foreach(var r in rows) {
            var so = LoadOrCreate<MythSO>(Path.Combine(OutFolder, "Myths"), r["id"]);
            so.id = r["id"];
            so.title = r["title"];
            so.difficulty = ParseInt(r["difficulty"], 1);
            so.baseReach = ParseFloat(r["baseReach"], 1000f);
            so.susceptibleAudiences = SplitPipe(r["susceptibleAudiences"]);
            so.counterFacts = SplitPipe(r["counterFacts"]);
            so.shortDebunk = r.GetOrDefault("shortDebunk");
            so.tags = SplitPipe(r.GetOrDefault("tags"));
            so.enabledFlag = ParseBool(r.GetOrDefault("enabled"), true);
            EditorUtility.SetDirty(so);
        }
    }

    private static void ImportTactics(string file) {
        if(!Check(file)) return;
        var rows = Csv.Read(file);
        foreach(var r in rows) {
            

            var so = LoadOrCreate<TacticSO>(Path.Combine(OutFolder, "Tactics"), r["id"]);

            so.id = r["id"];
            so.displayName = r["displayName"];
            so.type = EnumParse(r["type"], TacticType.Fear);
            so.engagementBonus = ParseFloat(r["engagementBonus"], 0f);
            so.credibilityCost = ParseFloat(r["credibilityCost"], 0f);
            so.synergies = SplitPipe(r.GetOrDefault("synergies"));
            so.tags = SplitPipe(r.GetOrDefault("tags"));
            so.enabledFlag = ParseBool(r.GetOrDefault("enabled"), true);

            Debug.Log(r.GetOrDefault("tacticImagePath", "").Trim());
            so.tacticImage = AssetDatabase.LoadAssetAtPath<Sprite>(r.GetOrDefault("tacticImagePath", "").Trim());
            
           
            EditorUtility.SetDirty(so);
        }
    }

    private static void ImportFormats(string file) {
        if(!Check(file)) return;
        var rows = Csv.Read(file);
        foreach(var r in rows) {
            var so = LoadOrCreate<FormatSO>(Path.Combine(OutFolder, "Formats"), r["id"]);
            so.id = r["id"];
            so.name = r["name"];
            so.enabledFlag = ParseBool(r.GetOrDefault("enabled"), true);
            so.audienceMultipliers.Clear();
            foreach(var pair in SplitPipe(r["audienceMultipliers"])) {
                // pair like "teens:0.10"
                var kv = pair.Split(':');
                if(kv.Length != 2) continue;
                so.audienceMultipliers.Add(new AudienceMultiplier {
                    audienceId = kv[0].Trim(),
                    multiplier = ParseFloat(kv[1], 0f)
                });
            }
            EditorUtility.SetDirty(so);
        }
    }

    private static void ImportAudiences(string file) {
        if(!Check(file)) return;
        var rows = Csv.Read(file);
        foreach(var r in rows) {
            var so = LoadOrCreate<AudienceSO>(Path.Combine(OutFolder, "Audiences"), r["id"]);
            so.id = r["id"];
            so.displayName = r["displayName"];
            so.emotionality = ParseFloat(r["emotionality"], 0.5f);
            so.skepticism = ParseFloat(r["skepticism"], 0.5f);
            so.interests = SplitPipe(r.GetOrDefault("interests"));
            so.enabledFlag = ParseBool(r.GetOrDefault("enabled"), true);
            EditorUtility.SetDirty(so);
        }
    }

    private static void ImportAvatars(string file) {
        if(!Check(file)) return;
        var rows = Csv.Read(file);
        foreach(var r in rows) {
            var so = LoadOrCreate<AvatarSO>(Path.Combine(OutFolder, "Avatars"), r["id"]);
            so.id = r["id"];
            so.name = r["name"];
            so.cosmeticSet = r.GetOrDefault("cosmeticSet");
            so.enabledFlag = ParseBool(r.GetOrDefault("enabled"), true);

            so.perk_tacticTypeBonus.Clear();
            foreach(var pair in SplitPipe(r.GetOrDefault("perk_tacticTypeBonus"))) {
                var kv = pair.Split(':'); if(kv.Length != 2) continue;
                if(!Enum.TryParse(kv[0], out TacticType tt)) continue;
                so.perk_tacticTypeBonus.Add(new TacticTypeBonus { type = tt, bonus = ParseFloat(kv[1], 0f) });
            }

            so.audienceAffinity.Clear();
            foreach(var pair in SplitPipe(r.GetOrDefault("audienceAffinity"))) {
                var kv = pair.Split(':'); if(kv.Length != 2) continue;
                so.audienceAffinity.Add(new AudienceAffinity { audienceId = kv[0].Trim(), bonus = ParseFloat(kv[1], 0f) });
            }

            EditorUtility.SetDirty(so);
        }
    }

    private static void ImportComments(string file) {
        if(!Check(file)) return;
        var rows = Csv.Read(file);
        foreach(var r in rows) {
            
            var so = LoadOrCreate<CommentLineSO>(Path.Combine(OutFolder, "Comments"), r["id"]);
            so.id = r["id"];            
            if(!Enum.TryParse(r["category"], true, out CommentCategory cat)) cat = CommentCategory.neutral;
            so.category = cat;
            so.text = r["text"];
            so.commenterName = r["commenterName"];            
            EditorUtility.SetDirty(so);
        }
    }

    private static void ImportQuests(string file) {
        if(!Check(file)) return;
        var rows = Csv.Read(file);
        foreach(var r in rows) {
            var so = LoadOrCreate<QuestSO>(Path.Combine(OutFolder, "Quests"), r["id"]);
            so.id = r["id"];
            so.title = r["title"];
            so.description = r["description"];
            so.reward_coins = ParseInt(r["reward_coins"], 0);
            so.reward_truthTokens = ParseInt(r["reward_truthTokens"], 0);
            so.enabledFlag = ParseBool(r.GetOrDefault("enabled"), true);
            EditorUtility.SetDirty(so);
        }
    }

    private static void ImportDebrief(string file) {
        if(!Check(file)) return;
        var rows = Csv.Read(file);
        foreach(var r in rows) {
            var so = LoadOrCreate<DebriefCardSO>(Path.Combine(OutFolder, "DebriefCards"), r["id"]);
            so.id = r["id"];
            so.text = r["text"];
            if(!Enum.TryParse(r["correctBin"], out DebriefBin bin)) bin = DebriefBin.RedFlag;
            so.correctBin = bin;
            so.explanation = r.GetOrDefault("explanation");
            EditorUtility.SetDirty(so);
        }
    }

    private static void ImportLocalization(string file) {
        if(!Check(file)) return;
        var rows = Csv.Read(file);
        foreach(var r in rows) {
            var id = r["key"];
            var so = LoadOrCreate<LocalizationEntrySO>(Path.Combine(OutFolder, "Localization"), id);
            so.key = id;
            so.en = r.GetOrDefault("en");
            EditorUtility.SetDirty(so);
        }
    }

    // ---------- Helpers ----------
    private static T LoadOrCreate<T>(string folder, string id) where T : ScriptableObject {
        Directory.CreateDirectory(folder);
        var assetPath = Path.Combine(folder, id + ".asset").Replace("\\", "/");
        var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if(existing != null) return existing;

        var so = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(so, assetPath);
        return so;
    }

    private static bool Check(string path) {
        if(File.Exists(path)) return true;
        Debug.LogWarning($"CSV not found: {path}");
        return false;
    }

    private static void EnsureDirs() {
        Directory.CreateDirectory(CsvFolder);
        Directory.CreateDirectory(OutFolder);
    }

    private static string[] SplitPipe(string s)
        => string.IsNullOrWhiteSpace(s) ? Array.Empty<string>()
           : s.Split('|').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToArray();

    private static int ParseInt(string s, int def) => int.TryParse(s, NumberStyles.Integer, CI, out var v) ? v : def;
    private static float ParseFloat(string s, float def) => float.TryParse(s, NumberStyles.Float, CI, out var v) ? v : def;
    private static bool ParseBool(string s, bool def) {
        if(string.IsNullOrWhiteSpace(s)) return def;
        if(bool.TryParse(s, out var b)) return b;
        if(int.TryParse(s, out var i)) return i != 0;
        var t = s.Trim().ToLowerInvariant();
        return t is "y" or "yes" or "true" ? true : t is "n" or "no" or "false" ? false : def;
    }

    private static T EnumParse<T>(string s, T def) where T : struct
        => Enum.TryParse<T>(s, true, out var v) ? v : def;

    // Simple CSV reader (comma-sep, quotes supported)
    private static class Csv {
        public static List<Dictionary<string, string>> Read(string path) {
            var lines = File.ReadAllLines(path);
            if(lines.Length == 0) return new();
            var headers = SplitLine(lines[0]).ToArray();
            var rows = new List<Dictionary<string, string>>();
            for(int i = 1; i < lines.Length; i++) {
                if(string.IsNullOrWhiteSpace(lines[i])) continue;
                var cols = SplitLine(lines[i]).ToArray();
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for(int c = 0; c < headers.Length; c++) {
                    var key = headers[c];
                    var val = c < cols.Length ? cols[c] : "";
                    dict[key] = val;
                }
                rows.Add(dict);
            }
            return rows;
        }

        private static IEnumerable<string> SplitLine(string line) {
            bool inQuotes = false;
            var cur = new System.Text.StringBuilder();
            for(int i = 0; i < line.Length; i++) {
                var ch = line[i];
                if(ch == '\"') {
                    if(inQuotes && i + 1 < line.Length && line[i + 1] == '\"') { cur.Append('\"'); i++; }
                    else inQuotes = !inQuotes;
                }
                else if(ch == ',' && !inQuotes) {
                    yield return cur.ToString();
                    cur.Length = 0;
                }
                else cur.Append(ch);
            }
            yield return cur.ToString();
        }
    }

    // convenience
    private static string GetOrDefault(this Dictionary<string, string> row, string key, string def = "")
        => row.TryGetValue(key, out var v) ? v : def;
}
#endif
