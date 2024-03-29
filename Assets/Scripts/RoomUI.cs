using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomUI : MonoBehaviour
{
    [SerializeField] Transform playerRoot;
    [SerializeField] Transform pokerRoot;
    [SerializeField] Transform activityToken;
    [SerializeField] List<Sprite> decorSprites;
    [SerializeField] Text totalPotText;
    [SerializeField] Text clockText;
    [SerializeField] Text playerTimeCard;
    [SerializeField] Text roomRoundNum;
    Room room;
    private void Awake()
    {
        room = GetComponent<Room>();
    }
    float timer = 9999;
    private void Update()
    {
        timer -= Time.deltaTime;
        clockText.text = ((int)timer).ToString();

        if (activityToken.gameObject.activeInHierarchy !=
            room.myTurn)
            activityToken.gameObject.SetActive(room.myTurn);
    }
    private int MianPlayerPos()
    {
        int playerPos = -1;
        for (int i = 0; i < room.players.Capacity; i++)
        {
            if (i < room.players.Count &&
                room.players[i] != null &&
                room.players[i].uid == Client.instance.loginUid)
            {
                playerPos = i;
                roomRoundNum.text = $"you have\n{room.players[i].timeCard} timecards";
                break;
            }
        }
        if (playerPos == -1)
        {
            Debug.LogWarning($"cannot find current player in room");
            return 0;
        }
        return playerPos;
    }
    public void ShowPoker()
    {
        int idx = 1;
        foreach (var c in room.flopTurnRiver)
        {
            Transform cardRoot = pokerRoot.Find($"pc0{idx}");
            Image decorImg = cardRoot.Find("decor").GetComponent<Image>();
            Text pointTxt = cardRoot.Find("point").GetComponent<Text>();
            if (c.notRevealed)
            {
                decorImg.color = Color.black;
                pointTxt.text = "?";
            }
            else
            {
                decorImg.color = Color.white;
                decorImg.sprite = decorSprites[(int)c.decor];
                pointTxt.text = c.point.ToString();
            }
            idx++;
        }
        while (idx <= room.flopTurnRiver.Capacity)
        {
            Transform cardRoot = pokerRoot.Find($"pc0{idx}");
            Image decorImg = cardRoot.Find("decor").GetComponent<Image>();
            Text pointTxt = cardRoot.Find("point").GetComponent<Text>();
            decorImg.color = Color.black;
            pointTxt.text = "?";
            idx++;
        }
    }
    public void ShowPlayer()
    {
        int prize = 0;
        int playerPos = MianPlayerPos();
        for (int i = 0; i < room.players.Capacity; i++)
        {
            int tempPos = i - playerPos;
            while (tempPos < 0)
                tempPos += 8;
            Transform pRoot = playerRoot.Find($"p{tempPos % 8 + 1}");
            if (i >= room.players.Count ||
                room.players[i] == null)
            {
                pRoot.Find("uid").GetComponent<Text>().text = "";
                pRoot.Find("pocket").GetComponent<Text>().text = "";
                pRoot.Find("pot").GetComponent<Text>().text = "";
                pRoot.Find("statue").GetComponent<Text>().text = "";
                pRoot.Find("action").GetComponent<Text>().text = "";
                pRoot.Find("position").GetComponent<Text>().text = "";
                pRoot.Find("hand1").gameObject.SetActive(false);
                pRoot.Find("hand2").gameObject.SetActive(false);
            }
            else
            {
                pRoot.Find("uid").GetComponent<Text>().text = room.players[i].uid.ToString();
                pRoot.Find("pocket").GetComponent<Text>().text = $"tot:{room.players[i].moneyInPocket}";
                pRoot.Find("pot").GetComponent<Text>().text = $"tot:{room.players[i].moneyInPot}";
                prize += room.players[i].moneyInPot;
                string statue = "in game";
                if (room.players[i].ifAllIn)
                    statue = "all in";
                else if (room.players[i].hasQuited)
                    statue = "quitted";
                else if (room.players[i].hasFolded)
                    statue = "folded";
                pRoot.Find("statue").GetComponent<Text>().text = room.players[i].name;
                pRoot.Find("action").GetComponent<Text>().text = statue;
                int disFromSb = 0;
                string position = "";
                int ii = i, loopCount = 0;
                while (ii != room.currentSmallBlind && loopCount < 10)
                {
                    ii--;
                    if (ii == -1)
                        ii = room.players.Capacity - 1;
                    if (room.players[ii] != null)
                        disFromSb++;
                    loopCount++;
                }
                switch (disFromSb)
                {
                    case 0:
                        position = "small bline";
                        break;
                    case 1:
                        position = "big bline";
                        break;
                    default:
                        break;
                }
                pRoot.Find("position").GetComponent<Text>().text = position;
                pRoot.Find("hand1").gameObject.SetActive(true);
                pRoot.Find("hand2").gameObject.SetActive(true);
            }
        }
        totalPotText.text = $"�׳�:{prize}";
    }
    public void ShowPlayerHand()
    {
        int playerPos = MianPlayerPos();
        for (int i = 0; i < room.players.Capacity; i++)
        {
            int tempPos = i - playerPos;
            while (tempPos < 0)
                tempPos += 8;
            Transform pRoot = playerRoot.Find($"p{tempPos % 8 + 1}");
            Image decorImg1 = pRoot.Find("hand1/decor").GetComponent<Image>();
            Text pointTxt1 = pRoot.Find("hand1/point").GetComponent<Text>();
            Image decorImg2 = pRoot.Find("hand2/decor").GetComponent<Image>();
            Text pointTxt2 = pRoot.Find("hand2/point").GetComponent<Text>();
            if (i >= room.players.Count ||
                room.players[i] == null)
            {
                decorImg1.color = Color.black;
                pointTxt1.text = "?";
                decorImg2.color = Color.black;
                pointTxt2.text = "?";
            }
            else
            {
                if (room.players[i].hand.Count > 0 &&
                    !room.players[i].hand[0].notRevealed)
                {
                    decorImg1.sprite = decorSprites[(int)room.players[i].hand[0].decor];
                    decorImg1.color = Color.white;
                    pointTxt1.text = room.players[i].hand[0].point.ToString();
                }
                else
                {
                    decorImg1.color = Color.black;
                    pointTxt1.text = "?";
                }
                if (room.players[i].hand.Count > 1 &&
                    !room.players[i].hand[1].notRevealed)
                {
                    decorImg2.sprite = decorSprites[(int)room.players[i].hand[1].decor];
                    decorImg2.color = Color.white;
                    pointTxt2.text = room.players[i].hand[1].point.ToString();
                }
                else
                {
                    decorImg2.color = Color.black;
                    pointTxt2.text = "?";
                }
            }
        }
    }
    public void ShowStat()
    {
        roomRoundNum.text = $"Round\nNo. {room.roundNum}";
        timer = room.timer;
    }
}
