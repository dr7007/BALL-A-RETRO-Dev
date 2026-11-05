using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// Run against an isolated fixture containing the unmodified UI objects extracted
// from Main_Scene_Tutorial; never open or save the developer's running scene.
public class TutorialCanvasTests
{
    [UnityTest]
    public IEnumerator TutorialCanvases_RenderAndReceiveClicksAcrossCameraSwitches()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
            "Assets/TutorialUI.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
        var pipeline = UniversalRenderPipelineAsset.Create(renderer);
        var previous = GraphicsSettings.defaultRenderPipeline;
        GraphicsSettings.defaultRenderPipeline = pipeline;
        yield return null;
        var renderGraph = GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
        var previousCompatibility = renderGraph.enableRenderCompatibilityMode;
        // Match the game's URP compatibility renderer.
        renderGraph.enableRenderCompatibilityMode = true;
        yield return null;
        var first = new GameObject("Gameplay Camera", typeof(Camera)).GetComponent<Camera>();
        first.cullingMask = 0;
        first.backgroundColor = Color.black;
        first.clearFlags = CameraClearFlags.SolidColor;
        var second = new GameObject("Alternate Gameplay Camera", typeof(Camera)).GetComponent<Camera>();
        second.cullingMask = 0;
        second.backgroundColor = Color.black;
        second.clearFlags = CameraClearFlags.SolidColor;
        second.enabled = false;
        var overlay = GameObject.Find("Tutorial UI Camera").GetComponent<Camera>();
        first.GetUniversalAdditionalCameraData().cameraStack.Add(overlay);
        second.GetUniversalAdditionalCameraData().cameraStack.Add(overlay);
        var es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Assert.That(canvases.Length, Is.EqualTo(2));
        foreach (var canvas in canvases)
        {
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceCamera));
            Assert.That(canvas.worldCamera, Is.EqualTo(overlay));
            Assert.That(canvas.planeDistance, Is.EqualTo(100));
            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920, 1080)));
            Assert.That(scaler.matchWidthOrHeight, Is.Zero);
        }
        yield return null;
        yield return null;
        var buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        Assert.That(buttons.Length, Is.EqualTo(3));
        try
        {
            foreach (var resolution in new[] { new Vector2Int(1920, 1080), new Vector2Int(2560, 1440) })
            {
                var target = RenderTexture.GetTemporary(resolution.x, resolution.y, 24);
                first.targetTexture = second.targetTexture = overlay.targetTexture = target;
                try
                {
                    foreach (var camera in new[] { first, second })
                    {
                        first.enabled = camera == first;
                        second.enabled = camera == second;
                        yield return null;
                        Canvas.ForceUpdateCanvases();
                        var pixels = RenderUI(camera, target);
                        try
                        {
                            foreach (var button in buttons)
                            {
                                var rect = (RectTransform)button.transform;
                                var point = RectTransformUtility.WorldToScreenPoint(overlay, rect.TransformPoint(rect.rect.center));
                                var hits = new List<RaycastResult>();
                                es.RaycastAll(new PointerEventData(es) { position = point }, hits);
                                Assert.That(hits.Count, Is.GreaterThan(0), button.name);
                                Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.EqualTo(button));
                                AssertButtonVisible(pixels, overlay, rect);
                            }
                            var path = System.IO.Path.Combine(Application.dataPath, "..", $"tutorial-ui-{resolution.x}-{camera.name}.png");
                            System.IO.File.WriteAllBytes(path, pixels.EncodeToPNG());
                        }
                        finally { Object.Destroy(pixels); }
                    }
                }
                finally
                {
                    first.targetTexture = second.targetTexture = overlay.targetTexture = null;
                    RenderTexture.ReleaseTemporary(target);
                }
            }
        }
        finally
        {
            renderGraph.enableRenderCompatibilityMode = previousCompatibility;
            GraphicsSettings.defaultRenderPipeline = previous;
            Object.Destroy(pipeline);
            Object.Destroy(renderer);
        }
    }

    private static void EmitCameraUI(ScriptableRenderContext context, Camera camera)
    {
        // Offscreen render requests need the native UI geometry explicitly emitted.
        // https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.ScriptableRenderContext.EmitGeometryForCamera.html
        ScriptableRenderContext.EmitGeometryForCamera(camera);
    }

    private static Texture2D RenderUI(Camera camera, RenderTexture target)
    {
        var previous = RenderTexture.active;
        RenderPipelineManager.beginCameraRendering += EmitCameraUI;
        try
        {
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            RenderTexture.active = target;
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            pixels.Apply();
            return pixels;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderPipelineManager.beginCameraRendering -= EmitCameraUI;
        }
    }

    private static void AssertButtonVisible(Texture2D pixels, Camera camera, RectTransform rect)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        var min = camera.WorldToScreenPoint(corners[0]);
        var max = camera.WorldToScreenPoint(corners[2]);
        Assert.That(min.x, Is.GreaterThanOrEqualTo(0), rect.name);
        Assert.That(min.y, Is.GreaterThanOrEqualTo(0), rect.name);
        Assert.That(max.x, Is.LessThanOrEqualTo(pixels.width), rect.name);
        Assert.That(max.y, Is.LessThanOrEqualTo(pixels.height), rect.name);
        int brightPixels = 0;
        for (int y = Mathf.CeilToInt(min.y); y < Mathf.FloorToInt(max.y); y++)
            for (int x = Mathf.CeilToInt(min.x); x < Mathf.FloorToInt(max.x); x++)
            {
                var color = pixels.GetPixel(x, y);
                if (color.r > .6f && color.g > .6f && color.b > .6f) brightPixels++;
            }
        Assert.That(brightPixels, Is.GreaterThan(20), $"{rect.name} must render visible text at {pixels.width}x{pixels.height}.");
    }
}
