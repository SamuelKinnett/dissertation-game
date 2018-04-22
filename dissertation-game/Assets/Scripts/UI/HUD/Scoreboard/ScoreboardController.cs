using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Assets.Scripts.Player.Enums;

public class ScoreboardController : MonoBehaviour
{
    public GameObject PlayerScorePrefab;

    private Dictionary<int, PlayerScoreController> playerScores;

    public void AddPlayer(int playerId, Team playerTeam = Team.Random)
    {
        if (playerScores == null)
        {
            playerScores = new Dictionary<int, PlayerScoreController>();
        }

        if (!playerScores.ContainsKey(playerId))
        {
            var newPlayerScoreController = Instantiate(PlayerScorePrefab).GetComponent<PlayerScoreController>();
            newPlayerScoreController.gameObject.transform.parent = gameObject.transform;
            playerScores.Add(playerId, newPlayerScoreController);
        }

        playerScores[playerId].PlayerTeam = playerTeam;
    }

    public void RemovePlayer(int playerId)
    {
        if (playerScores != null)
        {
            if (playerScores.ContainsKey(playerId))
            {
                Destroy(playerScores[playerId].gameObject);
                playerScores.Remove(playerId);
            }
        }
    }

    public void UpdatePlayerTeam(int playerId, Team newTeam)
    {
        if (playerScores == null)
        {
            playerScores = new Dictionary<int, PlayerScoreController>();
        }

        if (!playerScores.ContainsKey(playerId))
        {
            // Add the player if they don't already exist
            AddPlayer(playerId, newTeam);
        }

        playerScores[playerId].PlayerTeam = newTeam;
    }

    public void UpdatePlayerDeaths(int playerId, int newDeathsValue)
    {
        if (playerScores == null)
        {
            playerScores = new Dictionary<int, PlayerScoreController>();
        }

        if (!playerScores.ContainsKey(playerId))
        {
            // Add the player if they don't already exist
            AddPlayer(playerId);
        }

        playerScores[playerId].PlayerDeaths = newDeathsValue;
        SortPlayers();
    }

    public void UpdatePlayerKills(int playerId, int newKillsValue)
    {
        if (playerScores == null)
        {
            playerScores = new Dictionary<int, PlayerScoreController>();
        }

        if (!playerScores.ContainsKey(playerId))
        {
            // Add the player if they don't already exist
            AddPlayer(playerId);
        }

        playerScores[playerId].PlayerKills = newKillsValue;
        SortPlayers();
    }

    public void UpdatePlayerName(int playerId, string newName)
    {
        if (playerScores == null)
        {
            playerScores = new Dictionary<int, PlayerScoreController>();
        }

        if (!playerScores.ContainsKey(playerId))
        {
            // Add the player if they don't already exist
            AddPlayer(playerId);
        }

        playerScores[playerId].PlayerName = newName;
    }

    /// <summary>
    /// Sorts the player list by order of kills, then deaths
    /// </summary>
    private void SortPlayers()
    {
        var playerScoresAsList = playerScores.ToList();

        playerScoresAsList.Sort((firstPlayerScore, secondPlayerScore) =>
        {
            return firstPlayerScore.Value.PlayerKills == secondPlayerScore.Value.PlayerKills
                ? firstPlayerScore.Value.PlayerDeaths.CompareTo(secondPlayerScore.Value.PlayerDeaths)
                : -firstPlayerScore.Value.PlayerKills.CompareTo(secondPlayerScore.Value.PlayerKills);
        });

        playerScores = playerScoresAsList.ToDictionary(item => item.Key, item => item.Value);

        // Update the order of the game objects
        int currentPlayerScoreIndex = 1;

        foreach (var playerScore in playerScores)
        {
            playerScore.Value.transform.SetSiblingIndex(currentPlayerScoreIndex);
            ++currentPlayerScoreIndex;
        }
    }

    private void Awake()
    {
        if (playerScores == null)
        {
            playerScores = new Dictionary<int, PlayerScoreController>();
        }
    }
}
