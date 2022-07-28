using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public class PlayerInGameStat
    {
        public int uid;
        public int moneyInPocket;
        public int moneyInPot;
        public bool hasBidThisRound;
        public bool hasFolded;
        public bool ifAllIn;
        public bool hasQuited;
        public List<PokerCard> hand = new List<PokerCard>();
        public int timeCard;
    }
    public bool myTurn = false;

    public float timer = 0;
    public int roundNum = 0;
    public int timeCardPerRound = 1;
    public int smallBlindMoneyNum = 1;
    public int currentActivePlayer = 0;
    public int currentSmallBlind = 0;
    public bool gamePaused = false;

    public enum roundState
    {
        bidRound0 = 0,
        bidRound1 = 1,
        bidRound2 = 2,
        bidRound3 = 3,
        roundFinished = 4,
    }
    roundState curState;

    public List<PokerCard> flopTurnRiver = new List<PokerCard>();
    public List<PlayerInGameStat> players = new List<PlayerInGameStat>();
    RoomUI myUI;
    private void Awake()
    {
        myUI = this.GetComponent<RoomUI>();
        flopTurnRiver.Capacity = 5;
        players.Capacity = 8;
    }
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
            if (p != null && p.moneyInPot > max)
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
            stat.moneyInPocket >= amount &&
            (stat.moneyInPot + amount >= HighestBid() || amount >= stat.moneyInPocket))
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
        ClientSend.RpcSend(ClientPackets.quitRoom, (Packet p) =>
        {
            p.Write(Client.instance.loginUid);
        }, null);
        return true;
    }
    public void UpdateUI()
    {
        myUI.ShowPlayer();
        myUI.ShowPlayerHand();
        myUI.ShowPoker();
        myUI.ShowStat();
    }
}
