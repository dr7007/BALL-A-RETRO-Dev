using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class ButtonHoverTests
{
    GameObject root, cursorObject;
    PSH_Script_CursorManager cursor;
    [SetUp] public void Setup()
    {
        root = new GameObject("UI", typeof(RectTransform), typeof(Canvas));
        cursorObject = new GameObject("Cursor", typeof(PSH_Script_CursorManager));
        cursor = cursorObject.GetComponent<PSH_Script_CursorManager>();
    }
    [TearDown] public void Cleanup()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(cursorObject);
        Time.timeScale = 1;
    }
    PSH_Script_BaseButtonHover Button(bool text = true)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(root.transform, false);
        if (text)
        {
            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(go.transform, false);
            label.GetComponent<TextMeshProUGUI>().color = Color.white;
        }
        return go.AddComponent<PSH_Script_BaseButtonHover>();
    }
    Object Owner => (Object)typeof(PSH_Script_CursorManager).GetField("handOwner", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(cursor);

    [Test] public void MissingTextReference_AutoFindsLabel()
    {
        var hover = Button();
        hover.OnPointerEnter(null);
        Assert.That(hover.GetComponentInChildren<TextMeshProUGUI>().color, Is.EqualTo(Color.cyan));
        Assert.That(Owner, Is.EqualTo(hover));
        hover.OnPointerExit(null);
        Assert.That(hover.GetComponentInChildren<TextMeshProUGUI>().color, Is.EqualTo(Color.white));
        Assert.That(Owner, Is.Null);
    }
    [UnityTest] public IEnumerator HoverAnimatesWhilePaused_AndRestoresOnClose()
    {
        var hover = Button();
        Time.timeScale = 0;
        hover.OnPointerEnter(null);
        yield return new WaitForSecondsRealtime(.3f);
        Assert.That(hover.transform.localScale.x, Is.EqualTo(1.1f).Within(.001f));
        hover.gameObject.SetActive(false);
        Assert.That(hover.transform.localScale, Is.EqualTo(Vector3.one));
        Assert.That(Owner, Is.Null);
    }
    [Test] public void DisabledButton_DoesNotShowHandOrHighlight()
    {
        var hover = Button();
        hover.GetComponent<Button>().interactable = false;
        hover.OnPointerEnter(null);
        Assert.That(Owner, Is.Null);
        Assert.That(hover.GetComponentInChildren<TextMeshProUGUI>().color, Is.EqualTo(Color.white));
    }
    [Test] public void ExitingOldButton_DoesNotResetNewButtonsCursor()
    {
        var first = Button(); var second = Button();
        first.OnPointerEnter(null); second.OnPointerEnter(null);
        first.OnPointerExit(null);
        Assert.That(Owner, Is.EqualTo(second));
        first.gameObject.SetActive(false);
        Assert.That(Owner, Is.EqualTo(second));
    }
    [UnityTest] public IEnumerator IconOnlyButton_StillScales()
    {
        var hover = Button(false);
        hover.OnPointerEnter(null);
        yield return new WaitForSecondsRealtime(.3f);
        Assert.That(hover.transform.localScale.x, Is.GreaterThan(1));
        Assert.That(Owner, Is.EqualTo(hover));
    }
    [UnityTest] public IEnumerator ButtonBecomesUnavailable_ClearsHover()
    {
        var hover = Button(); hover.OnPointerEnter(null);
        hover.GetComponent<Button>().interactable = false;
        yield return null;
        Assert.That(Owner, Is.Null);
        Assert.That(hover.GetComponentInChildren<TextMeshProUGUI>().color, Is.EqualTo(Color.white));
    }
}
