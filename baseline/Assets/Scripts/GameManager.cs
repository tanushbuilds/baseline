using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField][TextArea(3, 6)] private string serveText = "LB to serve.";

    [SerializeField] private BallPhysics ball;

    [SerializeField] private TextMeshProUGUI player1ScoreText;
    [SerializeField] private TextMeshProUGUI player2ScoreText;

    // ── Static scores — survive scene reload ──────────────────────────────────
    private static int _player1Score = 0;
    private static int _player2Score = 0;

    private Server currentServer;
    private TextMeshProUGUI currentServerText;
    private string lastHit;
    private bool pointAwarded = false; // prevent double awarding


    private bool pitchedIn;

    // ─────────────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        player1Shot.OnServeHit += AfterServe;
        player2Shot.OnServeHit += AfterServe;
        BallOut.OnBallOut += AwardPoint;
        player1Shot.OnBallHit += () => lastHit = "p1";
        player2Shot.OnBallHit += () => lastHit = "p2";
    }

    private void OnDisable()
    {
        player1Shot.OnServeHit -= AfterServe;
        player2Shot.OnServeHit -= AfterServe;
        BallOut.OnBallOut -= AwardPoint;
    }

    void Start()
    {
        // Show scores carried over from last point
        UpdateScoreUI();

        currentServer = startingServer;
        SetServingState();

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    void SetServingState()
    {
        if (currentServer == Server.Player1)
        {
            player1Shot.SetIsServing(true);
            currentServerText = textP1;
            textP1.text = serveText;
            player1Movement.StartServe();
            player2Movement.EndServe();
            ball.Float();
        }
        else if (currentServer == Server.Player2)
        {
            player2Shot.SetIsServing(true);
            currentServerText = textP2;
            textP2.text = serveText;
            player2Movement.StartServe();
            player1Movement.EndServe();
            ball.Float();
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

        if (NAME == "p1")
        {
            player1Movement.EndServe();
            return;
        }
        player2Movement.EndServe();
    }

    public void SwitchServer()
    {
        currentServer = (currentServer == Server.Player1) ? Server.Player2 : Server.Player1;
        SetServingState();
    }

    public Server GetCurrentServer() => currentServer;

    private void AwardPoint()
    {
        if (pointAwarded) return;

        // Ball already bounced IN the court — it's a valid shot,
        // ignore the subsequent out bounce
        if (pitchedIn)
        {
            pitchedIn = false; // reset for next shot
            return;
        }

        pointAwarded = true;

        if (lastHit == "p1")
            _player2Score++;
        else if (lastHit == "p2")
            _player1Score++;

        UpdateScoreUI();
        ResetPoint();
    }

    private void ResetPoint()
    {
        player1Movement.StopMovement();
        player2Movement.StopMovement();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
            pitchedIn = true;
    }

    void UpdateScoreUI()
    {
        player1ScoreText.text = _player1Score.ToString();
        player2ScoreText.text = _player2Score.ToString();
    }
    public void RestartMatch()
    {
        _player1Score = 0;
        _player2Score = 0;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}