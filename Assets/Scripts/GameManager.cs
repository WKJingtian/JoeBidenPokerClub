using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
        {
            Destroy(this);
            return;
        }
    }

    private void Start()
    {
        Debug.LogWarning("Welcome to Joe Biden's Poker Club");
        UIManager.instance.Start();
        LoginUI login = UIManager.instance.OpenUI(UIManager.UIPrefab.loginUI).GetComponent<LoginUI>();
        if (PlayerPrefs.HasKey("userAccount") &&
            PlayerPrefs.HasKey("userPassword"))
        {
            login.accountField.text = PlayerPrefs.GetString("userAccount");
            login.passwordField.text = PlayerPrefs.GetString("userPassword");
        }
    }
}
