using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Server : MonoBehaviour
{
    public static Server server;

    // Max number of host connections
    private const int MAX_CONNECTION = 100;
    private const int MAX_PLAYERS = 6;

    private int port = 5701;
    [SerializeField]
    public int broadcastPort = 47777;

    [SerializeField]
    public int broadcastKey = 1000;

    [SerializeField]
    public int broadcastVersion = 1;

    [SerializeField]
    public int broadcastSubVersion = 1;
    private int hostId;
    private int webHostId;

    // Used for connecting for important data (Paypal, payments...)
    private int reliableChannel;
    // Used for less important data that can tolerate missing packets/corruption
    private int unreliableChannel;

    private bool isBroadcasting = false;
    private bool isListening = false;
    private bool isStarted = false;
    private bool gameStarted = false;
    private bool doneSetup = false;

    private byte error;

    private Dictionary<int, Player> players = new Dictionary<int, Player>();
    //private List<Player> playerOrder = new List<Player>();

    private float lastMovementUpdate;
    private float movementUpdateRate = 0.05f;

    private byte[] msgOutBuffer = new byte[1024];
    private string awaitingResponseFor;

    public GameObject playerPrefab;
    GameObject //startBroadcastButton,
               //stopBroadcastButton,
               leaveLobbyBtn,
               startGameBtn,
               settingsBtn,
               playersList,
               lobbyCanvas;
    //multiplayersCanvas;
    private Player myPlayer;

    private void Start()
    {
        if (!server)
        {
            server = this;
            DontDestroyOnLoad(server);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadSceneObjects();
        NetworkTransport.Init();
        //multiplayersCanvas = GameObject.Find("MultiplayersCanvas") as GameObject;
        //multiplayersCanvas.SetActive(false);

        ConnectionConfig cc = new ConnectionConfig();

        reliableChannel = cc.AddChannel(QosType.Reliable);
        unreliableChannel = cc.AddChannel(QosType.Unreliable);

        HostTopology topo = new HostTopology(cc, MAX_CONNECTION);

        hostId = NetworkTransport.AddHost(topo, 0);
        StartBroadcast();
        //webHostId = NetworkTransport.AddWebsocketHost(topo, port);

        /*ConnectionConfig cc = new ConnectionConfig();

        reliableChannel = cc.AddChannel(QosType.Reliable);
        unreliableChannel = cc.AddChannel(QosType.Unreliable);

        HostTopology topo = new HostTopology(cc, MAX_CONNECTION);

        hostId = NetworkTransport.AddHost(topo, port);
        webHostId = NetworkTransport.AddWebsocketHost(topo, port);

        byte error;
        msgOutBuffer = Encoding.Unicode.GetBytes("SERVER|");
        Debug.Log("braodcast");
        if (!NetworkTransport.StartBroadcastDiscovery(hostId, broadcastPort, broadcastKey, broadcastVersion, broadcastSubVersion, msgOutBuffer, msgOutBuffer.Length, 1000, out error))
        {
            Debug.LogError("NetworkDiscovery StartBroadcast failed err: " + error);
            return;
        }*/

        isStarted = true;
    }


    private void Update()
    {
        //Debug.Log("Time" + Time.time);
        //Debug.Log("DeltaTime" + Time.deltaTime);
        if (!isStarted || !isListening)
        {
            return;
        }
        //if (!gameStarted)
        //{
        //    return;
        //}

        int recHostId;
        int connectionId;
        int channelId;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error;
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
        //Debug.Log(recHostId + " " + connectionId + " " + channelId + " " + recBuffer + " " + bufferSize + " " + dataSize + " " + error);
        if (recData != NetworkEventType.Nothing)
            Debug.Log(recData);
        switch (recData)
        {
            case NetworkEventType.ConnectEvent:    //2
                Debug.Log("Player " + connectionId + " has connected");
                OnConnection(connectionId);
                break;
            case NetworkEventType.DataEvent:       //3
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                Debug.Log("Player " + connectionId + " has sent: " + msg);
                string[] splitData = msg.Split('|');

                switch (splitData[0])
                {
                    case "DONESETUP":
                        OnDoneSetup(connectionId);
                        break;
                    case "MYPOSITION":
                        OnMyPosition(connectionId, float.Parse(splitData[1]), float.Parse(splitData[2]));
                        break;
                    case "NAMEIS":
                        OnNameIs(connectionId, splitData[1]);
                        break;
                    case "NEEDDECK":
                        OnNeedDeck(connectionId);
                        break;
                    case "NEEDDECKCOUNT":
                        Send("DECKCOUNT|" + GameManager.gm.deck.Count(), reliableChannel, connectionId);
                        break;
                    case "NEEDHAND":
                        OnNeedHand(connectionId, splitData);
                        break;
                    case "NEEDPLAYERORDER":
                        OnNeedPlayerOrder(connectionId);
                        break;
                    case "NEEDTRUMP":
                        Send("TRUMP|" + GameManager.gm.trump.ToString(), reliableChannel, connectionId);
                        break;
                    case "PLAYERATTACK":
                        OnReceivePlayerAttack(connectionId, splitData);
                        Send(msg, reliableChannel, players);
                        break;
                    case "PLAYERENDBATTLE":
                //        Debug.Log("RECEVED ENBGBATTLE");
                        GameManager.gm.EndBattlePhase();
                        Send(msg, reliableChannel, players);
                        break;
                    case "PLAYERDEFEND":
                        OnReceivePlayerDefend(connectionId, splitData);
                        Send(msg, reliableChannel, players);
                        break;
                    case "PLAYERTRANSFER":
                        OnPlayerTransfer(connectionId, splitData);
                        Send(msg, reliableChannel, players);
                        break;
                    case "STARTGAME":
                        OnReceivedStartGame(connectionId);
                        break;
                    default:
                        Debug.Log("Invalid Message: " + msg);
                        break;
                }
                break;
            case NetworkEventType.DisconnectEvent: //4
                Debug.Log("Player " + connectionId + " has disconnected");
                if (gameStarted)
                {
                    if (GameManager.gm.durak != -1)
                        return;
                    LeaveGame();
                }
                else
                {
                    OnDisconnection(connectionId);
                }
                break;
        }
        /*
        // Ask player for their position
        if (Time.time - lastMovementUpdate > movementUpdateRate && clients.Count > 0)
        {
            lastMovementUpdate = Time.time;
            string m = "ASKPOSITION|";
            foreach (Player sc in clients)
            {
                m += sc.connectionId.ToString() + '%' + sc.position.x.ToString() + '%' + sc.position.y.ToString() + '|';
            }
            m = m.Trim('|');
            Send(m, unreliableChannel, clients);
        }*/

    }
    private void OnPlayerTransfer(int cnnId, string[] splitData)
    {
       // Debug.Log("XFERER");
        int player = int.Parse(splitData[1]);
        List<Card> toTransfer = new List<Card>();
        for (int i = 2; i < splitData.Length; i++)
        {
            Card c = Card.ToCard(splitData[i]);
            toTransfer.Add(c);
        }
        GameManager.gm.Transfer(cnnId, toTransfer);
    }

    private void OnReceivePlayerDefend(int cnnId, string[] data)
    {
      //  Debug.Log("player " + GameManager.gm.playerOrder[cnnId] + " defends" + data[1]);
        for (int i = 2; i < data.Length; i++)
        {
            string[] splitData = data[i].Split(' ');
            Card atk = Card.ToCard(splitData[0]),
                 def = Card.ToCard(splitData[1]);

            if (!GameManager.gm.Defend(cnnId, def, atk))
            {
                Debug.Log("FAILED DEF");
                return;
            }
        }
    }

    private void OnReceivePlayerAttack(int cnnId, string[] splitData)
    {
       // Debug.Log("player " + GameManager.gm.playerOrder[cnnId] + " attacks" + splitData[1]);
       // Debug.Log("recve player attk");
        if (GameManager.gm.phase == 2)
            return;
        int playerTurn = int.Parse(splitData[1]);
        List<Card> attackCards = new List<Card>();
        for (int i = 2; i < splitData.Length; i++)
        {
            attackCards.Add(Card.ToCard(splitData[i]));
        }
        if (GameManager.gm.Attack(playerTurn, attackCards))
        {
            Debug.Log("player success atk");
        }
        else
        {
            Debug.Log("Failed attak");
        }
    }

    public void CommitTransfer()
    {
       // Debug.Log("XFERING");
        string msg = "PLAYERTRANSFER|" + GameManager.gm.myTurn + "|";
        foreach (Card c in GameManager.gm.selected)
        {
            msg += c.ToString() + "|";
        }
        msg = msg.Trim('|');
        Send(msg, reliableChannel, players);
    }

    public void CommitEndBattle()
    {
     //   Debug.Log("ENDING BATTLE");
        string msg = "PLAYERENDBATTLE|" + GameManager.gm.myTurn + "|";
        Send(msg, reliableChannel, players);
    }

    public void CommitDefend()
    {
        string msg = "PLAYERDEFEND|" + GameManager.gm.myTurn + "|";
        foreach (FieldPair fp in GameManager.gm.defendCards)
        {
            msg += fp.attack.ToString() + " " + fp.defend.ToString() + "|";
        }
        msg = msg.Trim('|');
        Send(msg, reliableChannel, players);
        //Send(msg, reliableChannel, players);
    }

    public void CommitAttack()
    {
        string msg = "PLAYERATTACK|" + GameManager.gm.myTurn + "|";
        foreach (Card c in GameManager.gm.selected)
        {
            msg += c.ToString() + "|";
        }
        msg = msg.Trim('|');
        Send(msg, reliableChannel, players);
    }

    private bool ConfirmAllAcks()
    {

        foreach (KeyValuePair<int, Player> kvp in players)
        {
           // Debug.Log("PLayer" + kvp.Key + " is rdy " + kvp.Value.ack);
            if (!kvp.Value.ack)
                return false;
        }

        foreach (KeyValuePair<int, Player> kvp in players)
        {
            if (kvp.Key != 0)
                kvp.Value.ack = false;
        }
        return true;
    }

    private void SetupGame()
    {
        GameManager.gm.SetupUI();

       // Debug.Log("DONE START TURNS");
        awaitingResponseFor = "PLAYERSTARTTURN";
        GameManager.gm.StartTurn(GameManager.gm.originalTurn);
        Send(awaitingResponseFor + "|" + GameManager.gm.originalTurn, reliableChannel, players);
    }

    private void OnDoneSetup(int cnnId)
    {
       // Debug.Log("RECEIVED DONE SETUP BY" + cnnId);
        if (!gameStarted || doneSetup || !awaitingResponseFor.Equals("DONESETUP"))
            return;
        players[cnnId].ack = true;
        if (!ConfirmAllAcks())
            return;
        doneSetup = true;

        SetupGame();
    }

    private void OnNeedDeck(int cnnId)
    {
        if (!gameStarted || doneSetup || !awaitingResponseFor.Equals("DONESETUP"))
            return;
        Send("DECK|" + GameManager.gm.deck.ToString(), reliableChannel, players);
    }

    private void OnNeedPlayerOrder(int cnnId)
    {
        if (!gameStarted || doneSetup || !awaitingResponseFor.Equals("DONESETUP"))
            return;
        string m = "PLAYERORDER|";
        foreach (Player p in GameManager.gm.playerOrder)
        {
            m += p.connectionId + "|";
        }
        m = m.Trim('|');
        Send(m, reliableChannel, cnnId);
    }

    private void OnNeedHand(int cnnId, string[] cnnIds)
    {
        if (!gameStarted || doneSetup || !awaitingResponseFor.Equals("DONESETUP"))
            return;
        string playerHands = "PLAYERHAND|";
        for (int i = 1; i < cnnIds.Length; i++)
        {
            int playerCnnId = int.Parse(cnnIds[i]);
            if (playerCnnId == cnnId)
            {
                string msg = "HAND|";
                for (int j = 0; j < players[cnnId].hand.Count; j++)
                {
                    msg += players[cnnId].hand[j].ToString() + "|";//.cardSuit + "," + p.hand[j].cardValue + "|";
                }
                msg = msg.Trim('|');
                Send(msg, reliableChannel, cnnId);
            }
            else
            {
                playerHands += playerCnnId + "," + players[playerCnnId].hand.Count + "|";
            }
        }
        playerHands = playerHands.Trim('|');
        if (!playerHands.Equals("PLAYERHAND"))
            Send(playerHands, reliableChannel, cnnId);
    }

    private void OnLevelWasLoaded(int level)
    {
        if (this != server)
            return;
        //Debug.Log("Here" + level);
        switch (level)
        {
            // Main
            case 0:
                Destroy(gameObject);
                //destroyed = true;
                break;
            // Multiplayer
            case 1:
                //Destroy(gameObject);
                //destroyed = true;
                break;
            // Server
            case 2:
                break;
            // Client
            case 3:
                break;
            // Game (Multiplayer)
            case 4:
                //LoadObjects();
                break;
        }
    }

    void OnReceivedStartGame(int cnnId)
    {
        if (!gameStarted || !awaitingResponseFor.Equals("STARTGAME") || !players.ContainsKey(cnnId))
            return;
       // Debug.Log("Received from " + cnnId);
        players[cnnId].ack = true;

        if (!ConfirmAllAcks())
            return;

        GameManager.gm.StartGame(players.Count);
        int i = 0;
        string m = "PLAYERORDER|";
        Send("DECK|" + GameManager.gm.deck.ToString(), reliableChannel, players);
        foreach (KeyValuePair<int, Player> kvp in players)
        {
            Player p = kvp.Value;
            p.hand = GameManager.gm.deck.Draw(GameManager.gm.initialDrawAmount);
            GameManager.gm.playerOrder.Add(p);
            m += p.connectionId + "|";
            //p.hand = GameManager.gm.playerHands[i];
            if (p.connectionId > 0)
            {
                Send("TRUMP|" + GameManager.gm.trump.ToString(), reliableChannel, p.connectionId);
                //Send("DECKCOUNT|" + GameManager.gm.deck.Count(), reliableChannel, p.connectionId);
            }
            i++;
        }
        m = m.Trim('|');
        Send(m, reliableChannel, players);

        /*
        foreach (KeyValuePair<int, Player> kvp in players)
        {
            Player p = kvp.Value;
            string msg = "PLAYERHAND|";
            if (p.connectionId > 0)
            {
                foreach (KeyValuePair<int, Player> kvp2 in players)
                {
                    if (kvp2.Key != kvp.Key)
                    {
                        Player p2 = kvp2.Value;
                        msg += p2.connectionId + "," + p2.hand.Count + "|";
                    }
                }
                msg = msg.Trim('|');
                Send(msg, reliableChannel, p.connectionId);
            }
        }*/



        awaitingResponseFor = "DONESETUP";
        //GameManager.gm.playerOrder = playerOrder;
        Send("DONESETUP|", reliableChannel, players);
        /*
        for (int i = 0; i < players.Count; i++)
        {
            players[i].hand = GameManager.gm.playerHands[i];
            if (i > 0)
            {
                string msg = awaitingResponseFor + "|";
                for(int j = 0; j < players[i].hand.Count; j++)
                {
                    msg += players[i].hand[j].cardSuit + "," + players[i].hand[j].cardValue + "|";
                }
                msg = msg.Trim('|');
                Send(msg, reliableChannel, players[i].connectionId);
                Send("TRUMP|" + GameManager.gm.trump.cardSuit + "," + GameManager.gm.trump.cardValue, reliableChannel, players[i].connectionId);
                msg = "PLAYERHAND|";

                Send("PLAYERHAND|" + GameManager.gm.trump.cardSuit + "," + GameManager.gm.trump.cardValue, reliableChannel, players[i].connectionId);
            }
        }*/
    }

    public void LoadSceneObjects()
    {
        myPlayer = new Player();
        myPlayer.playerName = GameManager.gm.playerName;
        myPlayer.playerGO = Instantiate(playerPrefab) as GameObject;
        myPlayer.connectionId = 0;
        lobbyCanvas = GameObject.Find("LobbyCanvas");
        lobbyCanvas.transform.Find("LeaveLobbyBtn").GetComponent<Button>().onClick.AddListener(server.LeaveLobby);
        lobbyCanvas.transform.Find("LeaveLobbyBtn").GetComponent<Button>().onClick.AddListener(GameManager.gm.GoToMultiplayerScene);
        lobbyCanvas.transform.Find("StartBtn").GetComponent<Button>().onClick.AddListener(server.StartGame);
        lobbyCanvas.transform.Find("StartBtn").GetComponent<Button>().onClick.AddListener(GameManager.gm.GoToGameScene);
        //startBroadcastButton = GameObject.Find("LobbyCanvas").transform.Find("StartBroadcastBtn").gameObject;
        //stopBroadcastButton = GameObject.Find("LobbyCanvas").transform.Find("StopBroadcastBtn").gameObject;
        //stopBroadcastButton.SetActive(false);
        playersList = lobbyCanvas.transform.Find("PlayersList").gameObject;
        myPlayer.playerGO.transform.SetParent(playersList.transform);
        myPlayer.playerGO.transform.Find("NameText").GetComponent<Text>().text = myPlayer.playerName;
        myPlayer.playerGO.transform.localScale = new Vector3(1, 1, 1);
        players.Add(0, myPlayer);
    }

    public void StartGame()
    {
        //Debug.Log("START GAME");
        StopBroadcast();
        Send("STARTGAME|", reliableChannel, players);
        gameStarted = true;
        awaitingResponseFor = "STARTGAME";
        players[0].ack = true;
    }

    public void LeavingNetworkActivities()
    {
        CloseServer();
    }

    public void CloseServer()
    {
        if (isBroadcasting)
        {
            StopBroadcast();
        }
        ClearGameObjects();
        //NetworkTransport.RemoveHost(hostId);
        Destroy(gameObject);
    }

    public void LeaveGame()
    {
       // Debug.Log("LEAVE LOBBY");
        // Tell everybody to leave lobby
        Send("LEAVEGAME|", reliableChannel, players);
        CloseServer();
        GameManager.gm.GoToMainScene();
    }

    public void LeaveLobby()
    {
       // Debug.Log("LEAVE LOBBY");
        // Tell everybody to leave lobby
        Send("LEAVELOBBY|", reliableChannel, players);
        CloseServer();
    }

    public void CreateLobby()
    {
        StartBroadcast();
        lobbyCanvas.SetActive(true);
        //multiplayersCanvas.SetActive(false);
    }

    private void ClearGameObjects()
    {
        //players.Clear();

        foreach (KeyValuePair<int, Player> kvp in players)
        {
            //Debug.Log("removing" + kvp.Key + " " + kvp.Value.connectionId);
            Player p = kvp.Value;
            //Debug.Log(1);
            Destroy(p.playerGO);
            //Debug.Log(2);
            //players.Remove(kvp.Key);
            //Debug.Log(3);
        }
        players.Clear();
        GameManager.gm.ClearGame();
        /*
        while (players.Count > 1)
        {
            Destroy(players[1].playerGO);
            players.Remove(players[1]);
        }*/
    }

    public void StartBroadcast()
    {
        byte error;
        msgOutBuffer = Encoding.Unicode.GetBytes("SERVER|" + GameManager.gm.playerName + "|");
        //Debug.Log("braodcast");
        if (!NetworkTransport.StartBroadcastDiscovery(hostId, broadcastPort, broadcastKey, broadcastVersion, broadcastSubVersion, msgOutBuffer, msgOutBuffer.Length, 1000, out error))
        {
            Debug.LogError("NetworkDiscovery StartBroadcast failed err: " + error);
            return;
        }
        isBroadcasting = true;
        isListening = true;
        //startBroadcastButton.SetActive(false);
        //stopBroadcastButton.SetActive(true);
    }

    public void StopBroadcast()
    {
        //NetworkTransport.RemoveHost(hostId);
        NetworkTransport.StopBroadcastDiscovery();

        //startBroadcastButton.SetActive(true);
        //stopBroadcastButton.SetActive(false);
        isBroadcasting = false;
    }


    private void OnConnection(int cnnId)
    {
        //Debug.Log("CONNECTION ESTABLISHED" + gameStarted);
        // Cannot play with more than 5 players total
        if (players.Count == MAX_PLAYERS)
        {
            Send("FULLROOM|", reliableChannel, cnnId);
            return;
        }
        // Cannot play with more than 5 players total
        if (gameStarted)
        {
            Send("FULLROOM|", reliableChannel, (int)cnnId);
            return;
        }
        // Add him to list
        Player c = new Player();
        c.connectionId = cnnId;
        c.playerName = "TEMP";
        c.playerGO = Instantiate(playerPrefab) as GameObject;
        //c.playerGO.SetActive(false);
        players.Add(cnnId, c);
        if (players.Count == MAX_PLAYERS - 1)
        {
            StopBroadcast();
            //Send("FULLROOM|", reliableChannel, cnnId);
        }
        // When player joins server, tell him his ID
        // Request his name and send name of all other players
        string msg = "ASKNAME|" + cnnId + "|";// + myPlayer.playerName + "%0|";
        foreach (KeyValuePair<int, Player> kvp in players)
        {
            if (kvp.Key != cnnId)
            {
                Player sc = kvp.Value;
                msg += sc.playerName + "%" + sc.connectionId + "|";
            }
        }
        msg = msg.Trim('|');

        // ASKNAME|3|DAVE%1|Micheal%2|TEMP%3
        Send(msg, reliableChannel, cnnId);
    }

    private void OnNameIs(int cnnId, string playerName)
    {
        if (players.Count == 1 && !players.ContainsKey(cnnId))
            return;
        Player sc = players[cnnId];
        //Player sc = players.Find(x => x.connectionId == cnnId);
        sc.playerName = playerName;
        sc.playerGO.transform.Find("NameText").GetComponent<Text>().text = playerName;
        sc.playerGO.transform.SetParent(playersList.transform);
        sc.playerGO.transform.localScale = new Vector3(1, 1, 1);
        lobbyCanvas.transform.Find("StartBtn").GetComponent<Button>().interactable = true;
        //sc.playerGO.SetActive(true);

        // Tell everybody that new player has connected
        Send("CNN|" + playerName + '|' + cnnId, reliableChannel, players);
    }

    private void OnDisconnection(int cnnId)
    {
        if (players.Count == 1 || !players.ContainsKey(cnnId))
            return;
        Destroy(players[cnnId].playerGO);
        // Remove this player from our client list
        players.Remove(cnnId);

        lobbyCanvas.transform.Find("StartBtn").GetComponent<Button>().interactable = players.Count > 1;
        if (!gameStarted && !isBroadcasting)
        {
            StartBroadcast();
        }
        // Tell everyone that someone has disconnected
        Send("DC|" + cnnId, reliableChannel, players);
    }


    private void OnMyPosition(int cnnId, float x, float y)
    {
        if (players.Count == 1)
            return;
        //clients.Find(c => c.connectionId == cnnId).position = new Vector3(x, y, 0);
    }

    private void Send(string message, int channelId, int cnnId)
    {
        Debug.Log("send" + message);
        if (!players.ContainsKey(cnnId))
            return;
        NetworkTransport.Send(hostId, cnnId, channelId, Encoding.Unicode.GetBytes(message), message.Length * sizeof(char), out error);
    }

    private void Send(string message, int channelId, Dictionary<int, Player> c)
    {

        Debug.Log("Sending : " + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        foreach (KeyValuePair<int, Player> kvp in players)
        {
            Player p = kvp.Value;
            if (p != null && p != myPlayer)
                NetworkTransport.Send(hostId, p.connectionId, channelId, msg, message.Length * sizeof(char), out error);
            //return;
        }
    }

}
