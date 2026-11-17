using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Local player's gun: weapon name and icon, ammo counter and one icon per shell. Driven only by PlayerGun events.
public class PlayerGunUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI shellsText;
    [SerializeField] private Transform shellContainer;
    [SerializeField] private Image shellTemplate;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private Image weaponIconImage;
    [SerializeField] private Color shellFullColor = new Color(0.85f, 0.35f, 0.2f);
    [SerializeField] private Color shellEmptyColor = new Color(1f, 1f, 1f, 0.15f);

    private PlayerGun playerGun;
    private readonly List<Image> shellImageList = new List<Image>();

    private void Awake()
    {
        shellTemplate.gameObject.SetActive(false);
    }

    private void Start()
    {
        //Hidden until the local player's gun exists
        Hide();
        if (PlayerGun.LocalInstance != null)
        {
            Bind(PlayerGun.LocalInstance);
        }
        else
        {
            PlayerGun.OnLocalInstanceSpawned += Bind;
        }
    }

    private void OnDestroy()
    {
        PlayerGun.OnLocalInstanceSpawned -= Bind;
        if (playerGun != null)
        {
            playerGun.OnActiveWeaponChanged -= PlayerGun_OnActiveWeaponChanged;
            playerGun.OnAmmoChanged -= PlayerGun_OnAmmoChanged;
        }
    }

    private void Bind(PlayerGun playerGun)
    {
        this.playerGun = playerGun;
        playerGun.OnActiveWeaponChanged += PlayerGun_OnActiveWeaponChanged;
        playerGun.OnAmmoChanged += PlayerGun_OnAmmoChanged;

        //Initial state, read once
        Weapon activeWeapon = playerGun.GetActiveWeapon();
        PlayerGun_OnActiveWeaponChanged(activeWeapon);
        PlayerGun_OnAmmoChanged(activeWeapon.GetAmmoData());
        Show();
    }

    private void PlayerGun_OnActiveWeaponChanged(Weapon weapon)
    {
        //Both are new fields: null until they are wired in the Inspector, and the ammo counter must keep working
        if (weaponNameText != null)
        {
            weaponNameText.text = weapon.Data.weaponName;
        }
        if (weaponIconImage != null)
        {
            weaponIconImage.sprite = weapon.Data.weaponIcon;
            weaponIconImage.enabled = weapon.Data.weaponIcon != null;
        }
    }

    private void PlayerGun_OnAmmoChanged(AmmoData ammoData)
    {
        shellsText.text = $"{ammoData.ammoInClip}/{ammoData.clipSize}";

        //Clip sizes differ between weapons: grow the pool as needed and hide what is left over
        while (shellImageList.Count < ammoData.clipSize)
        {
            shellImageList.Add(Instantiate(shellTemplate, shellContainer));
        }
        for (int i = 0; i < shellImageList.Count; i++)
        {
            shellImageList[i].gameObject.SetActive(i < ammoData.clipSize);
            shellImageList[i].color = i < ammoData.ammoInClip ? shellFullColor : shellEmptyColor;
        }
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
