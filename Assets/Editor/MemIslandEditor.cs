#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MemIslandEditor
{
    // One-click new scene creation, safe in Unity batch mode.
    [MenuItem("MEM ISLAND/Создать игровую сцену")]
    public static void CreateScene()
    {
        if(!Directory.Exists("Assets/Scenes"))Directory.CreateDirectory("Assets/Scenes");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        GameObject starter=new GameObject("MEM ISLAND — GAME BOOTSTRAP");
        starter.AddComponent<MemIslandGame>();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/MemIsland.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/MemIsland.unity",true)};
        PlayerSettings.productName="MEM ISLAND";
        PlayerSettings.companyName="Negelinho";
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS,"com.negelinho.memisland");
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
        AssetDatabase.SaveAssets();
        Debug.Log("MEM ISLAND: scene generated. Open Assets/Scenes/MemIsland.unity and Play!");
    }
    public static void BuildWebGL()
    {
        CreateScene();
        BuildPipeline.BuildPlayer(new[]{"Assets/Scenes/MemIsland.unity"},"Build/WebGL",BuildTarget.WebGL,BuildOptions.None);
    }
    public static void BuildIOS()
    {
        CreateScene();
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.iOS,ScriptingImplementation.IL2CPP);
        BuildPipeline.BuildPlayer(new[]{"Assets/Scenes/MemIsland.unity"},"Build/iOS",BuildTarget.iOS,BuildOptions.None);
    }
}
#endif
