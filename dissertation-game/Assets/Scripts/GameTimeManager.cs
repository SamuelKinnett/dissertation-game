using System.Collections;
using System.Collections.Generic;

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
    private bool RedTeamCaptureTimerPaused;

    [SyncVar]
    private bool GameTimerPaused;

    [SyncVar]
    private bool BlueTeamCaptureTimerPaused;

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
            }
            if (!RedTeamCaptureTimerPaused)
            {
                RedTeamCaptureTimeRemaining -= Time.deltaTime;
            }
            if (!BlueTeamCaptureTimerPaused)
            {
                BlueTeamCaptureTimeRemaining -= Time.deltaTime;
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
