using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Prototype.NetworkLobby;

public class GameLobbyHook : LobbyHook
{
	public override void OnLobbyServerSceneLoadedForPlayer(
		NetworkManager manager, 
		GameObject lobbyPlayerObject, 
		GameObject gamePlayerObject)
	{
		var lobbyPlayer = lobbyPlayerObject.GetComponent<LobbyPlayer>();
		var gamePlayer = gamePlayerObject.GetComponent<Player>();

		// Assign the player's lobby name and colour to their game object
		gamePlayer.PlayerName = lobbyPlayer.PlayerName;
		gamePlayer.PlayerTeam = lobbyPlayer.PlayerTeam;
	}
}
