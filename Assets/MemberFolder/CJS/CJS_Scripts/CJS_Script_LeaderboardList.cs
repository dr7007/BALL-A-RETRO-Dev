using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CJS_Script_LeaderboardList : MonoBehaviour
{
    [SerializeField] private Transform content;    
    [SerializeField] private GameObject itemPrefab;

    void Awake()
    {
        if (content == null)
        {
            var sr = GetComponentInChildren<ScrollRect>(true);
            if (sr != null) content = sr.content;
        }
        if (content == null)
        {
            // 이름으로 최후 탐색
            var rects = GetComponentsInChildren<RectTransform>(true);
            foreach (var r in rects) if (r.name == "Content") { content = r; break; }
        }
        if (content == null) Debug.LogError("[LeaderboardList] Content not set.", this);
        if (itemPrefab == null) Debug.LogError("[LeaderboardList] itemPrefab not set.", this);
    }

    public void Clear()
    {
        if (content == null) return;
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    public void Populate(ScoreRow[] rows)
    {
        Clear();
        if (rows == null || content == null || itemPrefab == null) return;

        for (int i = 0; i < rows.Length; i++)
        {
            var go = Instantiate(itemPrefab, content);
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
