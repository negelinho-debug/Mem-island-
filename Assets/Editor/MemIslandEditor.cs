
#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

public static class MemIslandEditor
{
    // Bundle ID from Signulous
    public const string IOSBundleID = "app.chili6614.lake1071";

    public static void ConfigureIOS()
    {
        PlayerSettings.productName = "MEM ISLAND";
        PlayerSettings.companyName = "Negelinho";
        PlayerSettings.SetApplicationIdentifier(
            NamedBuildTarget.iOS,
            IOSBundleID
        );
        PlayerSettings.defaultInterfaceOrientation =
            UIOrientation.LandscapeLeft;
    }

    [MenuItem("MEM ISLAND/Создать игровую сцену")]
    public static void CreateScene()
    {
        if (!Directory.Exists("Assets/Scenes"))
            Directory.CreateDirectory("Assets/Scenes");

        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single
        );

        GameObject starter = new GameObject(
            "MEM ISLAND — GAME BOOTSTRAP"
        );
        starter.AddComponent<MemIslandGame>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(
            scene,
            "Assets/Scenes/MemIsland.unity"
        );

        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene(
                "Assets/Scenes/MemIsland.unity", true
            )
        };

        ConfigureIOS();
        AssetDatabase.SaveAssets();
    }

    public static void BuildWebGL()
    {
        CreateScene();
        BuildPipeline.BuildPlayer(
            new[] { "Assets/Scenes/MemIsland.unity" },
            "Build/WebGL",
            BuildTarget.WebGL,
            BuildOptions.None
        );
    }

    public static void BuildIOS()
    {
        CreateScene();

        PlayerSettings.SetScriptingBackend(
            NamedBuildTarget.iOS,
            ScriptingImplementation.IL2CPP
        );

        BuildPipeline.BuildPlayer(
            new[] { "Assets/Scenes/MemIsland.unity" },
            "Build/iOS",
            BuildTarget.iOS,
            BuildOptions.None
        );
    }
}

public sealed class MemIslandIOSPreBuild :
    IPreprocessBuildWithReport
{
    public int callbackOrder { get { return -1000; } }

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.iOS)
        {
            MemIslandEditor.ConfigureIOS();
            Debug.Log(
                "MEM ISLAND iOS Bundle ID: " +
                MemIslandEditor.IOSBundleID
            );
        }
    }
}
#endif
