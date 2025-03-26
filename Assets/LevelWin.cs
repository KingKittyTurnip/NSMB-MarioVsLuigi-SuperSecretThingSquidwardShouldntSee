using ExitGames.Client.Photon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using NSMB.Utils;

public class LevelWin : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject == GameManager.Instance.localPlayer)
        {
            GameManager.Instance.WinPlayer(GameManager.Instance.localPlayer.GetComponent<PlayerController>());
            Destroy(gameObject);
        }
    }
}
