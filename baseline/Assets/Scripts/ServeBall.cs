using UnityEngine;

public class ServeBall : MonoBehaviour
{
    [Header("Player 1 References")]
    [SerializeField] private Transform p1HandBone;
    [SerializeField] private Transform p1BallHoldPoint;
    [SerializeField] private Transform p1TossTarget;

    [Header("Player 2 References")]
    [SerializeField] private Transform p2HandBone;
    [SerializeField] private Transform p2BallHoldPoint;
    [SerializeField] private Transform p2TossTarget;

    [Header("Ball")]
    [SerializeField] private Rigidbody ball;
    [SerializeField] private float tossSpeed = 6f;

    private Transform handBone;
    private Transform ballHoldPoint;
    private Transform tossTarget;

    void ResolvePlayer(GameManager.Server player)
    {
        bool isPlayer1 = player == GameManager.Server.Player1;
        handBone = isPlayer1 ? p1HandBone : p2HandBone;
        ballHoldPoint = isPlayer1 ? p1BallHoldPoint : p2BallHoldPoint;
        tossTarget = isPlayer1 ? p1TossTarget : p2TossTarget;
    }

    void AttachBall()
    {
        ball.transform.SetParent(handBone);
        ball.transform.localPosition = Vector3.zero;
        ball.transform.localRotation = Quaternion.identity;
        ball.transform.position = ballHoldPoint.position;
        ball.isKinematic = true;
        ball.interpolation = RigidbodyInterpolation.None;
    }

    public void SetServingPlayer(GameManager.Server player)
    {
        if (player == GameManager.Server.None) return;
        ResolvePlayer(player);
        AttachBall();
        Debug.DrawLine(ballHoldPoint.position, tossTarget.position, Color.red, 2f);
    }

    public void ReleaseBall()
    {
        ball.transform.SetParent(null);
        ball.isKinematic = false;
        ball.interpolation = RigidbodyInterpolation.Interpolate;
        ball.linearVelocity = Vector3.zero;
        ball.angularVelocity = Vector3.zero;

        Vector3 dir = (tossTarget.position - ballHoldPoint.position).normalized;
        ball.linearVelocity = dir * tossSpeed;

        Debug.DrawLine(ballHoldPoint.position, tossTarget.position, Color.red, 2f);
    }
}