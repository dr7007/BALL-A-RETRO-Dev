using System;
using System.Collections.Generic;
namespace Steamworks
{
    // Controlled native boundary, including delayed callbacks and cancellation.
    public static class LeaderboardFixture
    {
        public static readonly Dictionary<ulong, object> Responses = new();
        public static readonly Queue<Action> Callbacks = new();
        public static bool Hold, FindFails, UploadFails, DownloadFails;
        public static string Board;
        public static int Uploads, Finds, Start, End, Best, BestStep;
        public static int[] Steps;
        public static ELeaderboardUploadScoreMethod Method;
        public static LeaderboardEntry_t[] Rows;
        static ulong next;
        public static void Reset()
        {
            Responses.Clear(); Callbacks.Clear(); Hold=FindFails=UploadFails=DownloadFails=false;
            Board="";Uploads=Finds=Start=End=Best=BestStep=0;next=0;Rows=Array.Empty<LeaderboardEntry_t>();Steps=Array.Empty<int>();
        }
        public static SteamAPICall_t Result(object value)
        { var id=++next;Responses[id]=value;return new SteamAPICall_t {m_SteamAPICall=id}; }
        public static void Dispatch()
        { if(Hold)return;int count=Callbacks.Count;while(count-->0)Callbacks.Dequeue()(); }
    }
    public struct SteamAPICall_t { public ulong m_SteamAPICall; }
    public struct SteamLeaderboard_t { public ulong Value; }
    public struct SteamLeaderboardEntries_t { public ulong Value; }
    public struct LeaderboardFindResult_t { public byte m_bLeaderboardFound; public SteamLeaderboard_t m_hSteamLeaderboard; }
    public struct LeaderboardScoresDownloaded_t { public int m_cEntryCount; public SteamLeaderboardEntries_t m_hSteamLeaderboardEntries; }
    public struct LeaderboardScoreUploaded_t { public byte m_bSuccess; public int m_nScore,m_nGlobalRankNew; }
    public struct LeaderboardEntry_t { public CSteamID m_steamIDUser; public int m_nGlobalRank,m_nScore,m_cDetails; }
    public enum ELeaderboardSortMethod { k_ELeaderboardSortMethodDescending }
    public enum ELeaderboardDisplayType { k_ELeaderboardDisplayTypeNumeric }
    public enum ELeaderboardDataRequest { k_ELeaderboardDataRequestGlobal }
    public enum ELeaderboardUploadScoreMethod { k_ELeaderboardUploadScoreMethodKeepBest }
    public sealed class CallResult<T> : IDisposable
    {
        Action<T,bool> callback;bool disposed;
        public static CallResult<T> Create(Action<T,bool> callback) => new CallResult<T>{callback=callback};
        public void Set(SteamAPICall_t handle)
        {
            var value=LeaderboardFixture.Responses[handle.m_SteamAPICall];
            bool failed=value is Exception;
            LeaderboardFixture.Callbacks.Enqueue(()=>{if(!disposed)callback(failed?default:(T)value,failed);});
        }
        public void Dispose(){disposed=true;}
    }
    public static class SteamUserStats
    {
        public static SteamAPICall_t FindOrCreateLeaderboard(string name,ELeaderboardSortMethod sort,ELeaderboardDisplayType display)
        { LeaderboardFixture.Board=name;LeaderboardFixture.Finds++;return LeaderboardFixture.Result(new LeaderboardFindResult_t{m_bLeaderboardFound=(byte)(LeaderboardFixture.FindFails?0:1),m_hSteamLeaderboard=new SteamLeaderboard_t{Value=1}}); }
        public static SteamAPICall_t UploadLeaderboardScore(SteamLeaderboard_t board,ELeaderboardUploadScoreMethod method,int score,int[] details,int count)
        {
            LeaderboardFixture.Uploads++;LeaderboardFixture.Method=method;
            if(!LeaderboardFixture.UploadFails && score>LeaderboardFixture.Best){LeaderboardFixture.Best=score;LeaderboardFixture.BestStep=count>0?details[0]:0;}
            return LeaderboardFixture.Result(new LeaderboardScoreUploaded_t{m_bSuccess=(byte)(LeaderboardFixture.UploadFails?0:1),m_nScore=LeaderboardFixture.Best,m_nGlobalRankNew=3});
        }
        public static SteamAPICall_t DownloadLeaderboardEntries(SteamLeaderboard_t board,ELeaderboardDataRequest request,int first,int last)
        { LeaderboardFixture.Start=first;LeaderboardFixture.End=last;return LeaderboardFixture.Result(LeaderboardFixture.DownloadFails?(object)new Exception():new LeaderboardScoresDownloaded_t{m_cEntryCount=LeaderboardFixture.Rows.Length,m_hSteamLeaderboardEntries=new SteamLeaderboardEntries_t{Value=1}}); }
        public static bool GetDownloadedLeaderboardEntry(SteamLeaderboardEntries_t entries,int i,out LeaderboardEntry_t entry,int[] details,int count)
        {entry=LeaderboardFixture.Rows[i];if(i<LeaderboardFixture.Steps.Length&&count>0){details[0]=LeaderboardFixture.Steps[i];entry.m_cDetails=1;}return true;}
    }
}
