using UnityEngine;
using EnumTypes;
using EventLibrary;

public class FollowerController : HeroController
{
    public HeroController leader;
    public Vector3 formationOffset;

    private IFollowerState _currentState;
    public readonly FollowState FollowState = new FollowState();
    public new readonly AttackState AttackState = new AttackState();
    public new readonly FollowManualState ManualState = new FollowManualState();

    private new void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        InitializeFollower();
        AddEvents();
    }
    
    private new void OnDestroy()
    {
        RemoveEvents();
    }

    // 이벤트 리스너 등록
    private void AddEvents()
    {
        EventManager<UIEvents>.StartListening(UIEvents.OnClickAutoButton, ToggleAutoMode);
        EventManager<UIEvents>.StartListening(UIEvents.OnTouchStartJoystick, OnUserControl);
        EventManager<UIEvents>.StartListening(UIEvents.OnTouchEndJoystick, OffUserControl);
        EventManager<HeroEvents>.StartListening(HeroEvents.LeaderAttackStarted, OnLeaderAttackStarted);
        EventManager<HeroEvents>.StartListening(HeroEvents.LeaderAttackStopped, OnLeaderAttackStopped);
        EventManager<HeroEvents>.StartListening<float>(HeroEvents.LeaderDirectionChanged, OnLeaderDirectionChanged);
    }

    // 이벤트 리스너 제거
    private void RemoveEvents()
    {
        EventManager<UIEvents>.StopListening(UIEvents.OnClickAutoButton, ToggleAutoMode);
        EventManager<UIEvents>.StopListening(UIEvents.OnTouchStartJoystick, OnUserControl);
        EventManager<UIEvents>.StopListening(UIEvents.OnTouchEndJoystick, OffUserControl);
        EventManager<HeroEvents>.StopListening(HeroEvents.LeaderAttackStarted, OnLeaderAttackStarted);
        EventManager<HeroEvents>.StopListening(HeroEvents.LeaderAttackStopped, OnLeaderAttackStopped);
        EventManager<HeroEvents>.StopListening<float>(HeroEvents.LeaderDirectionChanged, OnLeaderDirectionChanged);
    }

    public void InitializeFollower()
    {
        this.heroStats = this.HeroStatsManager.GetHeroStats();
        
        this.IsLeader = false;
        if (leader == null)
        {
            leader = FormationManager.Instance.leader;
        }
        
        if (leader != null)
        {
            if (formationOffset == Vector3.zero)
            {
                formationOffset = transform.position - leader.transform.position;
            }
            TransitionToState(FollowState);
        }
        else
        {
            DebugLogger.LogError("Leader not assigned to follower.");
        }
    }

    // 매 프레임 상태 업데이트
    private new void Update()
    {
        if (_currentState == null) return;
        _currentState.UpdateState();
    }

    // 고정된 시간 간격으로 물리 업데이트
    private new void FixedUpdate()
    {
        if (_currentState == null) return;
        _currentState.PhysicsUpdateState();
    }

    private void OnLeaderAttackStarted()
    {
        TransitionToState(AttackState);
    }

    private void OnLeaderAttackStopped()
    {
        TransitionToState(FollowState);
    }

    private void OnLeaderDirectionChanged(float moveHorizontal)
    {
        if (_currentState != AttackState && IsAutoMode) // AttackState일 때는 무시
        {
            HandleSpriteFlip(moveHorizontal);
        }
    }

    private void OnUserControl()
    {
        IsUserControlled = true;
        TransitionToState(ManualState);
    }

    public void TransitionToState(IFollowerState state)
    {
        if (_currentState == state) return; // 상태 전환 빈도 줄이기
        _currentState?.ExitState();
        _currentState = state;
        _currentState.EnterState(this);
    }

    // 캐릭터 방향 전환
    public new void HandleSpriteFlip(float moveHorizontal)
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
    }

    public void EndFormationChanged()
    {
        if(leader.IsFlipped != this.IsFlipped) HandleSpriteFlip(1f);
    }
}
