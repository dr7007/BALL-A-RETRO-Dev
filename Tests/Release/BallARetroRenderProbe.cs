#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// Isolated editor probe only: explicitly renders the real camera and UI at a
// fixed resolution. Counters are frame totals, not counts of Renderer objects.
public static class BallARetroRenderProbe
{
    [Serializable] class Frame { public long draws, batches, setPass, triangles; }
    [Serializable] class Report
    {
        public string scene, device, method;
        public string quality, pipeline;
        public bool[] effectSettings;
        public CameraInfo selectedCamera;
        public CameraInfo[] activeCameras;
        public int width = 1920, height = 1080;
        public Frame[] frames;
    }
    [Serializable] class CameraInfo
    {
        public string path, renderType, target;
        public string[] stack;
        public Vector3 position, euler;
        public int mask;
        public float fov, orthographicSize;
        public bool orthographic, postProcessing;
    }
    static CameraInfo Describe(Camera c)
    {
        var data = c.GetComponent<UniversalAdditionalCameraData>();
        return new CameraInfo {
            path = PathOf(c.transform), position = c.transform.position, euler = c.transform.eulerAngles,
            mask = c.cullingMask, fov = c.fieldOfView, orthographic = c.orthographic, orthographicSize = c.orthographicSize,
            target = c.targetTexture == null ? "display" : c.targetTexture.name,
            renderType = data == null ? "Base" : data.renderType.ToString(),
            postProcessing = data != null && data.renderPostProcessing,
            stack = data != null && data.renderType == CameraRenderType.Base
                ? data.cameraStack.Where(x => x != null).Select(x => PathOf(x.transform)).ToArray() : Array.Empty<string>()
        };
    }
    [Serializable] class RendererInfo
    {
        public string path, mesh, type;
        public string[] materials, ancestors;
        public bool enabled, active, batchingStatic, batched;
        public int layer, vertices, submeshes;
        public Bounds bounds;
    }
    [Serializable] class Inventory { public RendererInfo[] renderers; }
    static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    static void EmitUI(ScriptableRenderContext context, Camera camera) => ScriptableRenderContext.EmitGeometryForCamera(camera);

    public static IEnumerator Measure(string output, string label, Camera selectedCamera = null)
    {
        if (!Environment.GetCommandLineArgs().Contains("-measureRendering")) yield break;
        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        File.WriteAllText(Path.Combine(output, label + "-renderers.json"), JsonUtility.ToJson(new Inventory {
            renderers = renderers.Select(r => {
                var filter = r.GetComponent<MeshFilter>();
                var mesh = filter != null ? filter.sharedMesh : null;
                return new RendererInfo {
                    path = PathOf(r.transform), mesh = mesh == null ? "" : AssetDatabase.GetAssetPath(mesh) + ":" + mesh.name,
                    type = r.GetType().Name, enabled = r.enabled, active = r.gameObject.activeInHierarchy,
                    batchingStatic = GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.BatchingStatic),
                    batched = r.isPartOfStaticBatch, layer = r.gameObject.layer, bounds = r.bounds,
                    vertices = mesh == null ? 0 : mesh.vertexCount, submeshes = mesh == null ? 0 : mesh.subMeshCount,
                    materials = r.sharedMaterials.Select(m => m == null ? "null" : AssetDatabase.GetAssetPath(m) + ":" + m.name).ToArray(),
                    ancestors = r.GetComponentsInParent<MonoBehaviour>(true).Where(c => c != null)
                        .Select(c => c.GetType().Name + "@" + c.name).ToArray()
                };
            }).ToArray()
        }, true));
        var camera = selectedCamera != null ? selectedCamera : Camera.main;
        if (camera == null) throw new Exception("Rendering probe needs the scene main camera.");
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
            .Select(c => (canvas: c, camera: c.worldCamera, distance: c.planeDistance)).ToArray();
        var target = RenderTexture.GetTemporary(1920, 1080, 24);
        var oldTarget = camera.targetTexture;
        float oldAspect = camera.aspect, oldTimeScale = Time.timeScale;
        bool oldEnabled = camera.enabled;
        var frames = new List<Frame>();
        var effects = Enum.GetValues(typeof(PSH_Script_EffectSettings.Effect)).Cast<PSH_Script_EffectSettings.Effect>().ToArray();
        var previousEffects = effects.Select(PSH_Script_EffectSettings.Get).ToArray();
        using var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1);
        using var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count", 1);
        using var passes = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count", 1);
        using var triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count", 1);
        RenderPipelineManager.beginCameraRendering += EmitUI;
        try
        {
            if (!draws.Valid || !batches.Valid || !passes.Valid || !triangles.Valid)
                throw new Exception("Render counters unavailable.");
            foreach (var effect in effects) PSH_Script_EffectSettings.Set(effect, true);
            yield return null;
            Time.timeScale = 0;
            var selected = Describe(camera);
            var activeCameras = Camera.allCameras.OrderBy(c => PathOf(c.transform)).Select(Describe).ToArray();
            camera.enabled = false;
            camera.targetTexture = target;
            camera.aspect = 1920f / 1080;
            foreach (var c in canvases)
            {
                c.canvas.renderMode = RenderMode.ScreenSpaceCamera;
                c.canvas.worldCamera = camera;
                c.canvas.planeDistance = camera.nearClipPlane + .1f;
            }
            Canvas.ForceUpdateCanvases();
            for (int frame = 0; frame < 28; frame++)
            {
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                yield return null;
                if (frame >= 8) frames.Add(new Frame { draws = draws.LastValue, batches = batches.LastValue,
                    setPass = passes.LastValue, triangles = triangles.LastValue });
            }
            if (frames.Any(f => f.draws <= 0)) throw new Exception("Render measurement returned an empty frame.");
            File.WriteAllText(Path.Combine(output, label + "-rendering.json"), JsonUtility.ToJson(new Report {
                scene = label, device = SystemInfo.graphicsDeviceName,
                method = "Unity 6000.0.60f1 isolated editor; fixed 1920x1080 camera render request including UI; all effects ON; timeScale=0; 8 warmup + 20 measured frame totals; not player FPS",
                selectedCamera = selected, activeCameras = activeCameras,
                effectSettings = effects.Select(PSH_Script_EffectSettings.Get).ToArray(),
                quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                pipeline = GraphicsSettings.currentRenderPipeline == null ? "built-in" : GraphicsSettings.currentRenderPipeline.name,
                frames = frames.ToArray()
            }, true));
        }
        finally
        {
            foreach (var c in canvases)
            {
                c.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                c.canvas.worldCamera = c.camera;
                c.canvas.planeDistance = c.distance;
            }
            camera.targetTexture = oldTarget;
            camera.aspect = oldAspect;
            camera.enabled = oldEnabled;
            Time.timeScale = oldTimeScale;
            for (int i = 0; i < effects.Length; i++) PSH_Script_EffectSettings.Set(effects[i], previousEffects[i]);
            Canvas.ForceUpdateCanvases();
            RenderPipelineManager.beginCameraRendering -= EmitUI;
            RenderTexture.ReleaseTemporary(target);
        }
    }
}
#endif
