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
		LobbyPlayer lobbyPlayer = lobbyPlayerObject.GetComponent<LobbyPlayer>();
		Player gamePlayer = gamePlayerObject.GetComponent<Player>();

		gamePlayer.PlayerName = lobbyPlayer.playerName;
		gamePlayer.PlayerColour = lobbyPlayer.playerColor;
	}
}
