using UnityEngine;


public class PSH_Script_CursorManager : MonoBehaviour
{
    public static PSH_Script_CursorManager Instance { get; private set; }

    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D handCursor; // 버튼 호버용
    [SerializeField] private Vector2 hotspot = Vector2.zero;

    private void Awake()
    {
        // 씬에 이미 Instance가 있는지 확인
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {

        SetDefaultCursor();
    }

    public void SetDefaultCursor()
    {
        Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
    }

    public void SetHandCursor()
    {
        Cursor.SetCursor(handCursor, hotspot, CursorMode.Auto);
    }
}