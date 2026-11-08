#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Copy into the isolated release audit only. No rolls, selections, score events,
// save changes, or scene edits: the gallery binds detached copies of real UI.
public static class BallARetroChoiceIconProbe
{
    const string IconFolder = "Assets/Public/Roguelike/GeneratedIcons/";
    const string LegacyIcon = "Assets/Public/Roguelike/testIcon.jpg";
    [Serializable] sealed class IconRecord
    {
        public int funcIdx;
        public string key, sprite, asset, texture;
        public Rect rect;
        public bool packed, cardBound, inventoryBound, resultBound;
        public bool cardPreserveAspect, inventoryPreserveAspect, resultPreserveAspect;
    }
    [Serializable] sealed class Report
    {
        public string scene, language, method, cardPrefab, inventoryPrefab, resultPrefab, atlas, atlasTextureFormat;
        public int sharedTextureCount, atlasTextureWidth, atlasTextureHeight, renderRequestsConsumingRandom, resultViewCount;
        public bool editorLegacyDependency, gameplayStateUnchanged, choiceHistoryUnchanged, timeScaleUnchanged, randomUnchanged;
        public string[] editorLegacyDirectReferrers, runtimeLegacyReferences;
        public IconRecord[] icons;
    }

    public static void ValidateAndCapture(string output, string label, Action<bool, string> check,
        bool captureGallery = true)
    {
        var controller = InScene<CJS_Script_ChoiceUIController>().Single();
        var roller = Field<CJS_Script_ChoiceRoller>(controller, "rollerBehaviour");
        var choices = roller.allChoices.OrderBy(c => c.funcIdx).ToArray();
        check(choices.Length == 9 && choices.Select(c => c.funcIdx).SequenceEqual(Enumerable.Range(0, 9)),
            label + ": all nine choice IDs are present exactly once");
        var cardPrefab = Field<CJS_Script_ChoiceCard>(controller, "cardPrefab");
        var inventoryPrefab = Field<CJS_Script_ChoiceSlot>(InScene<CJS_Script_ChoiceInventoryUI>().Single(), "slotPrefab");
        var resultViews = InScene<CJS_Script_ChoiceSummaryUI>().ToArray();
        var resultPrefabs = resultViews.Select(view => Field<CJS_Script_ChoiceSlot>(view, "slotPrefab")).ToArray();
        check(resultPrefabs.Length > 0 && resultPrefabs.All(prefab => prefab != null) && resultPrefabs.Distinct().Count() == 1,
            label + ": every results view references the same validated choice slot prefab");
        var resultPrefab = resultPrefabs[0];
        check(cardPrefab != null && inventoryPrefab != null && resultPrefab != null,
            label + ": card, inventory and results reference their real UI prefabs");
        check(choices.All(c => c.icon != null && c.localizationKey == "Choice_" + c.funcIdx
            && AssetDatabase.GetAssetPath(c.icon).StartsWith(IconFolder + "Choice_" + c.funcIdx + "_", StringComparison.Ordinal)),
            label + ": all nine icons map to their intended generated asset and localization key");
        check(choices.Select(c => c.icon).Distinct().Count() == 9,
            label + ": all nine choices have distinct sprites");
        check(choices.All(c => {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(c.icon)) as TextureImporter;
            return importer != null && importer.textureType == TextureImporterType.Sprite
                && importer.spriteImportMode == SpriteImportMode.Single && !importer.mipmapEnabled
                && importer.DoesSourceTextureHaveAlpha() && importer.alphaIsTransparency;
        }), label + ": generated icons retain source alpha and use sprite imports without mipmaps");

        var atlas = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { "Assets/Public/Roguelike" })
            .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<SpriteAtlas>)
            .SingleOrDefault(a => a != null && choices.All(c => a.CanBindTo(c.icon)));
        check(atlas != null && atlas.spriteCount == 9,
            label + ": one atlas contains exactly the nine generated icons");
        var packables = atlas.GetPackables();
        var packing = atlas.GetPackingSettings();
        check(packables.Length == 9 && packables.All(p => p is Sprite && choices.Any(c => c.icon == p))
            && atlas.IsIncludeInBuild() && !packing.enableRotation && !packing.enableTightPacking
            && packing.padding >= 4 && !atlas.GetTextureSettings().generateMipMaps,
            label + ": atlas packs only the nine sprites and preserves UI orientation and alpha padding");
        int textureCount = choices.Select(c => c.icon.texture).Distinct().Count();
        check(textureCount == 1 && choices.All(c => c.icon.packed),
            label + ": all nine runtime icons share one packed texture");
        var releaseScenes = new[] {
            "Assets/MainScenes/Main_Scene_Game.unity", "Assets/MainScenes/Main_Scene_Tutorial.unity"
        };
        var dependencies = AssetDatabase.GetDependencies(releaseScenes, true);
        bool legacyReferenced = dependencies.Contains(LegacyIcon);
        // Editor dependency caches retain source/overridden references. Record the
        // direct referrers for investigation; only the packed-player asset gate can
        // prove exclusion from the submitted build. Do not substitute this gallery
        // result for that gate.
        var editorReferrers = legacyReferenced ? dependencies.Where(path => path != LegacyIcon
            && (path.EndsWith(".unity") || path.EndsWith(".prefab") || path.EndsWith(".asset"))
            && AssetDatabase.GetDependencies(path, false).Contains(LegacyIcon)).ToArray() : Array.Empty<string>();
        var runtimeLegacy = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(i => i.sprite != null && AssetDatabase.GetAssetPath(i.sprite) == LegacyIcon)
            .Select(i => "Image: " + i.gameObject.scene.name + "/" + i.name)
            .Concat(Object.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(i => i.texture != null && AssetDatabase.GetAssetPath(i.texture) == LegacyIcon)
                .Select(i => "RawImage: " + i.gameObject.scene.name + "/" + i.name))
            .Concat(new Component[] { cardPrefab, inventoryPrefab, resultPrefab }
                .SelectMany(p => p.GetComponentsInChildren<Image>(true))
                .Where(i => i.sprite != null && AssetDatabase.GetAssetPath(i.sprite) == LegacyIcon)
                .Select(i => "Prefab Image: " + AssetDatabase.GetAssetPath(i) + "/" + i.name)).ToArray();
        File.WriteAllLines(Path.Combine(output, label + "-choice-icons-editor-legacy-referrers.txt"), editorReferrers);
        File.WriteAllLines(Path.Combine(output, label + "-choice-icons-editor-direct-dependencies.txt"),
            releaseScenes.SelectMany(scene => AssetDatabase.GetDependencies(scene, false).Select(path => scene + " -> " + path)));
        check(runtimeLegacy.Length == 0,
            label + ": active/inactive runtime UI and choice UI prefabs have no old icon sheet references");

        var records = choices.Select(c => new IconRecord {
            funcIdx = c.funcIdx, key = c.localizationKey, sprite = c.icon.name,
            asset = AssetDatabase.GetAssetPath(c.icon), texture = c.icon.texture.name,
            rect = c.icon.rect, packed = c.icon.packed
        }).ToArray();
        var randomBefore = UnityEngine.Random.state;
        int renderRandomChanges = 0;
        float timeScaleBefore = Time.timeScale;
        var choiceState = CJS_Script_ChoiceState.I;
        var pickedBefore = choiceState == null ? Array.Empty<CJS_ChoiceSnapshot>() : choiceState.Picked.ToArray();
        var gallery = new GameObject("Choice icon audit gallery", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var cameraObject = new GameObject("Choice icon audit camera", typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>();
        var target = RenderTexture.GetTemporary(1920, 1080, 24);
        try
        {
            gallery.hideFlags = HideFlags.DontSave;
            gallery.layer = 5;
            cameraObject.hideFlags = HideFlags.DontSave;
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.015f, .02f, .04f, 1);
            camera.cullingMask = 1 << 5;
            camera.transform.position = new Vector3(10000, 10000, -10);
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.targetTexture = target;
            camera.aspect = 1920f / 1080;
            var canvas = gallery.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            var scaler = gallery.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            var font = cardPrefab.GetComponentInChildren<TMP_Text>(true).font;
            for (int page = 0; page < 3; page++)
            {
                var pageObject = new GameObject("Page " + page, typeof(RectTransform));
                pageObject.layer = 5;
                pageObject.transform.SetParent(gallery.transform, false);
                var pageRect = (RectTransform)pageObject.transform;
                pageRect.anchorMin = Vector2.zero; pageRect.anchorMax = Vector2.one;
                pageRect.offsetMin = Vector2.zero; pageRect.offsetMax = Vector2.zero;
                AddLabel(pageObject.transform, font, "REAL PREFABS / CHOICE ICONS " + (page * 3) + " - " + (page * 3 + 2),
                    new Vector2(0, 490), new Vector2(1700, 45));
                for (int column = 0; column < 3; column++)
                {
                    int index = page * 3 + column;
                    var choice = choices[index];
                    var record = records[index];
                    float x = (column - 1) * 600;
                    var card = Object.Instantiate(cardPrefab, pageObject.transform);
                    Position(card.transform, new Vector2(x, 140), .72f);
                    card.BindWithChance(choice, null, null);
                    var cardImage = Field<Image>(card, "imgIcon");
                    record.cardBound = Bound(cardImage, choice.icon);
                    record.cardPreserveAspect = cardImage.preserveAspect;
                    var snapshot = CJS_ChoiceSnapshot.From(choice);
                    var inventory = Object.Instantiate(inventoryPrefab, pageObject.transform);
                    Position(inventory.transform, new Vector2(x - 110, -310), 1);
                    inventory.Bind(snapshot);
                    var inventoryImage = Field<Image>(inventory, "imgIcon");
                    record.inventoryBound = Bound(inventoryImage, choice.icon);
                    record.inventoryPreserveAspect = inventoryImage.preserveAspect;
                    var result = Object.Instantiate(resultPrefab, pageObject.transform);
                    Position(result.transform, new Vector2(x + 110, -310), 1);
                    result.Bind(snapshot);
                    var resultImage = Field<Image>(result, "imgIcon");
                    record.resultBound = Bound(resultImage, choice.icon);
                    record.resultPreserveAspect = resultImage.preserveAspect;
                    AddLabel(pageObject.transform, font, "INVENTORY", new Vector2(x - 110, -410), new Vector2(210, 45));
                    AddLabel(pageObject.transform, font, "RESULT", new Vector2(x + 110, -410), new Vector2(210, 45));
                }
                Canvas.ForceUpdateCanvases();
                foreach (var layout in pageObject.GetComponentsInChildren<LayoutGroup>(true))
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)layout.transform);
                // These three pages are captured in one coroutine step. TMP cannot
                // wait for its normal LateUpdate between destroying/creating pages.
                foreach (var text in pageObject.GetComponentsInChildren<TMP_Text>(true))
                    text.ForceMeshUpdate(ignoreActiveState: false, forceTextReparsing: true);
                Canvas.ForceUpdateCanvases();
                if (captureGallery && Capture(camera, target, Path.Combine(output, label + "-choice-icons-" + (page + 1) + ".png")))
                    renderRandomChanges++;
                // Retain active TMP/shared submesh resources until all synchronous
                // render requests finish. Both disable and destruction clear TMP
                // meshes that subsequent same-frame pages may still reference.
                pageRect.anchoredPosition = new Vector2(-5000, 0);
            }
        }
        finally
        {
            Object.DestroyImmediate(gallery);
            camera.targetTexture = null;
            Object.DestroyImmediate(cameraObject);
            RenderTexture.ReleaseTemporary(target);
        }
        bool randomUnchanged = UnityEngine.Random.state.Equals(randomBefore);
        bool timeScaleUnchanged = Time.timeScale == timeScaleBefore;
        bool historyUnchanged = CJS_Script_ChoiceState.I == choiceState
            && (choiceState == null || choiceState.Picked.SequenceEqual(pickedBefore));
        bool unchanged = randomUnchanged && timeScaleUnchanged && historyUnchanged;
        var report = new Report {
            scene = SceneManager.GetActiveScene().path,
            language = PSH_Script_Localization.LanguageCode,
            method = "Detached copies of actual scene card/inventory/result prefabs, bound to all nine scene choices; no choice rolls or selections. Gallery is not a gameplay screenshot.",
            cardPrefab = AssetDatabase.GetAssetPath(cardPrefab), inventoryPrefab = AssetDatabase.GetAssetPath(inventoryPrefab),
            resultPrefab = AssetDatabase.GetAssetPath(resultPrefab), atlas = AssetDatabase.GetAssetPath(atlas),
            sharedTextureCount = textureCount, editorLegacyDependency = legacyReferenced,
            atlasTextureWidth = choices[0].icon.texture.width, atlasTextureHeight = choices[0].icon.texture.height,
            atlasTextureFormat = choices[0].icon.texture.format.ToString(),
            editorLegacyDirectReferrers = editorReferrers, runtimeLegacyReferences = runtimeLegacy,
            gameplayStateUnchanged = unchanged, choiceHistoryUnchanged = historyUnchanged,
            timeScaleUnchanged = timeScaleUnchanged, randomUnchanged = randomUnchanged,
            renderRequestsConsumingRandom = renderRandomChanges, icons = records
        };
        report.resultViewCount = resultViews.Length;
        File.WriteAllText(Path.Combine(output, label + "-choice-icons.json"), JsonUtility.ToJson(report, true));
        check(records.All(r => r.cardBound && r.inventoryBound && r.resultBound),
            label + ": card, inventory and results bind all nine icons with the default UI material");
        check(records.All(r => r.cardPreserveAspect && r.inventoryPreserveAspect && r.resultPreserveAspect),
            label + ": card, inventory and results preserve the aspect ratio of all nine icons");
        check(historyUnchanged, label + ": icon validation preserves the scene choice state and all picked snapshots");
        check(timeScaleUnchanged, label + ": icon validation preserves the gameplay time scale");
        check(randomUnchanged, label + ": UI binding preserves Unity RNG and explicit preview renders restore their RNG consumption");
    }

    static IEnumerable<T> InScene<T>() where T : Component => Object.FindObjectsByType<T>(FindObjectsInactive.Include,
        FindObjectsSortMode.None).Where(c => c.gameObject.scene == SceneManager.GetActiveScene());
    static T Field<T>(Object owner, string name) where T : Object =>
        new SerializedObject(owner).FindProperty(name).objectReferenceValue as T;
    static bool Bound(Image image, Sprite sprite) => image != null && image.isActiveAndEnabled
        && image.sprite == sprite && image.mainTexture == sprite.texture && image.color == Color.white
        && image.material == Graphic.defaultGraphicMaterial;
    static void Position(Transform transform, Vector2 position, float scale)
    {
        var rect = (RectTransform)transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one * scale;
    }
    static void AddLabel(Transform parent, TMP_FontAsset font, string text, Vector2 position, Vector2 size)
    {
        var label = new GameObject("Audit label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.layer = 5;
        label.transform.SetParent(parent, false);
        Position(label.transform, position, 1);
        ((RectTransform)label.transform).sizeDelta = size;
        var tmp = label.GetComponent<TextMeshProUGUI>();
        tmp.font = font; tmp.text = text; tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
    }
    static void EmitUI(ScriptableRenderContext context, Camera camera) => ScriptableRenderContext.EmitGeometryForCamera(camera);
    static bool Capture(Camera camera, RenderTexture target, string path)
    {
        var previous = RenderTexture.active;
        var randomBeforeRender = UnityEngine.Random.state;
        bool renderConsumedRandom = false;
        Texture2D pixels = null;
        RenderPipelineManager.beginCameraRendering += EmitUI;
        try
        {
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            renderConsumedRandom = !UnityEngine.Random.state.Equals(randomBeforeRender);
            RenderTexture.active = target;
            pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(path, pixels.EncodeToPNG());
        }
        finally
        {
            RenderPipelineManager.beginCameraRendering -= EmitUI;
            RenderTexture.active = previous;
            if (pixels != null) Object.DestroyImmediate(pixels);
            // URP 17.2 ScreenSpaceAmbientOcclusionPass.UpdateMaterialProperties
            // consumes Random.value twice for blue-noise offsets per render.
            // A synchronous, extra audit render must not advance gameplay RNG.
            // Binding/clone creation remains outside this restore scope and is
            // still checked against the original state by ValidateAndCapture.
            UnityEngine.Random.state = randomBeforeRender;
        }
        return renderConsumedRandom;
    }
}
#endif
