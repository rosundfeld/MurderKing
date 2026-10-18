using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class StoveBurnWarningUI : MonoBehaviour
{
    [SerializeField] private GameObject warningGameObject;
    [SerializeField] private StoveCounter stoveCounter;

    private void Start()
    {
        stoveCounter.OnProgressChanged += stoveCounter_OnProgressChanged;
        Hide();
    }

    private void stoveCounter_OnProgressChanged(object sender, IHasProgress.OnProgressChangedEventArgs e)
    {
        float burnShowProgressAmount = .5f;
        if (stoveCounter.IsFried() && e.progressNormalized >= burnShowProgressAmount)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    private void Show()
    {
        warningGameObject.SetActive(true);
    }

    private void Hide()
    {
        warningGameObject.SetActive(false);
    }
}
