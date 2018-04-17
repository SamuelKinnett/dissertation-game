using Assets.Scripts.Player.Enums;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// A singleton class responsible for managing all time related functions in
/// the game.
/// </summary>
public class GameTimeManager : NetworkBehaviour
{
    public static GameTimeManager Instance;

    // How long games should last in seconds
    public float GameLength;

    // How long each team has to hold the capture point for to win
    public float RequiredHoldTime;

    [SyncVar]
    public float GameTimeRemaining;

    [SyncVar]
    public float RedTeamCaptureTimeRemaining;

    [SyncVar]
    public float BlueTeamCaptureTimeRemaining;

    [SyncVar]
    public bool RedTeamCaptureTimerPaused;

    [SyncVar]
    public bool GameTimerPaused;

    [SyncVar]
    public bool BlueTeamCaptureTimerPaused;

    // Ensure there is only one GameTimeManager
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Use this for initialization
    void Start()
    {
        if (isServer)
        {
            GameTimeRemaining = GameLength;
            GameTimerPaused = true;
            RedTeamCaptureTimeRemaining = RequiredHoldTime;
            RedTeamCaptureTimerPaused = true;
            BlueTeamCaptureTimeRemaining = RequiredHoldTime;
            BlueTeamCaptureTimerPaused = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isServer)
        {
            if (!GameTimerPaused)
            {
                GameTimeRemaining -= Time.deltaTime;

                if (GameTimeRemaining <= 0)
                {
                    GameTimerPaused = true;
                    RedTeamCaptureTimerPaused = true;
                    BlueTeamCaptureTimerPaused = true;

                    if (RedTeamCaptureTimeRemaining < BlueTeamCaptureTimeRemaining)
                    {
                        Player.players.First().Won(Team.Red);
                        DatabaseManager.Instance.FinishGame(GameInstanceData.Instance.RedTeamId);
                    }
                    else if (BlueTeamCaptureTimeRemaining < RedTeamCaptureTimeRemaining)
                    {
                        Player.players.First().Won(Team.Blue);
                        DatabaseManager.Instance.FinishGame(GameInstanceData.Instance.BlueTeamId);
                    }
                    else
                    {
                        Player.players.First().Won(Team.Random);
                        DatabaseManager.Instance.FinishGame();
                    }
                }
            }
            if (!RedTeamCaptureTimerPaused)
            {
                RedTeamCaptureTimeRemaining -= Time.deltaTime;

                if (RedTeamCaptureTimeRemaining <= 0)
                {
                    RedTeamCaptureTimeRemaining = 0;
                    RedTeamCaptureTimerPaused = true;
                    GameTimerPaused = true;
                    BlueTeamCaptureTimerPaused = true;

                    Player.players.First().Won(Team.Red);
                    DatabaseManager.Instance.FinishGame(GameInstanceData.Instance.RedTeamId);
                }
            }
            if (!BlueTeamCaptureTimerPaused)
            {
                BlueTeamCaptureTimeRemaining -= Time.deltaTime;

                if (BlueTeamCaptureTimeRemaining <= 0)
                {
                    BlueTeamCaptureTimeRemaining = 0;
                    BlueTeamCaptureTimerPaused = true;
                    GameTimerPaused = true;
                    RedTeamCaptureTimerPaused = true;

                    Player.players.First().Won(Team.Blue);
                    DatabaseManager.Instance.FinishGame(GameInstanceData.Instance.BlueTeamId);
                }
            }
        }
    }

    [Server]
    public void SetGameTimerPaused(bool newValue)
    {
        GameTimerPaused = newValue;
    }

    [Server]
    public void SetRedTeamTimerPaused(bool newValue)
    {
        RedTeamCaptureTimerPaused = newValue;
    }

    [Server]
    public void SetBlueTeamTimerPaused(bool newValue)
    {
        BlueTeamCaptureTimerPaused = newValue;
    }
}
