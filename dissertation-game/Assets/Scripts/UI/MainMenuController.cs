using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{

	public GameObject MainMenuPanel;
	public GameObject JoinGamePanel;
	public GameObject HostGamePanel;
	public GameObject OptionsPanel;

	public void LoadSceneByIndex(int index)
	{
		SceneManager.LoadScene(index);
	}

	public void SwitchToMainMenuPanel()
	{
		SetAllPanelsInactive();
		MainMenuPanel.SetActive(true);
	}

	public void SwitchToJoinGamePanel()
	{
		SetAllPanelsInactive();
		JoinGamePanel.SetActive(true);
	}

	public void SwitchToHostGamePanel()
	{
		SetAllPanelsInactive();
		HostGamePanel.SetActive(true);
	}

	public void SwitchToOptionsPanel()
	{
		SetAllPanelsInactive();
		OptionsPanel.SetActive(true);
	}

	public void Quit()
	{
		Application.Quit();
	}

	private void SetAllPanelsInactive()
	{
		MainMenuPanel.SetActive(false);
		JoinGamePanel.SetActive(false);
		HostGamePanel.SetActive(false);
		OptionsPanel.SetActive(false);
	}
}
