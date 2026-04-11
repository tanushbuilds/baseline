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

    [Header("Serve Ball")]
    [SerializeField] private ServeBall serveBall;

    private Server currentServer;

    void Start()
    {
        currentServer = startingServer;
        SetServingState();

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    void SetServingState()
    {
        if (currentServer == Server.Player1)
        {
            player1Shot.StartServe();
            player2Movement.EndServe();
        }
        else if(currentServer == Server.Player2)
        {
            player2Shot.StartServe();
            player1Movement.EndServe();
        }
        else
        {
            player1Shot.EndServe();
            player1Movement.EndServe();
            player2Shot.EndServe();
            player2Movement.EndServe();
        }

        serveBall.SetServingPlayer(currentServer);
    }

    public void SwitchServer()
    {
        currentServer = (currentServer == Server.Player1) ? Server.Player2 : Server.Player1;
        SetServingState();
    }

    public Server GetCurrentServer() => currentServer;
}