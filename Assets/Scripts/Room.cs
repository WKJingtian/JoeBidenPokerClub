using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public class PlayerInGameStat
    {
        public int uid;
        public int moneyInPoxket;
        public int moneyInPot;
        public bool hasBidThisRound;
        public bool hasFolded;
        public bool ifAllIn;
        public bool hasQuited;
        public List<PokerCard> hand = new List<PokerCard>();
        public int timeCard;
    }
    bool myTurn = false;

    private float pauseBetweenRounds = 3;
    private float roundTime = 30;
    private float timer = 0;
    private int roundNum = 0;
    private int timeCardPerRound = 1;
    private int smallBlindMoneyNum = 1;
    private int currentActivePlayer = 0;
    private int currentSmallBlind = 0;
    private bool gamePaused = false;

    public enum roundState
    {
        bidRound0 = 0,
        bidRound1 = 1,
        bidRound2 = 2,
        bidRound3 = 3,
        roundFinished = 4,
    }
    roundState curState;

    List<PokerCard> deck;
    List<PokerCard> flopTurnRiver;
    List<PlayerInGameStat> players;
    public PlayerInGameStat GetPlayerInfoById(int id)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null && players[i].uid == id)
                return players[i];
        }
        return null;
    }
    public int HighestBid()
    {
        int max = 0;
        foreach (var p in players)
        {
            if (p.moneyInPot > max)
                max = p.moneyInPot;
        }
        return max;
    }
    public bool Bid(int amount)
    {
        if (!myTurn)
        {
            Debug.LogError("Now is not your time to make a move");
            return false;
        }
        var stat = GetPlayerInfoById(Client.instance.loginUid);
        if (stat != null && !stat.hasQuited && !stat.hasFolded &&
            stat.moneyInPoxket >= amount &&
            (stat.moneyInPot + amount >= HighestBid() || amount == stat.moneyInPoxket))
        {
            ClientSend.RpcSend(ClientPackets.bid, (Packet p) =>
            {
                p.Write(Client.instance.loginUid);
                p.Write(amount);
            }, null);
            return true;
        }
        else return false;
    }
    public bool CheckOrFold()
    {
        if (!myTurn)
        {
            Debug.LogError("Now is not your time to make a move");
            return false;
        }
        var stat = GetPlayerInfoById(Client.instance.loginUid);
        if (stat != null && !stat.hasQuited && !stat.hasFolded)
        {
            ClientSend.RpcSend(ClientPackets.fold, (Packet p) =>
            {
                p.Write(Client.instance.loginUid);
            }, null);
            return stat.moneyInPot >= HighestBid();
        }
        else return false;
    }
    public bool UseTimeCard()
    {
        if (!myTurn)
        {
            Debug.LogError("Now is not your time to make a move");
            return false;
        }
        var stat = GetPlayerInfoById(Client.instance.loginUid);
        if (stat != null && !stat.hasQuited && !stat.hasFolded &&
            stat.timeCard > 0)
        {
            ClientSend.RpcSend(ClientPackets.useTimeCard, (Packet p) =>
            {
                p.Write(Client.instance.loginUid);
            }, null);
            return true;
        }
        else return false;
    }
    public bool Quit()
    {
        var stat = GetPlayerInfoById(Client.instance.loginUid);
        if (stat != null && !stat.hasQuited)
        {
            ClientSend.RpcSend(ClientPackets.quitRoom, (Packet p) =>
            {
                p.Write(Client.instance.loginUid);
            }, null);
            return true;
        }
        else return false;
    }
}
