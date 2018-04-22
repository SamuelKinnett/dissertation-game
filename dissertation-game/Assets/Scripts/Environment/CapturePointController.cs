using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

    [SyncVar(hook = "OnCurrentControllingTeamChanged")]
    private Team currentControllingTeam;

    [SyncVar(hook = "OnRedTeamCapturePercentageChanged")]
    private float redTeamCapturePercentage;

    [SyncVar(hook = "OnBlueTeamCapturePercentageChanged")]
    private float blueTeamCapturePercentage;

    private List<Player> playersInCaptureZone;

    private bool meshFlipped;

    public void UpdateCapturePoint(Vector3 newPosition, Vector3 newDimensions)
    {
        this.transform.position = newPosition;
        this.transform.localScale = newDimensions;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (isServer)
        {
            var player = other.GetComponent<Player>();

            if (player != null && !playersInCaptureZone.Contains(player))
            {
                playersInCaptureZone.Add(player);
            }
        }
        else
        {
            var player = other.GetComponent<Player>();

            if (player.isLocalPlayer && !meshFlipped)
            {
                FlipMesh();
            }
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (isServer)
        {
            var player = other.GetComponent<Player>();

            if (player != null && playersInCaptureZone.Contains(player))
            {
                playersInCaptureZone.Remove(player);
            }
        }
        else
        {
            var player = other.GetComponent<Player>();

            if (player.isLocalPlayer && meshFlipped)
            {
                FlipMesh();
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
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isServer && !GameTimeManager.Instance.GameTimerPaused)
        {
            int redTeamCount = playersInCaptureZone.Where(p => p.PlayerTeam == Team.Red && p.IsAlive).Count();
            int blueTeamCount = playersInCaptureZone.Where(p => p.PlayerTeam == Team.Blue && p.IsAlive).Count();

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
                        DatabaseManager.Instance.AddNewCapture(null);
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
                            DatabaseManager.Instance.AddNewCapture(GameInstanceData.Instance.RedTeamId);
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
                        DatabaseManager.Instance.AddNewCapture(null);
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
                            DatabaseManager.Instance.AddNewCapture(GameInstanceData.Instance.BlueTeamId);
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

    private void FlipMesh()
    {
        var meshFilter = GetComponent<MeshFilter>();

        var triangles = meshFilter.mesh.triangles;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            var tempIndex = triangles[i];
            triangles[i] = triangles[i + 2];
            triangles[i + 2] = tempIndex;
        }

        meshFilter.mesh.SetTriangles(triangles, 0);

        meshFlipped = !meshFlipped;
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
