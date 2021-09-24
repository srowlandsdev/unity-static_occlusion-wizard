using System;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

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
    bool showCustomGroupTools;
    bool showVisOptions;

    UnityEngine.Object occlusionPortalTemplate;
    UnityEngine.Object occlusionAreaTemplate;

    GameObject target;

    Vector2 scrollViewPosition;

    const float defBackfaceThreshold = 100;
    const float defSmallestHole = 0.25f;
    const float defSmallestOccluder = 5;

    Dictionary<GUID, GUID> sceneOcclusionPairs;
    #endregion

    [MenuItem("External Tools/SOC Wizard")]
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
        DisplayCustomGroupCreator();
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        DisplayVisualizationOptions();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

    #region GUI Methods

    //TODO: Fix scaling issue with right column in bake tools and Vis options
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

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("Generate", GUILayout.Width(160)))
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

            if (GUILayout.Button("Generate In Background", GUILayout.Width(160)))
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

            if(GUILayout.Button("Default Parameters", GUILayout.Width(160)))
            {
                ResetBakeParametersToDefault();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("Cancel Bake", GUILayout.Width(160)))
            {
                StaticOcclusionCulling.Cancel();
                AssetDatabase.SaveAssets();
                UnityEngine.Debug.Log("SOC bake operation cancelled!");
            }

            if (GUILayout.Button("Clear Current Data", GUILayout.Width(160)))
            {
                StaticOcclusionCulling.Clear();
                AssetDatabase.SaveAssets();
                UnityEngine.Debug.Log("SOC bake data cleared");
            }

            if (GUILayout.Button("Remove Cache Data", GUILayout.Width(160)))
            {
                StaticOcclusionCulling.RemoveCacheFolder();
                AssetDatabase.SaveAssets();
                UnityEngine.Debug.Log("SOC cache data has been deleted!");
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    public void OcclussionBakeProfiler()
    {
        Stopwatch stopwatch = new Stopwatch();

        showTest = EditorGUILayout.BeginFoldoutHeaderGroup(showTest, "Static Occlusion Profiler");

        if(showTest)
        {
            if (GUILayout.Button("Run Static Occlusuion Test", GUILayout.Width(200)))
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
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    //TODO: Find a way to get the lib folder path
    void DisplayContextInformation()
    {
        float umbraSize = StaticOcclusionCulling.umbraDataSize;

        showUmbraInfo = EditorGUILayout.BeginFoldoutHeaderGroup(showUmbraInfo, "Umbra Cache Utils");

        if(showUmbraInfo)
        {
            if (StaticOcclusionCulling.isRunning)
            {
                EditorGUILayout.HelpBox("SOC bake is running", MessageType.Info);
            }

            if (StaticOcclusionCulling.doesSceneHaveManualPortals == true)
            {
                EditorGUILayout.HelpBox("Current scene has manual occlusion portals", MessageType.Info);
            }

            GUILayout.Label($"Umbra data size in Bytes: {umbraSize}");
            GUILayout.Label($"Umbra data size in Kilobytes: {umbraSize / 1024}");
            GUILayout.Label($"Umbra data size in Megabytes: {umbraSize / 1024 / 1024}");

            EditorGUILayout.BeginVertical();
            if (Directory.Exists(@"Library/Occlusion"))
            {
                if (GUILayout.Button("Open Umbra Folder", GUILayout.Width(200)))
                {
                    UnityEngine.Debug.Log("Opening Umbra data folder");
                    Process.Start("explorer.exe");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No Umbra cache present", MessageType.Info);
            }

            if (GUILayout.Button("Open Umbra Log File", GUILayout.Width(200)))
            {
                File.Open(@"E:\Projects\FFXR\Library\Occlusion\log.txt", FileMode.Open);
            }

            if (GUILayout.Button("Write Umbra Log to Console", GUILayout.Width(200)))
            {
                WriteUmbraLogToConsole();
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void OcclusionSceneTools()
    {
        showSceneTool = EditorGUILayout.BeginFoldoutHeaderGroup(showSceneTool, "Custom Occlusion Scene Tools");

        if(showSceneTool)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            spawnOnAsset = EditorGUILayout.ToggleLeft("Spawn Asset at Target", spawnOnAsset);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            isPortalOpen = EditorGUILayout.ToggleLeft("Portal Creation State", isPortalOpen);
            EditorGUILayout.EndHorizontal();

            occlusionPortalTemplate = EditorGUILayout.ObjectField("Occlusion Portal Template", occlusionPortalTemplate, typeof(GameObject), true, GUILayout.Width(350));
            occlusionAreaTemplate = EditorGUILayout.ObjectField("Occlusion Area Template", occlusionAreaTemplate, typeof(GameObject), true, GUILayout.Width(350));

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

    //TODO: Write paths to file and output path location
    void DisplayProjectOcclusionInformation()
    {
        string[] ocDataFiles = AssetDatabase.FindAssets("OcclusionCullingData");

        int totalOcDataFileCount = ocDataFiles.Length;

        showSocInfo = EditorGUILayout.BeginFoldoutHeaderGroup(showSocInfo, "Static Occlusion Information");

        if(showSocInfo)
        {
            GUILayout.Label($"# of occlusion data files in project: {totalOcDataFileCount}");
            GUILayout.Label($"Size of project occlusion data: {totalOcDataFileCount}");

            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Occlusion Paths to Console",GUILayout.Width(180)))
            {
                GetProjectOcclusionData();
            }

            if (GUILayout.Button("Occlusion Paths to File", GUILayout.Width(180)))
            {
                WriteOcclusionDataToFile();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Occlusion File to Console", GUILayout.Width(180)))
            {
                OcclusionFileDataToConsole();
            }

            if(GUILayout.Button("Update Occlusion Dictionary",GUILayout.Width(180)))
            {
                UpdateOcclusionDictionary();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Project Occlusion Size", GUILayout.Width(180)))
            {
                CalculateOverallOcclusionDataSize();
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void DisplayCustomGroupCreator()
    {
        showCustomGroupTools = EditorGUILayout.BeginFoldoutHeaderGroup(showCustomGroupTools, "Custom Group Creator");

        if(showCustomGroupTools)
        {
            EditorGUILayout.BeginVertical();
            int numberOfSpheres = EditorGUILayout.IntField("# of Bounding Spheres", 100, GUILayout.Width(200));
            float baseRadius = EditorGUILayout.FloatField("Base Radius Size", 10f, GUILayout.Width(200));
            float radiusStepValue = EditorGUILayout.FloatField("Radius Increment Value", 20f, GUILayout.Width(200));

            if (GUILayout.Button("Create Custom Culling Group", GUILayout.Width(200)))
            {
                CreateCustomCullingGroup(numberOfSpheres, baseRadius, radiusStepValue);
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
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            StaticOcclusionCullingVisualization.showDynamicObjectBounds = EditorGUILayout.ToggleLeft("Show Dynamic Object Bounds", StaticOcclusionCullingVisualization.showDynamicObjectBounds);
            StaticOcclusionCullingVisualization.showGeometryCulling = EditorGUILayout.ToggleLeft("Show Geometry Culling", StaticOcclusionCullingVisualization.showGeometryCulling);
            StaticOcclusionCullingVisualization.showOcclusionCulling = EditorGUILayout.ToggleLeft("Show Occlusion Culling", StaticOcclusionCullingVisualization.showOcclusionCulling);
            StaticOcclusionCullingVisualization.showPortals = EditorGUILayout.ToggleLeft("Show Portals", StaticOcclusionCullingVisualization.showPortals);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            StaticOcclusionCullingVisualization.showPreVisualization = EditorGUILayout.ToggleLeft("Show Pre-Visualization", StaticOcclusionCullingVisualization.showPreVisualization);
            StaticOcclusionCullingVisualization.showViewVolumes = EditorGUILayout.ToggleLeft("Show View Volumes", StaticOcclusionCullingVisualization.showViewVolumes);
            StaticOcclusionCullingVisualization.showVisibilityLines = EditorGUILayout.ToggleLeft("Show Visibility Lines", StaticOcclusionCullingVisualization.showVisibilityLines);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    #endregion

    #region Functional Methods
    void WriteUmbraLogToConsole()
    {
        string[] lines = File.ReadAllLines(@"E:\Projects\FFXR\Library\Occlusion\log.txt");

        foreach (string line in lines)
        {
            UnityEngine.Debug.Log($"[UMBRA LOG] {line}");
        }
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
                UnityEngine.Debug.Log($"Occlusion data: {path}:{file}");
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

    //TODO: Complete component and introduce AddComponent<CreateCustomCullingGroup>
    public Component CreateCustomCullingGroup(int sphereCount, float baseRadius, float radiusStepValue)
    {
        UnityEngine.Debug.Log($"Creating a new culling group: count {sphereCount} || base radius {baseRadius} || radius step value {radiusStepValue}");

        BoundingSphere[] bSpheres = new BoundingSphere[sphereCount];

        bSpheres[0] = new BoundingSphere(Vector3.zero, baseRadius);

        for(int i = 0; i > bSpheres.Length; i++)
        {
            bSpheres[i].radius = radiusStepValue;
        }

        CullingGroup newCullingGroup = new CullingGroup()
        {
            targetCamera = Camera.main,
            enabled = true,
            onStateChanged = CullingStateChange
        };

        newCullingGroup.SetBoundingSpheres(bSpheres);
        newCullingGroup.SetBoundingSphereCount(1);
        newCullingGroup.Dispose();
        newCullingGroup = null;

        return null;
    }

    void CullingStateChange(CullingGroupEvent evt)
    {
        if (evt.hasBecomeVisible)
        {
            UnityEngine.Debug.LogFormat("Asset {0} has become visible!", evt.index);
            UnityEngine.Debug.LogFormat("Asset {0} is this distance from the culling group", evt.currentDistance);
        }

        if (evt.hasBecomeInvisible)
        {
            UnityEngine.Debug.LogFormat("Asset {0} has become invisible!", evt.index);
        }
    }

    void ResetBakeParametersToDefault()
    {
        UnityEngine.Debug.Log($"Resetting bake parameters to default values, [Backface Threshold:{defBackfaceThreshold}], [Smallest Hole:{defSmallestHole}], [Smallest Occluder:{defSmallestOccluder}]");

        StaticOcclusionCulling.backfaceThreshold = defBackfaceThreshold;
        StaticOcclusionCulling.smallestHole = defSmallestHole;
        StaticOcclusionCulling.smallestOccluder = defSmallestOccluder;
    }

    //TODO: Take target file from object field or dictionary
    void OcclusionFileDataToConsole()
    {
        string[] ocDataFiles = AssetDatabase.FindAssets("OcclusionCullingData");

        foreach(string line in ocDataFiles)
        {
            string path = AssetDatabase.GUIDToAssetPath(line);
            string[] fileContents = File.ReadAllLines(path);

            foreach(string text in fileContents)
            {
                UnityEngine.Debug.Log(text);
            }
        }
    }

    //TODO: Fix output and seperate scene line
    void UpdateOcclusionDictionary()
    {
        string[] ocDataFiles = AssetDatabase.FindAssets("OcclusionCullingData");

        foreach (string ocGuidLine in ocDataFiles)
        {
            string path = AssetDatabase.GUIDToAssetPath(ocGuidLine);
            string[] fileContents = File.ReadAllLines(path);

            foreach (string text in fileContents)
            {
                if(text.Contains("scene: "))
                {
                    UnityEngine.Debug.Log(text);
                }
            }
        }
    }
    //TODO: Write method to compile overall size of OC data on a project !Not Umbra

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
            UnityEngine.Debug.Log($"File size for {path} is {fileSize/1024/1024} megabytes");
        }
    }

    void VerifyGuidDictionaryExistance(GUID ocGuid, GUID sceneGuid)
    {
        if(sceneOcclusionPairs.ContainsKey(ocGuid) || sceneOcclusionPairs.ContainsValue(sceneGuid))
        {
            UnityEngine.Debug.LogError("Occlusion or scene guid already exists in dictionary!");
        }
        else
        {
            UnityEngine.Debug.Log($"Adding new entry to dictionary, occlusion GUID:{ocGuid} scene GUID:{sceneGuid}");
            sceneOcclusionPairs.Add(ocGuid, sceneGuid);
            AssetDatabase.SaveAssets();
        }
    }
    #endregion
}