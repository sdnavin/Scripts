using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.Vehicles.Aeroplane;

public class FriendlyAI : MonoBehaviour
{

    PlayerBehaviour _player;

    GameObject _currentTarget = null;
    AeroplaneAiControl _aiControl;
    Rigidbody _rigidBody = null;
    EnemyMovmentController _enemyMovementController = null;

    [SerializeField]
    private float _distanceToStopMovementDirectly = 500;

    [SerializeField]
    private float _distanceToTravelBehindPlayer = 1000;

    [SerializeField]
    private float _highSpeedMultiplier = 1.3f;

    [SerializeField]
    private float _ultraSpeedMultiplier = 1.3f;

    [SerializeField]
    private float _timeToKeepRolling = 5f;

    [SerializeField]
    private float _timeToAvoidRolling = 2f;

    [SerializeField]
    private float _rollingSpeed = 4f;

    [SerializeField]
    private float _rollingChance = 0.5f;

    [SerializeField]
    private float _distanceForChasedRolling = 800;

    [SerializeField]
    private bool _canRoll = true;

    [SerializeField]
    private bool _canAvoidObstacles = true;

    [SerializeField]
    private bool _canAvoidPlayer = true;

    [SerializeField]
    private bool _useProjectileSpeedForInterception = true;

    [SerializeField]
    private bool _useInterception = true;

    [SerializeField]
    private bool _destroyOnImpactWithPlayer = true;


    [SerializeField]
    private bool _canMoveAwayFromEnemy = true;

    [SerializeField]
    private int _damageToPlayerOnImpact = 60;

    [SerializeField]
    private LayerMask _obstacleAvoidanceLayer;

    [SerializeField]
    float _returnToTargetCooldown;

    [SerializeField]
    [Tooltip("When having a special target and been hit by player")]
    float _findAnotherAsteroidCooldown;

    GameObject _tmpTarget;
    Coroutine _returnToTargetCoroutine;
    Coroutine _findAnotherAsteroidCoroutine;
    bool _isTargetLocked = false;


    public bool protectPlayer = true;
    [SerializeField]
    float protectionDistance = 40;
    [SerializeField]
    float protectionDispersion = 2;

    [SerializeField]
    float showOffDistance = 1000;
    [SerializeField]
    float showOffDispersion = 10;
    [SerializeField]
    float showTeleportTime = 10000;
    [SerializeField]
    float showLerpSpeed = 100;

    Rigidbody _playerRigidbody;

    float lastTeleportTime = float.MaxValue;
    static Dictionary<EnemyBehaviour, int> assignedEnemies = new Dictionary<EnemyBehaviour, int>();

    public float? assignedAngle = null;


    // Use this for initialization
    void Start()
    {
        _player = PlayerBehaviour.instance;
        if (_currentTarget == null)
            _currentTarget = _player.gameObject;
        _aiControl = GetComponent<AeroplaneAiControl>();
        _rigidBody = GetComponent<Rigidbody>();
        _enemyMovementController = GetComponent<EnemyMovmentController>();
        _playerRigidbody = PlayerBehaviour.instance.GetComponent<Rigidbody>();
        Teleport(assignedAngle.HasValue ? assignedAngle.Value : Random.Range(0.0f, 360.0f), _player.transform.position + _player.transform.forward * 100);
        StartCoroutine("AILogic");
    }

    public void SetTarget(bool isPlayer, GameObject target)
    {
        if (_isTargetLocked)
            return;

        if (!isPlayer && target != null)
        {
            _tmpTarget = target;
            if (target.CompareTag("AsteroidsBelt"))
            {
                AsteroidEnemy asteroid = null;
                while (asteroid != null)
                {
                    asteroid = target.transform.GetChild(Random.Range(10, 100)).GetComponent<AsteroidEnemy>();
                    if (!asteroid.gameObject.activeInHierarchy)
                        asteroid = null;
                }
                _currentTarget = target.GetComponentInChildren<AsteroidEnemy>().gameObject;

                if (_findAnotherAsteroidCoroutine != null)
                    _findAnotherAsteroidCoroutine = StartCoroutine(FindAnotherAsteroid());
            }
            else
                _currentTarget = target;
        }
        else if (_tmpTarget != null)
        {
            if (_returnToTargetCoroutine != null)
            {
                StopCoroutine(_returnToTargetCoroutine);
                _returnToTargetCoroutine = StartCoroutine(ReturnToTarget(_tmpTarget));
            }
            else
                _returnToTargetCoroutine = StartCoroutine(ReturnToTarget(_tmpTarget));

            _currentTarget = _player.gameObject;
        }
    }

    public void Flee(GameObject target)
    {
        _currentTarget = target;
        _isTargetLocked = true;
    }

    IEnumerator FindAnotherAsteroid()
    {
        while (gameObject.activeInHierarchy)
        {
            yield return new WaitForSeconds(_findAnotherAsteroidCooldown);
            SetTarget(false, _tmpTarget);
        }
    }

    IEnumerator ReturnToTarget(GameObject target)
    {
        yield return new WaitForSeconds(_returnToTargetCooldown);
        if (target != null)
            _currentTarget = _tmpTarget;
        _returnToTargetCoroutine = null;
    }

    bool GetNearestObstacle(out Vector3 center, out float radius, Vector3 movementDir, float speed)
    {
        Vector3 myLocation = GetComponent<DroneEnemy>().GetFiringSocketsCenter();

        Vector3 lookAhead = movementDir * speed * 5f;
        Vector3 ahead = myLocation + lookAhead;

        Ray ray = new Ray(myLocation, lookAhead.normalized);
        //RaycastHit[] hits;
        RaycastHit[] hits = Physics.RaycastAll(ray, lookAhead.magnitude, _obstacleAvoidanceLayer.value, QueryTriggerInteraction.Collide);

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            if (!_canAvoidPlayer && hit.transform.root.gameObject == _player.gameObject)
                continue;

            if (!_canAvoidObstacles && hit.transform.root.gameObject != _player.gameObject)
                continue;

            if (hit.transform.root.gameObject != gameObject)
            {
                center = hit.collider.bounds.center;
                radius = hit.collider.bounds.extents.magnitude;

                Debug.DrawLine(transform.position, ahead, Color.yellow);
                return true;
            }
        }

        lookAhead = transform.forward * speed * 5f;
        ray = new Ray(myLocation, lookAhead.normalized);
        hits = Physics.RaycastAll(ray, lookAhead.magnitude, _obstacleAvoidanceLayer.value, QueryTriggerInteraction.Collide);

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            if (!_canAvoidPlayer && hit.transform.root.gameObject == _player.gameObject)
                continue;

            if (!_canAvoidObstacles && hit.transform.root.gameObject != _player.gameObject)
                continue;

            if (hit.transform.root.gameObject != gameObject)
            {
                center = hit.collider.bounds.center;
                radius = hit.collider.bounds.extents.magnitude;

                Debug.DrawLine(myLocation, ahead, Color.white);
                return true;
            }
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, 1, _obstacleAvoidanceLayer.value, QueryTriggerInteraction.Collide);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (!_canAvoidPlayer && colliders[i].transform.root.gameObject == _player.gameObject)
                continue;

            if (!_canAvoidObstacles && colliders[i].transform.root.gameObject != _player.gameObject)
                continue;

            if (colliders[i].transform.root.gameObject != gameObject)
            {
                center = colliders[i].bounds.center;
                radius = colliders[i].bounds.extents.magnitude;

                Debug.DrawLine(transform.position, ahead, Color.blue);
                return true;
            }
        }

        //Debug.DrawLine(myLocation, ahead, Color.red);

        center = Vector3.zero;
        radius = 0;
        return false;
    }


    bool Avoidance(ref Vector3 movementTarget)
    {
        Vector3 center;
        float radius;

        Vector3 myLocation = GetComponent<DroneEnemy>().GetFiringSocketsCenter();

        Vector3 movementDir = movementTarget - myLocation;
        movementDir.Normalize();

        float speed = _rigidBody.velocity.magnitude + 10;

        if (GetNearestObstacle(out center, out radius, movementDir, speed))
        {
            Vector3 ahead = myLocation + movementDir;
            Vector3 avoidance = ahead - center;
            avoidance.Normalize();
            avoidance *= radius * 1.1f;
            avoidance += center;

            Vector3 avoidanceDirection = avoidance - myLocation;
            avoidanceDirection.Normalize();

            movementTarget = myLocation + avoidanceDirection * speed;

            Debug.DrawLine(myLocation, movementTarget, Color.green);

            return true;
        }

        return false;
    }

    enum MovementState
    {
        MovingToEnemyDirectly,
        MoveAwayFromEnemy,
        ProtectingPlayer,
        MakingAShow,
        AttackingEnemies
    }


    public GameObject GetCurrentTarget()
    {
        return _currentTarget;
    }


    IEnumerator AILogic()
    {
        MovementState movementState = MovementState.MovingToEnemyDirectly;

        Vector3 movementTarget = Vector3.zero;

        Vector3 offset = Vector3.zero;

        float originalRollEffect = _enemyMovementController.m_RollEffect;
        float originalEnginePower = _enemyMovementController.MaxEnginePower;
        float originalShowOffDistance = showOffDistance;
        float originalShowOffDispersion = showOffDispersion;

        float timeSinceStartedRolling = 0;
        float timeSinceStoppedRolling = 0;
        float rollDirection = 1;
        bool canRoll = true;

        float playerSpeedFactor = 1;
        float playerProtectionAngle = assignedAngle.HasValue ? assignedAngle.Value : Random.Range(0.0f, 360.0f);

        bool preventLerp = false;

        while (true)
        {
            if (_currentTarget == null)
                _currentTarget = _player.gameObject;

            _enemyMovementController.MaxEnginePower = originalEnginePower;

            Vector3 dirToEnemy = _currentTarget.transform.position - transform.position;
            float distanceToEnemy = dirToEnemy.magnitude;
            dirToEnemy.Normalize();

            bool inFrontOfEnemy = Vector3.Dot(-dirToEnemy, _currentTarget.transform.forward) > 0;
            bool enemyInFront = Vector3.Dot(dirToEnemy, transform.forward) > 0;

            float distanceToMovementTarget = Vector3.Distance(movementTarget, transform.position);
            float randomSpeedMultiplier = Random.Range(0.8f, 1.2f);


            if (protectPlayer)
            {
                movementState = MovementState.ProtectingPlayer;
                offset = _player.transform.forward * 10 + (Quaternion.AngleAxis(playerProtectionAngle, transform.forward) * transform.right) * protectionDispersion;
                offset.Normalize();
            }
            else
            {
                if (_currentTarget == _player.gameObject)
                {
                    //search for enemies
                    EnemyBehaviour enemyTarget = _player.GetRandomEnemy();

                    if (enemyTarget != null)
                    {
                        //there are enemies in the screen target them if less than 2 people are chasing them
                        if (enemyTarget is DroneEnemy)
                        {
                            if (assignedEnemies.ContainsKey(enemyTarget))
                            {
                                if (assignedEnemies[enemyTarget] < EnemiesManager.instance.maxFriendliesPerEnemy)
                                {
                                    assignedEnemies[enemyTarget]++;// = EnemiesManager.instance.maxFriendliesPerEnemy;

                                    SetTarget(false, enemyTarget.gameObject);
                                    movementState = MovementState.MovingToEnemyDirectly;
                                    GetComponent<DroneEnemy>().SetTarget(enemyTarget.transform);
                                }
                            }
                            else
                            {
                                assignedEnemies.Add(enemyTarget, 1);

                                SetTarget(false, enemyTarget.gameObject);
                                movementState = MovementState.MovingToEnemyDirectly;
                                GetComponent<DroneEnemy>().SetTarget(enemyTarget.transform);
                            }
                        }
                    }
                }

                if (_currentTarget == _player.gameObject)
                {
                    if (movementState != MovementState.MakingAShow) //prevent lerp for first state step
                        preventLerp = true;

                    movementState = MovementState.MakingAShow;
                    offset = _player.transform.forward * showOffDistance + (Quaternion.AngleAxis(playerProtectionAngle, _player.transform.forward) * _player.transform.right) * showOffDispersion;
                }
                else
                {
                    //State transitions
                    if (movementState == MovementState.MoveAwayFromEnemy && (distanceToEnemy > _distanceToTravelBehindPlayer || distanceToMovementTarget < 10))
                        movementState = MovementState.MovingToEnemyDirectly;

                    if (_canMoveAwayFromEnemy && movementState == MovementState.MovingToEnemyDirectly && distanceToEnemy < _distanceToStopMovementDirectly)
                    {
                        movementState = MovementState.MoveAwayFromEnemy;

                        offset = transform.forward * 10000 + (Quaternion.AngleAxis(Random.Range(0.0f, 360.0f), transform.forward) * transform.right) * 1000;
                    }
                }

            }

            //Movement states
            switch (movementState)
            {
                case MovementState.MovingToEnemyDirectly:
                    {
                        if (_useInterception)
                        {
                            List<BaseWeapon> currentWeapons = GetComponent<DroneEnemy>().GetBestWeapons(_currentTarget.transform.position, false);

                            //Move toward enemy interception point using current projectile speed to rotate the ship heading correctly

                            float interceptionSpeed = 0;

                            if (currentWeapons.Count > 0 && _useProjectileSpeedForInterception)
                            {
                                if (distanceToEnemy < currentWeapons[0].WeaponRange * 2 && enemyInFront)
                                    interceptionSpeed = currentWeapons[0].WeaponShotSpeed;
                                else
                                    interceptionSpeed = 0;
                            }
                            else
                            {
                                if (distanceToEnemy < 500 && enemyInFront)
                                    interceptionSpeed = _rigidBody.velocity.magnitude;
                                else
                                    interceptionSpeed = 0;
                            }

                            if (interceptionSpeed == 0)
                                movementTarget = _currentTarget.transform.position;
                            else
                            {
                                float timeToHit = distanceToEnemy / interceptionSpeed;

                                Rigidbody targetBody = _currentTarget.GetComponent<Rigidbody>();

                                Vector3 enemyVelocity = targetBody.velocity;

                                Vector3 interceptionDisplacement = enemyVelocity * timeToHit;

                                movementTarget = _currentTarget.transform.position + interceptionDisplacement;
                            }
                        }
                        else
                        {
                            movementTarget = _currentTarget.transform.position;
                        }

                        if (distanceToEnemy > _distanceToStopMovementDirectly * 3)
                            _enemyMovementController.MaxEnginePower = originalEnginePower * _highSpeedMultiplier;// * _ultraSpeedMultiplier;
                        break;
                    }
                case MovementState.MoveAwayFromEnemy:
                    {
                        movementTarget = _currentTarget.transform.position + offset;

                        _enemyMovementController.MaxEnginePower = originalEnginePower * _highSpeedMultiplier;
                        break;
                    }
                case MovementState.ProtectingPlayer:
                    {
                        movementTarget = _player.transform.position + offset * protectionDistance;

                        float targetDist = Vector3.Distance(movementTarget, transform.position);

                        if (targetDist < 50)
                            _enemyMovementController.MaxEnginePower = originalEnginePower * 0.05f;
                        else if (targetDist < 100)
                            _enemyMovementController.MaxEnginePower = originalEnginePower * 0.2f;
                        else if (targetDist < 200)
                            _enemyMovementController.MaxEnginePower = originalEnginePower * 0.5f;
                        else if (targetDist < 500)
                            _enemyMovementController.MaxEnginePower = originalEnginePower * _highSpeedMultiplier;
                        else
                            _enemyMovementController.MaxEnginePower = originalEnginePower * _highSpeedMultiplier;// * _ultraSpeedMultiplier;

                        Debug.Log("Distance " + Vector3.Distance(movementTarget, transform.position));
                        break;
                    }
                case MovementState.MakingAShow:
                    {
                        lastTeleportTime += 100;

                        if (preventLerp)
                        {
                            movementTarget = _player.transform.position + offset;
                            preventLerp = false;
                        }
                        else
                            movementTarget = Vector3.Lerp(movementTarget, _player.transform.position + offset, Time.smoothDeltaTime * showLerpSpeed);
                        _enemyMovementController.MaxEnginePower = originalEnginePower * _ultraSpeedMultiplier * randomSpeedMultiplier;
                        float targetDist = Vector3.Distance(movementTarget, transform.position);

                        if (targetDist < 500)
                        {
                            showOffDistance *= -1;
                            showOffDispersion = 2000;
                            if (showOffDistance > 0)
                                showOffDispersion = originalShowOffDispersion;
                        }
                        if (IsInViewport())
                        {
                            _enemyMovementController.MaxEnginePower = originalEnginePower * _highSpeedMultiplier * randomSpeedMultiplier * playerSpeedFactor;

                            if (_rigidBody.velocity.magnitude > _playerRigidbody.velocity.magnitude)
                                playerSpeedFactor -= 1 * Time.deltaTime;
                            else
                                playerSpeedFactor += 1 * Time.deltaTime;

                            playerSpeedFactor = Mathf.Clamp(playerSpeedFactor, 0.5f, 2);
                        }
                        else
                        {
                            playerSpeedFactor = 1;
                            if (lastTeleportTime > showTeleportTime)
                            {
                                showOffDistance = originalShowOffDistance;
                                showOffDispersion = originalShowOffDispersion;
                                playerSpeedFactor = 1;

                                Teleport(playerProtectionAngle, movementTarget);
                            }
                        }
                        Debug.DrawLine(transform.position, movementTarget, Color.white);
                        break;
                    }
            }

            bool closeToEnemyAimRange = Vector3.Dot(-dirToEnemy, _currentTarget.transform.forward) > 0.5f;

            //Roll while in enemy aim range
            if (_canRoll)
            {
                float targetRollEffect = originalRollEffect;
                if (closeToEnemyAimRange && distanceToEnemy < _distanceForChasedRolling && (Time.time - timeSinceStartedRolling) < _timeToKeepRolling)
                {
                    timeSinceStoppedRolling = Time.time;
                    if (canRoll)
                    {
                        _aiControl.InputRoll = 1;

                        if (enemyInFront)
                        {
                            _aiControl.CanRoll = false;
                            _aiControl.CanPitch = _aiControl.CanYaw = true;
                        }
                        else
                        {
                            _aiControl.CanRoll = _aiControl.CanYaw = false;
                            _aiControl.CanPitch = true;
                        }

                        targetRollEffect = originalRollEffect * _rollingSpeed * rollDirection;
                    }
                }
                else
                {
                    rollDirection = Random.Range(0.0f, 1.0f) > 0.5f ? 1 : -1;
                    if (Time.time - timeSinceStoppedRolling > _timeToAvoidRolling)
                    {
                        timeSinceStartedRolling = Time.time;
                        canRoll = Random.Range(0.0f, 1.0f) < _rollingChance;
                    }

                    _aiControl.InputRoll = 0;
                    _aiControl.CanPitch = _aiControl.CanRoll = _aiControl.CanYaw = true;
                }

                _enemyMovementController.m_RollEffect = Mathf.Lerp(_enemyMovementController.m_RollEffect, targetRollEffect, 0.1f);
            }

            //Increase speed if being chased or chasing enemy
            //if (!inFrontOfEnemy)
            //{
            //    _enemyMovementController.MaxEnginePower = originalEnginePower * _highSpeedMultiplier;
            //}

            //Obstacle avoidance
            if (_canAvoidObstacles)
                Avoidance(ref movementTarget);

            //Debug.DrawLine(transform.position, movementTarget, Color.magenta);
            _aiControl.SetTargetPosition(movementTarget);

            if (_enemyMovementController.Stuck)
                transform.LookAt(movementTarget);

            yield return new WaitForSeconds(0.1f);
        }

        _enemyMovementController.m_RollEffect = originalRollEffect;
        _enemyMovementController.MaxEnginePower = originalEnginePower;
    }
    
    void Teleport(float spawnAngle, Vector3 lookAt)
    {
        //teleport behind player
        transform.position = _player.transform.position - (_player.transform.forward * 900 - Quaternion.AngleAxis(spawnAngle, _player.transform.forward) * _player.transform.right * 10 * showOffDispersion);
        transform.LookAt(lookAt);
        lastTeleportTime = 0;

        TrailRenderer[] trailRenderers = GetComponentsInChildren<TrailRenderer>();
        foreach (TrailRenderer trail in trailRenderers)
            trail.Clear();
    }


    bool IsInViewport()
    {
        Vector3 viewportPoint = Camera.main.WorldToViewportPoint(transform.position);
        //return viewportPoint.z > 0;
        return !(viewportPoint.z <= -0.2 || viewportPoint.x >= 1.2 || viewportPoint.x <= -0.2 ||
            viewportPoint.y >= 1.2 || viewportPoint.y <= -0.2);
        // viewportPoint.z > 0 && viewportPoint.x > 0 && viewportPoint.x < 1 && viewportPoint.y > 0 && viewportPoint.y < 1;
    }


    public static void ResetStatic()
    {
        assignedEnemies = new Dictionary<EnemyBehaviour, int>();
    }
}
