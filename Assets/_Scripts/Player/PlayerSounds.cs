using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSounds : MonoBehaviour
{
    //---------------PRIVATE VARIABLES-----------------
    private Player player;
    private float footstepTimer;
    private float footstepTimerMax = 0.1f;

    //---------------UNITY METHODS-----------------
    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Update()
    {
        HandleFootSteps();
    }

    private void HandleFootSteps()
    {
        footstepTimer -= Time.deltaTime;
        if (player.IsWalking())
        {
            if (footstepTimer <= 0f)
            {
                footstepTimer = footstepTimerMax;
                SoundManager.Instance.PlayFootstepSound(player.transform.position);
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }
}
