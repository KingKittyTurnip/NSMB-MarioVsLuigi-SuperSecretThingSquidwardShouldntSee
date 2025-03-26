//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//
//public class Portal : MonoBehaviour
//{
//    public Portal other;
//    public Animator anim;
//    public SpriteRenderer sprite;
//    public List<Rigidbody2D> bodiesInside = new List<Rigidbody2D>();
//    
//    private void OnTriggerEnter2D(Collider2D collision)
//    {
//        if (other == null)
//            return;
//        Rigidbody2D body = collision.attachedRigidbody;
//        if (body != null)
//        {
//            Vector2 velocity = body.velocity;
//            body.position = other.transform.position + (other.transform.up * .25f);
//
//            Vector2 thisToOther = other.transform.position - transform.position;
//
//            float dotProduct = Vector2.Dot(thisToOther, transform.right);
//
//            if(dotProduct > 0)
//            {
//                body.velocity = RotateVector2(velocity, (other.transform.rotation.eulerAngles.z + 180) - transform.rotation.eulerAngles.z);
//            }
//            else
//            {
//                body.velocity = RotateVector2(velocity, (other.transform.rotation.eulerAngles.z + 180) - transform.rotation.eulerAngles.z);
//            }
//        }
//    }
//    private void Update()
//    {
//        anim.SetBool("Portal", other);
//    }
//
//    public static Vector2 RotateVector2(Vector2 vector, float angle)
//    {
//        // Convert the angle from degrees to radians
//        float radian = angle * Mathf.Deg2Rad;
//
//        // Calculate the sine and cosine of the angle
//        float sin = Mathf.Sin(radian);
//        float cos = Mathf.Cos(radian);
//
//        // Rotate the vector
//        float x = vector.x * cos - vector.y * sin;
//        float y = vector.x * sin + vector.y * cos;
//
//        return new Vector2(x, y);
//    }
//
//}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    public Portal other;
    public Animator anim;
    public SpriteRenderer sprite;
    public List<Rigidbody2D> bodiesInside = new List<Rigidbody2D>();
    public LayerMask includeMask, excludeMask, blankMask;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (other == null)
            return;

        // Add the Rigidbody2D to the list of objects inside the portal
        bodiesInside.Add(collision.attachedRigidbody);

        // Modify the collision layers
        collision.attachedRigidbody.excludeLayers = excludeMask;
        collision.attachedRigidbody.includeLayers = includeMask;
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        bodiesInside.Remove(collision.attachedRigidbody);

        // Reset collision layers when exiting the portal
        collision.attachedRigidbody.excludeLayers = blankMask;
        collision.attachedRigidbody.includeLayers = blankMask;
    }

    private void Update()
    {
        // Check and set portal animation state
        anim.SetBool("Portal", other);

        foreach (Rigidbody2D body in bodiesInside)
        {
            if (body != null)
            {
                Vector2 portalToBody = body.position - (Vector2)(transform.position - (transform.up * 0.25f));

                // Check if the object has crossed the event horizon (portal right direction)
                float dotProduct = Vector2.Dot(portalToBody, transform.up); // Check which side of the horizon the object is on

                if (dotProduct < 0) // Object is in front of the portal
                {
                    TeleportObject(body);
                }
            }
        }
    }

    private void TeleportObject(Rigidbody2D body)
    {
        // Move the object to the other portal's position
        body.position = other.transform.position - (other.transform.up * 0.25f);

        // Calculate the new velocity based on the rotation difference
        Vector2 velocity = body.velocity;
        body.velocity = RotateVector2(new Vector2(velocity.x, velocity.y), (other.transform.rotation.eulerAngles.z + 180) - transform.rotation.eulerAngles.z);
    }

    public static Vector2 RotateVector2(Vector2 vector, float angle)
    {
        // Convert the angle from degrees to radians
        float radian = angle * Mathf.Deg2Rad;

        // Calculate the sine and cosine of the angle
        float sin = Mathf.Sin(radian);
        float cos = Mathf.Cos(radian);

        // Rotate the vector
        float x = vector.x * cos - vector.y * sin;
        float y = vector.x * sin + vector.y * cos;

        return new Vector2(x, y);
    }
}
