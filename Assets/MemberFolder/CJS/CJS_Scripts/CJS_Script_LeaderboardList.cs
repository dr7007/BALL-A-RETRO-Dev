using TMPro;
using UnityEngine;

public class CJS_Script_LeaderboardList : MonoBehaviour
{
    [SerializeField] private GameObject itemPrefab;

    private Transform content;  // 동적

    void Start()
    {
        // 동적으로 content를 할당
        content = GameObject.Find("LeaderboardContent")?.transform;

        if (content == null)
        {
            Debug.LogError("LeaderboardContent가 씬에 존재하지 않거나 잘못된 이름입니다.");
            return;
        }
    }

    public void Clear()
    {
        // content가 null일 경우 처리를 추가
        if (content == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    public void Populate(ScoreRow[] rows)
    {
        Clear();
        if (rows == null || content == null) return;

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
