using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class EggmansBalls : MonoBehaviour
{
    public EggMove eggman;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<PlayerController>())
        {
            GetComponent<PlayerController>().photonView.RPC(nameof(PlayerController.Powerdown), RpcTarget.All, false);
            if(eggman != null)
            {
                eggman.OnDealDamage();
            }
        }
    }
}
