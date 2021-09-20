using System;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

[Serializable]
public class StaticOcclusionVariables
{
    public bool tomeMatch { get; set; }
    public int smallestOccluder { get; set; }
    public int hierDetail { get; set; }
    public int objGroupCost { get; set; }
    public float smallestHole { get; set; }
    public bool outputVisualizations { get; set; }
    public bool outputStrictViewVolumes { get; set; }
    public int minAccurateDistance { get; set; }
    public bool outputShadowOptimizations { get; set; }
    public bool outputObjOptimizations { get; set; }
    public bool outputAccurateDilation { get; set; }
    public int clusterSize { get; set; }
}

public class SOCWizard : EditorWindow
{
    bool bakeEditorBuildList = false;
    bool spawnOnAsset = true;
    bool isPortalOpen = true;
        
    [MenuItem("External Tools/SOC Wizard")]
    static void GetSocWindow()
    {
        SOCWizard window = (SOCWizard)GetWindow(typeof(SOCWizard));
        window.minSize = new Vector2(675, 630);
        window.Show();
    }

    private void OnGUI()
    {
        ToolsDisplayController();
    }

    void ToolsDisplayController()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();
        GUILayout.Space(15);
        GUILayout.Label("SOC Bake Tools", EditorStyles.boldLabel);
        DisplayOcclussionTools();
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("SOC Parameters", EditorStyles.boldLabel);
        DisplayParameterSettings();
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("SOC Scene Tools", EditorStyles.boldLabel);
        OcclusionSceneTools();
        EditorGUILayout.EndVertical();

        GUILayout.Space(20);

        EditorGUILayout.BeginVertical();
        GUILayout.Space(15);
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
		GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Custom Culling Group Tools", EditorStyles.boldLabel);
		DisplayCustomCullingTools();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
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
                    SyncBakeStaticOcclusion();
                    UnityEngine.Debug.Log($"Baking scene{null}");
                }
            }
            else
            {
                UnityEngine.Debug.Log($"SOC Baking current scene in sync mode {null}");
                SyncBakeStaticOcclusion();
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
                    AsyncBakeStaticOcclusion();
                    UnityEngine.Debug.Log($"Baking scene{null}");
                }
            }
            else
            {
                UnityEngine.Debug.Log($"SOC Baking current scene in async mode {null}");
                AsyncBakeStaticOcclusion();
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
            SyncBakeStaticOcclusion();
            stopwatch.Stop();
            StaticOcclusionCulling.Clear();

            stopwatch.Reset();
            stopwatch.Start();
            AsyncBakeStaticOcclusion();
            stopwatch.Stop();
        }
    }

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
                Process.Start("explorer.exe", @"Library/Occlusion");
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

    void DisplayParameterSettings()
    {
        StaticOcclusionVariables staticOcclusionVariables = new StaticOcclusionVariables();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Tome Match: ", GUILayout.Width(180));
        EditorGUILayout.LabelField("Smallest Occluder: ", GUILayout.Width(180));
        EditorGUILayout.LabelField("Hierarchy Detail: ", GUILayout.Width(180));
        EditorGUILayout.LabelField("Object Group Cost: ", GUILayout.Width(180));
        EditorGUILayout.LabelField("Smallest Hole: ", GUILayout.Width(180));
        EditorGUILayout.LabelField("Output Visualizations: ", GUILayout.Width(180));
        EditorGUILayout.LabelField("Output Strict View Volumes: ", GUILayout.Width(180));
        EditorGUILayout.LabelField("Minimum Accurate Distance: ", GUILayout.Width(180));
        EditorGUILayout.LabelField("Output Shadow Optimizations: ", GUILayout.Width(180));
        EditorGUILayout.LabelField("Output Object Optimizations: ", GUILayout.Width(180));
        EditorGUILayout.LabelField("Output Accurate Dilation: ", GUILayout.Width(180));
        EditorGUILayout.LabelField("Cluster Size: ", GUILayout.Width(180));
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        EditorGUILayout.Toggle(staticOcclusionVariables.tomeMatch);
        EditorGUILayout.IntField(staticOcclusionVariables.smallestOccluder, GUILayout.Width(50));
        EditorGUILayout.IntField(staticOcclusionVariables.hierDetail, GUILayout.Width(50));
        EditorGUILayout.IntField(staticOcclusionVariables.objGroupCost, GUILayout.Width(50));
        EditorGUILayout.Slider(staticOcclusionVariables.smallestHole,0f,100f,GUILayout.Width(150));
        EditorGUILayout.Toggle(staticOcclusionVariables.outputVisualizations);
        EditorGUILayout.Toggle(staticOcclusionVariables.outputStrictViewVolumes);
        EditorGUILayout.IntField(staticOcclusionVariables.minAccurateDistance, GUILayout.Width(50));
        EditorGUILayout.Toggle(staticOcclusionVariables.outputShadowOptimizations);
        EditorGUILayout.Toggle(staticOcclusionVariables.outputObjOptimizations);
        EditorGUILayout.Toggle(staticOcclusionVariables.outputAccurateDilation);
        EditorGUILayout.IntField(staticOcclusionVariables.clusterSize, GUILayout.Width(50));
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        EditorGUILayout.BeginVertical();
        if(GUILayout.Button("Save Parameter Settings"))
        {
            JsonSerializeUmbraInput();
        }
        EditorGUILayout.EndVertical();
    }

    void OcclusionSceneTools()
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal();
        spawnOnAsset = EditorGUILayout.Toggle("Spawn Asset at Target",spawnOnAsset);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        isPortalOpen= EditorGUILayout.Toggle("Portal Creation State",isPortalOpen);
        EditorGUILayout.EndHorizontal();

        if(isPortalOpen == false)
        {
            EditorGUILayout.HelpBox("Portal creation state is set to CLOSED", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Portal creation state is set to OPEN", MessageType.Info);
        }

        if(spawnOnAsset)
        {
            if (GUILayout.Button("Create SOC Portal"))
            {
                if(isPortalOpen)
                {
                    GenerateOcclusionPortal(isPortalOpen, spawnOnAsset);
                }
            }
        }

        if(spawnOnAsset)
        {
            if (GUILayout.Button("Create SOC Area"))
            {
                GenerateOcclusionArea(spawnOnAsset);
            }
        }

        EditorGUILayout.EndVertical();
    }

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
            UnityEngine.Debug.Log($"Writing SOC data paths to file: {null}");
        }
    }
	
	void DisplayCustomCullingTools()
	{
		if(GUILayout.Button("Create Custom Culling Group"))
		{
			GenerateNewCustomCullingGroup(1000);
		}
	}
    #endregion

    //TODO: Save and ser the params from UI to the json
    void JsonSerializeUmbraInput()
    {
        StaticOcclusionVariables staticOcclusionVariables = new StaticOcclusionVariables()
        {
            tomeMatch = false,
            smallestOccluder = 0,
            hierDetail = 0,
            objGroupCost = 0,
            smallestHole = 0.1f,
            outputVisualizations = false,
            outputStrictViewVolumes = false,
            minAccurateDistance = 0,
            outputShadowOptimizations = false,
            outputObjOptimizations = false,
            outputAccurateDilation = false,
            clusterSize = 0,
        };

        string json = JsonUtility.ToJson(staticOcclusionVariables);
        UnityEngine.Debug.Log("Writing SOC parameters to file: Library/Occlusion/input.scene.json");
    }

    void WriteUmbraLogToConsole()
    {
        string[] lines = File.ReadAllLines(@"E:\Projects\FFXR\Library\Occlusion\log.txt");

        foreach (string line in lines)
        {
            UnityEngine.Debug.Log($"[UMBRA LOG] {line}");
        }
    }

    void GenerateOcclusionPortal(bool isOpenDefault, bool spawnOnAsset)
    {
        OcclusionPortal newOcclusionPortal = new();
        GameObject target = Selection.activeGameObject;

        if(spawnOnAsset == true)
        {
            UnityEngine.Debug.Log($"Spawning occlusion portal on object: {target.name} at transform: {target.transform.position}");
            Instantiate(newOcclusionPortal, target.transform.position, Quaternion.identity);
        }
        else
        {
            UnityEngine.Debug.Log($"Spawning occlusion portal at world origin");
            Instantiate(newOcclusionPortal, Vector3.zero, Quaternion.identity);
        }

        newOcclusionPortal.open = isOpenDefault;
        AssetDatabase.SaveAssets();
    }

    void GenerateOcclusionArea(bool spawnOnAsset)
    {
        OcclusionArea newOcclusionArea = new();
        GameObject target = Selection.activeGameObject;

        if (spawnOnAsset == true)
        {
            UnityEngine.Debug.Log($"Spawning occlusion area on object: {target.name} @ transform: {target.transform.position}");
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

    public void SyncBakeStaticOcclusion()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        string currentSceneName = currentScene.name;

        Stopwatch stopwatch = new Stopwatch();
        UnityEngine.Debug.Log($"Starting SOC sync bake of scene {currentSceneName} at: {DateTime.Now}");
        stopwatch.Start();
        StaticOcclusionCulling.Compute();
        stopwatch.Stop();

        UnityEngine.Debug.Log($"Time to complete sync bake in milliseconds: {stopwatch.ElapsedMilliseconds}");
        UnityEngine.Debug.Log($"SOC sync bake completed at: {DateTime.Now}");

        AssetDatabase.SaveAssets();
        WriteUmbraLogToConsole();
    }

    public void AsyncBakeStaticOcclusion()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        string currentSceneName = currentScene.name;

        Stopwatch stopwatch = new Stopwatch();
        UnityEngine.Debug.Log($"Starting SOC async bake of scene {currentSceneName} at: {DateTime.Now}");
        stopwatch.Start();
        StaticOcclusionCulling.GenerateInBackground();
        stopwatch.Stop();

        UnityEngine.Debug.Log($"Time to complete async bake in milliseconds: {stopwatch.ElapsedMilliseconds}");
        UnityEngine.Debug.Log($"SOC async bake completed at: {DateTime.Now}");

        AssetDatabase.SaveAssets();
        WriteUmbraLogToConsole();
    }
	
	void GenerateNewCustomCullingGroup(int numOfSpheres)
	{
		UnityEngine.Debug.Log($"Creating new culling group with bounding sphere size of {numOfSpheres}");
		
		BoundingSphere[] spheres = new BoundingSphere[numOfSpheres];
		
		spheres[0] = new BoundingSphere(Vector3.zero, 1f);
		
		CullingGroup customCullingGroup = new CullingGroup();
		customCullingGroup.targetCamera = Camera.main;
		customCullingGroup.Dispose();
		customCullingGroup = null;
	}
}