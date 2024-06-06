using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    public int health = 3;
    public float speed = 2.0f;
    public float attackRange = 1.0f;
    public int damage = 10;
    public float attackCooldown = 2.0f; // 공격 쿨타임을 2초로 설정
    public float detectionRange = 5.0f;
    public Transform attackPoint; // 공격 범위의 중심점
    public float attackRadius = 0.5f; // 공격 범위의 반지름
    public float contactDamageCooldown = 1f; // 적과 접촉 시 데미지를 입히는 간격
    public float knockbackForce = 5.0f; // 넉백 힘

    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private float lastAttackTime;
    private bool isAttacking = false; // 공격 중인지 여부를 확인하는 변수
    private float lastContactDamageTime; // 마지막으로 접촉 데미지를 입힌 시간
    private bool isFacingRight = true; // 오른쪽을 바라보고 있는지 여부를 확인하는 변수
    private BoxCollider2D boxCollider;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();

        if (attackPoint == null)
        {
            Debug.LogError("Attack Point is not assigned in the Inspector");
        }

        // 초기 centerOfMass 설정
        Vector2 colliderCenter = boxCollider.bounds.center;
        Vector2 localColliderCenter = transform.InverseTransformPoint(colliderCenter);
        rb.centerOfMass = localColliderCenter;
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer < attackRadius)
        {
            if (!isAttacking && Time.time >= lastAttackTime + attackCooldown)
            {
                StartCoroutine(AttackPlayer());
                lastAttackTime = Time.time;
            }
        }
        else if (distanceToPlayer < detectionRange)
        {
            FollowPlayer();
        }
        else
        {
            StopFollowingPlayer();
        }
    }

    void FollowPlayer()
    {
        if (isAttacking) return;

        Vector2 direction = (player.position - transform.position).normalized;
        rb.velocity = new Vector2(direction.x * speed, rb.velocity.y);
        animator.SetBool("isWalking", true);
        animator.SetBool("isAttacking", false);

        // 이동 방향에 따라 적을 반전시킴
        if ((direction.x > 0 && isFacingRight) || (direction.x < 0 && !isFacingRight))
        {
            Flip(direction.x);
        }
    }

    void StopFollowingPlayer()
    {
        rb.velocity = Vector2.zero;
        animator.SetBool("isWalking", false);
        animator.SetBool("isAttacking", false);
    }

    IEnumerator AttackPlayer()
    {
        isAttacking = true;
        rb.velocity = Vector2.zero; // 공격 중에는 움직이지 않도록 설정
        animator.SetBool("isAttacking", true);
        animator.SetBool("isWalking", false);

        yield return new WaitForSeconds(0.5f); // 공격 애니메이션 시작까지 대기

        // 플레이어를 적이 공격
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, LayerMask.GetMask("Player"));
        foreach (Collider2D playerCollider in hitPlayers)
        {
            PlayerController playerController = playerCollider.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.TakeDamage(damage);
            }
        }


        yield return new WaitForSeconds(0.2f); // 공격 후 0.2초 동안 대기
        //isAttacking true일 때, Flip되지 않게 하기 위함임.

        isAttacking = false;
        animator.SetBool("isAttacking", false);
    }

    void Flip(float directionX)
    {
        isFacingRight = !isFacingRight;

        // Rigidbody2D의 중심을 반전시킴
        Vector2 colliderCenter = boxCollider.bounds.center;
        Vector2 localColliderCenter = transform.InverseTransformPoint(colliderCenter);
        rb.centerOfMass = localColliderCenter;

        // 스프라이트 반전
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;

        // Collider 반전 (BoxCollider2D 사용시 필요할 수 있음)
        boxCollider.offset = new Vector2(boxCollider.offset.x * -1, boxCollider.offset.y);

        // 스프라이트 반전 후의 위치 조정
        Vector2 newPosition = transform.position;
        newPosition.x += directionX * 0.1f; // 이동 방향에 맞춰 조금 이동
        transform.position = newPosition;
    }

    public void TakeDamage(int damage, Vector2 knockbackDirection)
    {
        health -= damage;
        Debug.Log("Enemy took damage. Current health: " + health);

        // 넉백 적용
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Enemy died");
        animator.SetBool("isDead", true);
        rb.velocity = Vector2.zero;
        StartCoroutine(DestroyAfterDeath());
    }

    IEnumerator DestroyAfterDeath()
    {
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (Time.time >= lastContactDamageTime + contactDamageCooldown)
            {
                PlayerController playerController = other.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.TakeDamage(damage);
                    lastContactDamageTime = Time.time;
                }
            }
        }
    }
}
