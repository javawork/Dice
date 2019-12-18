using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed;

    private Rigidbody2D rBody;
    private Vector2 moveVelocity;
    private float accumulatedTime;
    private Vector2 prevPos = new Vector2();
    private Queue<Vector2> posQue = new Queue<Vector2>();

    void Start()
    {
        rBody = GetComponent<Rigidbody2D>();
        prevPos = rBody.position;
        accumulatedTime = 0f;
    }

    void Update()
    {
        Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        moveVelocity = moveInput.normalized * speed;
    }

    void FixedUpdate()
    {
        rBody.MovePosition(rBody.position + moveVelocity * Time.fixedDeltaTime);
        accumulatedTime += Time.fixedDeltaTime;
        if (accumulatedTime > 0.2f && posQue.Count > 0)
        {
            GameManager.Instance.SendMove(posQue);
            accumulatedTime = 0f;
            //Debug.Log($"Sent Move {posQue.Count}");
            posQue.Clear();
        }

        if (Vector2.Distance(prevPos, rBody.position) > 0.01)
        {
            posQue.Enqueue(rBody.position);
            prevPos = rBody.position;
        }
    }
}
