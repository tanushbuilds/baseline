using System;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum Server { Player1, Player2, None }

    [Header("Serving")]
    [SerializeField] private Server startingServer = Server.Player1;

    [Header("Player References")]
    [SerializeField] private PlayerMovement player1Movement;
    [SerializeField] private PlayerMovement player2Movement;
    [SerializeField] private PlayerShot player1Shot;
    [SerializeField] private PlayerShot player2Shot;

    [SerializeField] private TextMeshProUGUI textP1;
    [SerializeField] private TextMeshProUGUI textP2;
    [SerializeField][TextArea(3, 6)] private string serveText = "Q to serve.";

    [SerializeField] private BallPhysics ball;

    private Server currentServer;
    private TextMeshProUGUI currentServerText;

    private void OnEnable()
    {
        player1Shot.OnServeHit += AfterServe;
    }
    private void OnDisable()
    {
        player1Shot.OnServeHit -= AfterServe;
    }

    void Start()
    {
        currentServer = startingServer;
        SetServingState();

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    void SetServingState()
    {
        ball.Float();
        if (currentServer == Server.Player1)
        {
            player1Shot.SetIsServing(true);

            currentServerText = textP1;
            textP1.text = serveText;
            player1Movement.StartServe();
            player2Movement.EndServe();
        }
        else if(currentServer == Server.Player2)
        {
            player2Shot.SetIsServing(true);

            currentServerText = textP2;
            textP2.text = serveText;
            player2Movement.StartServe();
            player1Movement.EndServe();
        }
        else
        {
            player1Movement.EndServe();
            player2Movement.EndServe();
        }

    }
    void AfterServe(string NAME)
    {
        ball.UnFloat();
        currentServerText.text = "";
        
        if (NAME == "P1"){
            player1Movement.EndServe();
        }
        else
        {
            player2Movement.EndServe();
        }

    }

    public void SwitchServer()
    {
        currentServer = (currentServer == Server.Player1) ? Server.Player2 : Server.Player1;
        SetServingState();
    }

    public Server GetCurrentServer() => currentServer;
}