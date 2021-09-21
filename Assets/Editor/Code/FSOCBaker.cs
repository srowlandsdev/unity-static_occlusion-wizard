using System;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class SOCWizard : EditorWindow
{
    bool bakeEditorBuildList = false;
    bool spawnOnAsset = true;
    bool isPortalOpen = true;
    OcclusionPortal occlusionPortal = new OcclusionPortal();

    GameObject target = Selection.activeGameObject;

    [MenuItem("External Tools/SOC Wizard")]
    static void GetSocWindow()
    {
        SOCWizard window = (SOCWizard)GetWindow(typeof(SOCWizard));
        window.minSize = new Vector2(360, 640);
        window.Show();
    }

    private void OnGUI()
    {
        ToolsDisplayController();
    }

    void ToolsDisplayController()
    {
        EditorGUILayout.BeginVertical();
        GUILayout.Space(15);
        GUILayout.Label("SOC Bake Tools", EditorStyles.boldLabel);
        DisplayOcclussionTools();
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("SOC Scene Tools", EditorStyles.boldLabel);
        OcclusionSceneTools();
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("SOC Stopwatch Test", EditorStyles.boldLabel);
        OcclussionBakeProfiler();
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Umbra Information", EditorStyles.boldLabel);
        DisplayContextInformation();
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Project SOC Information", EditorStyles.boldLabel);
        DisplayProjectOcclusionInformation();
        EditorGUILayout.EndVertical();
    }

    #region GUI Methods
    void DisplayOcclussionTools()
    {
        EditorGUILayout.BeginVertical();

        bakeEditorBuildList = EditorGUILayout.ToggleLeft("Bake Editor Build Scene List", bakeEditorBuildList);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate"))
        {
            if (bakeEditorBuildList)
            {
                UnityEngine.Debug.Log("SOC Baking all scene in editor build scene list");
                foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                {
                    UnityEngine.Debug.Log($"Loading scene {null} from editor build scene list");
                    SceneManager.LoadScene("");
                    BakeStaticOcclusion();
                    UnityEngine.Debug.Log($"Baking scene{null}");
                }
            }
            else
            {
                UnityEngine.Debug.Log($"SOC Baking current scene only");
                BakeStaticOcclusion();
            }
        }

        if (GUILayout.Button("Generate In Background"))
        {
            if (bakeEditorBuildList)
            {
                UnityEngine.Debug.Log("SOC Baking all scene in editor build scene list");
                foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                {
                    UnityEngine.Debug.Log($"Loading scene {null} from editor build scene list");
                    SceneManager.LoadScene("");
                    BackgroundBakeStaticOcclusion();
                    UnityEngine.Debug.Log($"Baking scene{null}");
                }
            }
            else
            {
                UnityEngine.Debug.Log($"SOC Baking current scene only");
                BackgroundBakeStaticOcclusion();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Cancel Bake"))
        {
            StaticOcclusionCulling.Cancel();
            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log("SOC bake operation cancelled!");
        }

        if (GUILayout.Button("Clear Current Data"))
        {
            StaticOcclusionCulling.Clear();
            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log("SOC bake data cleared");
        }

        if (GUILayout.Button("Remove Cache Data"))
        {
            StaticOcclusionCulling.RemoveCacheFolder();
            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log("SOC cache data has been deleted!");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("Removing cache data cannot be undone!", MessageType.Warning);

        EditorGUILayout.EndVertical();
    }

    public void OcclussionBakeProfiler()
    {
        Stopwatch stopwatch = new Stopwatch();

        if(GUILayout.Button("Run SOC Test"))
        {
            UnityEngine.Debug.Log($"Clearing old SOC data.");
            StaticOcclusionCulling.Clear();

            stopwatch.Start();
            BakeStaticOcclusion();
            stopwatch.Stop();
            StaticOcclusionCulling.Clear();

            stopwatch.Reset();
            stopwatch.Start();
            BackgroundBakeStaticOcclusion();
            stopwatch.Stop();
        }
    }

    //TODO: Find a way to get the lib folder path
    void DisplayContextInformation()
    {
        float umbraSize = StaticOcclusionCulling.umbraDataSize;

        if(StaticOcclusionCulling.isRunning)
        {
            EditorGUILayout.HelpBox("SOC bake is running", MessageType.Info);
        }

        if(StaticOcclusionCulling.doesSceneHaveManualPortals == true)
        {
            EditorGUILayout.HelpBox("Current scene has manual occlusion portals", MessageType.Info);
        }

        GUILayout.Label($"Umbra data size in Bytes: {umbraSize}");
        GUILayout.Label($"Umbra data size in Kilobytes: {umbraSize / 1024}");
        GUILayout.Label($"Umbra data size in Megabytes: {umbraSize / 1024 / 1024}");

        EditorGUILayout.BeginVertical();
        if (Directory.Exists(@"Library/Occlusion"))
        {
            if (GUILayout.Button("Open Umbra Folder"))
            {
                UnityEngine.Debug.Log("Opening Umbra data folder");
                Process.Start("explorer.exe");
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No Umbra cache present", MessageType.Info);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Open Umbra Log File"))
        {
            File.Open(@"E:\Projects\FFXR\Library\Occlusion\log.txt", FileMode.Open);
        }

        if (GUILayout.Button("Write Umbra Log to Console"))
        {
            WriteUmbraLogToConsole();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    void OcclusionSceneTools()
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal();
        spawnOnAsset = EditorGUILayout.ToggleLeft("Spawn Asset at Target",spawnOnAsset);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        isPortalOpen= EditorGUILayout.ToggleLeft("Portal Creation State",isPortalOpen);
        EditorGUILayout.EndHorizontal();

        if(isPortalOpen == false)
        {
            EditorGUILayout.HelpBox("Portal creation state is set to CLOSED", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Portal creation state is set to OPEN", MessageType.Info);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create SOC Portal"))
        {
            GenerateOcclusionPortal(isPortalOpen, spawnOnAsset);
        }

        if (GUILayout.Button("Create SOC Area"))
        {
            GenerateOcclusionArea(spawnOnAsset);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    //TODO: Write paths to file and output path location
    void DisplayProjectOcclusionInformation()
    {
        string[] ocDataFiles = AssetDatabase.FindAssets("OcclusionCullingData");

        int totalOcDataFileCount = ocDataFiles.Length;

        GUILayout.Label($"# of Occlusion Data files in project: {totalOcDataFileCount}");

        if (GUILayout.Button("Occlusion Data Paths to Console"))
        {
            GetProjectOcclusionData();
        }

        if (GUILayout.Button("Occlusion Data Paths to File"))
        {
			WriteOcclusionDataToFile("");
        }
    }
    #endregion

    void WriteUmbraLogToConsole()
    {
        string[] lines = File.ReadAllLines(@"E:\Projects\FFXR\Library\Occlusion\log.txt");

        foreach (string line in lines)
        {
            UnityEngine.Debug.Log($"[UMBRA LOG] {line}");
        }
    }

    //TODO: Fix instantiation
    void GenerateOcclusionPortal(bool isOpenDefault, bool spawnOnAsset)
    {
        if(spawnOnAsset == true)
        {
            UnityEngine.Debug.Log($"Spawning occlusion portal on object: {target.name} at transform: {Vector3.zero}");
            Instantiate(occlusionPortal, Vector3.zero, Quaternion.identity);
        }
        else
        {
            UnityEngine.Debug.Log($"Spawning occlusion portal at world origin");
            Instantiate(occlusionPortal, Vector3.zero, Quaternion.identity);
        }

        occlusionPortal.open = isOpenDefault;
        AssetDatabase.SaveAssets();
    }

    //TODO: Fix instantiation
    void GenerateOcclusionArea(bool spawnOnAsset)
    {
        OcclusionArea newOcclusionArea = new();
        GameObject target = Selection.activeGameObject;

        if (spawnOnAsset == true)
        {
            UnityEngine.Debug.Log($"Spawning occlusion area on object: {target.name} at transform: {target.transform.position}");
            Instantiate(newOcclusionArea, target.transform.position, Quaternion.identity);
        }
        else
        {
            UnityEngine.Debug.Log($"Spawning occlusion area at world origin");
            Instantiate(newOcclusionArea, Vector3.zero, Quaternion.identity);
        }

        UnityEngine.Debug.Log($"Creating SOC area at: {newOcclusionArea.transform.position}");
    }

    void GetProjectOcclusionData()
    {
        string[] ocDataFiles = AssetDatabase.FindAssets("OcclusionCullingData");

        foreach(string file in ocDataFiles)
        {
            string path = AssetDatabase.GUIDToAssetPath(file);
            UnityEngine.Debug.Log($"Occlusion data path: {path}");
        }
    }

	void WriteOcclusionDataToFile(string savePath)
	{
		string[] lines = AssetDatabase.FindAssets("OcclusionCullingData");
		
		UnityEngine.Debug.Log($"Writing SOC data paths to file: {savePath}");
		
		using(StreamWriter outputFile = new StreamWriter(Path.Combine(savePath, "WriteLines.txt")))
		{
			foreach(string text in lines)
			{
				string path = AssetDatabase.GUIDToAssetPath(text);
				outputFile.WriteLine(text);
			}
		}
	}
	
    public void BakeStaticOcclusion()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        string currentSceneName = currentScene.name;

        Stopwatch stopwatch = new Stopwatch();
        UnityEngine.Debug.Log($"Starting SOC bake of scene {currentSceneName} at: {DateTime.Now}");
        stopwatch.Start();
        StaticOcclusionCulling.Compute();
        stopwatch.Stop();

        UnityEngine.Debug.Log($"Time to complete bake in milliseconds: {stopwatch.ElapsedMilliseconds}");
        UnityEngine.Debug.Log($"SOC bake completed at: {DateTime.Now}");

        AssetDatabase.SaveAssets();
        WriteUmbraLogToConsole();
    }

    public void BackgroundBakeStaticOcclusion()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        string currentSceneName = currentScene.name;

        Stopwatch stopwatch = new Stopwatch();
        UnityEngine.Debug.Log($"Starting SOC background bake of scene {currentSceneName} at: {DateTime.Now}");
        stopwatch.Start();
        StaticOcclusionCulling.GenerateInBackground();
        stopwatch.Stop();

        UnityEngine.Debug.Log($"Time to complete background bake in milliseconds: {stopwatch.ElapsedMilliseconds}");
        UnityEngine.Debug.Log($"SOC background bake completed at: {DateTime.Now}");

        AssetDatabase.SaveAssets();
        WriteUmbraLogToConsole();
    }
}