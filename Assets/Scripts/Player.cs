using System;
using Cinemachine;
using Cinemachine.Utility;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Player : Entity
{
    private new Rigidbody rigidbody;
    private Animator anim;
    private float bodyToFeetDist;
    
    #region Config
    
    [SerializeField] private Camera cam;
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintSpeedMult = 1.5f;
    [SerializeField] private float jumpHeight = 6f;
    [SerializeField] private float jumpSpeed = 80f;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float shootCooldownSeconds = 0.75f;
    [SerializeField] private float shootPower = 500f;
    [SerializeField] private AudioClip shootSound;

    #endregion
    
    #region Animation
    
    private static readonly int AnimStateHurt = Animator.StringToHash("Hurt");
    private const string AnimStateDeathName = "Death";
    private static readonly int AnimStateDeath = Animator.StringToHash(AnimStateDeathName);
    
    private static readonly int AnimParamMove = Animator.StringToHash("move");
    private static readonly int AnimParamJump = Animator.StringToHash("jump");
    private static readonly int AnimParamShoot = Animator.StringToHash("shoot");
    
    #endregion
    
    #region State
    
    private Vector3 nextMoveDir = Vector3.zero;
    private bool isSprinting;
    private bool jumpNextPhysUpdate;
    private bool shootNextPhysUpdate;
    private float remainingShootCooldownSeconds;
    
    #endregion

    public override void Awake()
    {
        base.Awake();
        
        rigidbody = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        bodyToFeetDist = GetComponent<Collider>().bounds.extents.y;
    }

    public void Update()
    {
        /* FIXME ??? */ Cursor.lockState = CursorLockMode.Locked;
        
        #region Wait death animation & die

        var animStateInfo = anim.GetCurrentAnimatorStateInfo(0);
        
        if (isDeadOrDying && animStateInfo.IsName(AnimStateDeathName) && animStateInfo.normalizedTime >= 0.5f)
        {
            Destroy(gameObject);
            return;
        }
        
        #endregion
        
        if (isDeadOrDying || rigidbody == null || cam == null)
        {
            return;
        }
        
        #region Cooldowns

        if (remainingShootCooldownSeconds > 0f)
        {
            remainingShootCooldownSeconds -= Time.deltaTime;
        }
        
        #endregion

        #region Moving

        var inputHor = Input.GetAxis("Horizontal");
        var inputVer = Input.GetAxis("Vertical");
        nextMoveDir = new Vector3(inputHor, 0f, inputVer).normalized;
        nextMoveDir = GetHorizontalCameraTransform().TransformDirection(nextMoveDir);
        isSprinting = Input.GetKey(KeyCode.LeftShift);
        
        var isMoving = !nextMoveDir.AlmostZero();

        int animMoveState = (isMoving, isSprinting) switch
        {
            (false, false) => 0,
            (true, false) => 1,
            (true, true) => 2,
            _ => 999
        };

        anim.SetInteger(AnimParamMove, animMoveState);
        
        #endregion
        
        #region Jumping
        
        if (Input.GetButtonDown("Jump") && IsOnGround())
        {
            jumpNextPhysUpdate = true;
        }
        
        #endregion
        
        #region Shooting
        
        if (remainingShootCooldownSeconds <= 0f && Input.GetMouseButtonDown(0))
        {
            shootNextPhysUpdate = true;
        }
        
        #endregion
    } 

    public void FixedUpdate()
    {
        if (isDeadOrDying || rigidbody == null || cam == null)
        {
            return;
        }

        var isOnGround = IsOnGround();

        var move = nextMoveDir;
        var moveSpeed = isSprinting ? sprintSpeedMult * walkSpeed : walkSpeed;
        move *= moveSpeed * Time.fixedDeltaTime;
        move.y = 0f;
        nextMoveDir = Vector3.zero;
        
        #region Walking
        
        var isWalking = isOnGround && !move.AlmostZero();
        
        if (isWalking)
        {
            rigidbody.position += move;
            RotateCharacterTo(move);
        }
        
        #endregion
        
        #region Jumping
        
        if (isOnGround && jumpNextPhysUpdate)
        {
            var momentum = jumpSpeed * move;
            if (isSprinting) momentum /= sprintSpeedMult / 2f;
            var jumpForce = jumpHeight * Vector3.up + momentum;
            rigidbody.AddForce(jumpForce, ForceMode.VelocityChange);
            
            anim.SetTrigger(AnimParamJump);

            jumpNextPhysUpdate = false;
        }
        
        #endregion

        #region Shooting

        if (shootNextPhysUpdate)
        {
            Shoot();
            shootNextPhysUpdate = false;
        }

        #endregion
    }

    private void RotateCharacterTo(Vector3 face)
    {
        var charLookAt = face;
        charLookAt.Scale(new Vector3(1f, 0f, 1f));
        rigidbody.rotation = Quaternion.LookRotation(charLookAt);
    }

    private Transform GetHorizontalCameraTransform()
    {
        var camTransformHor = cam.transform;
        camTransformHor.position.Scale(new Vector3(1f, 0f, 1f));
        return camTransformHor;
    }

    private bool IsOnGround()
    {
        return Physics.Raycast(
            transform.position + new Vector3(0f, bodyToFeetDist, 0f),
            Vector3.down, 
            bodyToFeetDist + 0.1f
        );
    }

    private void Shoot()
    {
        remainingShootCooldownSeconds = shootCooldownSeconds;
        
        var camTransformHor = GetHorizontalCameraTransform();
        var spawnPos = transform.position + 1.1f * camTransformHor.forward + new Vector3(0f, 1f, 0f);
        var spawnRot = camTransformHor.rotation;
        
        var bullet = Instantiate(bulletPrefab, spawnPos, spawnRot);
        var bulletCtrl = bullet.GetComponent<CatBulletController>();
        var bulletVelocity = shootPower * (camTransformHor.forward + new Vector3(0f, 0.2f, 0f));
        bulletCtrl.shooter = gameObject;
        bulletCtrl.rigidbody.AddForce(bulletVelocity);

        RotateCharacterTo(new Vector3(bulletVelocity.x, 0f, bulletVelocity.z));
        anim.SetTrigger(AnimParamShoot);
        AudioSource.PlayClipAtPoint(shootSound, transform.position);
    }

    protected override void PlayHurtAnimation()
    {
        anim.Play(AnimStateHurt, -1, 0f);
    }
    
    protected override void PlayDeathAnimation()
    {
        anim.Play(AnimStateDeath, -1, 0f);
    }
}
