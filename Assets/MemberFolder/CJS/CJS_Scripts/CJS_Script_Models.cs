using System;

[System.Serializable]
public class ScoreRow
{
    public string nickname;
    public int score;
    public string game_mode;
    public int level;
    public string created_at;
}
[System.Serializable]
public class SubmitResp
{
    public bool ok;
    public int insert_id;
    public int your_score;
    public int rank;
    public ScoreRow[] top10;
}
[System.Serializable]
public class LbResp
{
    public bool ok;
    public ScoreRow[] top10;
}
