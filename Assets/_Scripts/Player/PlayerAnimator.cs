using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerAnimator : NetworkBehaviour
{
    //---------------CONSTANTS-----------------
    private const string IS_WALKING = "IsWalking";

    //---------------PRIVATE VARIABLES-----------------
    private Animator animator;
    [SerializeField] private Player player;

    //---------------UNITY METHODS-----------------
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!IsOwner) return;
        
        animator.SetBool(IS_WALKING, player.IsWalking());
    }
}
