using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    public float speed = 3f;
    public float jumpForce = 5f;
    public float wallJumpHorizontal = 5f;

    Rigidbody2D rb;

    private bool isGraunded = false;
    private bool isOnWall = false;

    // -1 = parede à direita
    //  1 = parede à esquerda
    private int wallDirection = 0;

    public MenuScene menu;

    Animator animator;
    SpriteRenderer spriteRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");

        // Movimento normal
        rb.linearVelocity = new Vector2(
            moveHorizontal * speed,
            rb.linearVelocity.y
        );

        // Animação
        if (moveHorizontal < 0)
        {
            animator.SetInteger("trans", 1);
            spriteRenderer.flipX = true;
        }
        else if (moveHorizontal > 0)
        {
            animator.SetInteger("trans", 1);
            spriteRenderer.flipX = false;
        }
        else
        {
            animator.SetInteger("trans", 0);
        }

        // Pulo normal
        if (Input.GetKeyDown(KeyCode.Space) && isGraunded)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpForce
            );
        }

        // WALL JUMP
        if (Input.GetKeyDown(KeyCode.Space) && isOnWall && !isGraunded)
        {
            rb.linearVelocity = new Vector2(
                wallDirection * wallJumpHorizontal,
                jumpForce
            );

            isOnWall = false;
        }
    }


    public void morte()
    {
        menu.MostrarGamerOver();
    }
}