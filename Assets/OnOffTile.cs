using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnOffTile : MonoBehaviour
{
    public BoxCollider2D coll;
    public bool flip;
    public SpriteRenderer sprite;
    public Sprite onSprite, offSprite;
    private void Update()
    {
        bool on = GameManager.Instance.onOffState;
        if (flip)
        {
            on = !on;
        }
        coll.enabled = on;
        sprite.sprite = on ? onSprite : offSprite;
    }
}
