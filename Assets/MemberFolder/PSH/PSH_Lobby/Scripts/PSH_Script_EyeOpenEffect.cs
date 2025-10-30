using UnityEngine;
using System.Collections;

namespace PSH
{
    /// <summary>
    /// 'EyeOpen' Plane의 머테리얼 애니메이션을 담당합니다.
    /// 애니메이션이 완료되면 OnEyeOpenComplete 이벤트를 방송합니다.
    /// </summary>
    public class PSH_Script_EyeOpenEffect : MonoBehaviour
    {
        // 애니메이션 완료 시 "다 끝났다!"고 알리는 방송
        public event System.Action OnEyeOpenComplete;

        [SerializeField] private Renderer planeRenderer;
        [SerializeField] private float animationDuration = 2.0f;

        // 셰이더 그래프의 '부드러운 정도' 속성의 Reference 이름
        [SerializeField] private string smoothnessPropertyName = "smoothnessPropertyName";

        private Material materialInstance;

        private void Awake()
        {
            if (planeRenderer == null)
            {
                planeRenderer = GetComponent<Renderer>();
            }
            // 원본 머테리얼이 아닌 인스턴스를 사용해야 합니다.
            materialInstance = planeRenderer.material;
        }

        /// <summary>
        /// 애니메이션을 즉시 완료 상태(눈 뜬 상태)로 설정합니다.
        /// </summary>
        public void SetOpenImmediate()
        {
            materialInstance.SetFloat(smoothnessPropertyName, 1f);
        }

        /// <summary>
        /// '눈 뜨기' 애니메이션 코루틴을 시작합니다.
        /// </summary>
        public void BeginAnimation()
        {
            StartCoroutine(AnimateEyeOpen());
        }

        private IEnumerator AnimateEyeOpen()
        {
            float elapsedTime = 0f;
            materialInstance.SetFloat(smoothnessPropertyName, 0f); // 0에서 시작

            while (elapsedTime < animationDuration)
            {
                // Lerp(보간)를 사용해 0에서 1로 부드럽게 값을 변경
                float smoothness = Mathf.Lerp(0f, 1f, elapsedTime / animationDuration);
                materialInstance.SetFloat(smoothnessPropertyName, smoothness);

                elapsedTime += Time.deltaTime;
                yield return null; // 다음 프레임까지 대기
            }

            materialInstance.SetFloat(smoothnessPropertyName, 1f); // 정확히 1로 완료

            Debug.Log("EyeOpen 애니메이션 완료.");

            // 애니메이션이 끝났음을 구독자들에게 방송
            OnEyeOpenComplete?.Invoke();
        }
    }
}
