using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

using Assets.Scripts.Player.Enums;
using Assets.Scripts.UI;

[System.Serializable]
public class ToggleEvent : UnityEvent<bool>
{
}

public class Player : NetworkBehaviour
{
    [SyncVar(hook = "OnNameChanged")]
    public string PlayerName;

    [SyncVar(hook = "OnTeamChanged")]
    public Team PlayerTeam;

    [SyncVar(hook = "OnIsCapturingChanged")]
    public bool IsCapturing;

    [SyncVar(hook = "OnDeathsChanged")]
    public int Deaths;

    [SyncVar]
    public int PlayerId;

    [SyncVar]
    public int PlayerTeamId;

    [SyncVar]
    public bool HasAnsweredQuestions;

    public static List<Player> players = new List<Player>();
    public static bool GameOver = false;

    public MapController mapController;
    public QuestionController questionController;
    public GameObject playerCapsule;

    [SerializeField] ToggleEvent onToggleShared;
    [SerializeField] ToggleEvent onToggleLocal;
    [SerializeField] ToggleEvent onToggleRemote;
    [SerializeField] float respawnTime = 5.0f;
    [SerializeField] Text playerNameText;

    private GameObject mainCamera;
    private bool initialised;

    /// <summary>
    /// Announce the winner and return to lobby
    /// </summary>
    [Server]
    public void Won(Team winningTeam)
    {
        GameOver = true;

        foreach (var currentPlayer in players)
        {
            currentPlayer.RpcGameOver(winningTeam);
        }
    }

    /// <summary>
    /// Kill this player and respawn them.
    /// </summary>
    public void Die()
    {
        if (isLocalPlayer)
        {
            PlayerCanvasController.Instance.WriteGameStatusText("You Died!");
            PlayerCanvasController.Instance.PlayDeathAudio();
        }

        DisablePlayer();

        Invoke("Respawn", respawnTime);
    }

    public void SubmitAnswers(string answers)
    {
        if (isLocalPlayer)
        {
            CmdSubmitAnswers(answers);
        }
    }

    [ClientRpc]
    public void RpcWarpToPosition(Vector3 newPosition)
    {
        transform.position = newPosition;
    }

    private void Start()
    {
        mainCamera = Camera.main.gameObject;

        initialised = !isLocalPlayer;
        if (isLocalPlayer)
        {
            DisablePlayer();
        }
        else
        {
            EnablePlayer();
        }

        if (!isServer)
        {
            PlayerCanvasController.Instance.AddPlayerToScoreboard(this);
        }
    }

    private void Update()
    {
        if (!initialised)
        {
            if (mapController == null)
            {
                var mapControllerObject = GameObject.Find("Map");
                if (mapControllerObject != null)
                {
                    mapController = mapControllerObject.GetComponent<MapController>();
                }
            }
            else
            {
                if (mapController.GetHasSpawnPositions(PlayerTeam))
                {
                    initialised = true;
                    Invoke("Respawn", mapController.previewTime);
                }
            }
        }

        if (isServer && GameOver)
        {
            if (players.TrueForAll(player => player.HasAnsweredQuestions))
            {
                BackToLobby();
            }
        }
    }

    /// <summary>
    /// Enables this player.
    /// </summary>
    private void EnablePlayer()
    {
        if (isLocalPlayer)
        {
            PlayerCanvasController.Instance.Initialise();
            mainCamera.SetActive(false);
        }

        onToggleShared.Invoke(true);

        if (isLocalPlayer)
        {
            onToggleLocal.Invoke(true);
        }
        else
        {
            onToggleRemote.Invoke(true);
        }
    }

    /// <summary>
    /// Disables this player.
    /// </summary>
    private void DisablePlayer()
    {
        if (isLocalPlayer)
        {
            PlayerCanvasController.Instance.HideCrosshair(true);
            mainCamera.SetActive(true);
        }

        onToggleShared.Invoke(false);

        if (isLocalPlayer)
        {
            onToggleLocal.Invoke(false);
        }
        else
        {
            onToggleRemote.Invoke(false);
        }
    }

    /// <summary>
    /// Respawn this player.
    /// </summary>
    private void Respawn()
    {
        if (isLocalPlayer)
        {
            PlayerCanvasController.Instance.PlayerLoaded();
            transform.position = mapController.GetSpawnPositionForTeam(PlayerTeam);
            transform.rotation = PlayerTeam == Team.Red ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
        }

        EnablePlayer();
    }

    /// <summary>
    /// Raises the name changed event.
    /// </summary>
    /// <param name="value">The new name value.</param>
    private void OnNameChanged(string value)
    {
        PlayerName = value;
        gameObject.name = PlayerName;
        playerNameText.text = PlayerName;

        if (!isServer)
        {
            PlayerCanvasController.Instance.UpdatePlayerNameOnScoreboard(this);
        }
    }

    private void OnTeamChanged(Team newValue)
    {
        var newColour = StaticColours.NeautralColour;

        switch (newValue)
        {
            case Team.Red:
                newColour = StaticColours.RedTeamColour;
                break;

            case Team.Blue:
                newColour = StaticColours.BlueTeamColour;
                break;
        }

        playerCapsule.GetComponent<Renderer>().material.color = newColour;

        if (!isServer)
        {
            PlayerCanvasController.Instance.UpdatePlayerTeamOnScoreboard(this);
        }
    }

    private void OnIsCapturingChanged(bool newValue)
    {
    }

    private void OnDeathsChanged(int newValue)
    {
        if (!isServer)
        {
            PlayerCanvasController.Instance.UpdatePlayerDeathsOnScoreboard(this);
        }
    }

    private void BackToLobby()
    {
        FindObjectOfType<NetworkLobbyManager>().ServerReturnToLobby();
    }

    [Command]
    private void CmdSubmitAnswers(string answers)
    {
        DatabaseManager.Instance.AddAnswers(PlayerId, answers);
        HasAnsweredQuestions = true;
    }

    [ServerCallback]
    private void OnEnable()
    {
        if (!players.Contains(this))
        {
            players.Add(this);
        }
    }

    [ServerCallback]
    private void OnDisable()
    {
        if (players.Contains(this))
        {
            players.Remove(this);
        }
    }

    [ClientRpc]
    void RpcGameOver(Team winningTeam)
    {
        DisablePlayer();

        // Unlock and show the cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (isLocalPlayer)
        {
            if (PlayerTeam == winningTeam)
            {
                PlayerCanvasController.Instance.WriteGameStatusText("Your team Won!");
            }
            else if (winningTeam == Team.Random)
            {
                PlayerCanvasController.Instance.WriteGameStatusText("Game Over!\nIt's a draw.");
            }
            else
            {
                PlayerCanvasController.Instance.WriteGameStatusText("Game Over!\nYour team lost.");
            }

            // Initialise question controller
            QuestionController.Instance.InitialiseQuestions(this);
        }
    }
}
