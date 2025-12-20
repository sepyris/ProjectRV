using UnityEngine;

public class PlayerMovement
{
    public Rigidbody2D rb;
    public float MoveSpeed = 5f;
    public bool ControlsLocked = false;
    public Vector2 LastMoveDirection { get; private set; } = Vector2.right;
    public Vector2 currentInput;

    public PlayerMovement(Rigidbody2D rb)
    {
        this.rb = rb;
        currentInput = Vector2.zero;
        LastMoveDirection = Vector2.right;
    }

    public void SetLastDirection(Vector2 direction)
    {
        if (direction.magnitude > 0.01f)
            LastMoveDirection = direction.normalized;
    }

    public void ApplyMovement()
    {
        if (rb == null) return;
        rb.velocity = ControlsLocked ? Vector2.zero : currentInput * MoveSpeed;
    }
}