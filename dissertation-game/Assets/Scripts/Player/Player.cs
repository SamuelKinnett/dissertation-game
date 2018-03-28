using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

using Assets.Scripts.Player.Enums;

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

    public MapController mapController;
    public GameObject playerCapsule;

    [SerializeField] ToggleEvent onToggleShared;
    [SerializeField] ToggleEvent onToggleLocal;
    [SerializeField] ToggleEvent onToggleRemote;
    [SerializeField] float respawnTime = 5.0f;
    [SerializeField] Text playerNameText;

    private static List<Player> players = new List<Player>();

    private GameObject mainCamera;
    private bool initialised;

    /// <summary>
    /// Announce the winner and return to lobby
    /// </summary>
    [Server]
    public void Won()
    {
        foreach (var currentPlayer in players)
        {
            currentPlayer.RpcGameOver(netId, PlayerName);
        }

        Invoke("BackToLobby", 5f);
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
    }

    private void OnTeamChanged(Team newValue)
    {
        var newColour = Color.grey;

        switch (newValue)
        {
            case Team.Red:
                newColour = Color.red;
                break;

            case Team.Blue:
                newColour = Color.blue;
                break;
        }

        playerCapsule.GetComponent<Renderer>().material.color = newColour;
    }

    private void OnIsCapturingChanged(bool newValue)
    {
    }

    private void BackToLobby()
    {
        FindObjectOfType<NetworkLobbyManager>().ServerReturnToLobby();
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
    void RpcGameOver(NetworkInstanceId networkId, string name)
    {
        DisablePlayer();

        // Unlock and show the cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (isLocalPlayer)
        {
            if (netId == networkId)
            {
                PlayerCanvasController.Instance.WriteGameStatusText("You Won!");
            }
            else
            {
                PlayerCanvasController.Instance.WriteGameStatusText("Game Over!\n" + name + " Won.");
            }
        }
    }

    [ClientRpc]
    private void RpcWarpToPosition(Vector3 newPosition)
    {
        transform.position = newPosition;
    }
}
