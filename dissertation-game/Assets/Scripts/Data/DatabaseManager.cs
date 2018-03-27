using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

using UnityEngine;

public class DatabaseManager : MonoBehaviour
{
    SQLiteConnection databaseConnection;

    // Use this for initialization
    void Start()
    {
        var databaseFilePath = $"{Application.dataPath}\\Database\\Database.db";

        databaseConnection = new SQLiteConnection($"Data Source={databaseFilePath};Version=3");
        databaseConnection.Open();
        string sql = "SELECT * FROM GameTypes";
        var command = new SQLiteCommand(sql, databaseConnection);
        var reader = command.ExecuteReader();
        while (reader.Read())
        {
            Debug.Log($"Game Type Id: {reader[0]}, Game Type Description: {reader[1]}");
        }
        databaseConnection.Close();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
