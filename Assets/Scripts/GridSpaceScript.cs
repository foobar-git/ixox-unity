using UnityEngine;
using UnityEngine.UI;											// include UI related namespace
using UnityEngine.Networking;

public class GridSpaceScript : NetworkBehaviour {

	public Button button;										// defining the button, set reference in Inspector
	public Text buttonText;										// the text to display inside the button, refernce to text gameObject
	private GameMasterScript referenceGameMaster;				// define variable to hold a reference to the script GameMasterScript

	////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////

	void Update () {
		
	}

	public void SetReferenceToGameMaster (GameMasterScript reference) {
		referenceGameMaster = reference;						// set reference to script GameMasterScript
	}

	// this function is called when clicking on the button (defined in the Inspector)
	public void SetButton () {
		buttonText.text = referenceGameMaster.CheckPlayer ();	// ask GameMasterScript to check which player's turn it is
		referenceGameMaster.EndTurn ();							// inform GameMasterScript to end the turn
		button.interactable = false;							// set button to inactive state (after it has been clicked)

		SendTurn ();
	}

	////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////

	void SendTurn () {
		referenceGameMaster.SendCustomMessage ();
	}
}