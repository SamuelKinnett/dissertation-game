DROP TABLE IF EXISTS Kills;
DROP TABLE IF EXISTS Shots;
DROP TABLE IF EXISTS Captures;
DROP TABLE IF EXISTS Victories;
DROP TABLE IF EXISTS Answers;
DROP TABLE IF EXISTS TeamPlayers;
DROP TABLE IF EXISTS Teams;
DROP TABLE IF EXISTS Players;
DROP TABLE IF EXISTS Maps;
DROP TABLE IF EXISTS Games;
DROP TABLE IF EXISTS GameTypes;
DROP TABLE IF EXISTS Sessions;

CREATE TABLE Sessions (
    SessionId INTEGER PRIMARY KEY NOT NULL
);

CREATE TABLE GameTypes (
    GameTypeId INTEGER PRIMARY KEY NOT NULL,
    GameTypeDescription NCHAR(50) NOT NULL
);

INSERT INTO GameTypes VALUES (0, "Control");
INSERT INTO GameTypes VALUES (1, "Procedural");

CREATE TABLE Games (
    GameId INTEGER PRIMARY KEY NOT NULL,
    SessionId INTEGER NOT NULL,
    GameTypeId INTEGER NOT NULL,
    Date INTEGER NOT NULL,
    Duration INTEGER NOT NULL,
    FOREIGN KEY (SessionId) REFERENCES Sessions(SessionId),
    FOREIGN KEY (GameTypeId) REFERENCES GameTypes(GameTypeId)
);

CREATE TABLE Maps (
    MapId INTEGER PRIMARY KEY NOT NULL,
    GameId INTEGER NOT NULL,
    Date INTEGER NOT NULL,
    Chromosome TEXT NOT NULL,
    FOREIGN KEY (GameId) REFERENCES Games (GameId)
);

CREATE TABLE Players (
    PlayerId INTEGER PRIMARY KEY NOT NULL,
    PlayerDeviceId TEXT NOT NULL UNIQUE
);

CREATE TABLE Teams (
    TeamId INTEGER PRIMARY KEY NOT NULL,
    GameId INTEGER NOT NULL,
    TeamNumber INTEGER NOT NULL,
    FOREIGN KEY (GameId) REFERENCES Games (GameId)
);

CREATE TABLE TeamPlayers (
    TeamId INTEGER NOT NULL,
    PlayerId INTEGER NOT NULL,
    FOREIGN KEY (TeamId) REFERENCES Teams (TeamId),
    FOREIGN KEY (PlayerId) REFERENCES Players (PlayerId),
    PRIMARY KEY (TeamId, PlayerId)
);

CREATE TABLE Answers (
    PlayerId INTEGER NOT NULL,
    GameId INTEGER NOT NULL,
    Answers TEXT NOT NULL,
    FOREIGN KEY (PlayerId) REFERENCES Players (PlayerId),
    FOREIGN KEY (GameId) REFERENCES Games (GameId),
    PRIMARY KEY (PlayerId, GameId)
);

CREATE TABLE Victories (
    GameId INTEGER NOT NULL,
    WinningTeamId INTEGER NOT NULL,
    FOREIGN KEY (GameId) REFERENCES Games (GameId),
    FOREIGN KEY (WinningTeamId) REFERENCES Teams (TeamId),
    PRIMARY KEY (GameId, WinningTeamId)
);

CREATE TABLE Captures (
    CaptureId INTEGER PRIMARY KEY NOT NULL,
    GameId INTEGER NOT NULL,
    TeamId INTEGER,
    Date INTEGER NOT NULL,
    FOREIGN KEY (GameId) REFERENCES Games (GameId),
    FOREIGN KEY (TeamId) REFERENCES Teams (TeamId)
);

CREATE TABLE Shots (
    ShotId INTEGER PRIMARY KEY NOT NULL,
    GameId INTEGER NOT NULL,
    ShooterId INTEGER NOT NULL,
    Origin TEXT NOT NULL,
    Direction TEXT NOT NULL,
    RecipientId INTEGER,
    RecipientPosition TEXT,
    Date INTEGER NOT NULL,
    FOREIGN KEY (GameId) REFERENCES Games (GameId),
    FOREIGN KEY (ShooterId) REFERENCES Players (PlayerId),
    FOREIGN KEY (RecipientId) REFERENCES Players (PlayerId)
);

CREATE TABLE Kills (
    KillId INTEGER PRIMARY KEY NOT NULL,
    KillerId INTEGER NOT NULL,
    TargetId INTEGER NOT NULL,
    GameId INTEGER NOT NULL,
    ShotId INTEGER NOT NULL,
    FOREIGN KEY (KillerId) REFERENCES Players (PlayerId),
    FOREIGN KEY (TargetId) REFERENCES Players (PlayerId),
    FOREIGN KEY (GameId) REFERENCES Games (GameId),
    FOREIGN KEY (ShotId) REFERENCES Shots (ShotId)
);