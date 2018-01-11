using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class NetworkManager : MonoBehaviour
{
    private const string typeName = "UniqueGameName";
    private const string gameName = "RoomName";

    private bool isRefreshingHostList = false;
    private HostData[] hostList;

    public GameObject playerPrefab;
    void OnGUI()
    {

        if (!Network.isClient && !Network.isServer)
        {
            if (GUI.Button(new Rect(100, 100, 250, 100), "Start Server"))
                StartServer();

            if (GUI.Button(new Rect(100, 250, 250, 100), "Refresh Hosts"))
                RefreshHostList();

            if (hostList != null)
            {
                for (int i = 0; i < hostList.Length; i++)
                {
                    if (GUI.Button(new Rect(400, 100 + (110 * i), 300, 100), hostList[i].gameName))
                        JoinServer(hostList[i]);
                }
            }
        }
    }

    private void StartServer()
    {
        Debug.Log(MasterServer.ipAddress);
        Debug.Log(MasterServer.port);
        Network.InitializeServer(5, 25000, !Network.HavePublicAddress());
        MasterServer.RegisterHost(typeName, gameName);
        //Network.

        Debug.Log(MasterServer.ipAddress);
        Debug.Log(MasterServer.port);
    }

    void OnServerInitialized()
    {
        SpawnPlayer();
    }


    void Update()
    {
        if (isRefreshingHostList && MasterServer.PollHostList().Length > 0)
        {
            isRefreshingHostList = false;
            hostList = MasterServer.PollHostList();

        }

        if (hostList != null)
            GameObject.Find("Canvas").transform.GetChild(0).GetComponent<Text>().text = ">" + hostList[0].port.ToString();
        else
            GameObject.Find("Canvas").transform.GetChild(0).GetComponent<Text>().text = "None found";
    }

    private void RefreshHostList()
    {
        if (!isRefreshingHostList)
        {
            isRefreshingHostList = true;
            MasterServer.RequestHostList(typeName);
        }
    }


    private void JoinServer(HostData hostData)
    {
        Debug.Log(hostData.ip + " " + hostData.port);
        Network.Connect(hostData);
        //hostData.
    }

    void OnConnectedToServer()
    {
        SpawnPlayer();
    }


    private void SpawnPlayer()
    {
        Network.Instantiate(playerPrefab, Vector3.up * 5, Quaternion.identity, 0);
    }
}
