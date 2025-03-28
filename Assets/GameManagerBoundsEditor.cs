using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Events;

public class GameManagerBoundsEditor : MonoBehaviour
{
    public int levelMinTileX, levelMinTileY, levelWidthTile, levelHeightTile;
    public float cameraMinY, cameraHeightY, cameraMinX = -1000, cameraMaxX = 1000;
    private int orig_levelMinTileX, orig_levelMinTileY, orig_levelWidthTile, orig_levelHeightTile;
    private float orig_cameraMinY, orig_cameraHeightY, orig_cameraMinX = -1000, orig_cameraMaxX = 1000;
    public UnityEvent onTrigger;
    public bool resetOnLeave;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject == GameManager.Instance.localPlayer)
        {
            if (resetOnLeave)
            {
                orig_levelMinTileX = GameManager.Instance.levelMinTileX;
                orig_levelMinTileY = GameManager.Instance.levelMinTileY;
                orig_levelWidthTile = GameManager.Instance.levelWidthTile;
                orig_levelHeightTile = GameManager.Instance.levelHeightTile;
                orig_cameraMinY = GameManager.Instance.cameraMinY;
                orig_cameraHeightY = GameManager.Instance.cameraHeightY;
                orig_cameraMinX = GameManager.Instance.cameraMinX;
                orig_cameraMaxX= GameManager.Instance.cameraMaxX;
            }
            onTrigger.Invoke();
            GameManager.Instance.levelMinTileX = levelMinTileX;
            GameManager.Instance.levelMinTileY = levelMinTileY;
            GameManager.Instance.levelWidthTile = levelWidthTile;
            GameManager.Instance.levelHeightTile = levelHeightTile;
            GameManager.Instance.cameraMinY = cameraMinY;
            GameManager.Instance.cameraHeightY = cameraHeightY;
            GameManager.Instance.cameraMinX = cameraMinX;
            GameManager.Instance.cameraMaxX = cameraMaxX;

            if (!resetOnLeave)
            {
                Destroy(gameObject);
            }
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.gameObject == GameManager.Instance.localPlayer)
        {
            if (resetOnLeave)
            {
                GameManager.Instance.levelMinTileX = orig_levelMinTileX;
                GameManager.Instance.levelMinTileY = orig_levelMinTileY;
                GameManager.Instance.levelWidthTile = orig_levelWidthTile;
                GameManager.Instance.levelHeightTile = orig_levelHeightTile;
                GameManager.Instance.cameraMinY = orig_cameraMinY;
                GameManager.Instance.cameraHeightY = orig_cameraHeightY;
                GameManager.Instance.cameraMinX = orig_cameraMinX;
                GameManager.Instance.cameraMaxX = orig_cameraMaxX;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 size = new(levelWidthTile / 2f, levelHeightTile / 2f);
        Vector3 origin = new(GetLevelMinX() + (levelWidthTile / 4f), GetLevelMinY() + (levelHeightTile / 4f), 1);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(origin, size);

        size = new Vector3(levelWidthTile / 2f, cameraHeightY);
        origin = new Vector3(GetLevelMinX() + (levelWidthTile / 4f), cameraMinY + (cameraHeightY / 2f), 1);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(origin, size);
    }


    private float? middleX, minX, minY, maxX, maxY;
    public float GetLevelMiddleX()
    {
        if (middleX == null)
            middleX = (GetLevelMaxX() + GetLevelMinX()) / 2;
        return (float)middleX;
    }
    public float GetLevelMinX()
    {
        if (minX == null)
            minX = (levelMinTileX * GameManager.Instance.tilemap.transform.localScale.x) + GameManager.Instance.tilemap.transform.position.x;
        return (float)minX;
    }
    public float GetLevelMinY()
    {
        if (minY == null)
            minY = (levelMinTileY * GameManager.Instance.tilemap.transform.localScale.y) + GameManager.Instance.tilemap.transform.position.y;
        return (float)minY;
    }
    public float GetLevelMaxX()
    {
        if (maxX == null)
            maxX = ((levelMinTileX + levelWidthTile) * GameManager.Instance.tilemap.transform.localScale.x) + GameManager.Instance.tilemap.transform.position.x;
        return (float)maxX;
    }
    public float GetLevelMaxY()
    {
        if (maxY == null)
            maxY = ((levelMinTileY + levelHeightTile) * GameManager.Instance.tilemap.transform.localScale.y) + GameManager.Instance.tilemap.transform.position.y;
        return (float)maxY;
    }
}
