using UnityEngine;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float jumpForce = 10.0f;
    public float attackMoveDistance = 0.5f;
    public float attackMoveDelay = 0.1f;
    public float attackMoveDuration = 0.5f;
    public float dodgeDistance = 3.0f;
    public float dodgeDuration = 0.5f;
    public float dodgeCooldown = 5.0f;
    public int attackDamage = 1;
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public int maxHealth = 100;
    private int currentHealth;
    public float contactDamageCooldown = 1f; // ���� ���� �� �������� ������ ����

    private Rigidbody2D rb;
    private Animator animator;
    private bool isGrounded = true;
    private bool isFacingRight = true;
    private bool isAttacking = false;
    private bool isDodging = false;
    private float lastDodgeTime;
    private float lastContactDamageTime; // ���������� ���� �������� ���� �ð�

    [SerializeField] private BossInterfaceManager bossInterfaceManager;
    public int bossMaxHealth = 100; // 보스의 최대 체력
    private int bossCurrentHealth; // 보스의 현재 체력

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (attackPoint == null)
        {
            Debug.LogError("Attack Point is not assigned in the Inspector");
        }
        else
        {
            Debug.Log("Attack Point assigned successfully. Position: " + attackPoint.position);
        }
    }

    void Update()
    {
        if (!isAttacking && !isDodging)
        {
            HandleMovement();
            HandleJump();
        }

        HandleAttack();
        HandleDodge();
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        Vector2 movement = new Vector2(horizontal * moveSpeed, rb.velocity.y);
        rb.velocity = new Vector2(movement.x, rb.velocity.y);

        bool isMoving = horizontal != 0;
        animator.SetBool("isMoving", isMoving);

        if (horizontal > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (horizontal < 0 && isFacingRight)
        {
            Flip();
        }
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            animator.SetBool("isJumping", true);
            animator.SetBool("isGrounded", false);
        }
    }

    void HandleAttack()
    {
        if (Input.GetKeyDown(KeyCode.Z) && !isAttacking && isGrounded)
        {
            isAttacking = true;
            animator.SetTrigger("isAttacking");
            animator.SetBool("isGrounded", true);

            if (attackPoint == null)
            {
                Debug.LogError("Attack Point is not assigned.");
                return;
            }

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, 1 << enemyLayer);
            foreach (Collider2D enemy in hitEnemies)
            {
                Enemy enemyComponent = enemy.GetComponent<Enemy>();
                if (enemyComponent != null)
                {
                    enemyComponent.TakeDamage(attackDamage);
                }
            }

            StartCoroutine(AttackMoveCoroutine());
        }
    }

    IEnumerator AttackMoveCoroutine()
    {
        yield return new WaitForSeconds(attackMoveDelay);

        float elapsedTime = 0f;
        while (elapsedTime < attackMoveDuration)
        {
            float moveStep = (isFacingRight ? attackMoveDistance : -attackMoveDistance) * Time.deltaTime / attackMoveDuration;
            rb.MovePosition(rb.position + new Vector2(moveStep, 0));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        isAttacking = false;
    }

    void HandleDodge()
    {
        if (Input.GetKeyDown(KeyCode.X) && isGrounded && !isDodging && Time.time - lastDodgeTime > dodgeCooldown)
        {
            isDodging = true;
            lastDodgeTime = Time.time;
            animator.SetTrigger("isDodging");

            StartCoroutine(DodgeMoveCoroutine());
        }
    }

    IEnumerator DodgeMoveCoroutine()
    {
        float elapsedTime = 0f;
        while (elapsedTime < dodgeDuration)
        {
            float moveStep = (isFacingRight ? dodgeDistance : -dodgeDistance) * Time.deltaTime / dodgeDuration;
            rb.MovePosition(rb.position + new Vector2(moveStep, 0));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        isDodging = false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            animator.SetBool("isJumping", false);
            animator.SetBool("isGrounded", true);
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (!isAttacking && collision.gameObject.CompareTag("Enemy"))
        {
            if (Time.time >= lastContactDamageTime + contactDamageCooldown)
            {
                Enemy enemy = collision.gameObject.GetComponent<Enemy>();
                if (enemy != null)
                {
                    TakeDamage(enemy.damage); // ���� ���� �� ü�� ����
                    lastContactDamageTime = Time.time;
                }
            }
        }
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log("Player took damage. Current health: " + currentHealth);
        if (currentHealth <= 0)
        {
            Debug.Log("Player died");
            // ���� ���� ó��
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) 
    {
        if (collision.CompareTag("Warning"))
        {
            Debug.Log("asdsad");
            FindObjectOfType<BossInterfaceManager>().OpenBossInterface();
        }
    }

}
