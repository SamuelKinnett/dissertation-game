SELECT IFNULL
((
    SELECT TeamNumber
    FROM Players 
    JOIN (
        SELECT PlayerId, TeamId 
        FROM TeamPlayers
        JOIN (
            SELECT * 
            FROM Teams 
            JOIN (
                SELECT GameId
                FROM Sessions 
                JOIN Games 
                USING (SessionId) 
                WHERE SessionId = 10) 
            USING (GameId))
        Using (TeamId))
    USING (PlayerId)
    WHERE PlayerId = 33),
    -1
);