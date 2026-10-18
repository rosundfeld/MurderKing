using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoveBurnFlashingBarUI : MonoBehaviour
{
    private const string IS_FLASHING = "IsFlashing";
    [SerializeField] private StoveCounter stoveCounter;
    private Animator animator;

    private void Start()
    {
        stoveCounter.OnProgressChanged += stoveCounter_OnProgressChanged;
        animator.SetBool(IS_FLASHING, false);
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void stoveCounter_OnProgressChanged(object sender, IHasProgress.OnProgressChangedEventArgs e)
    {
        float burnShowProgressAmount = .5f;
        if (stoveCounter.IsFried() && e.progressNormalized >= burnShowProgressAmount)
        {
            animator.SetBool(IS_FLASHING, true);
        }
        else
        {
            animator.SetBool(IS_FLASHING, false);
        }
    }
}
