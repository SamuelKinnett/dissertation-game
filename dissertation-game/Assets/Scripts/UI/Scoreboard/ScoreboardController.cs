﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Assets.Scripts.Player.Enums;

public class ScoreboardController : MonoBehaviour
{
    public GameObject PlayerScorePrefab;

    private Dictionary<int, PlayerScoreController> playerScores;

    // Use this for initialization
    void Start()
    {
        playerScores = new Dictionary<int, PlayerScoreController>();
    }

    public void AddPlayer(int playerId, Team playerTeam = Team.Random)
    {
        if (!playerScores.ContainsKey(playerId))
        {
            playerScores.Add(playerId, Instantiate(PlayerScorePrefab).GetComponent<PlayerScoreController>());
        }

        playerScores[playerId].PlayerTeam = playerTeam;
    }

    public void RemovePlayer(int playerId)
    {
        if (playerScores.ContainsKey(playerId))
        {
            Destroy(playerScores[playerId].gameObject);
            playerScores.Remove(playerId);
        }
    }

    public void UpdatePlayerTeam(int playerId, Team newTeam)
    {
        if (!playerScores.ContainsKey(playerId))
        {
            // Add the player if they don't already exist
            AddPlayer(playerId, newTeam);
        }

        playerScores[playerId].PlayerTeam = newTeam;
    }

    public void UpdatePlayerDeaths(int playerId, int newDeathsValue)
    {
        if (!playerScores.ContainsKey(playerId))
        {
            // Add the player if they don't already exist
            AddPlayer(playerId);
        }

        playerScores[playerId].PlayerDeaths = newDeathsValue;
    }

    public void UpdatePlayerKills(int playerId, int newKillsValue)
    {
        if (!playerScores.ContainsKey(playerId))
        {
            // Add the player if they don't already exist
            AddPlayer(playerId);
        }

        playerScores[playerId].PlayerKills = newKillsValue;
    }

    public void UpdatePlayerName(int playerId, string newName)
    {
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
        for (int currentPlayerScoreIndex = 0; currentPlayerScoreIndex < playerScores.Count; ++currentPlayerScoreIndex)
        {
            playerScores[currentPlayerScoreIndex].transform.SetSiblingIndex(currentPlayerScoreIndex + 1);
        }
    }
}
