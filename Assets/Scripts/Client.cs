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

    private Packet receivedPacket;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
        {
            Debug.LogWarning($"gameobject {gameObject.name} has a client component on it, but client is already initialized by {instance.gameObject.name}");
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
            Debug.Log($"I am already connected with id {id}");
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
            Debug.LogWarning("Disconnected from server");
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
            Console.WriteLine(e.Message);
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
                Console.WriteLine($"client {id} does not have a valid socket/stream");
                return;
            }
            stream.BeginWrite(p.ToArray(), 0, p.Length(), toRead, null);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
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
        ClientSend.RpcSend(ClientPackets.login, (Packet p) =>
        {
            p.Write(uid);
            p.Write(password);
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
        Debug.Log($"connection success with id {p.ReadString()}");
        id = p.ReadInt();
    }
    void ServerRpc_loginCallback(Packet p)
    {
        if (p.ReadBool())
            Debug.LogWarning($"login success with id {p.ReadInt()}");
        else
            Debug.LogWarning("login fail");
    }
    void ServerRpc_registerCallback(Packet p)
    {
        if (p.ReadBool())
            Debug.LogWarning($"register success with uid {p.ReadInt()}");
        else
            Debug.LogWarning("register fail");
    }
    #endregion
}
