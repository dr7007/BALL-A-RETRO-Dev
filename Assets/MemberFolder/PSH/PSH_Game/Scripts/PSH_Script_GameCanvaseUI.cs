using UnityEngine;

namespace PSH
{
    public class PSH_Script_GameCanvaseUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

        private PSH_Script_SceneLoader sceneLoader;
        [Header("Manager Prefabs")]
        [Tooltip("게임 시작 시 생성할 CursorManager 프리팹")]
        [SerializeField] private GameObject cursorManagerPrefab;
        private void Awake()
        {
            if (PSH_Script_CursorManager.Instance == null)
            {
                if (cursorManagerPrefab != null)
                {
                    Instantiate(cursorManagerPrefab);
                }
                else
                {
                    Debug.LogError("CursorManager 프리팹이 GameCanvaseUI에 할당되지 않았습니다!");
                }
            }

        }

        private void Start()
        {
            sceneLoader = FindFirstObjectByType<PSH_Script_SceneLoader>();

            if (sceneLoader == null)
            {
                Debug.LogError("SceneLoader를 찾을 수 없습니다! 씬에 SceneLoader 오브젝트가 있는지 확인하세요.");
            }


        }
}

}