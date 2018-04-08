using Assets.Scripts.Player.Enums;
using Assets.Scripts.UI;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class PlayerScoreController : MonoBehaviour
{
    public string PlayerName
    {
        get
        {
            return playerName;
        }
        set
        {
            playerName = value;
            SetName(playerName);
        }
    }

    public int PlayerKills
    {
        get
        {
            return playerKills;
        }
        set
        {
            playerKills = value;
            SetKills(playerKills.ToString());
        }
    }

    public int PlayerDeaths
    {
        get
        {
            return playerDeaths;
        }
        set
        {
            playerDeaths = value;
            SetDeaths(playerDeaths.ToString());
        }
    }

    public Team PlayerTeam
    {
        get
        {
            return playerTeam;
        }
        set
        {
            playerTeam = value;
            SetTeam(playerTeam);
        }
    }

    public Text PlayerNameText;
    public Text PlayerKillsText;
    public Text PlayerDeathsText;

    private string playerName;
    private int playerKills;
    private int playerDeaths;
    private Team playerTeam;

    private void SetTeam(Team newTeam)
    {
        playerTeam = newTeam;
        UpdateColours();
    }

    private void SetName(string newName)
    {
        PlayerNameText.text = newName;
    }

    private void SetKills(string newKills)
    {
        PlayerKillsText.text = newKills;
    }

    private void SetDeaths(string newDeaths)
    {
        PlayerDeathsText.text = newDeaths;
    }

    private void UpdateColours()
    {
        var newColour = StaticColours.NeautralColour;

        switch (playerTeam)
        {
            case Team.Red:
                newColour = StaticColours.RedTeamColour;
                break;

            case Team.Blue:
                newColour = StaticColours.BlueTeamColour;
                break;
        }

        PlayerNameText.color = newColour;
        PlayerKillsText.color = newColour;
        PlayerDeathsText.color = newColour;
    }
}
