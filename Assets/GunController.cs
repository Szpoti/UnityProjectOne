using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GunController : MonoBehaviour
{
    [Header("Gun Settings")]
    public float fireRate = 0.1f;
    public int clipSize = 30;
    public int reservedAmmoCapacity = 270;
    public float damage = 10f;
    public float range = 1000f;
    public Camera fpsCam;
    public GameObject muzzleFlashLight;
    public GameObject impactEffect;

    //Variables that change throughout code
    bool _canShoot;
    int _currentAmmoInClip;
    int _ammoInReserve;

    //Muzzle Flash
    public Image muzzleFlashImage1; 
    public Image muzzleFlashImage2;
    public Sprite[] flashes;

    //Aiming
    public Vector3 normalLocalPosition;
    public Vector3 aimingLocalPosition;

    public float aimSmoothing = 10;

    [Header("Mouse Settings")]
    public float mouseSensitivity = 50f;
    Vector2 _currentRotation;
    public float weaponSwayAmount = -2;

    //Weapon Recoil
    public bool randomizeRecoil;
    public Vector2 randomRecoilConstraits;
    //You only need to assign this if randomizeRecoil is false
    public Vector2[] recoilPattern;

    private void Start()
    {
        _currentAmmoInClip = clipSize;
        _ammoInReserve = reservedAmmoCapacity;
        _canShoot = true;
        muzzleFlashLight.SetActive(false);
    }

    private void Update()
    {
        DetermineAim();
        DetermineRotation();
        if(Input.GetMouseButton(0) && _canShoot && _currentAmmoInClip > 0)
        {
            _canShoot = false;
            _currentAmmoInClip--;
            StartCoroutine(ShootGun());
        }
        else if(Input.GetKeyDown(KeyCode.R) && _currentAmmoInClip < clipSize && _ammoInReserve > 0)
        {
            int amountNeeded = clipSize - _currentAmmoInClip;
            if(amountNeeded >= _ammoInReserve)
            {
                _currentAmmoInClip += _ammoInReserve;
                _ammoInReserve -= amountNeeded;
            }
            else
            {
                _currentAmmoInClip = clipSize;
                _ammoInReserve -= amountNeeded;
            }
        }
    }

    void DetermineRotation()
    {
        Vector2 mouseAxis = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        mouseAxis *= mouseSensitivity;
        _currentRotation += mouseAxis;

        transform.localPosition += (Vector3)mouseAxis * weaponSwayAmount / 1000;

    }

    void DetermineAim()
    {
        Vector3 target = normalLocalPosition;
        if (Input.GetMouseButton(1)) target = aimingLocalPosition;

        Vector3 desiredPosition = Vector3.Lerp(transform.localPosition, target, Time.deltaTime * aimSmoothing);

        transform.localPosition = desiredPosition;
    }

    private void DetermineRecoil()
    {
        transform.localPosition -= Vector3.forward * 0.1f;

        if(randomizeRecoil)
        {
            float xRecoil = Random.Range(-randomRecoilConstraits.x, randomRecoilConstraits.x);
            float yRecoil = Random.Range(-randomRecoilConstraits.y, randomRecoilConstraits.y);

            Vector2 recoil = new Vector2(xRecoil, yRecoil);

            _currentRotation += recoil;
        }
        else
        {
            int currentStep = clipSize + 1 - _currentAmmoInClip;
            currentStep = Mathf.Clamp(currentStep, 0, recoilPattern.Length - 1);

            _currentRotation += recoilPattern[currentStep];
        }
    }

    IEnumerator ShootGun()
    {
        DetermineRecoil();
        StartCoroutine(MuzzleFlash());

        RayCastForEnem();
        yield return new WaitForSeconds(fireRate);
        _canShoot = true;
    }

    void RayCastForEnem()
    {
        RaycastHit hit;
        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range))
        {
            try
            {

                Enemy enemy = hit.transform.GetComponent<Enemy>();
                if(enemy != null)
                {
                    Debug.Log(hit.collider.transform.gameObject.layer);
                    Debug.Log(LayerMask.NameToLayer("EnemyHead"));
                    if(hit.collider.transform.gameObject.layer == LayerMask.NameToLayer("EnemyHead"))
                    {
                        Debug.Log("Headshot");
                        enemy.TakeDamage(damage * 5);
                    }
                    else
                    {
                        Debug.Log("Bodyshot");
                        enemy.TakeDamage(damage);
                    }
                }

                
                Rigidbody rb = hit.transform.GetComponent<Rigidbody>();
                if(rb != null)
                {
                    rb.constraints = RigidbodyConstraints.None;
                    rb.AddForce(transform.parent.transform.forward * 500);
                }

                Debug.Log(hit.transform.name);

                GameObject go = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(go, 2f);
            }
            catch (Exception)
            {

                
            }
        }
    }

    IEnumerator MuzzleFlash()
    {
        Sprite randomFlash = flashes[Random.Range(0, flashes.Length)];
        muzzleFlashImage1.sprite = randomFlash;
        muzzleFlashImage2.sprite = randomFlash;
        muzzleFlashImage1.color = Color.white;
        muzzleFlashImage2.color = Color.white;
        muzzleFlashLight.SetActive(true);
        yield return new WaitForSeconds(0.05f);
        muzzleFlashImage1.sprite = null;
        muzzleFlashImage1.color = new Color(0, 0, 0, 0);
        muzzleFlashImage2.sprite = null;
        muzzleFlashImage2.color = new Color(0, 0, 0, 0);
        muzzleFlashLight.SetActive(false);
    }
}
