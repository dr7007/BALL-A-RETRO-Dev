using UnityEngine;
using System.Data;
using MySql.Data.MySqlClient;

public class DB_Manager : MonoBehaviour
{
    private MySqlConnection sqlConn = null;

    private readonly string server = "172.30.1.7";   // 서버 IP
    private readonly int port = 46224;               // 서버 포트번호
    private readonly string uid = "gstar";           // DB 아이디
    private readonly string pwd = "";           // DB 비밀번호
    private readonly string database = "test";   // DB 이름


    void Start()
    {
        try
        {
            // 접속정보 준비
            string strConn = string.Format(
                "Server={0};Port={1};Uid={2};Pwd={3};Database={4};charset=utf8;",
                 server, port, uid, pwd, database);
            // DB 접속
            sqlConn = new MySqlConnection(strConn);


            // 테스트
            string rb_name = "test1"; // 테이블 명
            string query = "select * from " + rb_name;
            DataSet ds = OnSelectRequest(query, rb_name);

            Debug.Log(ds.GetXml());
        }
        catch (System.Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    // Insert or Update
    public bool OnInsertOrUpdateRequest(string str_query)
    {
        try
        {
            // DB 연결
            sqlConn.Open();

            // 쿼리 준비
            MySqlCommand sqlCommand = new MySqlCommand();
            sqlCommand.Connection = sqlConn;
            sqlCommand.CommandText = str_query;

            // 쿼리 실행
            sqlCommand.ExecuteNonQuery();

            // DB 연결해제
            sqlConn.Close();

            return true;
        }
        catch (System.Exception e)
        {
            Debug.Log(e.ToString());
            return false;
        }
    }

    // Select
    public DataSet OnSelectRequest(string p_query, string table_name)
    {
        try
        {
            // DB 연결
            sqlConn.Open();

            // 쿼리 준비
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = sqlConn;
            cmd.CommandText = p_query;

            // 데이터 아답터를 이용해 데이터를 조회하고, 결과를 데이터셋에 채우기
            MySqlDataAdapter sd = new MySqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            sd.Fill(ds, table_name);

            // DB 연결해제
            sqlConn.Close();

            return ds;
        }
        catch (System.Exception e)
        {
            Debug.Log(e.ToString());
            return null;
        }
    }

    private void OnApplicationQuit()
    {
        sqlConn.Close();
    }
}