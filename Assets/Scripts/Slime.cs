using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public enum SlimeState { Idle, Walk, Jump, Attack, Damage }

public class Slime : Entity
{
    #region Config

    [SerializeField] protected float damageToPlayer = 10f;

    [SerializeField] protected float viewDist = 5f;
    [SerializeField] protected float viewAngle = 120f;
    [SerializeField] protected float patrolDestUpdateCdSecondsMin = 2f;
    [SerializeField] protected float patrolDestUpdateCdSecondsMax = 6f;
    [SerializeField] protected float patrolRange = 5f;

    #endregion
    
    #region Animation

    private static readonly string AnimStateIdleName = "Idle";
    private static readonly int AnimStateIdle = Animator.StringToHash(AnimStateIdleName);
    
    private static readonly string AnimStateWalkName = "Walk";
    private static readonly int AnimStateWalk = Animator.StringToHash(AnimStateWalkName);
    
    private static readonly int AnimStateHurt = Animator.StringToHash("Hurt");
    
    private static readonly string AnimStateDeathName = "Death";
    private static readonly int AnimStateDeath = Animator.StringToHash(AnimStateDeathName);
    
    private static readonly int AnimParamSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimParamDamage = Animator.StringToHash("Damage");
    private static readonly int AnimParamDamageType = Animator.StringToHash("DamageType");
    
    #endregion

    #region State

    protected SlimeState slimeState = SlimeState.Idle;
    protected float remPatrolDestUpdateCdSeconds;

    #endregion
    
    protected Animator anim;
    protected NavMeshAgent navAgent;
    protected GameObject player;

    public override void Awake()
    {
        base.Awake();
        
        anim = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        player = GameObject.Find("Player");
    }

    public void Update()
    {
        #region Wait death animation & die

        var animStateInfo = anim.GetCurrentAnimatorStateInfo(0);
        
        if (isDeadOrDying && animStateInfo.IsName(AnimStateDeathName) && animStateInfo.normalizedTime >= 0.7f)
        {
            Destroy(gameObject);
            return;
        }
        
        #endregion

        if (isDeadOrDying)
        {
            return;
        }
        
        UpdateSlimeState();

        if (remPatrolDestUpdateCdSeconds > 0f) remPatrolDestUpdateCdSeconds -= Time.deltaTime;

        UpdateDestination();
    }

    protected void UpdateSlimeState()
    {
        var animStateInfo = anim.GetCurrentAnimatorStateInfo(0);
        
        switch (slimeState)
        {
            case SlimeState.Idle:
                if (animStateInfo.IsName(AnimStateIdleName))
                {
                    return;
                }
                
                StopNavAgent();
                
                break;
            
            case SlimeState.Walk:
                if (animStateInfo.IsName(AnimStateWalkName))
                {
                    return;
                }
                
                if (navAgent.remainingDistance < navAgent.stoppingDistance)
                {
                    slimeState = SlimeState.Idle;
                    return;
                }
                
                StartNavAgent();
                
                anim.SetFloat(AnimParamSpeed, navAgent.velocity.magnitude);
                
                break;
        }
    }

    protected void StartNavAgent()
    {
        if (!navAgent.isStopped)
        {
            return;
        }
        
        navAgent.isStopped = false;
        navAgent.updateRotation = true;
    }
    
    protected void StopNavAgent()
    {
        if (navAgent.isStopped)
        {
            return;
        }
        
        navAgent.isStopped = true;
        navAgent.updateRotation = false;
        anim.SetFloat(AnimParamSpeed, 0);
    }

    protected void UpdateDestination()
    {
        var playerPos = SearchPlayerPos();
        
        if (playerPos.HasValue)
        {
            StartMovingTo(playerPos.Value);
            return;
        }
        
        if (remPatrolDestUpdateCdSeconds > 0f)
        {
            return;
        }

        remPatrolDestUpdateCdSeconds = Random.Range(patrolDestUpdateCdSecondsMin, patrolDestUpdateCdSecondsMax);
        
        var randDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
        var randDest = transform.position + patrolRange * randDir;
        StartMovingTo(randDest);
    }

    protected void StartMovingTo(Vector3 dest)
    {
        navAgent.SetDestination(dest);
        slimeState = SlimeState.Walk;
    }

    protected Vector3? SearchPlayerPos()
    {
        var playerPos = player.transform.position;
        var slimeToPlayerVec = playerPos - transform.position;
        var angle = Vector3.Angle(transform.forward, slimeToPlayerVec);

        if (angle > viewAngle / 2f)
        {
            return null;
        }
        
        if (Physics.SphereCast(transform.position, 1f, slimeToPlayerVec, out var hit, viewDist) 
            && hit.collider.CompareTag("Player"))
        {
            return playerPos;
        }
        

        return null;
    }

    protected void OnCollisionEnter(Collision other)
    {
        if (!other.collider.CompareTag("Player"))
        {
            return;
        }

        var otherEntity = other.collider.GetComponentInParent<Entity>();

        if (otherEntity == null)
        {
            return;
        }
        
        otherEntity.Hurt(damageToPlayer);
    }

    protected override void PlayHurtAnimation()
    {
        anim.SetInteger(AnimParamDamageType, 0);
        anim.SetTrigger(AnimParamDamage);
    }

    protected override void PlayDeathAnimation()
    {
        StopNavAgent();
        
        transform.LookAt(player.transform);
        
        anim.SetInteger(AnimParamDamageType, 2);
        anim.SetTrigger(AnimParamDamage);
    }
}
