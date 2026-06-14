using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hockey.Editor
{
    /// <summary>
    /// First-run editor setup: ensures an (empty) Main scene exists and is the first entry in Build
    /// Settings, so pressing Play / building "just works". The runtime GameBootstrap constructs the
    /// match, so the scene itself stays empty. Runs once — it never overwrites an existing Main.unity.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string SceneDir = "Assets/Scenes";
        const string ScenePath = "Assets/Scenes/Main.unity";

        static ProjectSetup()
        {
            EditorApplication.delayCall += EnsureSetup;
        }

        static void EnsureSetup()
        {
            EnsureMainScene();
            EnsureBuildSettings();
        }

        static void EnsureMainScene()
        {
            if (File.Exists(ScenePath)) return;
            if (!Directory.Exists(SceneDir)) Directory.CreateDirectory(SceneDir);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[Hockey] Created {ScenePath}. Press Play to bootstrap a match.");
        }

        static void EnsureBuildSettings()
        {
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path == ScenePath) return;

            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes)
            {
                new EditorBuildSettingsScene(ScenePath, true),
            };
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
