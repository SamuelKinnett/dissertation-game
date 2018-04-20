using System;
using System.IO;

using Mono.Data.Sqlite;

using UnityEngine;
using UnityEngine.Networking;

using Assets.Scripts.Environment.Enums;
using Assets.Scripts.Player.Enums;

public class DatabaseManager : NetworkBehaviour
{
    public static DatabaseManager Instance;

    private static string gameplayDatabasePath = "/Database/Database.db";
    private static string participantInfoDatabasePath = "/Database/ParticipantInfo.db";

    private SqliteConnection gameplayDatabaseConnection;
    private SqliteConnection participantInfoDatabaseConnection;

    private int currentSessionId;
    private int currentGameId;

    public static bool DoRequiredDatabasesExist()
    {
        return File.Exists(Application.streamingAssetsPath + gameplayDatabasePath) && File.Exists(Application.streamingAssetsPath + participantInfoDatabasePath);
    }

    public static bool TestConnections()
    {
        var connectionString = new SqliteConnectionStringBuilder();
        connectionString.DataSource = Application.streamingAssetsPath + gameplayDatabasePath;
        connectionString.Version = 3;

        try
        {
            using (var connection = new SqliteConnection(connectionString.ToString()))
            {
                connection.Open();
                connection.Close();
            }

            connectionString.DataSource = Application.streamingAssetsPath + participantInfoDatabasePath;

            using (var connection = new SqliteConnection(connectionString.ToString()))
            {
                connection.Open();
                connection.Close();
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public void InitialiseDatabases()
    {
        var connectionString = new SqliteConnectionStringBuilder();
        connectionString.DataSource = Application.streamingAssetsPath + gameplayDatabasePath;
        // connectionString.ForeignKeys = true;
        connectionString.Version = 3;

        gameplayDatabaseConnection = new SqliteConnection(connectionString.ToString());

        connectionString.DataSource = Application.streamingAssetsPath + participantInfoDatabasePath;

        participantInfoDatabaseConnection = new SqliteConnection(connectionString.ToString());
    }

    /// <summary>
    /// Add a new participant, or update existing participant details.
    /// </summary>
    public void AddParticipantInfo(string name, string email, string deviceId)
    {
        using (var command = new SqliteCommand(participantInfoDatabaseConnection))
        {
            participantInfoDatabaseConnection.Open();

            // Check to see if this participant has already been added
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM Participants WHERE DeviceId = @playerdeviceid);";
            command.Parameters.Add(new SqliteParameter("@playerdeviceid", deviceId));
            command.Parameters.Add(new SqliteParameter("@name", name));
            command.Parameters.Add(new SqliteParameter("@email", email));

            bool participantAlreadyPresent = Convert.ToBoolean(command.ExecuteScalar());

            if (participantAlreadyPresent)
            {
                command.CommandText = "UPDATE Participants SET Name = @name, Email = @email WHERE DeviceId = @playerdeviceid;";
                command.ExecuteNonQuery();
            }
            else
            {
                command.CommandText = "INSERT INTO Participants (Name, Email, DeviceId) VALUES (@name, @email, @playerdeviceid);";
                command.ExecuteNonQuery();
            }

            participantInfoDatabaseConnection.Close();
        }
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

        using (var command = new SqliteCommand(sql, gameplayDatabaseConnection))
        {
            gameplayDatabaseConnection.Open();

            currentSessionId = Convert.ToInt32(command.ExecuteScalar());

            gameplayDatabaseConnection.Close();
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

        using (var command = new SqliteCommand(sql, gameplayDatabaseConnection))
        {
            gameplayDatabaseConnection.Open();

            command.Parameters.Add(new SqliteParameter("@sessionid", currentSessionId));
            command.Parameters.Add(new SqliteParameter("@gametypeid", (int)gameType));
            command.Parameters.Add(new SqliteParameter("@date", DateTimeOffset.Now.ToUnixTimeSeconds()));
            currentGameId = Convert.ToInt32(command.ExecuteScalar());

            gameplayDatabaseConnection.Close();
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

        using (var command = new SqliteCommand(sql, gameplayDatabaseConnection))
        {
            gameplayDatabaseConnection.Open();

            command.Parameters.Add(new SqliteParameter("@gameid", currentGameId));
            command.Parameters.Add(new SqliteParameter("@date", DateTimeOffset.Now.ToUnixTimeSeconds()));
            command.Parameters.Add(new SqliteParameter("@chromosome", chromosome));
            command.ExecuteNonQuery();

            gameplayDatabaseConnection.Close();
        }

        Debug.Log("Map chromosome added to database");
    }

    /// <summary>
    /// If a player entry does not exist for the specified device ID, a new row
    /// is inserted into the Players table and the new player ID is returned.
    /// Otherwise the existing player ID is returned.
    /// </summary>
    public int AddPlayer(string playerDeviceId)
    {
        int newPlayerId = -1;

        using (var command = new SqliteCommand(gameplayDatabaseConnection))
        {
            gameplayDatabaseConnection.Open();

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
                // Add the player to the database
                command.CommandText = "INSERT INTO Players (PlayerDeviceId) VALUES (@playerdeviceid);" +
                    "SELECT last_insert_rowid();";
                newPlayerId = Convert.ToInt32(command.ExecuteScalar());
            }

            gameplayDatabaseConnection.Close();
        }

        return newPlayerId;
    }

    /// <summary>
    /// Inserts a new team into the Teams table for the current game and 
    /// returns the TeamId of the newly created row.
    /// </summary>
    public int AddTeam(Team teamType)
    {
        int teamnumber = (int)teamType;

        if (currentGameId == -1)
        {
            throw new Exception("Cannot add a team when a game has not been started.");
        }

        int newTeamId = -1;
        var sql = "INSERT INTO Teams (GameId, TeamNumber) VALUES (@gameid, @teamnumber);" +
            "SELECT last_insert_rowid();";

        using (var command = new SqliteCommand(sql, gameplayDatabaseConnection))
        {
            gameplayDatabaseConnection.Open();

            command.Parameters.Add(new SqliteParameter("@gameid", currentGameId));
            command.Parameters.Add(new SqliteParameter("@teamnumber", teamnumber));
            newTeamId = Convert.ToInt32(command.ExecuteScalar());

            gameplayDatabaseConnection.Close();
        }

        return newTeamId;
    }

    public void AddPlayerToTeam(int playerId, int teamId)
    {
        using (var command = new SqliteCommand(gameplayDatabaseConnection))
        {
            gameplayDatabaseConnection.Open();

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
                gameplayDatabaseConnection.Close();
                throw new Exception("The specified player or team does not exist in the database.");
            }

            gameplayDatabaseConnection.Close();
        }
    }

    /// <summary>
    /// If this player has already taken part in a game this session, then get
    /// their team ID. Used if a player disconnects then reconnects.
    /// </summary>
    /// <param name="playerId"></param>
    public Team GetPlayerTeamForSession(int playerId)
    {
        Team playerTeam;

        using (var command = new SqliteCommand(gameplayDatabaseConnection))
        {
            gameplayDatabaseConnection.Open();

            // Check that the player exists
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM Players WHERE PlayerId = @playerid);";
            command.Parameters.Add(new SqliteParameter("@playerid", playerId));
            bool playerExists = Convert.ToBoolean(command.ExecuteScalar());

            if (playerExists)
            {
                command.CommandText =
                    "SELECT IFNULL((" +
                    "   SELECT TeamNumber " +
                    "      FROM Players " +
                    "      JOIN( " +
                    "       SELECT PlayerId, TeamNumber " +
                    "       FROM TeamPlayers " +
                    "       JOIN( " +
                    "           SELECT * " +
                    "           FROM Teams " +
                    "           JOIN( " +
                    "               SELECT GameId " +
                    "               FROM Sessions " +
                    "               JOIN Games " +
                    "               USING(SessionId) " +
                    "               WHERE SessionId = @sessionid) " +
                    "           USING(GameId)) " +
                    "       Using(TeamId)) " +
                    "   USING(PlayerId) " +
                    "   WHERE PlayerId = @playerid)," +
                    "   -1);";
                command.Parameters.Add(new SqliteParameter("@sessionid", currentSessionId));
                var playerTeamNumber = Convert.ToInt32(command.ExecuteScalar());

                if (playerTeamNumber == -1)
                {
                    playerTeam = Team.Random;
                }
                else
                {
                    playerTeam = (Team)playerTeamNumber;
                }
            }
            else
            {
                gameplayDatabaseConnection.Close();
                throw new Exception("The specified player does not exist in the database.");
            }

            gameplayDatabaseConnection.Close();
        }

        return playerTeam;
    }

    /// <summary>
    /// Add answers for the specified player for the current game.
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="answers"></param>
    public void AddAnswers(int playerId, string answers)
    {
        using (var command = new SqliteCommand(gameplayDatabaseConnection))
        {
            gameplayDatabaseConnection.Open();

            // Check that the game exists
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM Games WHERE GameId = @gameid);";
            command.Parameters.Add(new SqliteParameter("@gameid", currentGameId));
            bool gameExists = Convert.ToBoolean(command.ExecuteScalar());

            if (gameExists)
            {
                // Check that the player exists
                command.CommandText = "SELECT EXISTS (SELECT 1 FROM Players WHERE PlayerId = @playerid);";
                command.Parameters.Add(new SqliteParameter("@playerid", playerId));
                bool playerExists = Convert.ToBoolean(command.ExecuteScalar());

                if (playerExists)
                {
                    command.CommandText = "INSERT INTO Answers (PlayerId, GameId, Answers) VALUES (@playerid, @gameid, @answers);";
                    command.Parameters.Add(new SqliteParameter("@answers", answers));
                    command.ExecuteNonQuery();
                }
                else
                {
                    gameplayDatabaseConnection.Close();
                    throw new Exception("The specified player does not exist in the database.");
                }
            }
            else
            {
                gameplayDatabaseConnection.Close();
                throw new Exception("The provided game does not exist.");
            }

            gameplayDatabaseConnection.Close();
        }
    }

    /// <summary>
    /// Finishes the current game, filling in the duration column and resetting
    /// the current game ID variable. If the game wasn't a draw, then it also
    /// creates a new row in the Victories table.
    /// </summary>
    public void FinishGame(int winningTeamId = -1)
    {
        using (var command = new SqliteCommand(gameplayDatabaseConnection))
        {
            gameplayDatabaseConnection.Open();

            // Check that the game exists
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM Games WHERE GameId = @gameid);";
            command.Parameters.Add(new SqliteParameter("@gameid", currentGameId));
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
                        gameplayDatabaseConnection.Close();
                        throw new Exception("The winning team does not exist.");
                    }
                }
            }
            else
            {
                gameplayDatabaseConnection.Close();
                throw new Exception("The provided game does not exist.");
            }

            gameplayDatabaseConnection.Close();
        }
    }

    /// <summary>
    /// Adds a new capture to the Captures table for the current game and the
    /// provided team.
    /// </summary>
    public void AddNewCapture(int? teamId)
    {
        if (currentGameId == -1)
        {
            throw new Exception("Cannot add a capture when a game has not been started.");
        }

        using (var command = new SqliteCommand(gameplayDatabaseConnection))
        {
            gameplayDatabaseConnection.Open();

            if (teamId.HasValue)
            {
                // Check the team exists
                command.CommandText = "SELECT EXISTS (SELECT 1 FROM Teams WHERE TeamId = @teamid);";
                command.Parameters.Add(new SqliteParameter("@teamid", teamId.Value));
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
                    gameplayDatabaseConnection.Close();
                    throw new Exception("The provided team does not exist");
                }
            }
            else
            {
                command.CommandText = "INSERT INTO Captures (GameId, Date) VALUES (@gameid, @date);";
                command.Parameters.Add(new SqliteParameter("@gameid", currentGameId));
                command.Parameters.Add(new SqliteParameter("@date", DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
                command.ExecuteNonQuery();
            }

            gameplayDatabaseConnection.Close();
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

        using (var command = new SqliteCommand(gameplayDatabaseConnection))
        {
            gameplayDatabaseConnection.Open();

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
                        gameplayDatabaseConnection.Close();
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
                gameplayDatabaseConnection.Close();
                throw new Exception("The provided player does not exist.");
            }

            gameplayDatabaseConnection.Close();
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

        using (var command = new SqliteCommand(gameplayDatabaseConnection))
        {
            gameplayDatabaseConnection.Open();

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
                gameplayDatabaseConnection.Close();
                throw new Exception("The specified player, target or shot does not exist in the database.");
            }

            gameplayDatabaseConnection.Close();
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
