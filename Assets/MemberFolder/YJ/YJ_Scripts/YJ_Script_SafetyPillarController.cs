using UnityEngine;

public class YJ_Script_SafetyPillarController : MonoBehaviour
{
    [Header("사운드 (선택 사항)")]
    [SerializeField] private AudioClip activateSound;
    private AudioSource sound_;

    private void OnEnable()
    {
        KHS_Script_BallOutController.BallOutEvt += ResetPillar;
    }

    private void OnDisable()
    {
        KHS_Script_BallOutController.BallOutEvt -= ResetPillar;
    }

    private void ResetPillar()
    {
        DesactivatePillar();
    }

    private void Awake()
    {
        sound_ = GetComponent<AudioSource>();
        if (sound_ == null)
        {
            sound_ = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        DesactivatePillar();
    }

    [ContextMenu("Pillar 활성화 테스트")]
    public void ActivatePillar()
    {
        gameObject.SetActive(true);

        if (sound_ && activateSound && !sound_.isPlaying)
        {
            sound_.PlayOneShot(activateSound);
        }
    }

    [ContextMenu("Pillar 비활성화 테스트")]
    public void DesactivatePillar()
    {
        gameObject.SetActive(false);
    }

    public void SetActivated(bool value)
    {
        if (value)
        {
            ActivatePillar();
        }
        else
        {
            DesactivatePillar();
        }
    }
}