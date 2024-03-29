using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Net;
using System.Net.Sockets;

// let this singleton class handle all tcp stuff
public class Client : MonoBehaviour
{
    [HideInInspector]public static Client instance;
    [HideInInspector]public static readonly int s_maxBufferSize = 4096;
    [HideInInspector]public int port = 4242;
    [HideInInspector]public int id = -1;
    [SerializeField] private string serverIp = "127.0.0.1";

    public TcpClient socket;
    private bool isConnected;
    private NetworkStream stream;
    private byte[] receiveBuffer = new byte[s_maxBufferSize];
    private byte[] sendBuffer = new byte[s_maxBufferSize];
    Dictionary<ClientPackets, AsyncCallback> serverCallbackMap = new Dictionary<ClientPackets, AsyncCallback>();
    Dictionary<ServerPackets, Action<Packet>> serverMessageHandlerMap = new Dictionary<ServerPackets, Action<Packet>>();

    [HideInInspector] public int loginUid = -1;
    [HideInInspector] public Room playRoom = null;

    private Packet receivedPacket;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
        {
            UIManager.instance.BidenSays($"gameobject {gameObject.name} has a client component on it, but client is already initialized by {instance.gameObject.name}");
            Destroy(this);
            return;
        }
        id = -1; // make sure when ever started, reconnect to the server

        // callback function map
        serverCallbackMap[ClientPackets.welcomeReceived] = ClientRpc_welcomeReceived;
        // server rpc handler map
        serverMessageHandlerMap[ServerPackets.welcome] = ServerRpc_welcome;
        serverMessageHandlerMap[ServerPackets.registerCallback] = ServerRpc_registerCallback;
        serverMessageHandlerMap[ServerPackets.loginCallback] = ServerRpc_loginCallback;
        serverMessageHandlerMap[ServerPackets.joinRoomCallback] = ServerRpc_joinRoomCallback;
        serverMessageHandlerMap[ServerPackets.createRoomCallback] = ServerRpc_createRoomCallback;
        serverMessageHandlerMap[ServerPackets.bidCallback] = ServerRpc_bidCallback;
        serverMessageHandlerMap[ServerPackets.foldCallback] = ServerRpc_foldCallback;
        serverMessageHandlerMap[ServerPackets.useTimeCardCallback] = ServerRpc_useTimeCardCallback;
        serverMessageHandlerMap[ServerPackets.quitRoomCallback] = ServerRpc_quitRoomCallback;
        serverMessageHandlerMap[ServerPackets.syncRoomStat] = ServerRpc_syncRoomStat;
        serverMessageHandlerMap[ServerPackets.syncPlayerStat] = ServerRpc_syncPlayerStat;
        serverMessageHandlerMap[ServerPackets.syncFlopTurnRiver] = ServerRpc_syncFlopTurnRiver;
        serverMessageHandlerMap[ServerPackets.syncPlayerHand] = ServerRpc_syncPlayerHand;
        serverMessageHandlerMap[ServerPackets.requestPlayerAction] = ServerRpc_requestPlayerAction;
        serverMessageHandlerMap[ServerPackets.congrateWinner] = ServerRpc_congrateWinner;
        serverMessageHandlerMap[ServerPackets.dispatchChat] = ServerRpc_dispatchChat;
        serverMessageHandlerMap[ServerPackets.sendAccountInfo] = ServerRpc_sendAccountInfo;
        serverMessageHandlerMap[ServerPackets.sendRoomList] = ServerRpc_sendRoomList;
        serverMessageHandlerMap[ServerPackets.observeRoomCallback] = ServerRpc_observeRoomCallback;
    }

    [SerializeField] private InputField ipField;
    [SerializeField] private Button connectBtn;
    public void DoConnect()
    {
        serverIp = ipField.text;
        // connect to the server
        Connect();
        ipField.gameObject.SetActive(false);
        connectBtn.gameObject.SetActive(false);
    }
    private void OnApplicationQuit()
    {
        Disconnect();
    }
    private void Connect()
    {
        if (id == -1)
        {
            socket = new TcpClient
            {
                ReceiveBufferSize = s_maxBufferSize,
                SendBufferSize = s_maxBufferSize,
            };
            socket.BeginConnect(serverIp, instance.port, ConnectCallback, socket);
        }
        else
        {
            UIManager.instance.BidenSays($"I am already connected with id {id}");
        }
    }
    private void Disconnect()
    {
        if (isConnected)
        {
            isConnected = false;
            socket.Close();
            stream = null;
            socket = null;
            receiveBuffer = null;
            receivedPacket = null;
            id = -1;
            UIManager.instance.BidenSays("Disconnected from server");
        }
    }
    private void ConnectCallback(IAsyncResult result)
    {
        socket.EndConnect(result);
        if (!socket.Connected) return;
        stream = socket.GetStream();
        receivedPacket = new Packet();
        isConnected = true;
        stream.BeginRead(receiveBuffer, 0, s_maxBufferSize, ReceiveCallback, null);
    }
    private void ReceiveCallback(IAsyncResult result)
    {
        try
        {
            int l = stream.EndRead(result);
            if (l <= 0)
            {
                Disconnect();
                return;
            }
            byte[] data = new byte[l];
            Array.Copy(receiveBuffer, data, l);
            receivedPacket.Reset(HandlePacket(data));
            stream.BeginRead(receiveBuffer, 0, s_maxBufferSize, ReceiveCallback, null);
        }
        catch (Exception e)
        {
            UIManager.instance.BidenSays(e.Message);
            Disconnect();
        }
    }
    bool HandlePacket(byte[] data)
    {
        int l = 0;
        receivedPacket.SetBytes(data);
        if (receivedPacket.Length() >= 4)
        {
            l = receivedPacket.ReadInt();
            if (l <= 0) return true;
        }
        while (l > 0 && l <= receivedPacket.UnreadLength())
        {
            byte[] bytes = receivedPacket.ReadBytes(l);
            ThreadManager.ExecuteOnMainThread(()=>
            {
                Packet p = new Packet(bytes);
                ServerPackets serverRpcId = (ServerPackets)p.ReadInt();
                if (serverMessageHandlerMap.ContainsKey(serverRpcId)) serverMessageHandlerMap[serverRpcId]?.Invoke(p);
            });
            l = 0;
            if (receivedPacket.UnreadLength() >= 4)
            {
                l = receivedPacket.ReadInt();
                if (l <= 0) return true;
            }
        }
        return l <= 1;
    }
    public void Send(Packet p, AsyncCallback? toRead = null)
    {
        try
        {
            if (socket == null || stream == null)
            {
                UIManager.instance.BidenSays($"client {id} does not have a valid socket/stream");
                return;
            }
            stream.BeginWrite(p.ToArray(), 0, p.Length(), toRead, null);
        }
        catch (Exception e)
        {
            UIManager.instance.BidenSays(e.Message);
        }
    }
    public void DoRegister(string name, string password)
    {
        ClientSend.RpcSend(ClientPackets.register, (Packet p) =>
        {
            p.Write(name);
            p.Write(password);
        },null);
    }
    public void DoLogin(int uid, string password)
    {
        if (loginUid != -1)
        {
            UIManager.instance.BidenSays("you have already logged in!");
            return;
        }
        ClientSend.RpcSend(ClientPackets.login, (Packet p) =>
        {
            p.Write(uid);
            p.Write(password);
        }, null);
    }
    public void RequestRoomList()
    {
        ClientSend.RpcSend(ClientPackets.requestRoomList, (Packet p) =>
        {
        }, null);
    }
    public void JoinRoom(int joinWithCash, int roomId = -1)
    {
        if (loginUid == -1)
        {
            UIManager.instance.BidenSays("you havn't logged in!");
            return;
        }
        ClientSend.RpcSend(ClientPackets.joinRoom, (Packet p) =>
        {
            p.Write(roomId);
            p.Write(joinWithCash);
        }, null);
    }
    public void ObRoom(int roomId = -1)
    {
        if (loginUid == -1)
        {
            UIManager.instance.BidenSays("you havn't logged in!");
            return;
        }
        ClientSend.RpcSend(ClientPackets.observeRoom, (Packet p) =>
        {
            p.Write(roomId);
        }, null);
    }
    public void CreateRoom(string name, int sb, int time, int rptc, int pLimit, int oLimit)
    {
        if (loginUid == -1)
        {
            UIManager.instance.BidenSays("you havn't logged in!");
            return;
        }
        ClientSend.RpcSend(ClientPackets.createRoom, (Packet p) =>
        {
            p.Write(name);
            p.Write(sb);
            p.Write(time);
            p.Write(rptc);
            p.Write(pLimit);
            p.Write(oLimit);
        }, null);
    }
    public void CheckProfile(int uid)
    {
        if (loginUid == -1)
        {
            UIManager.instance.BidenSays("you havn't logged in!");
            return;
        }
        ClientSend.RpcSend(ClientPackets.requestAccountInfo, (Packet p) =>
        {
            p.Write(uid);
        }, null);
    }
    #region client rpc callback
    void ClientRpc_welcomeReceived(IAsyncResult result)
    {

    }
    #endregion
    #region server rpc handler
    void ServerRpc_welcome(Packet p)
    {
        UIManager.instance.BidenSays($"connection success with id {p.ReadString()}");
        id = p.ReadInt();
    }
    void ServerRpc_loginCallback(Packet p)
    {
        UIManager.instance.BidenSays("xxxxwuwuwu");
        if (p.ReadBool())
        {
            loginUid = p.ReadInt();
            UIManager.instance.BidenSays($"login success with id {loginUid}");
            UIManager.instance.CloseUI(UIManager.UIPrefab.registerUI);
            UIManager.instance.CloseUI(UIManager.UIPrefab.loginUI);
            UIManager.instance.OpenUI(UIManager.UIPrefab.lobbyUI);
        }
        else
            UIManager.instance.BidenSays("login fail");
    }
    void ServerRpc_registerCallback(Packet p)
    {
        if (p.ReadBool())
        {
            loginUid = p.ReadInt();
            UIManager.instance.BidenSays($"register success with uid {loginUid}");
            UIManager.instance.CloseUI(UIManager.UIPrefab.registerUI);
            UIManager.instance.CloseUI(UIManager.UIPrefab.loginUI);
            UIManager.instance.OpenUI(UIManager.UIPrefab.lobbyUI);
        }
        else
            UIManager.instance.BidenSays("register fail");
    }
    void ServerRpc_joinRoomCallback(Packet p)
    {
        if (p.ReadBool())
        {
            int joinRoomIdx = p.ReadInt();
            UIManager.instance.BidenSays($"you have successfully joined room {joinRoomIdx}");
            playRoom = UIManager.instance.OpenUI(UIManager.UIPrefab.gameUI)
                .GetComponent<Room>();
        }
        else
        {
            UIManager.instance.BidenSays($"you fail to join a room");
        }
    }
    void ServerRpc_createRoomCallback(Packet p)
    {
        if (p.ReadBool())
        {
            int joinRoomIdx = p.ReadInt();
            UIManager.instance.BidenSays($"you have successfully created room {joinRoomIdx}");
        }
        else
        {
            UIManager.instance.BidenSays($"you fail to CREATE a room");
        }
    }
    void ServerRpc_bidCallback(Packet p)
    {
        bool actionSuccess = p.ReadBool();
        if (actionSuccess)
            UIManager.instance.BidenSays($"bid success");
        else
            UIManager.instance.BidenSays($"bid fail");
        playRoom.myTurn = !actionSuccess;
    }
    void ServerRpc_foldCallback(Packet p)
    {
        if (p.ReadBool())
            UIManager.instance.BidenSays($"check success");
        else
            UIManager.instance.BidenSays($"check fail or fold success");
        playRoom.myTurn = false;
    }
    void ServerRpc_useTimeCardCallback(Packet p)
    {
        if (p.ReadBool())
            UIManager.instance.BidenSays($"use time card success");
        else
            UIManager.instance.BidenSays($"use time card fail");
    }
    void ServerRpc_quitRoomCallback(Packet p)
    {
        if (p.ReadBool())
        {
            UIManager.instance.BidenSays($"quit room success");
            UIManager.instance.CloseUI(UIManager.UIPrefab.gameUI);
            playRoom = null;
        }
        else
            UIManager.instance.BidenSays($"quit room fail");
    }
    void ServerRpc_syncRoomStat(Packet p)
    {
        if (playRoom != null)
        {
            playRoom.timer = p.ReadFloat();
            playRoom.roundNum = p.ReadInt();
            playRoom.timeCardPerRound = p.ReadInt();
            playRoom.smallBlindMoneyNum = p.ReadInt();
            playRoom.currentActivePlayer = p.ReadInt();
            playRoom.currentSmallBlind = p.ReadInt();
            playRoom.UpdateUI();
        }
    }
    void ServerRpc_syncPlayerStat(Packet p)
    {
        if (playRoom != null)
        {
            for( int i = 0; i < playRoom.players.Capacity; i++ )
            {
                bool seatHaasPlayer = p.ReadBool();
                if (playRoom.players.Count > i)
                {
                    if (seatHaasPlayer)
                    {
                        if (playRoom.players[i] == null) playRoom.players[i] = new Room.PlayerInGameStat();
                        playRoom.players[i].uid = p.ReadInt();
                        playRoom.players[i].name = p.ReadString();
                        playRoom.players[i].moneyInPocket = p.ReadInt();
                        playRoom.players[i].moneyInPot = p.ReadInt();
                        playRoom.players[i].hasBidThisRound = p.ReadBool();
                        playRoom.players[i].hasFolded = p.ReadBool();
                        playRoom.players[i].ifAllIn = p.ReadBool();
                        playRoom.players[i].hasQuited = p.ReadBool();
                        playRoom.players[i].timeCard = p.ReadInt();
                    }
                    else
                        playRoom.players[i] = null;
                }
                else
                {
                    if (seatHaasPlayer)
                    {
                        playRoom.players.Add(new Room.PlayerInGameStat());
                        playRoom.players[i].uid = p.ReadInt();
                        playRoom.players[i].name = p.ReadString();
                        playRoom.players[i].moneyInPocket = p.ReadInt();
                        playRoom.players[i].moneyInPot = p.ReadInt();
                        playRoom.players[i].hasBidThisRound = p.ReadBool();
                        playRoom.players[i].hasFolded = p.ReadBool();
                        playRoom.players[i].ifAllIn = p.ReadBool();
                        playRoom.players[i].hasQuited = p.ReadBool();
                        playRoom.players[i].timeCard = p.ReadInt();
                    }
                    else
                        playRoom.players.Add(null);
                }
            }
            playRoom.UpdateUI();
        }
    }
    void ServerRpc_syncFlopTurnRiver(Packet p)
    {
        if (playRoom != null)
        {
            playRoom.flopTurnRiver.Clear();
            for (int i = 0; i < playRoom.flopTurnRiver.Capacity; i++)
            {
                if (p.ReadBool())
                    playRoom.flopTurnRiver.Add(new PokerCard((PokerCard.Decors)p.ReadInt(), p.ReadInt()));
                else
                {
                    PokerCard temp = new PokerCard();
                    temp.notRevealed = true;
                    playRoom.flopTurnRiver.Add(temp);
                }
            }
            playRoom.UpdateUI();
        }
    }
    void ServerRpc_syncPlayerHand(Packet p)
    {
        if (playRoom != null)
        {
            int targetId = p.ReadInt();
            foreach (var player in playRoom.players)
            {
                if (player != null && player.uid == targetId)
                {
                    player.hand.Clear();
                    for (int i = 0; i < 2; i++)
                    {
                        PokerCard temp = new PokerCard();
                        if (p.ReadBool())
                            temp = new PokerCard((PokerCard.Decors)p.ReadInt(), p.ReadInt());
                        else
                            temp.notRevealed = true;
                        player.hand.Add(temp);
                    }
                    break;
                }
            }
            playRoom.UpdateUI();
        }
    }
    void ServerRpc_requestPlayerAction(Packet p)
    {
        playRoom.myTurn = true;
        UIManager.instance.BidenSays("Now it is your time");
        if (playRoom != null) playRoom.UpdateUI();
    }
    void ServerRpc_congrateWinner(Packet p)
    {
        int targetUid = p.ReadInt();
        string isWinner = p.ReadBool() ? "winner" : "loser";
        if (targetUid == loginUid)
            UIManager.instance.BidenSays($"Congradulation! you are the\n{isWinner} of this round");
        if (playRoom != null) playRoom.UpdateUI();
    }
    void ServerRpc_dispatchChat(Packet p)
    {
        //todo
    }
    void ServerRpc_sendAccountInfo(Packet p)
    {
        bool playerFound = p.ReadBool();
        if (playerFound)
        {
            var obj = UIManager.instance.OpenUI(UIManager.UIPrefab.profileUI);
            if (obj && obj.TryGetComponent<ProfileUI>(out var pui))
                pui.SetUser(p.ReadInt(), p.ReadString(), p.ReadInt(), p.ReadFloat(),
                    p.ReadInt(), p.ReadInt(), p.ReadInt(), p.ReadFloat());
        }
        else
            UIManager.instance.BidenSays("找不到你想找的玩家");
    }
    void ServerRpc_sendRoomList(Packet p)
    {
        GameObject obj = UIManager.instance.uiInstances.ContainsKey(UIManager.UIPrefab.lobbyUI) ?
            UIManager.instance.uiInstances[UIManager.UIPrefab.lobbyUI] : null;
        if (obj && obj.TryGetComponent<LobbyUI>(out var ui))
        {

        } else return;
        int count = p.ReadInt();
        for (int i =0; i < count; i++)
        {
            RoomInfoUI.RoomInfo info;
            info.roomID = p.ReadInt();
            info.name = p.ReadString();
            info.maxPlayer = p.ReadInt();
            info.curPlayer = p.ReadInt();
            info.maxOb = p.ReadInt();
            info.curOb = p.ReadInt();
            info.sb = p.ReadInt();
            info.roundTime = p.ReadInt();
            info.roundPerTimeCard = p.ReadInt();
            info.roundPassed = p.ReadInt();
            ui.AddRoomToList(info);
        }
    }
    void ServerRpc_observeRoomCallback(Packet p)
    {
        //todo
    }
    #endregion
}
