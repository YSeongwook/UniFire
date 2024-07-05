using UnityEngine;

/**
 * @brief 캐릭터의 공격 상태를 정의하는 클래스입니다.
 */
public class HeroAttackState : IHeroState
{
    private HeroController _hero;
    private Transform _targetEnemy;
    private float _lastAttackTime;
    private float _maxDistanceFromLeader = 10f; // 리더로부터의 최대 거리

    public void EnterState(HeroController hero)
    {
        _hero = hero;
        _lastAttackTime = Time.time;
        FindClosestEnemy();
    }

    public void UpdateState()
    {
        if (!_hero.IsAutoMode) return; // 오토 모드가 아닌 경우 공격하지 않음

        if (_targetEnemy == null)
        {
            _hero.TransitionToState(_hero.IdleState);
            return;
        }

        float distanceToEnemy = Vector3.Distance(new Vector3(_hero.transform.position.x, 0, _hero.transform.position.z), new Vector3(_targetEnemy.position.x, 0, _targetEnemy.position.z));

        if (_hero is FollowerController follower)
        {
            float distanceToLeader = Vector3.Distance(_hero.transform.position, follower.leader.transform.position);
            if (distanceToLeader > _maxDistanceFromLeader)
            {
                _hero.TransitionToState(_hero.IdleState);
                return;
            }
        }

        if (distanceToEnemy > _hero.HeroStatsManager.AttackRange)
        {
            _hero.MoveToTarget(_targetEnemy.position);
            _hero.TransitionToState(_hero.MoveState);
        }
        else
        {
            if (Time.time - _lastAttackTime >= 1f / _hero.HeroStatsManager.AttackSpeed)
            {
                Attack();
                _lastAttackTime = Time.time;
            }
        }
    }

    public void ExitState()
    {
        // 상태를 나갈 때 수행할 작업이 있으면 여기에 작성합니다.
    }

    private void FindClosestEnemy()
    {
        Collider[] enemies = Physics.OverlapSphere(_hero.transform.position, Mathf.Infinity, _hero.EnemyLayer);
        float closestDistance = Mathf.Infinity;
        Transform closestEnemy = null;

        foreach (Collider enemy in enemies)
        {
            float distanceToEnemy = Vector3.Distance(new Vector3(_hero.transform.position.x, 0, _hero.transform.position.z), new Vector3(enemy.transform.position.x, 0, enemy.transform.position.z));
            if (distanceToEnemy < closestDistance)
            {
                closestDistance = distanceToEnemy;
                closestEnemy = enemy.transform;
            }
        }

        _targetEnemy = closestEnemy;
    }

    private void Attack()
    {
        _hero.Animator.SetTrigger(HeroController.AttackParameter);

        if (_targetEnemy != null)
        {
            // 적에게 데미지 입히기 (적 스크립트가 Damage 메서드를 가지고 있다고 가정)
            // _targetEnemy.GetComponent<Enemy>().TakeDamage(_hero.HeroStatsManager.AttackDamage);
        }
    }
}
