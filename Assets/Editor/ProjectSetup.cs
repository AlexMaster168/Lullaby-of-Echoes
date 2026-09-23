using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Автонастройка проекта: сцена, Build Settings, параметры окна
[InitializeOnLoad]
public static class ProjectSetup
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    static ProjectSetup() => EditorApplication.delayCall += Setup;

    public static void Setup()
    {
        if (!File.Exists(ScenePath))
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        if (EditorBuildSettings.scenes.Length == 0)
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        PlayerSettings.productName = "Lullaby of Echoes";
        PlayerSettings.companyName = "Lyokha";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 960;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
    }

    [MenuItem("Lullaby/Открыть главную сцену")]
    static void OpenMain()
    {
        Setup();
        EditorSceneManager.OpenScene(ScenePath);
    }

    [MenuItem("Lullaby/Сбросить сохранения и «Пустоту»")]
    static void ResetSaves()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("Сохранения сброшены.");
    }

    [MenuItem("Lullaby/Собрать игру (Windows)")]
    public static void Build()
    {
        Setup();
        var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, "Build/LullabyOfEchoes.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        Debug.Log("Build result: " + report.summary.result);
        if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }
}
