using System;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class SOCWizard : EditorWindow
{
    #region Variables
    bool bakeEditorBuildList;
    bool spawnOnAsset;
    bool isPortalOpen;
    bool showBakeTools;
    bool showSceneTool;
    bool showTest;
    bool showUmbraInfo;
    bool showSocInfo;
    bool showVisOptions;
    static GameObject occlusionPortalTemplate;
    static GameObject occlusionAreaTemplate;
    GameObject target;
    Vector2 scrollViewPosition;
    const float defBackfaceThreshold = 100;
    const float defSmallestHole = 0.25f;
    const float defSmallestOccluder = 5;
    #endregion

    [MenuItem("External Tools/Tech Art/Static Occlusion Culling Wizard")]
    static void GetSocWindow()
    {
        SOCWizard window = (SOCWizard)GetWindow(typeof(SOCWizard));
        window.minSize = new Vector2(360, 880);
        window.Show();
    }

    private void OnGUI()
    {
        ToolsDisplayController();
    }

    void ToolsDisplayController()
    {
        scrollViewPosition = EditorGUILayout.BeginScrollView(scrollViewPosition);
        EditorGUILayout.BeginVertical();
        GUILayout.Space(15);
        DisplayOcclussionTools();
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        OcclusionSceneTools();
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        OcclussionBakeProfiler();
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        DisplayContextInformation();
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        DisplayProjectOcclusionInformation();
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        DisplayVisualizationOptions();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

    #region GUI Methods
    void DisplayOcclussionTools()
    {
        showBakeTools = EditorGUILayout.BeginFoldoutHeaderGroup(showBakeTools, "Bake Parameters & Tools");

        if(showBakeTools)
        {
            EditorGUILayout.BeginVertical();

            bakeEditorBuildList = EditorGUILayout.ToggleLeft("Bake Editor Build Scene List", bakeEditorBuildList);

            StaticOcclusionCulling.backfaceThreshold = EditorGUILayout.FloatField("Backface Threshold", StaticOcclusionCulling.backfaceThreshold, GUILayout.Width(200));
            StaticOcclusionCulling.smallestHole = EditorGUILayout.FloatField("Smallest Hole", StaticOcclusionCulling.smallestHole, GUILayout.Width(200));
            StaticOcclusionCulling.smallestOccluder = EditorGUILayout.FloatField("Smallest Occluder", StaticOcclusionCulling.smallestOccluder, GUILayout.Width(200));

            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("Generate", GUILayout.Width(160)))
            {
                if (bakeEditorBuildList)
                {
                    UnityEngine.Debug.Log("SOC Baking all scenes in editor build scene list");
                    foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                    {
                        UnityEngine.Debug.Log($"Opening {scene.path}");
                        EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
                        UnityEngine.Debug.Log($"Baking scene{scene.path}");
                        BakeStaticOcclusion();
                    }
                }
                else
                {
                    UnityEngine.Debug.Log($"SOC Baking current scene only");
                    BakeStaticOcclusion();
                }
            }

            if (GUILayout.Button("Generate In Background", GUILayout.Width(160)))
            {
                if (bakeEditorBuildList)
                {
                    UnityEngine.Debug.Log("SOC Baking all scene in editor build scene list");
                    foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                    {
                        UnityEngine.Debug.Log($"Opening {scene.path}");
                        EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
                        UnityEngine.Debug.Log($"Baking scene{scene.path}");
                        BackgroundBakeStaticOcclusion();
                    }
                }
                else
                {
                    UnityEngine.Debug.Log($"SOC Baking current scene only");
                    BackgroundBakeStaticOcclusion();
                }
            }

            if(GUILayout.Button("Default Parameters", GUILayout.Width(160)))
            {
                ResetBakeParametersToDefault();
            }

            if (GUILayout.Button("Cancel Bake", GUILayout.Width(160)))
            {
                StaticOcclusionCulling.Cancel();
                AssetDatabase.SaveAssets();
                UnityEngine.Debug.Log("SOC bake operation cancelled!");
            }

            if (GUILayout.Button("Remove Cache Data", GUILayout.Width(160)))
            {
                StaticOcclusionCulling.RemoveCacheFolder();
                AssetDatabase.SaveAssets();
                UnityEngine.Debug.Log("SOC cache data has been deleted!");
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    public void OcclussionBakeProfiler()
    {
        showTest = EditorGUILayout.BeginFoldoutHeaderGroup(showTest, "Static Occlusion Profiler");

        if (showTest)
        {
            if (GUILayout.Button("Run Static Occlusion Test", GUILayout.Width(200)))
            {
                RunProfileTest();
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void DisplayContextInformation()
    {
        long umbraSize = StaticOcclusionCulling.umbraDataSize;
        long umbraSizeKb = umbraSize / 1024;
        long umbraSizeMb = umbraSize / (1024*1024);

        showUmbraInfo = EditorGUILayout.BeginFoldoutHeaderGroup(showUmbraInfo, "Umbra Cache Utils");

        if(showUmbraInfo)
        {
            if (StaticOcclusionCulling.isRunning)
            {
                EditorGUILayout.HelpBox("SOC bake is running", MessageType.Info);
            }

            GUILayout.Label($"Umbra data size in Bytes: {umbraSize}");
            GUILayout.Label($"Umbra data size in Kilobytes: {umbraSizeKb}");
            GUILayout.Label($"Umbra data size in Megabytes: {umbraSizeMb}");

            EditorGUILayout.BeginVertical();
            if (Directory.Exists(@"Library/Occlusion"))
            {
                if (GUILayout.Button("Open Umbra Folder", GUILayout.Width(200)))
                {
                    UnityEngine.Debug.Log("Opening Umbra data folder");
                    Process.Start("explorer.exe", @"Library\Occlusion");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No Umbra cache present", MessageType.Info);
            }

            if (GUILayout.Button("Write Umbra Log to Console", GUILayout.Width(200)))
            {
                WriteUmbraLogToConsole();
            }

            if (GUILayout.Button("Clear Current Umbra Data", GUILayout.Width(200)))
            {
                StaticOcclusionCulling.Clear();
                AssetDatabase.SaveAssets();
                UnityEngine.Debug.Log("SOC bake data cleared");
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void OcclusionSceneTools()
    {
        showSceneTool = EditorGUILayout.BeginFoldoutHeaderGroup(showSceneTool, "Custom Occlusion Scene Tools");

        if (StaticOcclusionCulling.doesSceneHaveManualPortals == true)
        {
            EditorGUILayout.HelpBox("Current scene has manual occlusion portals", MessageType.Info);
        }

        if (showSceneTool)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            spawnOnAsset = EditorGUILayout.ToggleLeft("Spawn Asset at Target", spawnOnAsset);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            isPortalOpen = EditorGUILayout.ToggleLeft("Portal Creation State", isPortalOpen);
            EditorGUILayout.EndHorizontal();

            occlusionPortalTemplate = (GameObject)EditorGUILayout.ObjectField("Occlusion Portal Template", occlusionPortalTemplate, typeof(GameObject), true, GUILayout.Width(350));
            occlusionAreaTemplate = (GameObject)EditorGUILayout.ObjectField("Occlusion Area Template", occlusionAreaTemplate, typeof(GameObject), true, GUILayout.Width(350));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create SOC Portal",GUILayout.Width(150)))
            {
                GenerateOcclusionPortal();
            }

            if (GUILayout.Button("Create SOC Area", GUILayout.Width(150)))
            {
                GenerateOcclusionArea();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void DisplayProjectOcclusionInformation()
    {
        string[] ocDataFiles = AssetDatabase.FindAssets("OcclusionCullingData");

        showSocInfo = EditorGUILayout.BeginFoldoutHeaderGroup(showSocInfo, "Static Occlusion Information");

        if(showSocInfo)
        {
            GUILayout.Label($"# of occlusion data files in project: {ocDataFiles.Length}");

            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("Occlusion Paths to Console",GUILayout.Width(180)))
            {
                GetProjectOcclusionData();
            }

            if (GUILayout.Button("Occlusion Paths to File", GUILayout.Width(180)))
            {
                WriteOcclusionDataToFile();
            }

            if (GUILayout.Button("Project Occlusion Size", GUILayout.Width(180)))
            {
                CalculateOverallOcclusionDataSize();
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void DisplayVisualizationOptions()
    {
        showVisOptions = EditorGUILayout.BeginFoldoutHeaderGroup(showVisOptions, "Visualization Options");

        if(showVisOptions)
        {
            StaticOcclusionCullingVisualization.showDynamicObjectBounds = EditorGUILayout.ToggleLeft("Show Dynamic Object Bounds", StaticOcclusionCullingVisualization.showDynamicObjectBounds);
            StaticOcclusionCullingVisualization.showGeometryCulling = EditorGUILayout.ToggleLeft("Show Geometry Culling", StaticOcclusionCullingVisualization.showGeometryCulling);
            StaticOcclusionCullingVisualization.showOcclusionCulling = EditorGUILayout.ToggleLeft("Show Occlusion Culling", StaticOcclusionCullingVisualization.showOcclusionCulling);
            StaticOcclusionCullingVisualization.showPortals = EditorGUILayout.ToggleLeft("Show Portals", StaticOcclusionCullingVisualization.showPortals);
            StaticOcclusionCullingVisualization.showPreVisualization = EditorGUILayout.ToggleLeft("Show Pre-Visualization", StaticOcclusionCullingVisualization.showPreVisualization);
            StaticOcclusionCullingVisualization.showViewVolumes = EditorGUILayout.ToggleLeft("Show View Volumes", StaticOcclusionCullingVisualization.showViewVolumes);
            StaticOcclusionCullingVisualization.showVisibilityLines = EditorGUILayout.ToggleLeft("Show Visibility Lines", StaticOcclusionCullingVisualization.showVisibilityLines);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    #endregion

    #region Functional Methods
    void WriteUmbraLogToConsole()
    {
        string[] lines = File.ReadAllLines(@"Library\Occlusion\log.txt");

        foreach (string line in lines)
        {
            UnityEngine.Debug.Log($"[UMBRA LOG] {line}");
        }
    }

    void RunProfileTest()
    {
        UnityEngine.Debug.Log($"Clearing old SOC data.");
        StaticOcclusionCulling.Clear();
        BakeStaticOcclusion();
        StaticOcclusionCulling.Clear();
        BackgroundBakeStaticOcclusion();
    }

    void GenerateOcclusionPortal()
    {
        GameObject newOcclusionPortal;

        target = Selection.activeGameObject;

        if (spawnOnAsset == true)
        {
            UnityEngine.Debug.Log($"Spawning occlusion portal on object: {target.name} at transform: {Vector3.zero}");
            newOcclusionPortal = Instantiate(occlusionPortalTemplate, target.transform.position, Quaternion.identity) as GameObject;

            OcclusionPortal portalState = newOcclusionPortal.GetComponent<OcclusionPortal>();
            portalState.open = isPortalOpen;
        }
        else
        {
            UnityEngine.Debug.Log($"Spawning occlusion portal at world origin");
            newOcclusionPortal = Instantiate(occlusionPortalTemplate, Vector3.zero, Quaternion.identity) as GameObject;
        }

        newOcclusionPortal.name = $"CustomOcclusionPortal_{newOcclusionPortal.transform.position}";

        AssetDatabase.SaveAssets();
    }

    void GenerateOcclusionArea()
    {
        GameObject newOcclusionArea;

        target = Selection.activeGameObject;

        if (spawnOnAsset == true)
        {
            UnityEngine.Debug.Log($"Spawning occlusion area on object: {target.name} at transform: {target.transform.position}");
            newOcclusionArea = Instantiate(occlusionAreaTemplate, target.transform.position, Quaternion.identity) as GameObject;

            OcclusionArea occlusionArea = newOcclusionArea.GetComponent<OcclusionArea>();
            occlusionArea.size = target.transform.localScale;
        }
        else
        {
            UnityEngine.Debug.Log($"Spawning occlusion area at world origin");
            newOcclusionArea = Instantiate(occlusionAreaTemplate, Vector3.zero, Quaternion.identity) as GameObject;
        }

        newOcclusionArea.name = $"CustomOcclusionArea_{newOcclusionArea.transform.position}";

        AssetDatabase.SaveAssets();
    }

    void GetProjectOcclusionData()
    {
        string[] ocDataFiles = AssetDatabase.FindAssets("OcclusionCullingData");

        if(ocDataFiles.Length == 0)
        {
            UnityEngine.Debug.Log($"No occlusion paths present in project");
        }
        else
        {
            foreach (string file in ocDataFiles)
            {
                string path = AssetDatabase.GUIDToAssetPath(file);
                UnityEngine.Debug.Log($"Occlusion asset path and guid: {path} : {file}");
            }
        }
    }

	void WriteOcclusionDataToFile()
	{
		string[] lines = AssetDatabase.FindAssets("OcclusionCullingData");

        if(lines.Length == 0)
        {
            UnityEngine.Debug.Log($"No occlusion paths present in project");
        }
        else
        {
            UnityEngine.Debug.Log(Application.temporaryCachePath + "WriteLines.txt");

            using (StreamWriter outputFile = new StreamWriter(Application.temporaryCachePath + "WriteLines.txt"))
            {
                foreach (string text in lines)
                {
                    string path = AssetDatabase.GUIDToAssetPath(text);
                    outputFile.WriteLine($"{path}:{text}");
                }
            }
        }
    }

    void LogOcclusionParameters()
    {
        UnityEngine.Debug.Log($"[SOC-PARAM] Backface Threshold: {StaticOcclusionCulling.backfaceThreshold}");
        UnityEngine.Debug.Log($"[SOC-PARAM] Smallest Hole: {StaticOcclusionCulling.smallestHole}");
        UnityEngine.Debug.Log($"[SOC-PARAM] Smallest Occluder: {StaticOcclusionCulling.smallestOccluder}");
    }

    public void BakeStaticOcclusion()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        string currentSceneName = currentScene.name;

        Stopwatch stopwatch = new Stopwatch();
        UnityEngine.Debug.Log($"Starting SOC bake of scene {currentSceneName} at: {DateTime.Now}");
        LogOcclusionParameters();
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
        LogOcclusionParameters();
        stopwatch.Start();
        StaticOcclusionCulling.GenerateInBackground();
        stopwatch.Stop();

        UnityEngine.Debug.Log($"Time to complete background bake in milliseconds: {stopwatch.ElapsedMilliseconds}");
        UnityEngine.Debug.Log($"SOC background bake completed at: {DateTime.Now}");

        AssetDatabase.SaveAssets();
        WriteUmbraLogToConsole();
    }

    void ResetBakeParametersToDefault()
    {
        UnityEngine.Debug.Log($"Resetting bake parameters to default values, [Backface Threshold:{defBackfaceThreshold}], [Smallest Hole:{defSmallestHole}], [Smallest Occluder:{defSmallestOccluder}]");

        StaticOcclusionCulling.backfaceThreshold = defBackfaceThreshold;
        StaticOcclusionCulling.smallestHole = defSmallestHole;
        StaticOcclusionCulling.smallestOccluder = defSmallestOccluder;
    }

    void CalculateOverallOcclusionDataSize()
    {
        string[] ocDataFiles = AssetDatabase.FindAssets("OcclusionCullingData");

        foreach(string ocFile in ocDataFiles)
        {
            string path = AssetDatabase.GUIDToAssetPath(ocFile);
            FileInfo fInfo = new FileInfo(path);
            long fileSize;

            fileSize = fInfo.Length;

            UnityEngine.Debug.Log($"File size for {path} is {fileSize/1024} kilobytes");
            UnityEngine.Debug.Log($"File size for {path} is {fileSize/(1024*1024)} megabytes");
        }
    }
    #endregion
}