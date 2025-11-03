using System.Collections.Generic;
using UnityEngine;

public class KHS_Script_RogueLikeManager : MonoBehaviour
{
    [SerializeField]
    private GameObject blockerHolderGo;
    private KHS_Script_DumpManager[] blockerDMs;

    private void Awake()
    {
        blockerDMs = blockerHolderGo.GetComponentsInChildren<KHS_Script_DumpManager>();
    }

    public void MatchingFunc(int idx)
    {
        switch(idx)
        {
            case 0:
                AddBlockerScore(5);
                break;
            case 1:
                AddBlockerScore(10);
                break;
            case 2:
                AddBlockerScore(20);
                break;
            case 3:
                AddBlockerScore(500);
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
}
