using UnityEngine;

//Everything you see and hear from the gun. Nobody calls it: it only reacts to PlayerGun events.
public class PlayerGunVisuals : MonoBehaviour
{
    [SerializeField] private PlayerGun playerGun;
    [Tooltip("Optional")]
    [SerializeField] private ParticleSystem muzzleFlash;

    private void Start()
    {
        playerGun.OnWeaponShoot += PlayerGun_OnWeaponShoot;
        playerGun.OnWeaponReloaded += PlayerGun_OnWeaponReloaded;
        playerGun.OnWeaponEmpty += PlayerGun_OnWeaponEmpty;
    }

    private void OnDestroy()
    {
        playerGun.OnWeaponShoot -= PlayerGun_OnWeaponShoot;
        playerGun.OnWeaponReloaded -= PlayerGun_OnWeaponReloaded;
        playerGun.OnWeaponEmpty -= PlayerGun_OnWeaponEmpty;
    }

    private void PlayerGun_OnWeaponShoot(WeaponShootData shootData)
    {
        WeaponSO weaponSO = shootData.weapon.Data;
        Vector3 muzzle = shootData.origin + Vector3.up * 1f + shootData.direction * 0.6f;
        //A single projectile flies the whole range, a burst of pellets scatters
        float minDistanceFactor = weaponSO.pelletCount > 1 ? 0.7f : 1f;

        for (int i = 0; i < weaponSO.pelletCount; i++)
        {
            float spread = Random.Range(-weaponSO.pelletSpreadAngle, weaponSO.pelletSpreadAngle);
            Vector3 pelletDirection = Quaternion.Euler(0f, spread, 0f) * shootData.direction;
            HotDogPellet.Spawn(muzzle, pelletDirection, shootData.distance * Random.Range(minDistanceFactor, 1f));
        }

        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
        SoundManager.Instance.PlayShotDogSound(shootData.origin);
    }

    private void PlayerGun_OnWeaponReloaded()
    {
        SoundManager.Instance.PlayReloadSound(transform.position);
    }

    private void PlayerGun_OnWeaponEmpty()
    {
        SoundManager.Instance.PlayEmptyGunSound(transform.position);
    }
}
