using System;
using UnityEngine;

public class BallOut : MonoBehaviour
{
    public static event Action OnBallOut;

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Ball")
        {
            OnBallOut?.Invoke();
        }
    }
}
