using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SOCWizard.Data;

namespace SOCWizard
{
    public class SOCWizardLogic
    {
        private UnityEngine.Object _occlusionPortalTemplate;
        private UnityEngine.Object _occlusionAreaTemplate;
        private GameObject _target;
        readonly SOCWizardData data = new();

        public void WriteUmbraLogToConsole()
        {
            var lines = File.ReadAllLines(@"Library\Occlusion\log.txt");

            foreach (var line in lines)
            {
                UnityEngine.Debug.Log($"[UMBRA LOG] {line}");
            }
        }

        public void RunProfileTest()
        {
            UnityEngine.Debug.Log($"Clearing old SOC data.");
            StaticOcclusionCulling.Clear();
            BakeStaticOcclusion();
            StaticOcclusionCulling.Clear();
            BackgroundBakeStaticOcclusion();
        }

        public void GenerateOcclusionPortal()
        {
            GameObject newOcclusionPortal;

            _target = Selection.activeGameObject;

            if (data._spawnOnAsset)
            {
                UnityEngine.Debug.Log($"Spawning occlusion portal on object: {_target.name} at transform: {Vector3.zero}");
                newOcclusionPortal = UnityEngine.GameObject.Instantiate(_occlusionPortalTemplate, _target.transform.position, Quaternion.identity) as GameObject;

                if (newOcclusionPortal is not null)
                {
                    var portalState = newOcclusionPortal.GetComponent<OcclusionPortal>();
                    portalState.open = data._isPortalOpen;
                }
            }
            else
            {
                UnityEngine.Debug.Log($"Spawning occlusion portal at world origin");
                newOcclusionPortal = UnityEngine.GameObject.Instantiate(_occlusionPortalTemplate, Vector3.zero, Quaternion.identity) as GameObject;
            }

            if (newOcclusionPortal is not null)
                newOcclusionPortal.name = $"CustomOcclusionPortal_{newOcclusionPortal.transform.position}";

            AssetDatabase.SaveAssets();
        }

        public void GenerateOcclusionArea()
        {
            GameObject newOcclusionArea;

            _target = Selection.activeGameObject;

            if (data._spawnOnAsset)
            {
                var position1 = _target.transform.position;
                UnityEngine.Debug.Log($"Spawning occlusion area on object: {_target.name} at transform: {position1}");
                newOcclusionArea = UnityEngine.GameObject.Instantiate(_occlusionAreaTemplate, position1, Quaternion.identity) as GameObject;

                if (newOcclusionArea is not null)
                {
                    var occlusionArea = newOcclusionArea.GetComponent<OcclusionArea>();
                    occlusionArea.size = _target.transform.localScale;
                }
            }
            else
            {
                UnityEngine.Debug.Log($"Spawning occlusion area at world origin");
                newOcclusionArea = UnityEngine.GameObject.Instantiate(_occlusionAreaTemplate, Vector3.zero, Quaternion.identity) as GameObject;
            }

            if (newOcclusionArea is not null)
                newOcclusionArea.name = $"CustomOcclusionArea_{newOcclusionArea.transform.position}";

            AssetDatabase.SaveAssets();
        }

        public void GetProjectOcclusionData()
        {
            var ocDataFiles = AssetDatabase.FindAssets("OcclusionCullingData");

            if (ocDataFiles.Length == 0)
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

        public void WriteOcclusionDataToFile()
        {
            var lines = AssetDatabase.FindAssets("OcclusionCullingData");

            if (lines.Length == 0)
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

        public void LogOcclusionParameters()
        {
            UnityEngine.Debug.Log($"[SOC-PARAM] Backface Threshold: {StaticOcclusionCulling.backfaceThreshold}");
            UnityEngine.Debug.Log($"[SOC-PARAM] Smallest Hole: {StaticOcclusionCulling.smallestHole}");
            UnityEngine.Debug.Log($"[SOC-PARAM] Smallest Occluder: {StaticOcclusionCulling.smallestOccluder}");
        }

        public void BakeStaticOcclusion()
        {
            var currentScene = SceneManager.GetActiveScene();
            var currentSceneName = currentScene.name;

            var stopwatch = new Stopwatch();
            UnityEngine.Debug.Log($"Starting SOC bake of scene {currentSceneName} at: {System.DateTime.Now}");
            LogOcclusionParameters();
            stopwatch.Start();
            StaticOcclusionCulling.Compute();
            stopwatch.Stop();

            UnityEngine.Debug.Log($"Time to complete bake in milliseconds: {stopwatch.ElapsedMilliseconds}");
            UnityEngine.Debug.Log($"SOC bake completed at: {System.DateTime.Now}");

            AssetDatabase.SaveAssets();
            WriteUmbraLogToConsole();
        }

        public void BackgroundBakeStaticOcclusion()
        {
            var currentScene = SceneManager.GetActiveScene();
            var currentSceneName = currentScene.name;

            var stopwatch = new Stopwatch();
            UnityEngine.Debug.Log($"Starting SOC background bake of scene {currentSceneName} at: {System.DateTime.Now}");
            LogOcclusionParameters();
            stopwatch.Start();
            StaticOcclusionCulling.GenerateInBackground();
            stopwatch.Stop();

            UnityEngine.Debug.Log($"Time to complete background bake in milliseconds: {stopwatch.ElapsedMilliseconds}");
            UnityEngine.Debug.Log($"SOC background bake completed at: {System.DateTime.Now}");

            AssetDatabase.SaveAssets();
            WriteUmbraLogToConsole();
        }

        public void ResetBakeParametersToDefault()
        {
            UnityEngine.Debug.Log($"Resetting bake parameters to default values, [Backface Threshold:{data.DefBackfaceThreshold}], [Smallest Hole:{data.DefSmallestHole}], [Smallest Occluder:{data.DefSmallestOccluder}]");

            StaticOcclusionCulling.backfaceThreshold = data.DefBackfaceThreshold;
            StaticOcclusionCulling.smallestHole = data.DefSmallestHole;
            StaticOcclusionCulling.smallestOccluder = data.DefSmallestOccluder;
        }

        //TODO: Take target file from object field or dictionary
        public void OcclusionFileDataToConsole()
        {
            var ocDataFiles = AssetDatabase.FindAssets("OcclusionCullingData");

            foreach (var line in ocDataFiles)
            {
                var path = AssetDatabase.GUIDToAssetPath(line);
                var fileContents = File.ReadAllLines(path);

                foreach (var text in fileContents)
                {
                    UnityEngine.Debug.Log(text);
                }
            }
        }

        public void CalculateOverallOcclusionDataSize()
        {
            var ocDataFiles = AssetDatabase.FindAssets("OcclusionCullingData");

            foreach (var ocFile in ocDataFiles)
            {
                var path = AssetDatabase.GUIDToAssetPath(ocFile);
                var fInfo = new FileInfo(path);

                var fileSize = fInfo.Length;

                UnityEngine.Debug.Log($"File size for {path} is {fileSize / 1024} kilobytes");
                UnityEngine.Debug.Log($"File size for {path} is {fileSize / (1024 * 1024)} megabytes");
            }
        }
    }
}
