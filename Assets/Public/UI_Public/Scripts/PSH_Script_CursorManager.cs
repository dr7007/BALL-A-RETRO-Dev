using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class PSH_Script_CursorManager : MonoBehaviour
{
    public static PSH_Script_CursorManager Instance { get; private set; }

    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D handCursor;
    [SerializeField] private Vector2 hotspot = Vector2.zero;
    private Object handOwner;
    private readonly List<RaycastResult> hits = new();
    private readonly List<Component> components = new();
    private readonly List<CanvasGroup> groups = new();
    private EventSystem pointerEventSystem;
    private PointerEventData pointer;
    private bool showingHand;
    private bool cursorApplied;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetDefaultCursor();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    // Start() 대신 OnEnable/OnDisable을 사용해 씬 로드 이벤트를 구독합니다.
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        PSH_Script_GameplayInput.Resumed += SetDefaultCursor;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        PSH_Script_GameplayInput.Resumed -= SetDefaultCursor;
        if (Instance == this) SetDefaultCursor();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void LateUpdate()
    {
        if (Instance != this) return;
        if (!Cursor.visible || Cursor.lockState == CursorLockMode.Locked)
        {
            SetDefaultCursor();
            return;
        }
        if (EventSystem.current != null) UpdateCursorAt(Input.mousePosition);
    }

    // Re-evaluate even without pointer movement: menus, dropdowns and interactability
    // can change underneath a stationary pointer while the game is paused.
    internal void UpdateCursorAt(Vector2 position)
    {
        var current = EventSystem.current;
        if (current == null) { SetDefaultCursor(); return; }
        if (pointer == null || pointerEventSystem != current)
        {
            pointerEventSystem = current;
            pointer = new PointerEventData(current);
        }
        pointer.Reset();
        pointer.position = position;
        hits.Clear();
        current.RaycastAll(pointer, hits);
        Object owner = null;
        // Only the frontmost hit receives input. Never show a hand for controls
        // behind a warning panel, dropdown blocker or another overlay.
        if (hits.Count > 0 && hits[0].module is GraphicRaycaster)
            owner = FindInteractiveOwner(hits[0].gameObject);
        if (owner != null) SetHandCursor(owner);
        else SetDefaultCursor();
    }

    private Object FindInteractiveOwner(GameObject hit)
    {
        var selectable = hit.GetComponentInParent<Selectable>();
        if (selectable != null)
            return selectable.IsActive() && selectable.IsInteractable() ? selectable : null;

        // Custom clickable UI (for example dialogue panels and lobby tiles).
        for (var node = hit.transform; node != null; node = node.parent)
        {
            node.GetComponents(components);
            foreach (var component in components)
            {
                if (!(component is IPointerClickHandler) || !(component is Behaviour behaviour)
                    || !behaviour.isActiveAndEnabled) continue;
                if (component is EventTrigger trigger && !HasClickEvent(trigger)) continue;
                return AllowsInteraction(node) ? component : null;
            }
        }
        return null;
    }

    private static bool HasClickEvent(EventTrigger trigger)
    {
        foreach (var entry in trigger.triggers)
            if (entry.eventID == EventTriggerType.PointerClick) return true;
        return false;
    }

    private bool AllowsInteraction(Transform node)
    {
        for (; node != null; node = node.parent)
        {
            node.GetComponents(groups);
            bool ignoreParents = false;
            foreach (var group in groups)
            {
                if (!group.isActiveAndEnabled) continue;
                if (!group.interactable) return false;
                ignoreParents |= group.ignoreParentGroups;
            }
            if (ignoreParents) break;
        }
        return true;
    }

    private void ApplyCursor(bool hand)
    {
        if (cursorApplied && showingHand == hand) return;
        showingHand = hand;
        cursorApplied = true;
        Cursor.SetCursor(hand ? handCursor : defaultCursor, hand ? hotspot : Vector2.zero, CursorMode.Auto);
    }

    // 씬이 로드될 때마다 이 함수가 호출됩니다.
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetDefaultCursor(); // 씬이 바뀔 때마다 기본 커서로 다시 설정
        if (Instance != this || mode != LoadSceneMode.Single) return;
        if (scene.name == "Main_Scene_Lobby")
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (scene.name == "Main_Scene_Game" || scene.name == "Main_Scene_Tutorial")
        {
            // Intro/tutorial dialogue opens its pointer explicitly in Start.
            // Retry paths that skip that dialogue still start with gameplay input.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void SetDefaultCursor()
    {
        handOwner = null;
        ApplyCursor(false);
    }

    public void SetHandCursor()
    {
        ApplyCursor(true);
    }

    public void SetHandCursor(Object owner)
    {
        handOwner = owner;
        SetHandCursor();
    }

    public void ReleaseHandCursor(Object owner)
    {
        if (handOwner == owner) SetDefaultCursor();
    }
}
