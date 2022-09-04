using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
public class LobbyUI : MonoBehaviour
{
    [SerializeField] private GameObject roomInfoUiObj;
    [SerializeField] private Transform roomInfoUiRoot;
    [SerializeField] private Text moneyInputField;
    [SerializeField] private Text profileToCheckField;
    [SerializeField] private Text playerIdField;
    public int MoneyToBring()
    {
        try
        {
            int count = Int32.Parse(moneyInputField.text);
            return count;
        }
        catch (Exception e) { }
        return -1;
    }

    private void OnEnable()
    {
        RefreshRoomList();
        playerIdField.text = $"ID: {Client.instance.loginUid}";
    }
    public void RefreshRoomList()
    {
        Client.instance.RequestRoomList();
        foreach (var obj in roomInfoUiRoot.GetComponentsInChildren<RoomInfoUI>())
            Destroy(obj.gameObject);
    }
    public void AddRoomToList(RoomInfoUI.RoomInfo info)
    {
        GameObject obj = Instantiate(roomInfoUiObj, roomInfoUiRoot);
        if (obj.TryGetComponent<RoomInfoUI>(out var ui))
        {
            ui.SetRoom(info);
            ui.lobby = this;
        }
    }
    public void CheckProfile()
    {
        try
        {
            int pid;
            if (profileToCheckField.text == "")
                pid = Client.instance.loginUid;
            else 
                pid= Int32.Parse(profileToCheckField.text);
            Client.instance.CheckProfile(pid);
        }
        catch (Exception e) { Client.instance.CheckProfile(Client.instance.loginUid);  }
    }
    public void CreateRoom()
    {
        UIManager.instance.OpenUI(UIManager.UIPrefab.roomCreateUI);
    }
    public void JoinAnyRoom()
    {
        if (MoneyToBring() < 30)
        {
            UIManager.instance.BidenSays("you need to bring more cash");
            return;
        }
        Client.instance.JoinRoom(MoneyToBring());
    }
}
