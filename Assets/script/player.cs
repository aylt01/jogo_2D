using UnityEngine;

public class Player : MonoBehaviour
{
    public float speed = 5f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

    }
    private bool isGrounded = false;

    // Update is called once per frame
    void Update()
    {
        float moveHorizontal = Input.GetAxis("Horizontal"); //Vai reconhecer o movimento horizontal

        rb.linearVelocity = new Vector2(moveHorizontal * speed, rb.linearVelocity.y); //Vai aplicar a velocidade horizontal ao Rigidbody2D

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(new Vector2(0f, 5f), ForceMode2D.Impulse); //Vai aplicar uma força vertical ao Rigidbody2D
        }

    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true; // vai saber quando esta no chao
        }

    }
    private void OnCollisionExit2D(Collision2D collision)
    {
            if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false; //vai saber quando nao esta no chao
        }
    }
}