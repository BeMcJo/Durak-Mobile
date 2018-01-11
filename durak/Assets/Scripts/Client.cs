using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Host
{
    public string ip;
    public int port;

    public Host()
    {
        ip = "";
        port = -1;
    }

    public Host(string addr, int p)
    {
        ip = addr;
        port = p;
    }

    public bool Equals(Host h)
    {
        return h.ip.Equals(ip) && port == h.port;
    }
}

public class Client : MonoBehaviour
{
    public static Client client;
    public static bool oneStarted;
    // Max number of host connections
    private const int MAX_CONNECTION = 100;

    private int port = 5702;

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
    private int ourClientId;
    private int connectionId;
    private string serverIp;
    private int serverPort;
    private Host connectedHost;

    // Used for connecting for important data (Paypal, payments...)
    private int reliableChannel;
    // Used for less important data that can tolerate missing packets/corruption
    private int unreliableChannel;

    private float connectionTime;
    private bool isStarted = false;
    private bool inLobby = false;
    private bool inGame = false;
    private bool isConnected = false;
    private bool destroyed = false;
    private bool isOriginal = false;
    private bool initialConfirmation = true;

    private byte error;

    private bool findingHosts = false;


    int recHostId;
    int channelId;
    byte[] recBuffer = new byte[1024];
    int bufferSize = 1024;
    int dataSize;

    private byte[] buffer = new byte[1024];

    [SerializeField]
    public Dictionary<int, Player> players = new Dictionary<int, Player>();
    //public List<Player> playerOrder;

    [SerializeField]
    public Dictionary<Host, GameObject> hosts = new Dictionary<Host, GameObject>();

    public Transform hostsList;
    public GameObject playerPrefab, joinButtonPrefab;

    private string playerName;
    private int deckCount;

    public GameObject lobbyCanvas,
                      lobbySearchCanvas,
                      //multiplayersCanvas,
                      playersList;
    //inGameCanvas;

    private void Start()
    {
        if (!client)
        {
            DontDestroyOnLoad(gameObject);
            client = this;
        }
        else
        {
            Destroy(gameObject);
            destroyed = true;
            return;
        }
        LoadObjects();
        // No hostId yet
        hostId = -1;

        NetworkTransport.Init();
    }


    private void Update()
    {
        if (this != client)
            return;
        // Update the expiration time for each existing available room
        if (hosts.Count > 0)
        {
            List<Host> expiredHosts = new List<Host>();
            foreach (KeyValuePair<Host, GameObject> kvp in hosts)
            {
                kvp.Value.transform.GetComponent<ExpiringButton>().Decrement();
                //Debug.Log(kvp.Value.transform.GetComponent<ExpiringButton>().timer);
                if (kvp.Value.transform.GetComponent<ExpiringButton>().Expired())
                {
                    Destroy(kvp.Value);
                    expiredHosts.Add(kvp.Key);
                }
            }
            for (int i = 0; i < expiredHosts.Count; i++)
            {
                hosts.Remove(expiredHosts[i]);
            }
        }
        if (!isConnected)// || findingHosts)
        {
            FindHosts();
        }
        if (!isConnected)
        {
            //FindHosts();
            return;
        }
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
        //Debug.Log(recData);
        switch (recData)
        {
            case NetworkEventType.DataEvent:       //3
                //Debug.Log("data");
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                Debug.Log("Receiving : " + msg);
                string[] splitData = msg.Split('|');

                switch (splitData[0])
                {
                    case "ASKNAME":
                        OnAskName(splitData);
                        break;
                    case "ASKPOSITION":
                        OnAskPosition(splitData);
                        break;
                    case "CNN":
                        SpawnPlayer(splitData[1], int.Parse(splitData[2]));
                        break;
                    case "DC":
                        PlayerDisconnected(int.Parse(splitData[1]));
                        break;
                    case "DECK":
                        ReceiveDeck(splitData);
                        break;
                    case "DECKCOUNT":
                        ReceiveDeckCount(splitData[1]);
                        break;
                    case "DONESETUP":
                        ConfirmDoneSetup();
                        break;
                    case "FULLROOM":
                        Disconnect();
                        //lobbySearchCanvas.transform.Find("StopSearchBtn").gameObject.SetActive(true);
                        FindHosts();
                        break;
                    case "HAND":
                        ReceiveHand(splitData);
                        break;
                    case "LEAVELOBBY":
                        LeaveLobby();
                        break;
                    case "LEAVEGAME":
                        LeaveGame();
                        break;
                    case "PLAYERATTACK":
                        OnReceivePlayerAttack(splitData);
                        break;
                    case "PLAYERENDBATTLE":
                       // Debug.Log("RECEVED ENBGBATTLE");
                        GameManager.gm.EndBattlePhase();
                        break;
                    case "PLAYERDEFEND":
                        OnReceivePlayerDefend(splitData);
                        break;
                    case "PLAYERHAND":
                        ReceivePlayersHands(splitData);
                        break;
                    case "PLAYERORDER":
                       // Debug.Log("P ORDER");
                        //foreach (string s in splitData)
                        //    Debug.Log(s);
                        ReceivePlayerOrder(splitData);
                        break;
                    case "PLAYERSTARTTURN":
                        OnPlayerStartTurn(int.Parse(splitData[1]));
                        break;
                    case "PLAYERTRANSFER":
                        OnPlayerTransfer(splitData);
                        break;
                    case "STARTGAME":
                        StartGame();
                        break;
                    case "TRUMP":
                        ReceiveTrumpCard(splitData[1]);
                        break;
                    default:
                        Debug.Log("Invalid Message: " + msg);
                        break;
                }
                break;
            case NetworkEventType.BroadcastEvent:
                /*Debug.Log("DISCOVER");
                //string m = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                int rcvSize;
                NetworkTransport.GetBroadcastConnectionMessage(hostId, buffer, buffer.Length, out rcvSize, out error);
                string senderAddr;
                int senderPort;
                NetworkTransport.GetBroadcastConnectionInfo(hostId, out senderAddr, out senderPort, out error);
               
                OnReceivedBroadcast(senderAddr, senderPort, Encoding.Unicode.GetString(buffer));
                //connectionId = NetworkTransport.Connect(hostId, "", port, 0, out error);
                //Debug.Log(m);*/
                break;
            case NetworkEventType.DisconnectEvent:
                Debug.Log("SOMEONE DC");
                if (connectionId == ourClientId)
                {
                    if (GameManager.gm.durak != -1)
                        return;
                    Debug.Log("THATS OUR MAIN CON");
                    LeaveGame();
                }
                break;
        }
    }

    public void CommitTransfer()
    {
      //  Debug.Log("XFERING");
        string msg = "PLAYERTRANSFER|" + GameManager.gm.myTurn + "|";
        foreach (Card c in GameManager.gm.selected)
        {
            msg += c.ToString() + "|";
        }
        msg = msg.Trim('|');
        Send(msg, reliableChannel);
    }

    public void CommitEndBattle()
    {
      //  Debug.Log("ENDING BATTLE");
        string msg = "PLAYERENDBATTLE|" + GameManager.gm.myTurn + "|";
        Send(msg, reliableChannel);
    }

    public void CommitDefend()
    {
        string msg = "PLAYERDEFEND|" + GameManager.gm.myTurn + "|";
        foreach (FieldPair fp in GameManager.gm.defendCards)
        {
            msg += fp.attack.ToString() + " " + fp.defend.ToString() + "|";
        }
        msg = msg.Trim('|');
        Send(msg, reliableChannel);
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
        Send(msg, reliableChannel);
    }

    private void OnPlayerTransfer(string[] splitData)
    {
        //Debug.Log("XFERER");
        int player = int.Parse(splitData[1]);
        if (player == GameManager.gm.myTurn)
            return;
        List<Card> toTransfer = new List<Card>();
        for(int i = 2; i < splitData.Length; i++)
        {
            Card c = Card.ToCard(splitData[i]);
            toTransfer.Add(c);
        }
        GameManager.gm.Transfer(player, toTransfer);
    }

    private void OnReceivePlayerDefend(string[] data)
    {
        //Debug.Log("player defends" + data[1]);
        int playerTurn = int.Parse(data[1]);
        if (playerTurn == GameManager.gm.myTurn)
            return;
        for (int i = 2; i < data.Length; i++)
        {
            string[] splitData = data[i].Split(' ');
            Card atk = Card.ToCard(splitData[0]),
                 def = Card.ToCard(splitData[1]);

            if (!GameManager.gm.Defend(playerTurn, def, atk))
            {
                Debug.Log("FAILED DEF");
                return;
            }
        }
    }


    private void OnReceivePlayerAttack(string[] splitData)
    {
       // Debug.Log("recve player attk");
        int playerTurn = int.Parse(splitData[1]);
        if (GameManager.gm.phase == 2 || GameManager.gm.myTurn == playerTurn) 
            return;
        List<Card> attackCards = new List<Card>();
        for(int i = 2; i < splitData.Length; i++)
        {
            attackCards.Add(Card.ToCard(splitData[i]));
        }
        if(GameManager.gm.Attack(playerTurn, attackCards))
        {
          //  Debug.Log("player success atk");
        }
        else
        {
            Debug.Log("Failed attak");
        }
    }

    private void OnPlayerStartTurn(int turn)
    {
        Debug.Log("Player " + turn + " starting");
        GameManager.gm.StartTurn(turn);
        if (turn == ourClientId)
        {
            Debug.Log("IM STARTING TURN");
        }
        else
        {
            Debug.Log("NOT ME STARTING");
        }
    }

    private bool HavePlayerHands()
    {
        foreach (KeyValuePair<int, Player> kvp in players)
        {
            if (kvp.Value.hand == null)
            {
                return false;
            }
        }
        return true;
    }

    private void SetupGame()
    {
        GameManager.gm.SetupUI();
        Send("DONESETUP|", reliableChannel);
    }

    private void ConfirmDoneSetup()
    {
        if (initialConfirmation)
            initialConfirmation = false;
        bool doneSetup = true;
        //string msg;
        /*msg = "NEEDHAND|";
        foreach (KeyValuePair<int, Player> kvp in players)
        {
            if (kvp.Value.hand == null)
            {
                doneSetup = false;
                msg += kvp.Key + "|";
            }
        }
        msg = msg.Trim('|');
        */
        if(GameManager.gm.deck == null)
        {
            doneSetup = false;
            Send("NEEDDECK|",reliableChannel);
        }
        if (GameManager.gm.trump == null)
        {
            doneSetup = false;
            Send("NEEDTRUMP|", reliableChannel);
        }
        if (GameManager.gm.playerOrder == null)
        {
            doneSetup = false;
            Send("NEEDPLAYERORDER|", reliableChannel);
        }
        //if (!msg.Equals("NEEDHAND"))
        //    Send(msg, reliableChannel);
        //if (deckCount == -1)
        //    Send("NEEDDECKCOUNT|", reliableChannel);
        if (doneSetup)
        {
            Debug.Log("SETTING GAME");
            SetupGame();
        }
    }


    private void ReceiveDeck(string[] data)
    {
        if (GameManager.gm.deck != null)
            return;
        List<string> cards = new List<string>();
        for (int i = 1; i < data.Length; i++)
            cards.Add(data[i]);
        GameManager.gm.deck = new Deck(cards);
        if (!initialConfirmation)
            ConfirmDoneSetup();
    }

    private void ReceiveDeckCount(string count)
    {
        if (deckCount == -1)
            return;
        Debug.Log("receving deck count" + count);
        deckCount = int.Parse(count);
        if (!initialConfirmation)
            ConfirmDoneSetup();
    }

    private void ReceivePlayerOrder(string[] order)
    {
        Debug.Log("Here");
        if (GameManager.gm.playerOrder != null && GameManager.gm.playerOrder.Count > 1)
            return;
        //playerOrder = new List<Player>();
        for (int i = 1; i < order.Length; i++)
        {
            int playerTurn = int.Parse(order[i]);
            if (playerTurn == ourClientId)
                GameManager.gm.myTurn = playerTurn;
            GameManager.gm.playerOrder.Add(players[playerTurn]);
            players[playerTurn].hand = GameManager.gm.deck.Draw(GameManager.gm.initialDrawAmount);
        }
        //GameManager.gm.playerOrder = playerOrder;
        if (!initialConfirmation)
            ConfirmDoneSetup();
    }

    private void ReceiveTrumpCard(string trump)
    {
        if (GameManager.gm.trump != null)
            return;
        GameManager.gm.trump = Card.ToCard(trump);
        if (!initialConfirmation)
            ConfirmDoneSetup();
    }

    private void ReceivePlayersHands(string[] playerHands)
    {
        if (HavePlayerHands())
            return;
        Debug.Log("Recigin other palyers hand count");
        for (int i = 1; i < playerHands.Length; i++)
        {
            Debug.Log(">>>" + playerHands[i]);
            string[] splitData = playerHands[i].Split(',');
            int cnnId = int.Parse(splitData[0]), 
                handCt = int.Parse(splitData[1]);
            players[cnnId].hand = GameManager.gm.deck.Draw(handCt);
        }
        if (!initialConfirmation)
            ConfirmDoneSetup();
    }

    private void ReceiveHand(string[] data)
    {
        if (players[ourClientId].hand != null)
            return;
        Debug.Log("Receing hand");
        players[ourClientId].hand = new List<Card>();
        for (int i = 1; i < data.Length; i++)
        {
            players[ourClientId].hand.Add(Card.ToCard(data[i]));
        }
        //GameManager.gm.playerOrder = playerOrder;
        if (!initialConfirmation)
            ConfirmDoneSetup();
    }

    private void LoadObjects()
    {
        if (this != client)
            return;
        lobbySearchCanvas = GameObject.Find("LobbySearchCanvas");
        lobbyCanvas = GameObject.Find("LobbyCanvas");
        //Debug.Log(lobbyCanvas == null);
        lobbyCanvas.SetActive(false);
        //inGameCanvas = GameObject.Find("InGameCanvas");
        //inGameCanvas.SetActive(false);
        //multiplayersCanvas = GameObject.Find("MultiplayersCanvas");
        //multiplayersCanvas.SetActive(false);
        hostsList = lobbySearchCanvas.transform.Find("Slider").Find("HostsList");
        playersList = lobbyCanvas.transform.Find("PlayersList").gameObject;
        //lobbySearchCanvas.transform.Find("StopSearchBtn").gameObject.SetActive(false);
        //lobbySearchCanvas.transform.Find("StopSearchBtn").GetComponent<Button>().onClick.AddListener(client.StopFindingHosts);
        //lobbySearchCanvas.transform.Find("FindHostBtn").GetComponent<Button>().onClick.AddListener(client.FindHosts);
        lobbySearchCanvas.transform.Find("BackBtn").GetComponent<Button>().onClick.AddListener(client.LeavingNetworkActivities);
        lobbySearchCanvas.transform.Find("BackBtn").GetComponent<Button>().onClick.AddListener(GameManager.gm.GoToMultiplayerScene);

        lobbyCanvas.transform.Find("LeaveLobbyBtn").GetComponent<Button>().onClick.AddListener(client.LeaveLobby);
        lobbyCanvas.transform.Find("StartBtn").GetComponent<Button>().onClick.AddListener(client.StartGame);

        //Debug.Log(lobbyCanvas == null);
    }

    private void OnLevelWasLoaded(int level)
    {
        if (this != client)
            return;
        Debug.Log("Here" + level);
        switch (level)
        {
            // Main
            case 0:
                Destroy(gameObject);
                destroyed = true;
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
                LoadObjects();
                break;
            // Game (Multiplayer)
            case 4:
                break;
        }
    }

    public void LeavingNetworkActivities()
    {
        CloseClient();
    }

    public void CloseClient()
    {
        if (findingHosts)
        {
            StopFindingHosts();
        }
        else if (isConnected)
        {
            Disconnect();
        }
        ClearGameObjects();
        ClearHostList();
        Destroy(gameObject);
        destroyed = true;
    }

    private void OnDestroy()
    {
        //CloseClient();
    }

    private void OnEnable()
    {
        //Debug.Log("Enable");
    }

    private void OnDisable()
    {
        //Debug.Log("Disable");
    }


    public void StartGame()
    {
        ClearHostList();
        GameManager.gm.GoToGameScene();
        //deckCount = -1;
        GameManager.gm.StartGame(ourClientId);
        Send("STARTGAME|", reliableChannel);
    }

    public void Disconnect()
    {
        if (hostId != -1)
            NetworkTransport.Disconnect(hostId, connectionId, out error);
        hostId = -1;
        connectedHost = null;
        Debug.Log("Disconnected");
        isConnected = false;
    }

    public void LeaveGame()
    {
        Debug.Log("LEAVING Game");
        Disconnect();
        inLobby = false;
        //ClearGameObjects();
        CloseClient();
        GameManager.gm.GoToMainScene();
        //lobbySearchCanvas.SetActive(true);
        //lobbySearchCanvas.transform.Find("FindHostBtn").gameObject.SetActive(true);
        //lobbyCanvas.SetActive(false);

    }

    public void LeaveLobby()
    {
        Debug.Log("LEAVING LOBBYU");
        Disconnect();
        inLobby = false;
        ClearGameObjects();
        lobbySearchCanvas.SetActive(true);
        //lobbySearchCanvas.transform.Find("FindHostBtn").gameObject.SetActive(true);
        lobbyCanvas.SetActive(false);

    }

    private void ClearHostList()
    {
        //List<Host> expiredHosts = new List<Host>();
        foreach (KeyValuePair<Host, GameObject> kvp in hosts)
        {
            Destroy(kvp.Value);
            //expiredHosts.Add(kvp.Key);

        }
        hosts.Clear();
    }

    private void ClearGameObjects()
    {
        //List<int> connIds = new List<int>();
        foreach (KeyValuePair<int, Player> player in players)
        {
            Destroy(player.Value.playerGO);
            //connIds.Add(player.Key);
            //players.Remove(player.Key);
        }
        players.Clear();
        GameManager.gm.ClearGame();
        //for (int i = 0; i < connIds.Count; i++)
        //    players.Remove(connIds[i]);
    }

    public void ConnectTo(Host host)
    {
        //Debug.Log("Connecting");
        connectedHost = host;
        //Debug.Log(host != null);
        NetworkTransport.RemoveHost(hostId);

        ConnectionConfig cc = new ConnectionConfig();

        reliableChannel = cc.AddChannel(QosType.Reliable);
        unreliableChannel = cc.AddChannel(QosType.Unreliable);

        HostTopology topo = new HostTopology(cc, MAX_CONNECTION);
        hostId = NetworkTransport.AddHost(topo, 0);
        if (hostId == -1)
        {
            Debug.LogError("NetworkDiscovery StartAsClient - addHost failed");
            return;
        }
        connectionId = NetworkTransport.Connect(hostId, connectedHost.ip, connectedHost.port, 0, out error);
        isConnected = true;

        //lobbySearchCanvas.transform.Find("FindHostBtn").gameObject.SetActive(false);
        //lobbySearchCanvas.transform.Find("StopSearchBtn").gameObject.SetActive(false);
        //Debug.Log("connected ot host");
        findingHosts = false;
        //Debug.Log(connectionId);

    }

    public void StopFindingHosts()
    {
        if (findingHosts)
        {
            findingHosts = false;
            if (hostId != -1)
            {
                NetworkTransport.RemoveHost(hostId);
            }
            hostId = -1;

            //lobbySearchCanvas.transform.Find("FindHostBtn").gameObject.SetActive(true);
            //lobbySearchCanvas.transform.Find("StopSearchBtn").gameObject.SetActive(false);
        }
    }

    public void FindHosts()
    {
        //Debug.Log("Finding Hosts");
        if (!findingHosts)
        {
            findingHosts = true;
            //lobbySearchCanvas.transform.Find("FindHostBtn").gameObject.SetActive(false);
            //lobbySearchCanvas.transform.Find("StopSearchBtn").gameObject.SetActive(true);

        }
        // Does player have name?
        /*string pName = GameObject.Find("NameInput").GetComponent<InputField>().text;
        pName = "LOLS";
        if (pName == "")
        {
            Debug.Log("You must enter a name");
            return;
        }*/
        //playerName = pName;
        // Does player know who the host is?
        if (hostId == -1)
        {
            //Debug.Log("Haven't received port");
            ConnectionConfig cc = new ConnectionConfig();

            reliableChannel = cc.AddChannel(QosType.Reliable);
            unreliableChannel = cc.AddChannel(QosType.Unreliable);

            HostTopology topo = new HostTopology(cc, MAX_CONNECTION);
            hostId = NetworkTransport.AddHost(topo, broadcastPort);
            //Debug.Log(hostId);
            if (hostId == -1)
            {
                Debug.LogError("NetworkDiscovery StartAsClient - addHost failed");
                return;
            }
        }


        NetworkTransport.SetBroadcastCredentials(hostId, broadcastKey, broadcastVersion, broadcastSubVersion, out error);

        //int counter = 0;
        NetworkEventType recData = NetworkEventType.Nothing;
        //do
        // {
        //Debug.Log("Listening...");
        recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
        //Debug.Log(recData);
        if (recData == NetworkEventType.BroadcastEvent)
        {
            //Debug.Log("YE");

            int rcvSize;
            NetworkTransport.GetBroadcastConnectionMessage(hostId, buffer, buffer.Length, out rcvSize, out error);
            string senderAddr;
            int senderPort;
            NetworkTransport.GetBroadcastConnectionInfo(hostId, out senderAddr, out senderPort, out error);
            //Debug.Log(rcvSize);
            OnReceivedBroadcast(senderAddr, senderPort, Encoding.Unicode.GetString(buffer));
        }
        /*counter++;
        //Waits roughly 10seconds before timeout
        if (counter > 1 << 24 - 1)
        {
            Debug.Log("WAITED TOO LONG");
            NetworkTransport.RemoveHost(hostId);
            hostId = -1;
            return;
        }*/
        // } while (recData != NetworkEventType.BroadcastEvent);
    }

    public void OnReceivedBroadcast(string fromAddress, int fromPort, string data)
    {

        string[] splitAddr = fromAddress.Split(':');
        string[] splitData = data.Split('|');
        
        switch (splitData[0])
        {
            case "SERVER":
                //Debug.Log("Found Connection");
                string tempIp = splitAddr[splitAddr.Length - 1];
                //hostPort;
                //Debug.Log(serverIp);
                int tempPort = fromPort;
                //Debug.Log(serverPort);
                Host host = new Host(tempIp, tempPort);
                bool newHost = true;
                foreach (KeyValuePair<Host, GameObject> kvp in hosts)
                {
                    if (kvp.Key.Equals(host))
                    {
                        hosts[kvp.Key].transform.GetComponent<ExpiringButton>().Reset();
                        newHost = false;
                        break;
                    }
                }
                if (newHost)
                {
                    GameObject joinBtn = Instantiate(joinButtonPrefab) as GameObject;
                    joinBtn.transform.SetParent(hostsList);
                    joinBtn.transform.GetComponent<ExpiringButton>().SetExpiringButton(host);
                    hosts.Add(host, joinBtn);
                    joinBtn.transform.Find("Text").GetComponent<Text>().text = splitData[1] + "'s\nRoom";
                    //joinBtn.transform.Find("Text").GetComponent<Text>().text = "Room #" + host.port + ", " + host.ip;
                    joinBtn.transform.localScale = new Vector3(1, 1, 1);
                }

                break;
        }


    }

    private void OnAskName(string[] data)
    {
        // Set this client's ID
        ourClientId = int.Parse(data[1]);

        // Send our name to server
        Send("NAMEIS|" + GameManager.gm.playerName, reliableChannel);

        // Create all the other players
        for (int i = 2; i < data.Length; i++)
        {
            string[] d = data[i].Split('%');
            SpawnPlayer(d[0], int.Parse(d[1]));
        }

    }

    private void PlayerDisconnected(int cnnId)
    {
        Destroy(players[cnnId].playerGO);
        players.Remove(cnnId);
    }

    private void SpawnPlayer(string playerName, int cnnId)
    {
        Debug.Log("Spawn player " + playerName + cnnId);
        GameObject go = Instantiate(playerPrefab) as GameObject;
        // Our client?
        if (cnnId == ourClientId)
        {
            // Add mobility
            //go.AddComponent<PlayerMotor>();
            lobbyCanvas.SetActive(true);
            lobbySearchCanvas.SetActive(false);
            //inGameCanvas.SetActive(true);
            playerName = GameManager.gm.playerName;
            isStarted = true;
        }

        Player p = new Player();
        p.playerGO = go;
        p.playerName = playerName;
        p.connectionId = cnnId;
        //p.playerGO.GetComponentInChildren<TextMesh>().text = playerName;
        p.playerGO.transform.SetParent(playersList.transform);
        p.playerGO.transform.Find("NameText").GetComponent<Text>().text = playerName;
        go.transform.localScale = new Vector3(1, 1, 1);
        players.Add(cnnId, p);
    }

    private void OnAskPosition(string[] data)
    {
        if (!isStarted)
            return;
        Debug.Log("DATA>" + data.Length);
        // Update everyone else
        for (int i = 1; i < data.Length; i++)
        {
            string[] d = data[i].Split('%');

            // Prevent Server from updating self
            if (ourClientId != int.Parse(d[0]))
            {
                Vector3 position = Vector3.zero;
                position.x = float.Parse(d[1]);
                position.y = float.Parse(d[2]);
                players[int.Parse(d[0])].playerGO.transform.position = position;
            }
        }

        // Send our position
        Vector3 myPosition = players[ourClientId].playerGO.transform.position;
        string m = "MYPOSITION|" + myPosition.x.ToString() + '|' + myPosition.y.ToString();
        Send(m, unreliableChannel);
    }

    private void Send(string message, int channelId)
    {
        Debug.Log("Sending : " + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        NetworkTransport.Send(hostId, connectionId, channelId, msg, message.Length * sizeof(char), out error);
    }
}
