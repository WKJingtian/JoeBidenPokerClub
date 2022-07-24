using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomUI : MonoBehaviour
{
    [SerializeField] Transform playerRoot;
    [SerializeField] Transform pokerRoot;
    [SerializeField] List<Sprite> decorSprites;
    Vector2[] playerPos =
    {
        new Vector2(-180,  135), new Vector2(-60,  135),new Vector2(60,  135),new Vector2(180,  135),
        new Vector2(-180, -135), new Vector2(-60, -135),new Vector2(60, -135),new Vector2(180, -135),
    };

    public void ShowPoker()
    {

    }
    public void ShowPlayer()
    {

    }
}
