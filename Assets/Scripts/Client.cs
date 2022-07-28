using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        serverMessageHandlerMap[ServerPackets.syncAccountStat] = ServerRpc_syncAccountStat;
        serverMessageHandlerMap[ServerPackets.syncPlayerStat] = ServerRpc_syncPlayerStat;
        serverMessageHandlerMap[ServerPackets.syncFlopTurnRiver] = ServerRpc_syncFlopTurnRiver;
        serverMessageHandlerMap[ServerPackets.syncPlayerHand] = ServerRpc_syncPlayerHand;
        serverMessageHandlerMap[ServerPackets.requestPlayerAction] = ServerRpc_requestPlayerAction;
        serverMessageHandlerMap[ServerPackets.congrateWinner] = ServerRpc_congrateWinner;

        // connect to the server
        Connect();
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
    public void CreateRoom()
    {
        if (loginUid == -1)
        {
            UIManager.instance.BidenSays("you havn't logged in!");
            return;
        }
        ClientSend.RpcSend(ClientPackets.createRoom, (Packet p) =>
        {

        }, null);
    }
    public void Bid(int amount)
    {
        if (playRoom == null)
        {
            UIManager.instance.BidenSays("You havn't joined any room yet!");
            return;
        }
        if (loginUid == -1)
        {
            UIManager.instance.BidenSays("you havn't logged in!");
            return;
        }
        if (!playRoom.Bid(amount))
        {
            UIManager.instance.BidenSays($"invalid bet with amount {amount}");
            return;
        }
    }
    public void CheckOrFold()
    {
        if (playRoom == null)
        {
            UIManager.instance.BidenSays("You havn't joined any room yet!");
            return;
        }
        if (loginUid == -1)
        {
            UIManager.instance.BidenSays("you havn't logged in!");
            return;
        }
        playRoom.CheckOrFold();
    }
    public void UseTimeCard()
    {
        if (playRoom == null)
        {
            UIManager.instance.BidenSays("You havn't joined any room yet!");
            return;
        }
        if (loginUid == -1)
        {
            UIManager.instance.BidenSays("you havn't logged in!");
            return;
        }
        playRoom.UseTimeCard();
    }
    public void QuitRoom()
    {
        if (playRoom == null)
        {
            UIManager.instance.BidenSays("You havn't joined any room yet!");
            return;
        }
        if (loginUid == -1)
        {
            UIManager.instance.BidenSays("you havn't logged in!");
            return;
        }
        playRoom.Quit();
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
        }
        else
            UIManager.instance.BidenSays("login fail");
    }
    void ServerRpc_registerCallback(Packet p)
    {
        if (p.ReadBool())
            UIManager.instance.BidenSays($"register success with uid {p.ReadInt()}");
        else
            UIManager.instance.BidenSays("register fail");
    }
    void ServerRpc_joinRoomCallback(Packet p)
    {
        if (p.ReadBool())
        {
            int joinRoomIdx = p.ReadInt();
            UIManager.instance.BidenSays($"you have successfully joined room {joinRoomIdx}");
            UIManager.instance.roomUI.gameObject.SetActive(true);
            playRoom = UIManager.instance.roomUI.GetComponent<Room>();
        }
        else
        {
            UIManager.instance.BidenSays($"you fail to join a room");
            //UIManager.instance.roomUI.gameObject.SetActive(false);
            //playRoom = null;
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
            UIManager.instance.roomUI.gameObject.SetActive(false);
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
    void ServerRpc_syncAccountStat(Packet p)
    {
        // temporarily not used
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
                    for (int i = 0; i < 2; i++)
                    {
                        PokerCard temp = new PokerCard();
                        if (p.ReadBool())
                            temp = new PokerCard((PokerCard.Decors)p.ReadInt(), p.ReadInt());
                        else
                            temp.notRevealed = true;
                        if (player.hand.Count > i)
                            player.hand[i] = player.hand[i].notRevealed ? temp : player.hand[i];
                        else
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
    #endregion
}
