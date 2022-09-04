using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Net;
using System.Net.Sockets;
public class UIManager : MonoBehaviour
{
    public enum UIPrefab
    {
        loginUI = 0,
        registerUI = 1,
        lobbyUI = 2,
        gameUI = 3,
        profileUI = 4,
        bidenUI = 5,
        roomCreateUI = 6,
    }
    public enum UILevel
    {
        TOP = 0,
        MIDDLE = 1,
        BOTTOM = 2
    }
    public class UITemplate
    {
        public UITemplate(string p, UILevel l)
        {
            path = p;
            level = l;
        }
        public string path;
        public UILevel level;
    }
    private Dictionary<UIPrefab, UITemplate> templates = 
        new Dictionary<UIPrefab, UITemplate>();
    public Dictionary<UIPrefab, GameObject> uiInstances =
        new Dictionary<UIPrefab, GameObject>();
    private Dictionary<UILevel, Transform> levelRotts =
        new Dictionary<UILevel, Transform>();
    public GameObject OpenUI(UIPrefab ui)
    {
        if (uiInstances.ContainsKey(ui) &&
            uiInstances[ui] != null)
        {
            uiInstances[ui].SetActive(true);
            return uiInstances[ui];
        }
        else if (templates.ContainsKey(ui))
        {
            GameObject uiObj = Resources.Load(templates[ui].path) as GameObject;
            uiInstances[ui] = Instantiate(uiObj, levelRotts[templates[ui].level]);
            return uiInstances[ui];
        }
        return null;
    }
    public void CloseUI(UIPrefab ui)
    {
        if (uiInstances.ContainsKey(ui) &&
            uiInstances[ui] != null)
        {
            uiInstances[ui].SetActive(false);
        }
    }

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
    public void SendRpcRequest()
    {
        //switch (rpcSelecter.value)
        //{
        //    case 5:
        //        Client.instance.Bid(Int32.Parse(textField1.text));
        //        break;
        //    case 6:
        //        Client.instance.CheckOrFold();
        //        break;
        //    case 7:
        //        Client.instance.UseTimeCard();
        //        break;
        //    case 8:
        //        Client.instance.QuitRoom();
        //        break;
        //    default:
        //        break;
        //}
    }
    public void BidenSays(string says = "我的牌太多了!!!")
    {
        if (!uiInstances.ContainsKey(UIPrefab.bidenUI) ||
            uiInstances[UIPrefab.bidenUI] == null)
            OpenUI(UIPrefab.bidenUI);
        uiInstances[UIPrefab.bidenUI].
            GetComponent<BidenUI>().
            BidenSays(says);
    }
    public void Start()
    {
        templates[UIPrefab.gameUI] =
            new UITemplate("Prefabs/RoomUI", UILevel.BOTTOM);
        templates[UIPrefab.loginUI] =
            new UITemplate("Prefabs/LoginUI", UILevel.BOTTOM);
        templates[UIPrefab.bidenUI] =
            new UITemplate("Prefabs/BidenUI", UILevel.TOP);
        templates[UIPrefab.registerUI] =
            new UITemplate("Prefabs/RegisterUI", UILevel.MIDDLE);
        templates[UIPrefab.lobbyUI] =
            new UITemplate("Prefabs/LobbyUI", UILevel.BOTTOM);
        templates[UIPrefab.profileUI] =
            new UITemplate("Prefabs/ProfileUI", UILevel.MIDDLE);
        templates[UIPrefab.roomCreateUI] =
            new UITemplate("Prefabs/CreateRoomUI", UILevel.MIDDLE);

        levelRotts[UILevel.TOP] = transform.Find("topRoot");
        levelRotts[UILevel.MIDDLE] = transform.Find("middleRoot");
        levelRotts[UILevel.BOTTOM] = transform.Find("bottomRoot");
        UIManager.instance.BidenSays("Welcome to Joe Biden's Poker Club");
    }
}
