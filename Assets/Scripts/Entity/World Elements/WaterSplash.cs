using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class WaterSplash : MonoBehaviour {

    public int widthTiles = 64, pointsPerTile = 8, splashWidth = 2;
    public float heightTiles = 1;
    public float tension = 40, kconstant = 1.5f, damping = 0.92f, splashVelocity = 50f, resistance = 0f, animationSpeed = 1f;
    public string splashParticle;

    private Texture2D heightTex;
    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock properties;
    private Color32[] colors;
    private float[] pointHeights, pointVelocities;
    private float animTimer;
    private int totalPoints;
    private bool initialized;
    public bool isWater;


    private void Awake() {
        Initialize();
    }
    private void OnValidate() {
        ValidationUtility.SafeOnValidate(Initialize);
    }

    private void Initialize() {
        if (this == null)
            return;

        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        totalPoints = widthTiles * pointsPerTile;
        pointHeights = new float[totalPoints];
        pointVelocities = new float[totalPoints];

        heightTex = new Texture2D(totalPoints, 1, TextureFormat.RGBA32, false);

        Color32 gray = new(128, 0, 0, 255);
        colors = new Color32[totalPoints];
        for (int i = 0; i < totalPoints; i++)
            colors[i] = gray;

        heightTex.Apply();

        collider.offset = new(0, heightTiles * 0.25f - 0.2f);
        collider.size = new(widthTiles * 0.5f, heightTiles * 0.5f - 0.1f);
        spriteRenderer.size = new(widthTiles * 0.5f, heightTiles * 0.5f + 0.5f);

        properties = new();
        properties.SetTexture("Heightmap", heightTex);
        properties.SetFloat("WidthTiles", widthTiles);
        properties.SetFloat("Height", heightTiles);
        spriteRenderer.SetPropertyBlock(properties);
    }

    public void FixedUpdate() {
        if (!initialized) {
            Initialize();
            initialized = true;
        }
        if(GameManager.Instance.musicState == Enums.MusicState.OVERTIME)
        {
            Initialize();
        }
        float delta = Time.fixedDeltaTime;

        bool valuesChanged = false;

        for (int i = 0; i < totalPoints; i++) {
            float height = pointHeights[i];
            pointVelocities[i] += tension * -height;
            pointVelocities[i] *= damping;
        }
        for (int i = 0; i < totalPoints; i++) {
            pointHeights[i] += pointVelocities[i] * delta;
        }
        for (int i = 0; i < totalPoints; i++) {
            float height = pointHeights[i];

            pointVelocities[i] -= kconstant * delta * (height - pointHeights[(i + totalPoints - 1) % totalPoints]); //left
            pointVelocities[i] -= kconstant * delta * (height - pointHeights[(i + totalPoints + 1) % totalPoints]); //right
        }
        for (int i = 0; i < totalPoints; i++) {
            byte newR = (byte) (Mathf.Clamp01((pointHeights[i] / 20f) + 0.5f) * 255f);
            valuesChanged |= colors[i].r != newR;
            colors[i].r = newR;
        }

        if (valuesChanged) {
            heightTex.SetPixels32(colors);
            heightTex.Apply(false);
        }

        animTimer += animationSpeed * Time.fixedDeltaTime;
        animTimer %= 8;
        properties.SetFloat("TextureIndex", animTimer);
        spriteRenderer.SetPropertyBlock(properties);
    }
    public void OnTriggerEnter2D(Collider2D collider) {
        if (isWater)
        {
            PlayerController con = collider.GetComponent<PlayerController>();
            if (con != null)
            {
                if (!con.WaterSplashList.Contains(this))
                {
                    con.WaterSplashList.Add(this);
                }
            }
        }

        BoxCollider2D myCollider = GetComponent<BoxCollider2D>();
        Rigidbody2D body = collider.attachedRigidbody;
        if (body != null)
        {
            if (body.worldCenterOfMass.y > transform.position.y + myCollider.offset.y + (myCollider.size.y / 2) - .5f)
            {
                Instantiate(Resources.Load(splashParticle), collider.transform.position, Quaternion.identity);
                float power = body.velocity.y;
                float tile = (transform.InverseTransformPoint(collider.transform.position).x / widthTiles + 0.25f) * 2f;
                int px = (int)(tile * totalPoints);
                for (int i = -splashWidth; i <= splashWidth; i++)
                {
                    int pointsX = (px + totalPoints + i) % totalPoints;
                    pointVelocities[pointsX] = -splashVelocity * power;
                }
            }
        }
        else
        {
            if (collider.transform.position.y > transform.position.y + myCollider.offset.y + (myCollider.size.y / 2) - .5f)
            {
                Instantiate(Resources.Load(splashParticle), collider.transform.position, Quaternion.identity);
                float power = -1;
                float tile = (transform.InverseTransformPoint(collider.transform.position).x / widthTiles + 0.25f) * 2f;
                int px = (int)(tile * totalPoints);
                for (int i = -splashWidth; i <= splashWidth; i++)
                {
                    int pointsX = (px + totalPoints + i) % totalPoints;
                    pointVelocities[pointsX] = -splashVelocity * power;
                }
            }
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (isWater)
        {
            PlayerController con = collision.GetComponent<PlayerController>();
            if (con != null)
            {
                if (con.WaterSplashList.Contains(this))
                {
                    con.WaterSplashList.Remove(this);
                    con.stillDrownTimer = .05f;
                }
            }
            BoxCollider2D myCollider = GetComponent<BoxCollider2D>();
            Rigidbody2D body = collision.attachedRigidbody;
            if (body != null)
            {
                if (body.worldCenterOfMass.y > transform.position.y + myCollider.offset.y + (myCollider.size.y / 2) - .5f)
                {
                    Instantiate(Resources.Load(splashParticle), collision.transform.position, Quaternion.identity);
                    float power = body.velocity.y;
                    float tile = (transform.InverseTransformPoint(collision.transform.position).x / widthTiles + 0.25f) * 2f;
                    int px = (int)(tile * totalPoints);
                    for (int i = -splashWidth; i <= splashWidth; i++)
                    {
                        int pointsX = (px + totalPoints + i) % totalPoints;
                        pointVelocities[pointsX] = -splashVelocity * power;
                    }
                }
            }
            else
            {
                if (collision.transform.position.y > transform.position.y + myCollider.offset.y + (myCollider.size.y / 2) - .5f)
                {
                    Instantiate(Resources.Load(splashParticle), collision.transform.position, Quaternion.identity);
                    float power = -1;
                    float tile = (transform.InverseTransformPoint(collision.transform.position).x / widthTiles + 0.25f) * 2f;
                    int px = (int)(tile * totalPoints);
                    for (int i = -splashWidth; i <= splashWidth; i++)
                    {
                        int pointsX = (px + totalPoints + i) % totalPoints;
                        pointVelocities[pointsX] = -splashVelocity * power;
                    }
                }
            }
        }

    }
    public void OnTriggerStay2D(Collider2D collision) {

        if (isWater)
        {
            PlayerController con = collision.GetComponent<PlayerController>();
            if (con != null)
            {
                if (!con.WaterSplashList.Contains(this))
                {
                    con.WaterSplashList.Add(this);
                }
            }
        }
        if (collision.attachedRigidbody == null)
            return;

        collision.attachedRigidbody.velocity *= 1-Mathf.Clamp01(resistance);
    }
}
