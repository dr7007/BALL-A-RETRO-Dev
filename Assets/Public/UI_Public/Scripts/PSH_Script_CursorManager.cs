using UnityEngine;
using UnityEngine.SceneManagement; // 씬 매니저 사용을 위해 추가

public class PSH_Script_CursorManager : MonoBehaviour
{
    public static PSH_Script_CursorManager Instance { get; private set; }

    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D handCursor; // ư ȣ
    [SerializeField] private Vector2 hotspot = Vector2.zero;

    private void Awake()
    {
        //  ̹ Instance ִ Ȯ
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

    // Start() 대신 OnEnable/OnDisable을 사용해 씬 로드 이벤트를 구독합니다.
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬이 로드될 때마다 이 함수가 호출됩니다.
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetDefaultCursor(); // 씬이 바뀔 때마다 기본 커서로 다시 설정
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