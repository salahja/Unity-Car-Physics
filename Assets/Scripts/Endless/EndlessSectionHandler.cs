using UnityEngine;
using System.Collections;
using System.Collections.Generic;


 

public class EndlessSectionHandler: MonoBehaviour
{
    Transform playerCarTransform;
    
    void Start()
    {
        playerCarTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        float distanceToPlayer = transform.position.z - playerCarTransform.position.z;
        float lerpPercentage;

        // Calculate lerp based on player position relative to the section
        if (distanceToPlayer > 0) // Player is behind the section
        {
            // Rise as player approaches (from 150 units behind to 100 units behind)
            lerpPercentage = 1.0f - Mathf.Clamp01((distanceToPlayer - 100) / 50.0f);
        }
        else // Player is ahead of the section
        {
            // Lower as player moves away (from 50 units ahead to 100 units ahead)
            lerpPercentage = 1.0f - Mathf.Clamp01((-distanceToPlayer - 50) / 50.0f);
        }

        // Interpolate section's y-position
        transform.position = Vector3.Lerp(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(transform.position.x, 0, transform.position.z),
            lerpPercentage);
    }
}
 
public class EndlessSectionHandlerss : MonoBehaviour
{
   Transform playerCarTransform;
   void Start()
   {
      playerCarTransform = GameObject.FindGameObjectWithTag("Player").transform;

   }

   void Update()
   {
      float distanceToPlayer = transform.position.z - playerCarTransform.position.z;
      float lerpPercentage = 1.0f - ((distanceToPlayer - 100) / 150.0f);
   lerpPercentage = Mathf.Clamp01(lerpPercentage);

  transform.position = Vector3.Lerp(new Vector3(transform.position.x, -10, transform.position.z), new Vector3(transform.position.x, 0, transform.position.z), lerpPercentage);
   }
}
 
 
public class EndlessSectionHandlerSSS : MonoBehaviour
{
   Transform playerCarTransform;
   void Start()
   {
      playerCarTransform = GameObject.FindGameObjectWithTag("Player").transform;

   }

   void Update()
   {
      float distanceToPlayer = transform.position.z - playerCarTransform.position.z;
      float lerpPercentage = 1.0f - ((distanceToPlayer - 100) / 150.0f);
   lerpPercentage = Mathf.Clamp01(lerpPercentage);

  transform.position = Vector3.Lerp(new Vector3(transform.position.x, -5, transform.position.z), new Vector3(transform.position.x, 0, transform.position.z), lerpPercentage);
   }
}
 