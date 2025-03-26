using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class BossFireball : MonoBehaviourPun, IPunObservable
{
    [PunRPC]
    public void OnFireballShot(Vector2 throwAngle)
    {
        Rigidbody2D firebody = GetComponent<Rigidbody2D>();

        firebody.velocity = throwAngle;
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
        }
        else
        {
            transform.position = (Vector3)stream.ReceiveNext();
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController con = collision.GetComponent<PlayerController>();
        if (con)
        {
            con.photonView.RPC(nameof(PlayerController.Powerdown), Photon.Pun.RpcTarget.All, false);
            PhotonNetwork.Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerController con = collision.collider.GetComponent<PlayerController>();
        if (con)
        {
            con.photonView.RPC(nameof(PlayerController.Powerdown), Photon.Pun.RpcTarget.All, false);
        }
        PhotonNetwork.Destroy(gameObject);
    }

    public void OnDestroy()
    {
        if (!GameManager.Instance.gameover)
            Instantiate(Resources.Load("Prefabs/Particle/" + "FireballWall"), transform.position, Quaternion.identity);
    }
}
