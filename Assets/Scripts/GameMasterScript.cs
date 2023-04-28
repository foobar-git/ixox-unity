using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
////////////////////////////////////////////////////////////////////////
[System.Serializable]
public class CustomColors {
	public Color textColor;
}

[System.Serializable]
public class PlayerColor {
	public Color panelColor;						// define custom color panelColor in this class
	public Color textColor;							// define custom text color in this class
}

[System.Serializable]
public class Player {								// defining custom class Player

	public Image panel;								// define custom image (will enable button-like behaviour) for player panel
	public Text text;								// define custom text to be displayed in player panel
}

public class GameMasterScript : NetworkBehaviour {

	//public Camera MainCamera;
	public Player playerXpanel;						// define reference for playerXpanel.image and playerXpanel.text
	public Player playerOpanel;						// define reference for playerOpanel.image and playerOpanel.text
	public PlayerColor activePlayerColor;			// define reference for activePlayerColor.panelColor and activePlayerColor.textColor
	public PlayerColor inactivePlayerColor;			// define reference for inactivePlayerColor.panelColor and inactivePlayerColor.textColor
	public CustomColors winColor;					// define color for text (displayed in buttons) of the winning player 
	public CustomColors neutralColor;				// default color for clickable buttons
	public CustomColors color69;					// easter egg color
	public Text[] buttonList;						// define array "buttonList", to be able to check for win conditions
	public Text infoPanelText;						// text to display at the Game Over Panel
	public GameObject infoPanel;					// reference to Game Over Panel gameObject
	public GameObject optionsPanel;					// reference to Options Panel gameObject
	public string player;							// variable to hold info which player is playing
	private string playedFirst;						// variable to track who played first
	private bool playerSelected;					// is true if player has selected to play as X or O
	private bool firstTurn;							// variable helps with saving who played first
	private int turnCount;							// variable for counting turns; 9 is max before declaring draw
	public int playerXscore;						// score for player X
	public int playerOscore;						// score for player O
	public Text playerXscoreText;					// text for player X score
	public Text playerOscoreText;					// text for player O score
	public AudioClip selectSound;					// sound clip reference to play when selecting player
	public AudioClip winSound;						// sound clip reference to play when a win condition is met
	public AudioClip restartSound;					// sound clip reference to play when restarting game and when there is a draw
	// SyncVar hooks will link a function to the SyncVar, these functions are invoked on the server and all clients when the value of the SyncVar changes
	//public bool networkMultiplayer;
	//public GameObject buttonPrefab;
	//public Transform referenceBoard;
	//public Transform[] buttonSpawnList;
	//public Text[] buttonTextList;
	public GameObject playerPrefab;
////////////////////////////////////////////////////////////////////////
	 
	NetworkClient myClient;

	// Use to initialize variables or game state before the game starts. Awake () is called before Start ()
	void Awake () {
		
	}

	// Use this for initialization
	void Start () {
		/*if (MainCamera == null) {
			MainCamera = Camera.main;
		}*/

		//ConnectionConfig config = new ConnectionConfig ();
		//config.AddChannel (QosType.ReliableSequenced);
		//NetworkServer.Configure (config, 1000);
		NetworkServer.RegisterHandler (MyMsgType.Custom, OnCustomMessage);

		myClient = new NetworkClient ();
		myClient.RegisterHandler (MyMsgType.Custom, OnCustomMessage);
		myClient.Connect ("127.0.0.1", 7777);
	}

////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////

	void SetUpGame () {								// setup the game prior start
		optionsPanel.SetActive (false);				// hide the options panel
		infoPanel.SetActive (false);				// hide the info panel
		turnCount = 0;								// set turn to 0
		playerXscore = 0;
		playerOscore = 0;
		playerSelected = false;                     // player not yet selected to play as X or O
        player = "X";                               // if player does not select to play as X or O
        playedFirst = player;					    // ... and since default is X, also X played first
	}

	void StartGame () {
		EnableButtons (true);						// set buttons to be clickable
		SetGameMasterReferenceOnButtons ();			// Populate GameMaster's array with buttons (set references)
	}

	void EnableButtons (bool setup) {
		for (int i = 0; i < buttonList.Length; i++) {
			buttonList [i].GetComponentInParent<Button> ().interactable = setup;
		}
		////////////////////////////////////
		if (setup) {	// if setup is true (when restarting game), clear prior player markings ("X" or "O")
			for (int i = 0; i < buttonList.Length; i++) {
				buttonList [i].text = "";
			}
		}
		else return;
	}

	// check parent for each text gameObject and push reference from GridSpaceScript to GameMasterScript
	void SetGameMasterReferenceOnButtons () {
		for (int i = 0; i < buttonList.Length; i++) {
			buttonList [i].GetComponentInParent<GridSpaceScript> ().SetReferenceToGameMaster (this);
		}
	}

	public void SelectXO (string selected) {
		if (!playerSelected) {						// if player has not selected to play as X or O, accept selection
			player = selected;						// set player to selected
			playedFirst = player;					// save who played first
			HighlightActivePlayerPanel ();			// highlight selected player panel
			PlaySound ("select");					// play sound when a player has been selected
		}
		else return;								// if playerSelected true (if player has selected to play as X or O), simply exit this loop (ignore it)
	}

	// check which player is playing (called by GridSpaceScript)
	public string CheckPlayer () {
		SaveWhoPlayedFirst ();						// runs only in the first turn of every match
		return player;								// return content of "player" to GridSpaceScript.SetButton ()
	}

	void SaveWhoPlayedFirst () {
		if (firstTurn) {
			playedFirst = player;
			firstTurn = false;
		}
	}

	void HighlightActivePlayerPanel () {
		if (player == "X") SetPlayerColor (playerXpanel, playerOpanel);	// set player X panel color (as active player) and player O panel color (as inactive player)
		else SetPlayerColor (playerOpanel, playerXpanel);	// set player O color (as active player) and player X color (as inactive player)
	}

	void UpdateScore () {
		if (player == "X") {
			playerXscore++;
			playerXscoreText.text = playerXscore.ToString ();	// take int playerXscore and convert to string and set as text
		}
		if (player == "O") {
			playerOscore++;
			playerOscoreText.text = playerOscore.ToString ();	// take int playerOscore and convert to string and set as text
		}
		else return;
	}

	void Check69 () {
		if (playerXscore == 6 && playerOscore == 9) {
			SetInfoPanelText ("Uh oh 69...");
			HighlightWinningPlayerButtons (69);
		}
	}

	public void EndTurn () {
		playerSelected = true;						// at the end of the turn, a player has selected to play as X or O, and if not, will set it to true (if player has not selected X or O, default is X)
		turnCount++;								// increment turns by one

		if (turnCount >= 9) {
			PlaySound ("restart");
			SetInfoPanelText("It's a draw!");		// if there are 9 or more turns, end game with a draw
		}

        ////////////////////////////////////
        /// WINNING CONDITIONS
        CheckWinConditions();
        //if (!networkMultiplayer) CheckOneDeviceWinCon ();
        //else NetworkMultiplayerWinCon ();

    }

	void ChangePlayer() {							// after one player has clicked a button, switch to new player
		player = (player == "X") ? "O" : "X";		// "?" operator in C#
		// same as:
		// if (player == "X") player = "O";
		// else player = X;
		HighlightActivePlayerPanel ();
	}

	void GameOver (int n) {
		PlaySound ("win");
		HighlightWinningPlayerButtons (n);			// change color of text displayed in the buttons to indicate winning condition
		EnableButtons (false);						// go through all buttons, setting them to inactive state
		UpdateScore ();								// update player score
		SetInfoPanelText(player + " wins!");		// display this text at the game over panel
		Check69 ();									// easter egg
		// at this point a dialog (Info Panel) asks the player whether to play a new game and if so, calls RestartGame () by pressing button
	}

	void HighlightWinningPlayerButtons (int m) {
		////////////////////////////////////
		/// rows
		if (m == 1) {
			for (int i = 0; i <= 2; i++) {
				buttonList [i].GetComponent<Text>().color = winColor.textColor;
			}
		}
		if (m == 2) for (int i = 3; i <= 5; i++) buttonList [i].GetComponent<Text>().color = winColor.textColor;
		if (m == 3) for (int i = 6; i <= 8; i++) buttonList [i].GetComponent<Text>().color = winColor.textColor;
		////////////////////////////////////
		/// columns
		if (m == 4) for (int i = 0; i <= 6; i = i + 3) buttonList [i].GetComponent<Text>().color = winColor.textColor;
		if (m == 5) for (int i = 1; i <= 7; i = i + 3) buttonList [i].GetComponent<Text>().color = winColor.textColor;
		if (m == 6) for (int i = 2; i <= 8; i = i + 3) buttonList [i].GetComponent<Text>().color = winColor.textColor;
		////////////////////////////////////
		/// diagonals
		if (m == 7) for (int i = 0; i <= 8; i = i + 4) buttonList [i].GetComponent<Text>().color = winColor.textColor;
		if (m == 8) for (int i = 2; i <= 6; i = i + 2) buttonList [i].GetComponent<Text>().color = winColor.textColor;
		////////////////////////////////////
		/// 69
		if (m == 69) {
			for (int i = 0; i < buttonList.Length; i++) buttonList [i].GetComponent<Text>().color = color69.textColor;
		}
	}

	void SetInfoPanelText (string text) {			// display the game over panel with some text
		infoPanel.SetActive (true);					// display the game over panel
		infoPanelText.text = text;					// game over panel text
	}

	// restarting the game
	public void RestartGame () {
		PlaySound ("restart");
		infoPanel.SetActive (false);
		turnCount = 0;
		StartGame ();								// initialize functions need for gameplay
		ResetButtonTextColor ();					// after highlighting the winning button's texts (change of color), reset it to "neutral color"
		firstTurn = true;							// reset to true so who played first can be saved
		player = (playedFirst == "X") ? "O" : "X";	// if player X was playing first, make player O play first and vice versa
		HighlightActivePlayerPanel ();				// highlight selected player panel
	}

	// at first run, set both player panels to active player color
	void FirstRunHighlightPlayerPanels (Player playerX, Player playerO) {
		playerX.panel.color = activePlayerColor.panelColor;
		playerX.text.color = activePlayerColor.textColor;
		playerO.panel.color = activePlayerColor.panelColor;
		playerO.text.color = activePlayerColor.textColor;
	}

	// set active player to have active player color and inactive player to have inactive player color (colors picked in the Inspector)
	void SetPlayerColor (Player activePlayer, Player inactivePlayer) {
		activePlayer.panel.color = activePlayerColor.panelColor;
		activePlayer.text.color = activePlayerColor.textColor;
		inactivePlayer.panel.color = inactivePlayerColor.panelColor;
		inactivePlayer.text.color = inactivePlayerColor.textColor;
	}

	void ResetButtonTextColor () {
		for (int i = 0; i < buttonList.Length; i++) {
			buttonList [i].GetComponent<Text> ().color = neutralColor.textColor; // reset button "default" color
		}
	}

	public void PlaySound (string state) {
		if (state == "win")	GetComponent<AudioSource> ().clip = winSound;
		if (state == "restart")	GetComponent<AudioSource> ().clip = restartSound;
		if (state == "select")	GetComponent<AudioSource> ().clip = selectSound;
		GetComponent<AudioSource> ().Play ();
	}

////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////

	public void OneDeviceMultiplayerGame () {
		SetUpGame ();								// set up game for one device multiplayer
		StartGame ();
		// high	light both player panels as to indicate that the player can choose to play as X or O
		FirstRunHighlightPlayerPanels (playerXpanel, playerOpanel);
	}

	public void NetworkMultiplayerGame () {

		//SetUpGame ();
        //StartGame ();

        if (isServer) {
            RpcSetUpGame ();
        }
		
        // high	light both player panels as to indicate that the player can choose to play as X or O
        FirstRunHighlightPlayerPanels (playerXpanel, playerOpanel);
    }

	public override void OnStartServer () {
		Debug.Log ("Testing...");
		if (isServer) {
			Debug.Log ("This is Server.");

		}
	}

	public override void OnStartClient () {
		Debug.Log("Client connected.");
		if (!isServer) {
			Debug.Log("Client is ready: " + ClientScene.ready);
		}
    }

    [ClientRpc]
    void RpcSetUpGame () {
		Debug.Log ("RPC Call: SetUpGame + StartGame");
		SetUpGame ();
		StartGame ();
    }

	

    ////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////

	void CheckWinConditions () {
		// test if a player has met the winning conditions using "brute force"
		// if statements: check all possible win conditions, if one tests true, call game over and exit if-loop, but if none test true including draw, then change player
		////////////////////////////////////
		/// checking rows:
		if (buttonList [0].text == player && buttonList [1].text == player && buttonList[2].text == player) GameOver (1);
		else if (buttonList [3].text == player && buttonList [4].text == player && buttonList[5].text == player) GameOver (2);
		else if (buttonList [6].text == player && buttonList [7].text == player && buttonList[8].text == player) GameOver (3);
		////////////////////////////////////
		// checking columns:
		else if (buttonList [0].text == player && buttonList [3].text == player && buttonList[6].text == player) GameOver (4);
		else if (buttonList [1].text == player && buttonList [4].text == player && buttonList[7].text == player) GameOver (5);
		else if (buttonList [2].text == player && buttonList [5].text == player && buttonList[8].text == player) GameOver (6);
		////////////////////////////////////
		/// checking diagonals:
		else if (buttonList [0].text == player && buttonList [4].text == player && buttonList[8].text == player) GameOver (7);
		else if (buttonList [2].text == player && buttonList [4].text == player && buttonList[6].text == player) GameOver (8);
		////////////////////////////////////
		else ChangePlayer ();									// change player after a turn has been made
	}

	////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////

	public class MyMsgType {
		public static short Custom = MsgType.Highest + 1;
	}

	public class CustomMessage : MessageBase {
		public string text;
	}

	public void SendCustomMessage (/*string text*/) {
		CustomMessage msg = new CustomMessage ();
		//msg.text = text;
		
		if (isServer) {
			msg.text = "Server sent a message.";
			NetworkServer.SendToAll (MyMsgType.Custom, msg);
		}
		if (!isServer) {
			msg.text = "Client sent a message.";
			myClient.Send (MyMsgType.Custom, msg);
		}
	}

	public void OnCustomMessage (NetworkMessage netMsg) {
		CustomMessage msg = netMsg.ReadMessage<CustomMessage> ();
		Debug.Log ("Message: " + msg.text);
	}
}
