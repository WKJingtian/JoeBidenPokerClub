using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomInfoUI : MonoBehaviour
{
    [SerializeField] private Text nameField;
    [SerializeField] private Text playerNumField;
    [SerializeField] private Text obNumField;
    [SerializeField] private Text sbField;
    [SerializeField] private Text timeField;
    public struct RoomInfo
    {
        public int roomID;
        public string name;
        public int maxPlayer;
        public int curPlayer;
        public int maxOb;
        public int curOb;
        public int sb;
        public int roundTime;
        public int roundPerTimeCard;
        public int roundPassed;
    }
    RoomInfo room;
    [HideInInspector]public LobbyUI lobby;
    public void DoJoinRoom()
    {
        //if (room.sb <= 0 ||
        //    !lobby ||
        //    lobby.MoneyToBring() < room.sb * 30)
        //{
        //    UIManager.instance.BidenSays("You mast have at least 15bb to join a room");
        //    return;
        //}
        //Client.instance.JoinRoom(lobby.MoneyToBring(), room.roomID);
        Client.instance.JoinRoom(1000, room.roomID);
    }
    public void DoObRoom()
    {
        Client.instance.ObRoom(room.roomID);
    }
    public void SetRoom(RoomInfo info)
    {
        room = info;
        nameField.text = info.name;
        playerNumField.text = $"player: {info.curPlayer}/{info.maxPlayer}";
        obNumField.text = $"ob: {info.curOb}/{info.maxOb}";
        sbField.text = $"sb: {info.sb}/{info.sb * 2}";
        timeField.text = $"{info.roundPassed}r,{info.roundTime}s,{info.roundPerTimeCard}rptc";

        if (info.curPlayer >= info.maxPlayer)
            playerNumField.GetComponent<Outline>().effectColor = Color.red;
        else
            playerNumField.GetComponent<Outline>().effectColor = Color.green;
        if (info.curOb >= info.maxOb)
            obNumField.GetComponent<Outline>().effectColor = Color.red;
        else
            obNumField.GetComponent<Outline>().effectColor = Color.green;
    }
}
