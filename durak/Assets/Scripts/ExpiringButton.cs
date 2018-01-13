using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExpiringButton:MonoBehaviour
{
    public int timer;
    public Host host;
    //bool pressed = false;

    public void SetExpiringButton(Host h)
    {
        Reset();
        host = new Host();
        host.ip = new string(h.ip.ToCharArray());
        host.port = h.port;
        //Debug.Log("host" + host != null);
    }
    
    public void Reset()
    {
        timer = 100;
    }

    public void Decrement()
    {
        timer--;
    }

    public bool Expired()
    {
        return timer <= 0;
    }

    public void Join()
    {

        //Debug.Log("Join room");
        //Client c = GameObject.Find("Client").transform.GetComponent<Client>();
        //if (!pressed)
        //{
          //  pressed = true;
        Client.client.ConnectTo(host);
        GetComponent<Button>().interactable = false;
        Client.client.ClearHostList();
    }
}
