using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoxCub : Entity
{
    private Animator anim;

    #region Animation

    private static readonly int AnimParamPetting = Animator.StringToHash("petting");

    #endregion

    public override void Awake()
    {
        base.Awake();
        anim = GetComponent<Animator>();
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }
        
        anim.SetBool(AnimParamPetting, true);
    }

    public void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }
        
        anim.SetBool(AnimParamPetting, false);
    }
}
