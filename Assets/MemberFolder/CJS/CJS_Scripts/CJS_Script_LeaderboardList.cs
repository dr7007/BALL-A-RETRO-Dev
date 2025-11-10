using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CJS_Script_LeaderboardList : MonoBehaviour
{
    [SerializeField] private Transform content;      // ScrollRect의 Content
    [SerializeField] private GameObject itemPrefab;  // 자식: RankText / NameText / ScoreText

    [Header("Options")]
    [Tooltip("이름 앞에 1,2,3 같은 순번 숫자를 보일지 여부")]
    public bool showInlineRank = false;              // 요청: 숫자 숨김 기본
    [Tooltip("순위별 텍스트 색 적용 여부")]
    public bool useRankColors = true;

    [Header("Rank Colors")]
    public Color rank1Color = new Color(1f, 0.25f, 0.25f);  // 1st 빨강
    public Color rank2Color = new Color(1f, 0.90f, 0.30f);  // 2st 노랑
    public Color rank3Color = new Color(1f, 0.40f, 0.80f);  // 3st 핑크
    public Color rank4Color = new Color(0.35f, 1f, 0.35f);  // 4st 초록
    public Color rank5Color = new Color(1f, 0.72f, 0.85f);  // 5st 연핑크
    public Color rank6Color = new Color(0.95f, 0.75f, 0f);  // 6st 진한노랑(앰버)
    public Color rank7Color = new Color(0.0f, 0.75f, 0.35f);// 7st 진한초록
    public Color rank8to10Color = new Color(0.70f, 0.70f, 0.70f); // 8~10st 회색
    public Color defaultNameColor = Color.white;             // 그 외 기본색
    public Color defaultScoreColor = Color.white;

    void Awake()
    {
        // Content 자동 탐색(미지정 대비)
        if (content == null)
        {
            var sr = GetComponentInChildren<ScrollRect>(true);
            if (sr != null) content = sr.content;
        }
        if (content == null)
        {
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
        if (content == null || itemPrefab == null) return;
        rows ??= System.Array.Empty<ScoreRow>();

        for (int i = 0; i < rows.Length; i++)
        {
            var go = Instantiate(itemPrefab, content);

            var rankText = go.transform.Find("RankText")?.GetComponent<TMP_Text>();
            var nameText = go.transform.Find("NameText")?.GetComponent<TMP_Text>();
            var scoreText = go.transform.Find("ScoreText")?.GetComponent<TMP_Text>();

            // 1) 앞 숫자 숨기기(요청)
            if (rankText)
            {
                rankText.gameObject.SetActive(showInlineRank);
                if (showInlineRank) rankText.text = (i + 1).ToString();
            }

            // 2) 값 채우기
            if (nameText) nameText.text = Safe(rows[i].nickname, 18);
            if (scoreText) scoreText.text = rows[i].score.ToString("N0");

            // 3) 순위별 색상
            if (useRankColors)
            {
                var c = GetRankColor(i); // i: 0-based
                if (nameText) nameText.color = c;
                if (scoreText) scoreText.color = c;
            }
            else
            {
                if (nameText) nameText.color = defaultNameColor;
                if (scoreText) scoreText.color = defaultScoreColor;
            }
        }
    }

    Color GetRankColor(int index0) // index0: 0=1st, 1=2nd, ...
    {
        switch (index0)
        {
            case 0: return rank1Color;       // 1st
            case 1: return rank2Color;       // 2st
            case 2: return rank3Color;       // 3st
            case 3: return rank4Color;       // 4st
            case 4: return rank5Color;       // 5st
            case 5: return rank6Color;       // 6st
            case 6: return rank7Color;       // 7st
            case 7:  // 8st
            case 8:  // 9st
            case 9:  // 10st
                return rank8to10Color;
            default:
                return defaultNameColor;
        }
    }

    string Safe(string s, int maxLen)
    {
        if (string.IsNullOrEmpty(s)) return "Player";
        s = s.Trim();
        return s.Length > maxLen ? s.Substring(0, maxLen) + "…" : s;
    }
}
