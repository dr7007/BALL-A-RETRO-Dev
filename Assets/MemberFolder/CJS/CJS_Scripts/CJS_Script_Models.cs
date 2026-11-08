using System;

[Serializable]
public class ScoreRow
{
    public string steam_id;
    public int rank;
    public int step;
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
    // Demo score saved locally while online ranking is unavailable.
    public bool local_only;
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
    public bool local_only;
    public ScoreRow[] top10;
    // A logged-in player without a submitted record has rank 0.
    public ScoreRow personal;
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
