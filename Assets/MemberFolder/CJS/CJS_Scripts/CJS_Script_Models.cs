using System;

[Serializable]
public class ScoreRow
{
    public string nickname;
    public int score;
    public string game_mode;
    public int level;
    public string created_at;
}

[Serializable]
public class SubmitResp
{
    public bool ok;
    public int insert_id;
    public int your_score;
    public int rank;
    public ScoreRow[] top10;
    public string msg;
}

[Serializable]
public class LbResp
{
    public bool ok;
    public ScoreRow[] top10;
    public string msg;
}

[Serializable]
public class RegisterResp
{
    public bool ok;
    public long player_id;
    public string nickname;
    public string msg;
}
