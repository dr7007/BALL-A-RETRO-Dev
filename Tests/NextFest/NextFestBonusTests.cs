using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public partial class NextFestBuildTests
{
    [UnityTest] public IEnumerator OverlappingBonusExitsSettleOnlyOnceAndDisableCancelsPendingWork()
    {
        var exit = New("Exit").AddComponent<KHS_Script_PlincoFunction>();
        var otherExit = New("Overlapping exit").AddComponent<KHS_Script_PlincoFunction>();
        var portal = New("Return portal").AddComponent<KHS_Script_PortalController>();
        Set(exit, "scoreMulti", 5); Set(exit, "portalCon", portal);
        Set(otherExit, "scoreMulti", 5); Set(otherExit, "portalCon", portal);
        yield return null;
        score.curScore = 6000;
        var collider = ball.GetComponent<Collider>();
        Invoke(exit, "OnTriggerEnter", collider);
        Invoke(exit, "OnTriggerEnter", collider);
        Invoke(otherExit, "OnTriggerEnter", collider);
        yield return new WaitForSecondsRealtime(1.1f);
        Assert.That(score.curScore, Is.EqualTo(30000), "One ball may claim only one overlapping bonus exit.");
        exit.enabled = false;
        score.curScore = 100;
        Invoke(otherExit, "OnTriggerEnter", collider);
        otherExit.enabled = false;
        yield return new WaitForSecondsRealtime(1.1f);
        Assert.That(score.curScore, Is.EqualTo(100), "Disabled exits must not pay or teleport later.");
    }
}
