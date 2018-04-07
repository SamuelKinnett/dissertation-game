using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

using Mono.Data.Sqlite;

using UnityEngine;
using UnityEngine.Networking;

using Assets.Scripts.Environment.Enums;

public class DatabaseManager : NetworkBehaviour
{
    public static DatabaseManager Instance;

    private SqliteConnection databaseConnection;

    private int currentSessionId;
    private int currentGameId;

    public void InitialiseDatabase()
    {
        var connectionString = new SqliteConnectionStringBuilder();
        connectionString.DataSource = $"{Application.streamingAssetsPath}/Database/Database.db";
        // connectionString.ForeignKeys = true;
        connectionString.Version = 3;

        databaseConnection = new SqliteConnection(connectionString.ToString());
    }

    /// <summary>
    /// Called when starting a new testing session. Inserts a new row into the
    /// Sessions table and updates the current session ID variable.
    /// </summary>
    public void StartNewSession()
    {
        var sql =
            "INSERT INTO Sessions DEFAULT VALUES;" +
            "SELECT last_insert_rowid();";

        using (var command = new SqliteCommand(sql, databaseConnection))
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

        using (var command = new SqliteCommand(sql, databaseConnection))
        {
            databaseConnection.Open();

            command.Parameters.Add(new SqliteParameter("@sessionid", currentSessionId));
            command.Parameters.Add(new SqliteParameter("@gametypeid", (int)gameType));
            command.Parameters.Add(new SqliteParameter("@date", DateTimeOffset.Now.ToUnixTimeSeconds()));
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

        using (var command = new SqliteCommand(sql, databaseConnection))
        {
            databaseConnection.Open();

            command.Parameters.Add(new SqliteParameter("@gameid", currentGameId));
            command.Parameters.Add(new SqliteParameter("@date", DateTimeOffset.Now.ToUnixTimeSeconds()));
            command.Parameters.Add(new SqliteParameter("@chromosome", chromosome));
            command.ExecuteNonQuery();

            databaseConnection.Close();
        }

        Debug.Log("Map chromosome added to database");
    }

    /// <summary>
    /// If a player entry does not exist for the specified device ID, a new row
    /// is inserted into the Players table and the new player ID is returned.
    /// Otherwise the existing player ID is returned.
    /// </summary>
    public int AddPlayer(string playerName, string playerDeviceId)
    {
        int newPlayerId = -1;

        using (var command = new SqliteCommand(databaseConnection))
        {
            databaseConnection.Open();

            // Check to see if this player has connected before
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM Players WHERE PlayerDeviceId = @playerdeviceid);";
            command.Parameters.Add(new SqliteParameter("@playerdeviceid", playerDeviceId));
            bool playerAlreadyPresent = Convert.ToBoolean(command.ExecuteScalar());

            if (playerAlreadyPresent)
            {
                Debug.Log("Player already present");

                // Retrieve the player ID for the player device ID
                command.CommandText = "SELECT PlayerId FROM Players WHERE PlayerDeviceId = @playerdeviceid;";
                newPlayerId = Convert.ToInt32(command.ExecuteScalar());
            }
            else
            {
                // Ensure the player name isn't longer than the allowed maximum (50
                // characters) and, if it is, truncate it.
                playerName = playerName.Length > 50
                    ? playerName.Substring(0, 50)
                    : playerName;

                // Add the player to the database
                command.CommandText = "INSERT INTO Players (PlayerDeviceId, Name) VALUES (@playerdeviceid, @name);" +
                    "SELECT last_insert_rowid();";
                command.Parameters.Add(new SqliteParameter("@name", playerName));
                newPlayerId = Convert.ToInt32(command.ExecuteScalar());
            }

            databaseConnection.Close();
        }

        return newPlayerId;
    }

    /// <summary>
    /// Inserts a new team into the Teams table for the current game and 
    /// returns the TeamId of the newly created row.
    /// </summary>
    public int AddTeam()
    {
        if (currentGameId == -1)
        {
            throw new Exception("Cannot add a team when a game has not been started.");
        }

        int newTeamId = -1;
        var sql = "INSERT INTO Teams (GameId) VALUES (@gameid);" +
            "SELECT last_insert_rowid();";

        using (var command = new SqliteCommand(sql, databaseConnection))
        {
            databaseConnection.Open();

            command.Parameters.Add(new SqliteParameter("@gameid", currentGameId));
            newTeamId = Convert.ToInt32(command.ExecuteScalar());

            databaseConnection.Close();
        }

        return newTeamId;
    }

    public void AddPlayerToTeam(int playerId, int teamId)
    {
        using (var command = new SqliteCommand(databaseConnection))
        {
            databaseConnection.Open();

            // Check that the player and team exist
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM Players WHERE PlayerId = @playerid);";
            command.Parameters.Add(new SqliteParameter("@playerid", playerId));
            bool playerExists = Convert.ToBoolean(command.ExecuteScalar());
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM Teams WHERE TeamId = @teamid);";
            command.Parameters.Add(new SqliteParameter("@teamid", teamId));
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

            databaseConnection.Close();
        }
    }

    /// <summary>
    /// Finishes the current game, filling in the duration column and resetting
    /// the current game ID variable. If the game wasn't a draw, then it also
    /// creates a new row in the Victories table.
    /// </summary>
    public void FinishGame(int gameId, int winningTeamId = -1)
    {
        using (var command = new SqliteCommand(databaseConnection))
        {
            databaseConnection.Open();

            // Check that the game exists
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM Games WHERE GameId = @gameid);";
            command.Parameters.Add(new SqliteParameter("@gameid", gameId));
            bool gameExists = Convert.ToBoolean(command.ExecuteScalar());

            if (gameExists)
            {
                // Calculate the game duration
                command.CommandText = "SELECT Date FROM Games WHERE GameId = @gameid;";
                long datetimeGameStarted = Convert.ToInt64(command.ExecuteScalar());
                long duration = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - datetimeGameStarted;

                command.CommandText = "UPDATE Games SET Duration = @duration WHERE GameId = @gameid;";
                command.Parameters.Add(new SqliteParameter("@duration", duration));
                command.ExecuteNonQuery();

                if (winningTeamId != -1)
                {
                    // Check that the winning team exists
                    command.CommandText = "SELECT EXISTS (SELECT 1 FROM Teams WHERE TeamId = @teamid);";
                    command.Parameters.Add(new SqliteParameter("@teamid", winningTeamId));
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

            databaseConnection.Close();
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

        using (var command = new SqliteCommand(databaseConnection))
        {
            databaseConnection.Open();

            // Check the team exists
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM Teams WHERE TeamId = @teamid);";
            command.Parameters.Add(new SqliteParameter("@teamid", teamId));
            bool teamExists = Convert.ToBoolean(command.ExecuteScalar());

            if (teamExists)
            {
                command.CommandText = "INSERT INTO Captures (GameId, TeamId, Date) VALUES (@gameid, @teamid, @date);";
                command.Parameters.Add(new SqliteParameter("@gameid", currentGameId));
                command.Parameters.Add(new SqliteParameter("@date", DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
                command.ExecuteNonQuery();
            }
            else
            {
                throw new Exception("The provided team does not exist");
            }

            databaseConnection.Close();
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

        using (var command = new SqliteCommand(databaseConnection))
        {
            databaseConnection.Open();

            // Check that the player exists
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM Players WHERE PlayerId = @playerid);";
            command.Parameters.Add(new SqliteParameter("@playerid", playerId));
            bool playerExists = Convert.ToBoolean(command.ExecuteScalar());

            if (playerExists)
            {
                var currentDateTimeInt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                command.Parameters.Add(new SqliteParameter("@gameid", currentGameId));
                command.Parameters.Add(new SqliteParameter("@origin", origin));
                command.Parameters.Add(new SqliteParameter("@direction", direction));
                command.Parameters.Add(new SqliteParameter("@date", currentDateTimeInt));

                if (recipientId != -1)
                {
                    // Check that the recipient exists
                    command.CommandText = "SELECT EXISTS (SELECT 1 FROM Players WHERE PlayerId = @recipientid);";
                    command.Parameters.Add(new SqliteParameter("@recipientid", recipientId));
                    bool recipientExists = Convert.ToBoolean(command.ExecuteScalar());

                    if (recipientExists)
                    {
                        command.CommandText =
                            "INSERT INTO Shots (GameId, ShooterId, Origin, Direction, RecipientId, RecipientPosition, Date) VALUES (@gameid, @playerid, @origin, @direction, @recipientid, @recipientposition, @date);" +
                            "SELECT last_insert_rowid();";
                        command.Parameters.Add(new SqliteParameter("@recipientid", recipientId));
                        command.Parameters.Add(new SqliteParameter("@recipientposition", recipientPosition));
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

            databaseConnection.Close();
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

        using (var command = new SqliteCommand(databaseConnection))
        {
            databaseConnection.Open();

            // Check that the player, target and shot all exist
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM Players WHERE PlayerId = @playerid);";
            command.Parameters.Add(new SqliteParameter("@playerid", playerId));
            bool playerExists = Convert.ToBoolean(command.ExecuteScalar());
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM Players WHERE PlayerId = @targetid);";
            command.Parameters.Add(new SqliteParameter("@targetid", targetId));
            bool targetExists = Convert.ToBoolean(command.ExecuteScalar());
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM Shots WHERE ShotId = @shotid);";
            command.Parameters.Add(new SqliteParameter("@shotid", shotId));
            bool shotExists = Convert.ToBoolean(command.ExecuteScalar());

            if (playerExists && targetExists && shotExists)
            {
                command.CommandText = "INSERT INTO Kills (KillerId, TargetId, GameId, ShotId) values (@playerid, @targetid, @gameid, @shotid);";
                command.Parameters.Add(new SqliteParameter("@gameid", currentGameId));
                command.ExecuteNonQuery();
            }
            else
            {
                throw new Exception("The specified player, target or shot does not exist in the database.");
            }

            databaseConnection.Close();
        }
    }

    // Ensure there is only one DatabaseManager
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            currentSessionId = -1;
            currentGameId = -1;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
}
