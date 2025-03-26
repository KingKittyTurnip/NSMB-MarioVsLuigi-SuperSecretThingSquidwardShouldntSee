using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThatBackgroundBox : MonoBehaviour
{
    public bool lightning;
    public SpriteRenderer box;
    private void Update()
    {
        if(GameManager.Instance.musicState == Enums.MusicState.OVERTIME)
        {
            //sex.enabled
            box.color = Color.Lerp(box.color, new Color(0, 0, 0, .75f), Time.deltaTime * 5);

        }
        else
        {
            box.color = new Color(0, 0, 0, 0);
        }
    }
    private void FixedUpdate()
    {
        if (lightning && Random.value > .995f)
        {
            box.color = new Color(1, 1, 1, .75f);
        }
    }
}
