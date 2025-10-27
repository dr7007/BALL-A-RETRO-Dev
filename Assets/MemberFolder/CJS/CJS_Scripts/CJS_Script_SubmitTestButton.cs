using TMPro;
using UnityEngine;

public class CJS_Script_SubmitTestButton : MonoBehaviour
{
    public CJS_Script_PinballRankingService service;
    public TMP_Text resultText; 

    public void OnClickSubmitTest()
    {
        service.SubmitScore(12345, "Classic", 1,
            onDone: resp =>
            {
                var txt = $"myScore={resp.your_score}, rank=#{resp.rank}\n";
                for (int i = 0; i < resp.top10.Length; i++)
                    txt += $"{i + 1}. {resp.top10[i].nickname} - {resp.top10[i].score}\n";

                if (resultText) resultText.text = txt;
                Debug.Log(txt);
            },
            onFail: err =>
            {
                if (resultText) resultText.text = "Fail: " + err;
                Debug.LogError(err);
            }
        );
    }
}
