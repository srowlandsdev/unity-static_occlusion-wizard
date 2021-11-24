using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor.Code
{
    public class SocWizard : EditorWindow
    {
        #region Variables

        private bool _bakeEditorBuildList;
        private bool _spawnOnAsset;
        private bool _isPortalOpen;
        private bool _showBakeTools;
        private bool _showSceneTool;
        private bool _showTest;
        private bool _showUmbraInfo;
        private bool _showSocInfo;
        private bool _showCustomGroupTools;
        private bool _showVisOptions;
        
        private UnityEngine.Object _occlusionPortalTemplate;
        private UnityEngine.Object _occlusionAreaTemplate;

        private GameObject _target;

        private Vector2 _scrollViewPosition;

        private const float DefBackfaceThreshold = 100;
        private const float DefSmallestHole = 0.25f;
        private const float DefSmallestOccluder = 5;
        #endregion

        [MenuItem("External Tools/SOC Wizard")]
        private static void GetSocWindow()
        {
            var window = (SocWizard)GetWindow(typeof(SocWizard));
            window.minSize = new Vector2(360, 880);
            window.Show();
        }

        private void OnGUI()
        {
            ToolsDisplayController();
        }

        private void ToolsDisplayController()
        {
            _scrollViewPosition = EditorGUILayout.BeginScrollView(_scrollViewPosition);
            EditorGUILayout.BeginVertical();
            GUILayout.Space(15);
            DisplayOcclusionTools();
            GUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            OcclusionSceneTools();
            GUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            OcclusionBakeProfiler();
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
        //TODO: Fix scaling issue with right column in bake tools and Vis options
        private void DisplayOcclusionTools()
        {
            _showBakeTools = EditorGUILayout.BeginFoldoutHeaderGroup(_showBakeTools, "Bake Parameters & Tools");

            if(_showBakeTools)
            {
                EditorGUILayout.BeginVertical();

                _bakeEditorBuildList = EditorGUILayout.ToggleLeft("Bake Editor Build Scene List", _bakeEditorBuildList);

                StaticOcclusionCulling.backfaceThreshold = EditorGUILayout.FloatField("Backface Threshold", StaticOcclusionCulling.backfaceThreshold, GUILayout.Width(200));
                StaticOcclusionCulling.smallestHole = EditorGUILayout.FloatField("Smallest Hole", StaticOcclusionCulling.smallestHole, GUILayout.Width(200));
                StaticOcclusionCulling.smallestOccluder = EditorGUILayout.FloatField("Smallest Occluder", StaticOcclusionCulling.smallestOccluder, GUILayout.Width(200));

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                if (GUILayout.Button("Generate", GUILayout.Width(160)))
                {
                    if (_bakeEditorBuildList)
                    {
                        UnityEngine.Debug.Log("SOC Baking all scenes in editor build scene list");
                        foreach (var scene in EditorBuildSettings.scenes)
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
                    if (_bakeEditorBuildList)
                    {
                        UnityEngine.Debug.Log("SOC Baking all scene in editor build scene list");
                        foreach (var scene in EditorBuildSettings.scenes)
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
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
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
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void OcclusionBakeProfiler()
        {
            _showTest = EditorGUILayout.BeginFoldoutHeaderGroup(_showTest, "Static Occlusion Profiler");

            if (_showTest)
            {
                if (GUILayout.Button("Run Static Occlusion Test", GUILayout.Width(200)))
                {
                    RunProfileTest();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        //TODO: Find a way to get the lib folder path
        private void DisplayContextInformation()
        {
            long umbraSize = StaticOcclusionCulling.umbraDataSize;
            var umbraSizeKb = umbraSize / 1024;
            var umbraSizeMb = umbraSize / (1024*1024);

            _showUmbraInfo = EditorGUILayout.BeginFoldoutHeaderGroup(_showUmbraInfo, "Umbra Cache Utils");

            if(_showUmbraInfo)
            {
                if (StaticOcclusionCulling.isRunning)
                {
                    EditorGUILayout.HelpBox("SOC bake is running", MessageType.Info);
                }

                if (StaticOcclusionCulling.doesSceneHaveManualPortals)
                {
                    EditorGUILayout.HelpBox("Current scene has manual occlusion portals", MessageType.Info);
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
                        Process.Start("explorer.exe");
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

        private void OcclusionSceneTools()
        {
            _showSceneTool = EditorGUILayout.BeginFoldoutHeaderGroup(_showSceneTool, "Custom Occlusion Scene Tools");

            if(_showSceneTool)
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.BeginHorizontal();
                _spawnOnAsset = EditorGUILayout.ToggleLeft("Spawn Asset at Target", _spawnOnAsset);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                _isPortalOpen = EditorGUILayout.ToggleLeft("Portal Creation State", _isPortalOpen);
                EditorGUILayout.EndHorizontal();

                _occlusionPortalTemplate = EditorGUILayout.ObjectField("Occlusion Portal Template", _occlusionPortalTemplate, typeof(GameObject), true, GUILayout.Width(350));
                _occlusionAreaTemplate = EditorGUILayout.ObjectField("Occlusion Area Template", _occlusionAreaTemplate, typeof(GameObject), true, GUILayout.Width(350));

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
        private void DisplayProjectOcclusionInformation()
        {
            var ocDataFiles = AssetDatabase.FindAssets("OcclusionCullingData");

            var totalOcDataFileCount = ocDataFiles.Length;

            _showSocInfo = EditorGUILayout.BeginFoldoutHeaderGroup(_showSocInfo, "Static Occlusion Information");

            if(_showSocInfo)
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

                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Project Occlusion Size", GUILayout.Width(180)))
                {
                    CalculateOverallOcclusionDataSize();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DisplayVisualizationOptions()
        {
            _showVisOptions = EditorGUILayout.BeginFoldoutHeaderGroup(_showVisOptions, "Visualization Options");

            if(_showVisOptions)
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

        private static void WriteUmbraLogToConsole()
        {
            var lines = File.ReadAllLines(@"E:\Projects\FFXR\Library\Occlusion\log.txt");

            foreach (var line in lines)
            {
                UnityEngine.Debug.Log($"[UMBRA LOG] {line}");
            }
        }

        private void RunProfileTest()
        {
            UnityEngine.Debug.Log($"Clearing old SOC data.");
            StaticOcclusionCulling.Clear();
            BakeStaticOcclusion();
            StaticOcclusionCulling.Clear();
            BackgroundBakeStaticOcclusion();
        }

        private void GenerateOcclusionPortal()
        {
            GameObject newOcclusionPortal;

            _target = Selection.activeGameObject;

            if (_spawnOnAsset)
            {
                UnityEngine.Debug.Log($"Spawning occlusion portal on object: {_target.name} at transform: {Vector3.zero}");
                newOcclusionPortal = Instantiate(_occlusionPortalTemplate, _target.transform.position, Quaternion.identity) as GameObject;

                if (newOcclusionPortal is not null)
                {
                    var portalState = newOcclusionPortal.GetComponent<OcclusionPortal>();
                    portalState.open = _isPortalOpen;
                }
            }
            else
            {
                UnityEngine.Debug.Log($"Spawning occlusion portal at world origin");
                newOcclusionPortal = Instantiate(_occlusionPortalTemplate, Vector3.zero, Quaternion.identity) as GameObject;
            }

            if (newOcclusionPortal is not null)
                newOcclusionPortal.name = $"CustomOcclusionPortal_{newOcclusionPortal.transform.position}";

            AssetDatabase.SaveAssets();
        }

        private void GenerateOcclusionArea()
        {
            GameObject newOcclusionArea;

            _target = Selection.activeGameObject;

            if (_spawnOnAsset)
            {
                var position1 = _target.transform.position;
                UnityEngine.Debug.Log($"Spawning occlusion area on object: {_target.name} at transform: {position1}");
                newOcclusionArea = Instantiate(_occlusionAreaTemplate, position1, Quaternion.identity) as GameObject;

                if (newOcclusionArea is not null)
                {
                    var occlusionArea = newOcclusionArea.GetComponent<OcclusionArea>();
                    occlusionArea.size = _target.transform.localScale;
                }
            }
            else
            {
                UnityEngine.Debug.Log($"Spawning occlusion area at world origin");
                newOcclusionArea = Instantiate(_occlusionAreaTemplate, Vector3.zero, Quaternion.identity) as GameObject;
            }

            if (newOcclusionArea is not null)
                newOcclusionArea.name = $"CustomOcclusionArea_{newOcclusionArea.transform.position}";

            AssetDatabase.SaveAssets();
        }

        private static void GetProjectOcclusionData()
        {
            var ocDataFiles = AssetDatabase.FindAssets("OcclusionCullingData");

            if(ocDataFiles.Length == 0)
            {
                UnityEngine.Debug.Log($"No occlusion paths present in project");
            }
            else
            {
                foreach (var file in ocDataFiles)
                {
                    var path = AssetDatabase.GUIDToAssetPath(file);
                    UnityEngine.Debug.Log($"Occlusion asset path and guid: {path} : {file}");
                }
            }
        }

        private static void WriteOcclusionDataToFile()
        {
            var lines = AssetDatabase.FindAssets("OcclusionCullingData");

            if(lines.Length == 0)
            {
                UnityEngine.Debug.Log($"No occlusion paths present in project");
            }
            else
            {
                UnityEngine.Debug.Log(Application.temporaryCachePath + "WriteLines.txt");

                using var outputFile = new StreamWriter(Application.temporaryCachePath + "WriteLines.txt");
                foreach (var text in lines)
                {
                    var path = AssetDatabase.GUIDToAssetPath(text);
                    outputFile.WriteLine($"{path}:{text}");
                }
            }
        }

        private static void LogOcclusionParameters()
        {
            UnityEngine.Debug.Log($"[SOC-PARAM] Backface Threshold: {StaticOcclusionCulling.backfaceThreshold}");
            UnityEngine.Debug.Log($"[SOC-PARAM] Smallest Hole: {StaticOcclusionCulling.smallestHole}");
            UnityEngine.Debug.Log($"[SOC-PARAM] Smallest Occluder: {StaticOcclusionCulling.smallestOccluder}");
        }

        private void BakeStaticOcclusion()
        {
            var currentScene = SceneManager.GetActiveScene();
            var currentSceneName = currentScene.name;

            var stopwatch = new Stopwatch();
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

        private void BackgroundBakeStaticOcclusion()
        {
            var currentScene = SceneManager.GetActiveScene();
            var currentSceneName = currentScene.name;

            var stopwatch = new Stopwatch();
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

        private static void ResetBakeParametersToDefault()
        {
            UnityEngine.Debug.Log($"Resetting bake parameters to default values, [Backface Threshold:{DefBackfaceThreshold}], [Smallest Hole:{DefSmallestHole}], [Smallest Occluder:{DefSmallestOccluder}]");

            StaticOcclusionCulling.backfaceThreshold = DefBackfaceThreshold;
            StaticOcclusionCulling.smallestHole = DefSmallestHole;
            StaticOcclusionCulling.smallestOccluder = DefSmallestOccluder;
        }

        //TODO: Take target file from object field or dictionary
        private static void OcclusionFileDataToConsole()
        {
            var ocDataFiles = AssetDatabase.FindAssets("OcclusionCullingData");

            foreach(var line in ocDataFiles)
            {
                var path = AssetDatabase.GUIDToAssetPath(line);
                var fileContents = File.ReadAllLines(path);

                foreach(var text in fileContents)
                {
                    UnityEngine.Debug.Log(text);
                }
            }
        }

        private static void CalculateOverallOcclusionDataSize()
        {
            var ocDataFiles = AssetDatabase.FindAssets("OcclusionCullingData");

            foreach(var ocFile in ocDataFiles)
            {
                var path = AssetDatabase.GUIDToAssetPath(ocFile);
                var fInfo = new FileInfo(path);

                var fileSize = fInfo.Length;

                UnityEngine.Debug.Log($"File size for {path} is {fileSize/1024} kilobytes");
                UnityEngine.Debug.Log($"File size for {path} is {fileSize/(1024*1024)} megabytes");
            }
        }
        #endregion
    }
}