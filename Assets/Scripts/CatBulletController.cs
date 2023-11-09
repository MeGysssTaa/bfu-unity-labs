using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CatBulletController : MonoBehaviour
{
    public new Rigidbody rigidbody;

    public GameObject shooter;
    
    [SerializeField] private GameObject terrainHitParticles;
    [SerializeField] private GameObject livingHitParticles;
    [SerializeField] private float damage;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        transform.Rotate(new Vector3(1000f * Time.deltaTime, 0f, 0f));
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject == shooter)
        {
            return;
        }
        
        var entity = other.gameObject.GetComponentInParent<Entity>();
        
        if (entity == null)
        {
            // Hit terrain.
            Instantiate(terrainHitParticles, transform.position, Quaternion.identity);
        }
        else
        {
            // Hit entity.
            entity.Hurt(damage);
            Instantiate(livingHitParticles, transform.position, Quaternion.identity);
        }
        
        Destroy(gameObject);
    }
}
