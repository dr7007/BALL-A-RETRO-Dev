using UnityEngine;

/// <summary>Hide Steam-only entry points in STOVE builds.</summary>
public sealed class BallARetroSteamOnly : MonoBehaviour
{
    private void Awake()
    {
        if (!BallARetroPlatform.IsSteamBuild) gameObject.SetActive(false);
    }
}
