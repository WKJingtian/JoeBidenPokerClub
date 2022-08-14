using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BidenUI : MonoBehaviour
{
    public Text bidenSays;
    private Stack<string> bidenWantToSay = new Stack<string>();
    public static readonly float s_bidenSpeechCd = 3.0f;
    private float bidenClock;
    public void BidenSays(string says = "我的牌太多了!!!")
    {
        bidenWantToSay.Push(says);
    }
    private void Update()
    {
        bidenClock -= Time.deltaTime;
        if (bidenWantToSay.Count > 0 &&
            bidenClock <= 0)
        {
            bidenClock = s_bidenSpeechCd;
            string says = bidenWantToSay.Peek();
            bidenSays.text = says;
            bidenWantToSay.Pop();
        }
    }
}
