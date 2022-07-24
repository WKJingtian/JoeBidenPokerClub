using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Net;
using System.Net.Sockets;
public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
        {
            Destroy(this);
            return;
        }
    }
    public InputField textField1;
    public InputField textField2;
    public InputField textField3;
    public Text bidenSays;
    public Dropdown rpcSelecter;

    public void SendRpcRequest()
    {
        switch (rpcSelecter.value)
        {
            case 0:
                //Client.instance.Connect();
                break;
            case 1:
                Client.instance.DoRegister(textField1.text, textField2.text);
                break;
            case 2:
                Client.instance.DoLogin(Int32.Parse(textField1.text), textField2.text);
                break;
            case 3:
                Client.instance.CreateRoom();
                break;
            case 4:
                Client.instance.JoinRoom(Int32.Parse(textField1.text), -1);
                break;
            default:
                break;
        }
    }

    public void BidenSays(string says = "我的牌太多了!!!")
    {
        bidenSays.text = says;
    }
}
