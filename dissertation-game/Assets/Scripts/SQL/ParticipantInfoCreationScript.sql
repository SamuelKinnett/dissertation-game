DROP TABLE IF EXISTS Participants;

CREATE TABLE Participants (
    ParticipantId INTEGER PRIMARY KEY NOT NULL,
    Name TEXT NOT NULL,
    Email TEXT NOT NULL,
    DeviceId TEXT NOT NULL
);