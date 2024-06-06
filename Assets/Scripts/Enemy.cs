using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    public int health = 10;
    public float speed = 2.0f;
    public float attackRange = 1.0f;
    public int damage = 10;
    public float attackCooldown = 2.0f; // ���� ��Ÿ���� 2�ʷ� ����
    public float detectionRange = 5.0f;
    public Transform attackPoint; // ���� ������ �߽���
    public float attackRadius = 0.5f; // ���� ������ ������
    public float contactDamageCooldown = 1f; // ���� ���� �� �������� ������ ����

    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private float lastAttackTime;
    private bool isAttacking = false; // ���� ������ ���θ� Ȯ���ϴ� ����
    private float lastContactDamageTime; // ���������� ���� �������� ���� �ð�
    private bool isFacingRight = true; // �������� �ٶ󺸰� �ִ��� ���θ� Ȯ���ϴ� ����
    private BoxCollider2D boxCollider;
    public int maxHealth = 10; // 최대 체력
    private BossInterfaceManager bossInterfaceManager;
    
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

        // �ʱ� centerOfMass ����
        Vector2 colliderCenter = boxCollider.bounds.center;
        Vector2 localColliderCenter = transform.InverseTransformPoint(colliderCenter);
        rb.centerOfMass = localColliderCenter;
        bossInterfaceManager = FindObjectOfType<BossInterfaceManager>();
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer < attackRange)
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
        Vector2 direction = (player.position - transform.position).normalized;
        rb.velocity = new Vector2(direction.x * speed, rb.velocity.y);
        animator.SetBool("isWalking", true);
        animator.SetBool("isAttacking", false);

        // �̵� ���⿡ ���� ���� ������Ŵ
        if ((direction.x > 0 && isFacingRight) || (direction.x < 0 && !isFacingRight))
        {
            Flip();
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
        rb.velocity = Vector2.zero; // ���� �߿��� �������� �ʵ��� ����
        animator.SetBool("isAttacking", true);
        animator.SetBool("isWalking", false);

        yield return new WaitForSeconds(0.5f); // ���� �ִϸ��̼� ���۱��� ���

        // �÷��̾ ���� ����
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, LayerMask.GetMask("Player"));
        foreach (Collider2D playerCollider in hitPlayers)
        {
            PlayerController playerController = playerCollider.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.TakeDamage(damage);
            }
        }

        isAttacking = false;
        animator.SetBool("isAttacking", false);
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;

        // Rigidbody2D�� �߽��� ������Ŵ
        Vector2 colliderCenter = boxCollider.bounds.center;
        Vector2 localColliderCenter = transform.InverseTransformPoint(colliderCenter);
        rb.centerOfMass = localColliderCenter;

        // ��������Ʈ ����
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log("Enemy took damage. Current health: " + health);
        
        if (health <= 0)
        {
            Die();
        }
         UpdateHealthUI();
    }
    void UpdateHealthUI()
    {
        if (bossInterfaceManager != null)
        {
            bossInterfaceManager.SetBossHealth(health, maxHealth);
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
