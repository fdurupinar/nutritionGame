using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildWebGame
{
    [MenuItem("Tools/Food for Thought/Build Web Game")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before building.");
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            throw new InvalidOperationException("Install Web Build Support for this editor in Unity Hub, then restart Unity.");
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        // Use gzip for native browser decoding over both localhost and HTTPS.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.nameFilesAsHashes = true;
        PlayerSettings.WebGL.threadsSupport = false;
        PlayerSettings.WebGL.initialMemorySize = 128;
        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        string output = Path.GetFullPath("Builds/WebGL");
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0) throw new InvalidOperationException("No scenes enabled in build settings.");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = output, target = BuildTarget.WebGL, options = BuildOptions.None });
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Web build failed: " + report.summary.result);
        string index = Path.Combine(output, "index.html");
        string html = File.ReadAllText(index);
        // Keep the browser wrapper free of Unity branding and the internal project name.
        html = Regex.Replace(html, @"<title>.*?</title>", "<title>Food for Thought</title>");
        html = Regex.Replace(html, @"<div id=""(?:unity-logo|unity-logo-title-footer|unity-build-title)"">.*?</div>", "");
        html = html.Replace("<link rel=\"shortcut icon\" href=\"TemplateData/favicon.ico\">", "<link rel=\"icon\" href=\"data:,\">");
        // Preserve the game's right-click hint interaction instead of opening the browser menu.
        html = html.Replace("</body>", "<script>document.getElementById('unity-canvas').addEventListener('contextmenu', function(e) { e.preventDefault(); });</script>\n</body>");
        File.WriteAllText(index, html);
        Debug.Log("WEB_GAME_BUILD_SUCCESS: " + output);
    }
}
