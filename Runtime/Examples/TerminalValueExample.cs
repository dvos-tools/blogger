using UnityEngine;
using com.DvosTools.blogger.Attributes;

namespace com.DvosTools.blogger.Examples
{
    /// <summary>
    /// Example showing multiple player instances with a single class
    /// Each instance defines its own unique key via the [BLoggerAggregateId] field
    /// Access pattern: @Players.player1.health, @Players.player2.health
    /// </summary>
    [BLoggerAggregate("Players")]
    public class PlayerController : MonoBehaviour
    {
        [BLoggerAggregateId]
        public string PlayerName = "player1"; // This defines the unique instance key

        [BLoggerValue("health")]
        public int Health = 100;

        [BLoggerValue("position")]
        public Vector3 Position => transform.position;

        [BLoggerValue("isAlive")]
        public bool IsAlive => Health > 0;

        [BLoggerValue("damage")]
        public int DamageDealt = 0;

        private void Start()
        {
            BLogger.Log($"Player '{PlayerName}' spawned! Try: @Players.{PlayerName}.health @Players.{PlayerName}.position");
        }

        private void Update()
        {
            // Simulate health decay
            Health = Mathf.Max(0, Health - (int)(Time.deltaTime * 2));
            DamageDealt += (int)(Time.deltaTime * 10);
        }
    }

    /// <summary>
    /// Example showing enemy instances
    /// Access pattern: @Enemies.boss.health, @Enemies.minion1.health
    /// </summary>
    [BLoggerAggregate("Enemies")]
    public class EnemyController : MonoBehaviour
    {
        [BLoggerAggregateId]
        public string EnemyId = "boss";

        [BLoggerValue("health")]
        public int Health = 1000;

        [BLoggerValue("phase")]
        public int CurrentPhase => Health switch
        {
            > 600 => 1,
            > 300 => 2,
            _ => 3
        };

        [BLoggerValue("threat")]
        public string ThreatLevel()
        {
            return Health > 500 ? "HIGH" : "LOW";
        }

        private void Start()
        {
            BLogger.Log($"Enemy '{EnemyId}' spawned! Try: @Enemies.{EnemyId}.health @Enemies.{EnemyId}.phase");
            
            // Example log showing all values at once
            InvokeRepeating(nameof(LogStatus), 2f, 2f);
        }

        private void LogStatus()
        {
            BLogger.Log("Status: P1=@Players.player1.health P2=@Players.player2.health | Boss=@Enemies.boss.health (Phase @Enemies.boss.phase) | FPS: @fps");
        }

        private void Update()
        {
            Health = Mathf.Max(0, Health - (int)(Time.deltaTime * 5));
        }
    }

    /// <summary>
    /// Static global values accessible anywhere
    /// Access pattern: @fps, @frameCount, @timeScale
    /// </summary>
    public static class GlobalStats
    {
        [BLoggerValue("fps")]
        public static float FPS => 1f / Time.deltaTime;

        [BLoggerValue("frameCount")]
        public static int FrameCount => Time.frameCount;

        [BLoggerValue("timeScale")]
        public static float TimeScale
        {
            get => Time.timeScale;
            set => Time.timeScale = value;
        }

        [BLoggerValue("playerCount")]
        public static int PlayerCount => Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length;

        [BLoggerValue("enemyCount")]
        public static int EnemyCount => Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None).Length;
    }
}
