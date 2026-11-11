using NUnit.Framework;
using UnityEngine;

public partial class NextFestBuildTests
{
    [Test] public void RepeatedTotalScoreMultiplicationNeverWrapsNegative()
    {
        Invoke(score, "BeginRecordChallenge");
        score.curScore = 6000;
        for (int i = 0; i < 20; i++) { score.MultiplyScore(5); Assert.That(score.curScore, Is.GreaterThanOrEqualTo(6000)); }
        Assert.That(score.curScore, Is.EqualTo(int.MaxValue));
        score.AddScore(100);
        Assert.That(score.curScore, Is.EqualTo(int.MaxValue));
    }
    [Test] public void CeilingReportsOnlyActuallyCreditedScoreAndContributions()
    {
        Invoke(score, "BeginRecordChallenge");
        Run.ApplyUpgrade(9); Run.ApplyUpgrade(10); ball.SetBallCount(1); Hit(1);
        score.curScore = int.MaxValue - 3100;
        int popup = -1; System.Action<int> receive = value => popup = value;
        KHS_Script_ScoreManager.OnScoreGained += receive;
        try
        {
            score.AddBlackholeScoreAt(3000, Vector3.zero);
            Assert.That(score.curScore, Is.EqualTo(int.MaxValue));
            Assert.That(popup, Is.EqualTo(3100));
            Assert.That(Run.ChargePaidTotal, Is.EqualTo(100));
            Assert.That(Run.LastBallBonusTotal, Is.Zero);
            score.multiplier = float.MaxValue; score.AddScore(10000);
            Assert.That(popup, Is.Zero); Assert.That(score.curScore, Is.EqualTo(int.MaxValue));
        }
        finally { KHS_Script_ScoreManager.OnScoreGained -= receive; }
    }
    [Test] public void PausedAndInvalidMultiplicationCannotAlterFullGameScore()
    {
        score.curScore = 500;
        score.MultiplyScore(-5); score.MultiplyScore(0);
        Time.timeScale = 0; score.MultiplyScore(3);
        Assert.That(score.curScore, Is.EqualTo(500));
    }
}
