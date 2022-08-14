using UnityEngine;
using UnityEngine.UI;
using System;

public class RegisterUI : MonoBehaviour
{
    public Text nameField;
    public Text passwordField;
    public void SendRegister()
    {
        PlayerPrefs.SetString("userPassword", passwordField.text);
        try
        {
            Client.instance.DoRegister(nameField.text, passwordField.text);
        }
        catch (Exception e)
        {
            UIManager.instance.BidenSays(e.ToString());
        }
    }
    public void CloseSelf()
    {
        UIManager.instance.CloseUI(UIManager.UIPrefab.registerUI);
    }
}
