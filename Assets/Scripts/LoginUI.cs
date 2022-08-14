using UnityEngine;
using UnityEngine.UI;
using System;

public class LoginUI : MonoBehaviour
{
    public Text accountField;
    public Text passwordField;
    public void SendLogIn()
    {
        PlayerPrefs.SetString("userAccount", accountField.text);
        PlayerPrefs.SetString("userPassword", passwordField.text);
        try
        {
            Client.instance.DoLogin(Int32.Parse(accountField.text), passwordField.text);
        }
        catch (Exception e)
        {
            UIManager.instance.BidenSays(e.ToString());
        }
    }
    public void OpenRegisterUI()
    {
        UIManager.instance.OpenUI(UIManager.UIPrefab.registerUI);
    }
}
