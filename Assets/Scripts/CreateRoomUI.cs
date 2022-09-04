using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
public class CreateRoomUI : MonoBehaviour
{
    [SerializeField] private Text nameField;
    [SerializeField] private Text sbField;
    [SerializeField] private Text timeField;
    [SerializeField] private Text rptcField;
    [SerializeField] private Text playerLimitField;
    [SerializeField] private Text obLimitField;

    private void OnEnable()
    {
        nameField.text = $"{Client.instance.loginUid}'s game";
        sbField.text = $"1";
        timeField.text = $"30";
        rptcField.text = $"1";
        playerLimitField.text = $"8";
        obLimitField.text = $"4";
    }

    public void CreateRoom()
    {
        int sb = 1, time = 30, rptc = 1, p = 8, o = 4;
        try
        {
            sb = Int32.Parse(sbField.text);
        } catch (Exception e) { }
        try
        {
            time = Int32.Parse(timeField.text);
        } catch (Exception e) { }
        try
        {
            rptc = Int32.Parse(rptcField.text);
        } catch (Exception e) { }
        try
        {
            p = Int32.Parse(playerLimitField.text);
        } catch (Exception e) { }
        try
        {
            o = Int32.Parse(obLimitField.text);
        } catch (Exception e) { }
        Client.instance.CreateRoom(nameField.text,
            sb, time, rptc, p, o);
        UIManager.instance.CloseUI(UIManager.UIPrefab.roomCreateUI);
    }
    public void Close()
    {
        UIManager.instance.CloseUI(UIManager.UIPrefab.roomCreateUI);
    }
}