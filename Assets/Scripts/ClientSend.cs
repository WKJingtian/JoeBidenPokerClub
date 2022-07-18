using System;
using System.Collections.Generic;
using UnityEngine;

public class ClientSend
{
    public static void RpcSend(ClientPackets rpc, Action<Packet> toWrite, AsyncCallback? toRead = null)
    {
        Packet p = new Packet(rpc);
        toWrite(p);
        p.WriteLength();
        if (p.Length() > Packet.s_maxBufferSize)
        {
            Console.WriteLine($"RPC packet {(int)rpc} has exceeded the max length of a single packet");
            return;
        }
        Client.instance.Send(p, toRead);
    }
}
