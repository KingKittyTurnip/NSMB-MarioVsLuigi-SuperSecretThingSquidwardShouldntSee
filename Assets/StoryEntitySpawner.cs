using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class StoryEntitySpawner : MonoBehaviour
{
    public string prefab;
    public void Spawn()
    {
        if(PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Instantiate(prefab, transform.position, Quaternion.identity);
            Destroy(gameObject); //this is so we can attach a model to this object, and it appears as if this object turns into the object. 
        }
    }
}
