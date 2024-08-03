using System;
using EnumTypes;
using EventLibrary;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public class HeroController : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float zSpeedMultiplier = 1.777f;
    public Transform hud;

    #region Property

    public bool IsLeader { get; set; }
    public Rigidbody Rb { get; private set; }
    public SpriteRenderer SpriteRenderer { get; private set; }
    public Animator Animator { get; private set; }
    public Camera MainCamera { get; private set; }
    public BoxCollider BoxCollider { get; private set; }
    public float InitialY { get; private set; }
    public bool IsFlipped { get; set; }
    public Vector3 BoxColliderCenter { get; protected set; }
    public Vector3 HudPosition { get; protected set; }
    public LayerMask EnemyLayer { get; private set; }
    public bool IsUserControlled { get; set; }
    [ShowInInspector] public bool IsAutoMode { get; set; }
    public float LastUserInputTime { get; set; }
    public Vector2 MoveInput { get; set; }
    public Transform CurrentTarget { get; set; }
    public bool IsColliding { get; private set; }

    #endregion
   
    private IHeroState _currentState;
    public IHeroState CurrentState => _currentState;

    public readonly HeroIdleState IdleState = new HeroIdleState();
    public readonly HeroMoveState MoveState = new HeroMoveState();
    public readonly HeroAttackState AttackState = new HeroAttackState();
    public readonly HeroManualState ManualState = new HeroManualState();

    // 애니메이터 파라미터 캐싱
    public readonly int SpeedParameter = Animator.StringToHash("Speed");
    public readonly int AttackParameter = Animator.StringToHash("Attack");
    public readonly int IsAttacking = Animator.StringToHash("IsAttacking");
    
    public HeroStats heroStats;
    
    protected HeroStatsManager HeroStatsManager;
    
    private Collider[] _cachedEnemies;
    private float _lastCacheTime;
    private const float CacheInterval = 1.0f;

    // 초기화 작업 수행
    protected virtual void Awake()
    {
        Initialize();
        TransitionToState(IdleState);
        AddEvents();
    }

    private void Start()
    {
        LoadHeroStats();
    }

    // 오브젝트 파괴 시 호출
    protected virtual void OnDestroy()
    {
        RemoveEvents();
    }

    // 이벤트 리스너 등록
    private void AddEvents()
    {
        EventManager<UIEvents>.StartListening(UIEvents.OnClickAutoButton, ToggleAutoMode);
        EventManager<UIEvents>.StartListening(UIEvents.OnTouchStartJoystick, OnUserControl);
        EventManager<UIEvents>.StartListening(UIEvents.OnTouchEndJoystick, OffUserControl);
    }

    // 이벤트 리스너 제거
    private void RemoveEvents()
    {
        EventManager<UIEvents>.StopListening(UIEvents.OnClickAutoButton, ToggleAutoMode);
        EventManager<UIEvents>.StopListening(UIEvents.OnTouchStartJoystick, OnUserControl);
        EventManager<UIEvents>.StopListening(UIEvents.OnTouchEndJoystick, OffUserControl);
    }

    // 컴포넌트 및 변수 초기화
    protected void Initialize()
    {
        // 컴포넌트 초기화
        HeroStatsManager = GetComponent<HeroStatsManager>();
        HeroStatsManager.Initialize();

        Rb = GetComponent<Rigidbody>();
        SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        Animator = GetComponentInChildren<Animator>();
        MainCamera = Camera.main;
        BoxCollider = GetComponent<BoxCollider>();

        // 변수 초기화
        MoveInput = Vector2.zero;
        InitialY = transform.position.y;
        BoxColliderCenter = BoxCollider.center;
        IsAutoMode = false;
        IsUserControlled = false;
        IsColliding = false;

        // HUD 초기화
        if (hud == null)
        {
            hud = transform.GetChild(0);
        }
        HudPosition = hud.localPosition;

        // 레이어 초기화
        EnemyLayer = LayerMask.GetMask("Enemy");

        // HeroStats 초기화
        if (HeroStatsManager != null)
        {
            heroStats = HeroStatsManager.GetHeroStats();
        }
    }

    // 매 프레임 상태 업데이트
    protected void Update()
    {
        _currentState.UpdateState();
    }

    // 고정된 시간 간격으로 물리 업데이트
    protected void FixedUpdate()
    {
        _currentState.PhysicsUpdateState();
    }

    // 상태 전환
    public void TransitionToState(IHeroState state)
    {
        if (_currentState == state) return; // 상태 전환 빈도 줄이기
        _currentState?.ExitState();
        _currentState = state;
        _currentState.EnterState(this);
    }
    
    // 이동 입력 처리
    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
        LastUserInputTime = Time.time;
    }

    public void LoadHeroStats()
    {
        if (HeroStatsManager != null)
        {
            heroStats = HeroStatsManager.GetHeroStats();
        }
    }

    // 이동 중지
    public void StopMoving()
    {
        MoveInput = Vector2.zero;
        Animator.SetFloat(SpeedParameter, 0);
        TransitionToState(IdleState);
    }
    
    // 자동 모드 전환
    protected void ToggleAutoMode()
    {
        IsAutoMode = !IsAutoMode;
    }

    private void OnUserControl()
    {
        IsUserControlled = true;
        TransitionToState(ManualState);
    }

    protected void OffUserControl()
    {
        IsUserControlled = false;
    }

    // HeroStatsManager 가져오기
    public HeroStatsManager GetHeroStatsManager()
    {
        return HeroStatsManager;
    }

    // 가장 가까운 적 찾기
    public void FindClosestEnemy()
    {
        if (_cachedEnemies == null || Time.time - _lastCacheTime >= CacheInterval)
        {
            _cachedEnemies = Physics.OverlapSphere(transform.position, 30, EnemyLayer);
            _lastCacheTime = Time.time;
        }

        Transform closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider enemy in _cachedEnemies)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy < closestDistance)
            {
                closestDistance = distanceToEnemy;
                closestEnemy = enemy.transform;
            }
        }

        CurrentTarget = closestEnemy;
        
        // 적이 발견되면 스프라이트 반전
        if (CurrentTarget != null && IsAutoMode)
        {
            float moveHorizontal = CurrentTarget.position.x - transform.position.x;
            HandleSpriteFlip(moveHorizontal);
            EventManager<HeroEvents>.TriggerEvent(HeroEvents.LeaderDirectionChanged, moveHorizontal);
        }
    }

    // 타겟과의 거리 계산
    public float GetDistanceToTarget(Transform target)
    {
        return Vector3.Distance(transform.position, target.position);
    }
    
    // 캐릭터 방향 전환
    public void HandleSpriteFlip(float moveHorizontal)
    {
        if (moveHorizontal == 0) return;

        bool shouldFlip = moveHorizontal < 0;

        if (shouldFlip == IsFlipped) return;

        IsFlipped = shouldFlip;
        SpriteRenderer.flipX = shouldFlip;

        Vector3 boxColliderCenter = BoxCollider.center;
        boxColliderCenter.x *= -1;
        BoxCollider.center = boxColliderCenter;

        Vector3 hudPosition = hud.localPosition;
        hudPosition.x *= -1;
        hud.localPosition = hudPosition;
        
        EventManager<HeroEvents>.TriggerEvent(HeroEvents.LeaderDirectionChanged, moveHorizontal);
    }

    protected void OnCollisionEnter(Collision col)
    {
        // 벽과 부딪혔다면
        if (col.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            // 충돌 중인 상태로 설정
            IsColliding = true;
            // 벽에 부딪히면 이동 멈춤
            Rb.velocity = Vector3.zero;
            MoveInput = Vector2.zero;
        }
    }

    protected void OnCollisionExit(Collision col)
    {
        // 벽과의 충돌이 끝났다면
        if (col.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            // 충돌 중이지 않은 상태로 설정
            IsColliding = false;
        }
    }
}
