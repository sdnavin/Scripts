using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum EnemyType { Normal, Mothership, None, All };

public class JuniverseBattleship : MonoBehaviour, IHittable
{
    [SerializeField]
    float _totalHealth;
    public MothershipCannon[] ShipSockets;
    [SerializeField]
    WeaponDictionry[] ShuttleWeapons;
    [SerializeField]
    [Range(0, 100)]
    protected int _damageFactor = 100;
    [SerializeField]
    MarkerData _markerData;
    [SerializeField]
    GameObject _explosionPrefab;
    [SerializeField]
    float _sphereCastRadius;
    [SerializeField]
    float velocity = 0;
    public EnemyType TargetEnemyType;
    [SerializeField]
    GameObject _trail;


    public bool canShoot = true;
    bool _isGameOver;

    float StartLockTime;
    float _curHealth;
    UIMarker _marker;
    List<GameObject> _enemies = new List<GameObject>();

    void Awake()
    {
        for (int i = 0; i < ShipSockets.Length; i++)
        {
            ShipSockets[i].weaponPrefab = ShuttleWeapons[i].Weapon;
            ShipSockets[i].potentialTargets = _enemies;
        }
        _curHealth = _totalHealth;
        GameManager.Instance.OnEnemySpawned += Instance_OnEnemySpawned;
        //TODO add marker
    }

    private void Instance_OnEnemySpawned(EnemyBehaviour obj)
    {
        if (TargetEnemyType == EnemyType.All)
            _enemies.Add(obj.gameObject);
        else if (TargetEnemyType == EnemyType.Mothership && obj is SpawnerMothership)
            _enemies.Add(obj.gameObject);
        else if (TargetEnemyType == EnemyType.Normal && obj is DroneEnemy)
            _enemies.Add(obj.gameObject);
    }

    public void TakeDamage(float damage, Vector3 HitPoint, Transform firingPoint)
    {
        if (!firingPoint)
            return;
    }

    public void SetVelocity(float velocity)
    {
        this.velocity = velocity;
    }

    void OnPlayerRespawn()
    {
        _isGameOver = false;
    }
    void OnGameOver()
    {
        _isGameOver = true;
    }

    public void DestroyShip(Vector3 hit)
    {
        if (_marker != null)
            Destroy(_marker.gameObject);

        GameObject explosion = Instantiate(_explosionPrefab, transform.position, Quaternion.identity) as GameObject;
        Destroy(explosion, 3.0f);
        Destroy(this.gameObject, 2.0f);
    }

    void OnDestroy()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.UnsubscribeToGameOver(OnGameOver);
            GameManager.Instance.UnsubscribeToPlayerRespawning(OnPlayerRespawn);
        }
    }

    void Update()
    {
        if (velocity > 0)
            transform.Translate(Vector3.forward * Time.deltaTime * velocity, Space.Self);

        for (int i = 0; i < ShipSockets.Length; i++)
        {
            ShipSockets[i].canShoot = canShoot;
            ShipSockets[i].damageFactor = _damageFactor;
        }
    }

    public void FixedUpdate()
    {
        if (_isGameOver)
            return;

        for (int i = 0; i < _enemies.Count; i++)
        {
            if (_enemies[i] == null)
            {
                _enemies.RemoveAt(i);
                i--;
            }
        }
    }

    public void EnableTrail()
    {
        if (_trail != null)
            _trail.SetActive(true);
    }

    void OnEnable()
    {
        if (_trail != null)
            _trail.SetActive(false);
    }
}
