using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
public class ProfileUI : MonoBehaviour
{
    [SerializeField] private Text idField;
    [SerializeField] private Text nameField;
    [SerializeField] private Text cashField;
    [SerializeField] private Text winRateField;
    [SerializeField] private Text cashWinField;
    [SerializeField] private Text cashLoseField;
    [SerializeField] private Text roundCountField;
    [SerializeField] private Text playRateField;
    public void SetUser(int id, string name, int cash, float winRate,
        int cashWin, int cashLose, int havePlayed, float playRate)
    {
        idField.text = id.ToString();
        nameField.text = name;
        cashField.text = cash.ToString();
        winRateField.text = winRate.ToString();
        cashWinField.text = cashWin.ToString();
        cashLoseField.text = cashLose.ToString();
        roundCountField.text = havePlayed.ToString();
        playRateField.text = playRate.ToString();
    }
    public void Close()
    {
        UIManager.instance.CloseUI(UIManager.UIPrefab.profileUI);
    }
}