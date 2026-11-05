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
        foreach (var camera in new[] { first, second })
        {
            first.enabled = camera == first;
            second.enabled = camera == second;
            yield return null;
            Canvas.ForceUpdateCanvases();
            foreach (var button in buttons)
            {
                var rect = (RectTransform)button.transform;
                var point = RectTransformUtility.WorldToScreenPoint(overlay, rect.TransformPoint(rect.rect.center));
                var hits = new List<RaycastResult>();
                es.RaycastAll(new PointerEventData(es) { position = point }, hits);
                Assert.That(hits.Count, Is.GreaterThan(0), button.name);
                Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.EqualTo(button));
            }
        }
        GraphicsSettings.defaultRenderPipeline = previous;
        Object.Destroy(pipeline);
        Object.Destroy(renderer);
    }
}
