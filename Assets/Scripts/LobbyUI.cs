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
            int pid = Int32.Parse(profileToCheckField.text);
            Client.instance.CheckProfile(pid);
        }
        catch (Exception e) { Client.instance.CheckProfile(Client.instance.loginUid);  }
    }
    public void CreateRoom()
    {
        // open the create room ui instead of creating a default room
        Client.instance.CreateRoom();
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
