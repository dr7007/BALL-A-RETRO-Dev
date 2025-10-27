// CJS_Script_LeaderboardList.cs
using TMPro;
using UnityEngine;

public class CJS_Script_LeaderboardList : MonoBehaviour
{
    [SerializeField] Transform content;    
    [SerializeField] GameObject itemPrefab;

    public void Clear()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    public void Populate(ScoreRow[] rows)
    {
        Clear();
        if (rows == null) return;

        for (int i = 0; i < rows.Length; i++)
        {
            var go = Instantiate(itemPrefab, content);
            // 프리팹의 자식
            var rankText = go.transform.Find("RankText")?.GetComponent<TMP_Text>();
            var nameText = go.transform.Find("NameText")?.GetComponent<TMP_Text>();
            var scoreText = go.transform.Find("ScoreText")?.GetComponent<TMP_Text>();

            if (rankText) rankText.text = (i + 1).ToString();
            if (nameText) nameText.text = Safe(rows[i].nickname, 18);
            if (scoreText) scoreText.text = rows[i].score.ToString("N0");
        }
    }

    string Safe(string s, int maxLen)
    {
        if (string.IsNullOrEmpty(s)) return "Player";
        s = s.Trim();
        return s.Length > maxLen ? s.Substring(0, maxLen) + "…" : s;
    }
}
