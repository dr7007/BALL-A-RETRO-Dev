using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class KHS_Script_RogueLikeManager : MonoBehaviour
{
    [SerializeField]
    private YJ_Script_BallController ballCon;
    [SerializeField]
    private KHS_Script_UIToBall[] ballUI;

    [SerializeField]
    private GameObject blockerHolderGo;
    private KHS_Script_DumpManager[] blockerDMs;

    [SerializeField]
    private GameObject plincoColliderGo;
    private KHS_Script_PlincoFunction[] plincoFunctions;
    [SerializeField]
    private KHS_Script_UIToPlinco plincoUI;

    [SerializeField]
    private KHS_Script_ScoreManager scoreManager;

    [SerializeField]
    private GameObject secretBumpersHolder;
    [SerializeField]
    private YJ_Script_BlackholeController blackHole;

    [SerializeField]
    private KHS_Script_FliperController fliper;
    [SerializeField]
    private KHS_Script_BatteryLedManager batLedManager;

    private bool on_Special_Func = false;
    private float special_Chance = 0f;
    private CJS_Script_ChoiceRoller roller;

    private void Awake()
    {
        fliper = FindAnyObjectByType<KHS_Script_FliperController>();
        roller = FindAnyObjectByType<CJS_Script_ChoiceRoller>();
        scoreManager = FindAnyObjectByType<KHS_Script_ScoreManager>();
        blockerDMs = blockerHolderGo.GetComponentsInChildren<KHS_Script_DumpManager>();
        plincoFunctions = plincoColliderGo.GetComponentsInChildren<KHS_Script_PlincoFunction>();
    }

    private void OnEnable()
    {

        KHS_Script_PortalController.portalEvt += SpecialBlockerGenerate;
        KHS_Script_PlincoFunction.ReturnPortalEvt += ResetSpecialBlocker;

    }
    private void OnDisable()
    {
        KHS_Script_PortalController.portalEvt -= SpecialBlockerGenerate;
        KHS_Script_PlincoFunction.ReturnPortalEvt -= ResetSpecialBlocker;
    }
    public void MatchingFunc(int idx)
    {
        switch (idx)
        {
            case 0:
                AddBlockerScore(5);
                break;
            case 1:
                AddBlockerBounce(0.3f);
                break;
            case 2:
                PlincoMultipierInhence(1);
                break;
            case 3:
                AddSpecialBlocker(0.1f);
                break;
            case 4:
                AddScoreMultiplier(0.2f);
                break;
            case 5:
                SecretWeaponActivate(5);
                break;
            case 6:
                AddFliperCount(5);
                break;
            case 7:
                ClosetheHole(7);
                break;
            case 8:
                BallAdditive(3);
                break;
        }
    }

    public void AddBlockerScore(int _score)
    {
        foreach (var blocker in blockerDMs)
        {
            blocker.bumpScore *= _score;
        }
        Debug.Log($"로그라이크 선택지 동작 | 블로커 기본 점수 {_score}배");
    }

    public void AddBlockerBounce(float _bounceAmount)
    {
        foreach (var blocker in blockerDMs)
        {
            blocker.bounceForce += _bounceAmount;
        }
        Debug.Log($"로그라이크 선택지 동작 | 블로커 반박력 {_bounceAmount}배");
    }

    public void AddSpecialBlocker(float _chance)
    {
        on_Special_Func = true;
        special_Chance += _chance;
        Debug.Log($"special Blocker 생성 여부 {on_Special_Func} | 생성 확률 {special_Chance * 100}%");
    }

    public void SpecialBlockerGenerate(int _idx)
    {
        if (on_Special_Func || _idx == 1)
        {
            Debug.Log($"로그라이크 선택지 동작 | Special Blocker 발동 확률: {special_Chance * 100f}%");

            foreach (var blocker in blockerDMs)
            {
                bool activate = Random.value < special_Chance;
                blocker.isSpecial = activate;

                if (blocker.transform.childCount > 0)
                {
                    var renderers = blocker.transform.GetChild(0).GetComponentsInChildren<Renderer>();
                    if (renderers.Length != 0)
                    {
                        foreach (var renderer in renderers)
                        {
                            var mat = renderer.material; // 인스턴스화됨
                            if (activate)
                            {
                                mat.EnableKeyword("_EMISSION");
                            }
                            else
                            {
                                mat.DisableKeyword("_EMISSION");
                            }
                        }
                    }
                }
            }
        }
    }
    public void ResetSpecialBlocker()
    {
        if (on_Special_Func)
        {
            foreach (var blocker in blockerDMs)
            {
                blocker.isSpecial = false;

                if (blocker.transform.childCount > 0)
                {
                    var renderers = blocker.transform.GetChild(0).GetComponentsInChildren<Renderer>();
                    if (renderers.Length != 0)
                    {
                        foreach (var renderer in renderers)
                        {
                            var mat = renderer.material;
                            mat.DisableKeyword("_EMISSION");
                        }
                    }
                }
            }

            Debug.Log($"Special Blocker 효과 종료");
        }
    }

    private void PlincoMultipierInhence(int _multi)
    {
        foreach (var plinco in plincoFunctions)
        {
            plinco.AddScoreMulti(_multi);
        }
            plincoUI.UpdatePlincoUI();
    }

    private void AddScoreMultiplier(float _multi)
    {
        scoreManager.multiplier += _multi;
    }

    private void SecretWeaponActivate(int _idx)
    {
        secretBumpersHolder.SetActive(true);
        blackHole.isForceActive = true;
        blackHole.isActivated = true;
        UnActiveChoice(_idx);
    }

    private void UnActiveChoice(int _idx)
    {
        roller.UnActiveChoices(_idx);
    }

    private void AddFliperCount(int _count)
    {
        fliper.fliper_Count += _count;
        fliper.FlipperCountUp(_count);
        fliper.FlipperCountUpdate();
    }

    private void ClosetheHole(int _idx)
    {
        batLedManager.ForcedOn();
        UnActiveChoice(_idx);
    }

    private void BallAdditive(int _creadits)
    {
        ballCon.AddBallFunc(_creadits);
        foreach(var bui in ballUI)
            bui.BallInfoUpdate();
    }
}
