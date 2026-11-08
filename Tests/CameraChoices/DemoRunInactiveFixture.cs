// Full-game camera/audio fixtures deliberately do not enable demo gameplay.
using UnityEngine;
public sealed class BallARetroDemoRun : MonoBehaviour
{
    public static BallARetroDemoRun Current => null;
    public bool IsEnabled => false;
    public static bool BlocksLaunch => false;
    public bool CanChoose(int round) => false;
    public bool TryBeginPick(int round) => false;
    public void NotifyStateChanged() { }
}
