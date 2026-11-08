using System;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>One local best per mode/level, independent of nickname changes.</summary>
public static class BallARetroPersonalRecordStore
{
    private static string NormalizeMode(string mode) => string.IsNullOrWhiteSpace(mode) ? "Classic" : mode.Trim();
    public static string StorageKey(string mode, int level) =>
        (BallARetroPlatform.IsDemoBuild ? "BallARetro.SteamDemo.Best.v1." : "BallARetro.STOVE.Best.v1.") +
        Math.Max(1, level).ToString(CultureInfo.InvariantCulture) + "." +
        Convert.ToBase64String(Encoding.UTF8.GetBytes(NormalizeMode(mode)));

    public static ScoreRow[] Read(string nickname, string mode, int level)
    {
        string key = StorageKey(mode, level);
        if (!PlayerPrefs.HasKey(key)) return Array.Empty<ScoreRow>();
        ScoreRow row;
        try { row = JsonUtility.FromJson<ScoreRow>(PlayerPrefs.GetString(key)); }
        catch (ArgumentException) { return Array.Empty<ScoreRow>(); }
        if (row == null || row.score < 0 || row.game_mode != NormalizeMode(mode) || row.level != Math.Max(1, level))
            return Array.Empty<ScoreRow>();
        row.nickname = string.IsNullOrWhiteSpace(nickname) ? "Guest" : nickname.Trim();
        return new[] { row };
    }

    public static ScoreRow[] Submit(string nickname, int score, string mode, int level)
    {
        if (score < 0) throw new ArgumentOutOfRangeException(nameof(score));
        var previous = Read(nickname, mode, level);
        if (previous.Length != 0 && previous[0].score >= score) return previous;
        var row = new ScoreRow
        {
            nickname = string.IsNullOrWhiteSpace(nickname) ? "Guest" : nickname.Trim(),
            score = score, game_mode = NormalizeMode(mode), level = Math.Max(1, level),
            created_at = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
        };
        PlayerPrefs.SetString(StorageKey(mode, level), JsonUtility.ToJson(row));
        PlayerPrefs.Save();
        return new[] { row };
    }
}
