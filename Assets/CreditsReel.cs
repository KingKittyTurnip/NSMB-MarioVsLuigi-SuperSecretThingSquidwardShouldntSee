using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreditsReel : MonoBehaviour
{
    public RectTransform rect;
    void Update()
    {
        rect.pivot -= Vector2.up * Time.deltaTime / 10;
    }
}
