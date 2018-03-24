using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Assets.Scripts.Player.Enums;

public class CapturePointController : NetworkBehaviour
{
    // How much capture percentage is reduced per person per second
    public float CapturePercentagePerSecond;

    // How much capture percentage is restored per second
    public float CapturePercentageRecoveryPerSecond;

    // The maximum bonus gained from having more than one person capturing
    public float MaxBonus;

    public BoxCollider collider;

    [SyncVar(hook = "OnCurrentControllingTeamChanged")]
    private Team currentControllingTeam;

    [SyncVar(hook = "OnRedTeamCapturePercentageChanged")]
    private float redTeamCapturePercentage;

    [SyncVar(hook = "OnBlueTeamCapturePercentageChanged")]
    private float blueTeamCapturePercentage;

    private int redTeamCount;
    private int blueTeamCount;
    private List<Player> playersInCaptureZone;

    public void UpdateCapturePoint(Vector3 newPosition, Vector3 newDimensions)
    {
        this.transform.position = newPosition;
        this.transform.localScale = newDimensions;
    }

    [Server]
    public void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<Player>();

        if (player != null && !playersInCaptureZone.Contains(player))
        {
            playersInCaptureZone.Add(player);

            switch (player.PlayerTeam)
            {
                case Team.Red:
                    ++redTeamCount;
                    break;

                case Team.Blue:
                    ++blueTeamCount;
                    break;
            }
        }
    }

    [Server]
    public void OnTriggerExit(Collider other)
    {
        var player = other.GetComponent<Player>();

        if (player != null && playersInCaptureZone.Contains(player))
        {
            playersInCaptureZone.Remove(player);

            switch (player.PlayerTeam)
            {
                case Team.Red:
                    --redTeamCount;
                    break;

                case Team.Blue:
                    --blueTeamCount;
                    break;
            }
        }
    }

    // Use this for initialization
    void Start()
    {
        if (isServer)
        {
            currentControllingTeam = Team.Random;
            playersInCaptureZone = new List<Player>();
            collider.enabled = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isServer)
        {
            // Capturing can only take place if the capture point is uncontested
            if (redTeamCount > 0 && blueTeamCount == 0)
            {
                var captureChangeAmount = Mathf.Clamp(redTeamCount, 0, MaxBonus) * CapturePercentagePerSecond * Time.deltaTime;
                if (blueTeamCapturePercentage > 0)
                {
                    if (blueTeamCapturePercentage - captureChangeAmount > 0)
                    {
                        blueTeamCapturePercentage -= captureChangeAmount;
                    }
                    else
                    {
                        var remainder = captureChangeAmount - blueTeamCapturePercentage;
                        blueTeamCapturePercentage = 0;
                        redTeamCapturePercentage += remainder;

                        // The capture point is now unowned
                        currentControllingTeam = Team.Random;
                        GameTimeManager.Instance.SetBlueTeamTimerPaused(true);
                    }
                }
                else
                {
                    if (redTeamCapturePercentage < 1.0f)
                    {
                        redTeamCapturePercentage = Mathf.Clamp(redTeamCapturePercentage + captureChangeAmount, 0, 1);
                        if (redTeamCapturePercentage == 1.0f)
                        {
                            // The capture point is now owned by the red team
                            currentControllingTeam = Team.Red;
                            GameTimeManager.Instance.SetRedTeamTimerPaused(false);
                        }
                    }
                }
            }
            else if (blueTeamCount > 0 && redTeamCount == 0)
            {
                var captureChangeAmount = Mathf.Clamp(blueTeamCount, 0, MaxBonus) * CapturePercentagePerSecond * Time.deltaTime;
                if (redTeamCapturePercentage > 0)
                {
                    if (redTeamCapturePercentage - captureChangeAmount > 0)
                    {
                        redTeamCapturePercentage -= captureChangeAmount;
                    }
                    else
                    {
                        var remainder = captureChangeAmount - redTeamCapturePercentage;
                        redTeamCapturePercentage = 0;
                        blueTeamCapturePercentage += remainder;

                        // The capture point is now unowned
                        currentControllingTeam = Team.Random;
                        GameTimeManager.Instance.SetRedTeamTimerPaused(true);
                    }
                }
                else
                {
                    if (blueTeamCapturePercentage < 1.0f)
                    {
                        blueTeamCapturePercentage = Mathf.Clamp(blueTeamCapturePercentage + captureChangeAmount, 0, 1);
                        if (blueTeamCapturePercentage == 1)
                        {
                            // The capture point is now owned by the blue team
                            currentControllingTeam = Team.Blue;
                            GameTimeManager.Instance.SetBlueTeamTimerPaused(false);
                        }
                    }
                }
            }
            else
            {
                // If the capture point is owned and no-one is capturing it, slowly restore it to 100% ownership
                if (currentControllingTeam == Team.Red)
                {
                    var percentageChange = CapturePercentageRecoveryPerSecond * Time.deltaTime;

                    if (redTeamCapturePercentage < 1.0f)
                    {
                        redTeamCapturePercentage = Mathf.Clamp(redTeamCapturePercentage + percentageChange, 0, 1);
                    }
                }
                else if (currentControllingTeam == Team.Blue)
                {
                    var percentageChange = CapturePercentageRecoveryPerSecond * Time.deltaTime;
                    if (blueTeamCapturePercentage < 1.0f)
                    {
                        blueTeamCapturePercentage = Mathf.Clamp(blueTeamCapturePercentage + percentageChange, 0, 1);
                    }
                }
                else
                {
                    var percentageChange = CapturePercentageRecoveryPerSecond * Time.deltaTime;
                    if (redTeamCapturePercentage > 0)
                    {
                        redTeamCapturePercentage = Mathf.Clamp(redTeamCapturePercentage - percentageChange, 0, 1);
                    }
                    else if (blueTeamCapturePercentage > 0)
                    {
                        blueTeamCapturePercentage = Mathf.Clamp(blueTeamCapturePercentage - percentageChange, 0, 1);
                    }
                }
            }
        }
    }

    private void OnCurrentControllingTeamChanged(Team newValue)
    {
        var newColour = new Color(0.75f, 0.75f, 0.75f, 0.2f);
        switch (newValue)
        {
            case Team.Red:
                newColour = new Color(1, 0, 0, 0.2f);
                break;

            case Team.Blue:
                newColour = new Color(0, 0, 1, 0.2f);
                break;
        }

        GetComponent<Renderer>().material.color = newColour;
    }

    private void OnRedTeamCapturePercentageChanged(float newValue)
    {
        PlayerCanvasController.Instance.SetRedTeamPercentage(newValue);
    }

    private void OnBlueTeamCapturePercentageChanged(float newValue)
    {
        PlayerCanvasController.Instance.SetBlueTeamPercentage(newValue);
    }
}
