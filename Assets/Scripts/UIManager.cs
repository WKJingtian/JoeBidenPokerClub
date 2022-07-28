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
    public RoomUI roomUI;
    private void Update()
    {
        bidenClock -= Time.deltaTime;
        if (bidenWantToSay.Count > 0 &&
            bidenClock <= 0)
        {
            bidenClock = s_bidenSpeechCd;
            string says = bidenWantToSay.Peek();
            bidenSays.text = says;
            bidenWantToSay.Pop();
        }
    }
    public void SendRpcRequest()
    {
        //try
        //{
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
                case 5:
                    Client.instance.Bid(Int32.Parse(textField1.text));
                    break;
                case 6:
                    Client.instance.CheckOrFold();
                    break;
                case 7:
                    Client.instance.UseTimeCard();
                    break;
                case 8:
                    Client.instance.QuitRoom();
                    break;
                default:
                    break;
            }
        //}
        //catch(Exception e)
        //{
        //    BidenSays($"unity error: {e.Message}");
        //}
    }
    private Stack<string> bidenWantToSay = new Stack<string>();
    private float bidenClock;
    public static readonly float s_bidenSpeechCd = 3.0f;
    public void BidenSays(string says = "我的牌太多了!!!")
    {
        bidenWantToSay.Push(says);
    }
}
