using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

using UnityEngine;
using UnityEngine.Networking;

using Assets.Scripts.Environment.Enums;
using System;

public class DatabaseManager : NetworkBehaviour
{
    SQLiteConnection databaseConnection;

    int currentSessionId;
    int currentGameId;

    /// <summary>
    /// Called when starting a new testing session. Inserts a new row into the
    /// Sessions table and updates the current session ID variable.
    /// </summary>
    public void StartNewSession()
    {
        var sql =
            "INSERT INTO Sessions DEFAULT VALUES;" +
            "SELECT last_insert_rowid();";

        using (var command = new SQLiteCommand(sql, databaseConnection))
        {
            databaseConnection.Open();

            currentSessionId = Convert.ToInt32(command.ExecuteScalar());

            databaseConnection.Close();
        }

        Debug.Log($"Current session ID: {currentSessionId}");
    }

    /// <summary>
    /// Called when starting a new game. Inserts a new row into the Games table
    /// and updates the current game ID variable.
    /// </summary>
    public void StartNewGame(GameType gameType)
    {
        if (currentSessionId == -1)
        {
            StartNewSession();
        }

        var sql =
            "INSERT INTO Games (SessionId, GameTypeId, Date, Duration) VALUES (@sessionid, @gametypeid, @date, 0);" +
            "SELECT last_insert_rowid();";

        using (var command = new SQLiteCommand(sql, databaseConnection))
        {
            databaseConnection.Open();

            command.Parameters.Add(new SQLiteParameter("@sessionid", currentSessionId));
            command.Parameters.Add(new SQLiteParameter("@gametypeid", (int)gameType));
            command.Parameters.Add(new SQLiteParameter("@date", DateTimeOffset.Now.ToUnixTimeSeconds()));
            currentGameId = Convert.ToInt32(command.ExecuteScalar());

            databaseConnection.Close();
        }

        Debug.Log($"Current Game ID: {currentGameId}");
    }

    /// <summary>
    /// Inserts a new map chromosome into the Maps table for the current game and
    /// session.
    /// </summary>
    /// <param name="genotype">A JSON encoded string containing the current chromosome</param>
    public void InsertNewMap(string chromosome)
    {
        if (currentGameId == -1)
        {
            throw new Exception("Cannot add a map when a game has not been started.");
        }

        var sql = "INSERT INTO Maps (GameId, Date, Chromosome) VALUES (@gameid, @date, @chromosome);";

        using (var command = new SQLiteCommand(sql, databaseConnection))
        {
            databaseConnection.Open();

            command.Parameters.Add(new SQLiteParameter("@gameid", currentGameId));
            command.Parameters.Add(new SQLiteParameter("@date", DateTimeOffset.Now.ToUnixTimeSeconds()));
            command.Parameters.Add(new SQLiteParameter("@chromosome", chromosome));
            command.ExecuteNonQuery();

            databaseConnection.Close();
        }

        Debug.Log("Map chromosome added to database");
    }

    /// <summary>
    /// Inserts a new player into the Players table and returns the PlayerId
    /// of the newly created row.
    /// </summary>
    public int InsertNewPlayer(string playerName)
    {
        int newPlayerId = -1;
        var sql = "INSERT INTO Players (Name) VALUES (@name);" +
            "SELECT last_insert_rowid();";

        // Ensure the player name isn't longer than the allowed maximum (50
        // characters) and, if it is, truncate it.
        playerName = playerName.Length > 50
            ? playerName.Substring(0, 50)
            : playerName;

        using (var command = new SQLiteCommand(sql, databaseConnection))
        {
            databaseConnection.Open();

            command.Parameters.Add(new SQLiteParameter("@name", playerName));
            newPlayerId = Convert.ToInt32(command.ExecuteScalar());

            databaseConnection.Close();
        }

        return newPlayerId;
    }

    /// <summary>
    /// Inserts a new team into the Teams table for the current game and 
    /// returns the TeamId of the newly created row.
    /// </summary>
    public int InsertNewTeam()
    {
        if (currentGameId == -1)
        {
            throw new Exception("Cannot add a team when a game has not been started.");
        }

        int newTeamId = -1;
        var sql = "INSERT INTO Teams (GameId) VALUES (@gameid);" +
            "SELECT last_insert_rowid();";

        using (var command = new SQLiteCommand(sql, databaseConnection))
        {
            databaseConnection.Open();

            command.Parameters.Add(new SQLiteParameter("@gameid", currentGameId));
            newTeamId = Convert.ToInt32(command.ExecuteScalar());

            databaseConnection.Close();
        }

        return newTeamId;
    }

    public void AddPlayerToTeam(int playerId, int teamId)
    {
        using (var command = new SQLiteCommand(databaseConnection))
        {
            // Check that the player and team exist
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM Players WHERE PlayerId = @playerid);";
            command.Parameters.Add(new SQLiteParameter("@playerid", playerId));
            bool playerExists = Convert.ToBoolean(command.ExecuteScalar());
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM Teams WHERE TeamId = @teamid);";
            command.Parameters.Add(new SQLiteParameter("@teamid", teamId));
            bool teamExists = Convert.ToBoolean(command.ExecuteScalar());

            if (playerExists && teamExists)
            {
                command.CommandText = "INSERT INTO TeamPlayers VALUES (@teamid, @playerid);";
                command.ExecuteNonQuery();
            }
            else
            {
                throw new Exception("The specified player or team does not exist in the database.");
            }
        }
    }

    /// <summary>
    /// Finishes the current game, filling in the duration column and resetting
    /// the current game ID variable. If the game wasn't a draw, then it also
    /// creates a new row in the Victories table.
    /// </summary>
    public void FinishGame(int gameId, int winningTeamId = -1)
    {
        using (var command = new SQLiteCommand(databaseConnection))
        {
            // Check that the game exists
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM Games WHERE GameId = @gameid);";
            command.Parameters.Add(new SQLiteParameter("@gameid", gameId));
            bool gameExists = Convert.ToBoolean(command.ExecuteScalar());

            if (gameExists)
            {
                // Calculate the game duration
                command.CommandText = "SELECT Date FROM Games WHERE GameId = @gameid;";
                long datetimeGameStarted = Convert.ToInt64(command.ExecuteScalar());
                long duration = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - datetimeGameStarted;

                command.CommandText = "UPDATE Games SET Duration = @duration WHERE GameId = @gameid;";
                command.Parameters.Add(new SQLiteParameter("@duration", duration));
                command.ExecuteNonQuery();

                if (winningTeamId != -1)
                {
                    // Check that the winning team exists
                    command.CommandText = "SELECT EXISTS (SELECT 1 FROM Teams WHERE TeamId = @teamid);";
                    command.Parameters.Add(new SQLiteParameter("@teamid", winningTeamId));
                    bool winningTeamExists = Convert.ToBoolean(command.ExecuteScalar());

                    if (winningTeamExists)
                    {
                        command.CommandText = "INSERT INTO Victories VALUES (@gameid, @teamid);";
                        command.ExecuteNonQuery();
                    }
                    else
                    {
                        throw new Exception("The winning team does not exist.");
                    }
                }
            }
            else
            {
                throw new Exception("The provided game does not exist.");
            }
        }
    }

    /// <summary>
    /// Adds a new capture to the Captures table for the current game and the
    /// provided team.
    /// </summary>
    public void AddNewCapture(int teamId)
    {
        if (currentGameId == -1)
        {
            throw new Exception("Cannot add a capture when a game has not been started.");
        }

        using (var command = new SQLiteCommand(databaseConnection))
        {
            // Check the team exists
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM Teams WHERE TeamId = @teamid);";
            command.Parameters.Add(new SQLiteParameter("@teamid", teamId));
            bool teamExists = Convert.ToBoolean(command.ExecuteScalar());

            if (teamExists)
            {
                command.CommandText = "INSERT INTO Captures (GameId, TeamId, Date) VALUES (@gameid, @teamid, @date);";
                command.Parameters.Add(new SQLiteParameter("@gameid", currentGameId));
                command.Parameters.Add(new SQLiteParameter("@date", DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
                command.ExecuteNonQuery();
            }
            else
            {
                throw new Exception("The provided team does not exist");
            }
        }
    }

    /// <summary>
    /// Adds a new shot to the Shots table for the current game and returns the
    /// id of the newly created row.
    /// </summary>
    /// <returns></returns>
    public int AddNewShot(int playerId, string origin, string direction, int recipientId = -1, string recipientPosition = "")
    {
        if (currentGameId == -1)
        {
            throw new Exception("Cannot add a shot when a game has not been started.");
        }

        int newShotId = -1;

        using (var command = new SQLiteCommand(databaseConnection))
        {
            // Check that the player exists
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM Players WHERE PlayerId = @playerid);";
            command.Parameters.Add(new SQLiteParameter("@playerid", playerId));
            bool playerExists = Convert.ToBoolean(command.ExecuteScalar());

            if (playerExists)
            {
                var currentDateTimeInt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                command.Parameters.Add(new SQLiteParameter("@gameid", currentGameId));
                command.Parameters.Add(new SQLiteParameter("@origin", origin));
                command.Parameters.Add(new SQLiteParameter("@direction", direction));
                command.Parameters.Add(new SQLiteParameter("@date", currentDateTimeInt));

                if (recipientId != -1)
                {
                    // Check that the recipient exists
                    command.CommandText = "SELECT EXISTS (SELECT 1 FROM Players WHERE PlayerId = @recipientid);";
                    command.Parameters.Add(new SQLiteParameter("@recipientid", recipientId));
                    bool recipientExists = Convert.ToBoolean(command.ExecuteScalar());

                    if (recipientExists)
                    {
                        command.CommandText =
                            "INSERT INTO Shots (GameId, ShooterId, Origin, Direction, RecipientId, RecipientPosition, Date) VALUES (@gameid, @playerid, @origin, @direction, @recipientid, @recipientposition, @date);" +
                            "SELECT last_insert_rowid();";
                        command.Parameters.Add(new SQLiteParameter("@recipientid", recipientId));
                        command.Parameters.Add(new SQLiteParameter("@recipientposition", recipientPosition));
                        newShotId = Convert.ToInt32(command.ExecuteScalar());
                    }
                    else
                    {
                        throw new Exception("The provided recipient does not exist.");
                    }
                }
                else
                {
                    command.CommandText = 
                        "INSERT INTO Shots (GameId, ShooterId, Origin, Direction, Date) VALUES (@gameid, @playerid, @origin, @direction, @date);" +
                        "SELECT last_insert_rowid();";
                    newShotId = Convert.ToInt32(command.ExecuteScalar());
                }
            }
            else
            {
                throw new Exception("The provided player does not exist.");
            }
        }

        return newShotId;
    }

    /// <summary>
    /// Adds a new kill to the Kills table for the current game and returns the
    /// id of the newly created row.
    /// </summary>
    public void AddKill(int playerId, int targetId, int shotId)
    {
        if (currentGameId == -1)
        {
            throw new Exception("Cannot add a kill when a game has not been started.");
        }

        using (var command = new SQLiteCommand(databaseConnection))
        {
            // Check that the player, target and shot all exist
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM Players WHERE PlayerId = @playerid);";
            command.Parameters.Add(new SQLiteParameter("@playerid", playerId));
            bool playerExists = Convert.ToBoolean(command.ExecuteScalar());
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM Players WHERE PlayerId = @targetid);";
            command.Parameters.Add(new SQLiteParameter("@targetid", targetId));
            bool targetExists = Convert.ToBoolean(command.ExecuteScalar());
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM Shots WHERE ShotId = @shotid);";
            command.Parameters.Add(new SQLiteParameter("@shotid", shotId));
            bool shotExists = Convert.ToBoolean(command.ExecuteScalar());

            if (playerExists && targetExists && shotExists)
            {
                command.CommandText = "INSERT INTO Kills (KillerId, TargetId, GameId, ShotId) values (@playerid, @targetid, @gameid, @shotid);";
                command.Parameters.Add(new SQLiteParameter("@gameid", currentGameId));
                command.ExecuteNonQuery();
            }
            else
            {
                throw new Exception("The specified player, target or shot does not exist in the database.");
            }
        }
    }

    // Use this for initialization
    void Start()
    {
        currentSessionId = -1;
        currentGameId = -1;

        //if (isServer)
        //{
        InitialiseDatabase();
        StartNewSession();
        StartNewGame(GameType.Control);
        StartNewGame(GameType.Procedural);
        //}
        //
        //var databaseFilePath = $"{Application.dataPath}\\Database\\Database.db";
        //databaseConnection = new SQLiteConnection($"Data Source={databaseFilePath};Version=3");
        //databaseConnection.Open();
        //string sql = "SELECT * FROM GameTypes";
        //var command = new SQLiteCommand(sql, databaseConnection);
        //var reader = command.ExecuteReader();
        //while (reader.Read())
        //{
        //    Debug.Log($"Game Type Id: {reader[0]}, Game Type Description: {reader[1]}");
        //}
        //databaseConnection.Close();
    }

    private void InitialiseDatabase()
    {
        var connectionString = new SQLiteConnectionStringBuilder();
        connectionString.DataSource = $"{Application.dataPath}\\Database\\Database.db";
        connectionString.ForeignKeys = true;
        connectionString.Version = 3;

        databaseConnection = new SQLiteConnection(connectionString.ToString());
    }
}
