using UnityEngine;
using com.DvosTools.blogger.Attributes;

namespace com.DvosTools.blogger.Examples
{
    /// <summary>
    /// Example showing multiple player instances with a single class
    /// Each instance defines its own unique key via the [BLoggerAggregateId] field
    /// Access pattern: @Players.player1.health, @Players.player2.health
    /// Action pattern: !Players.player1.heal(50)
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
            BLogger.Log($"Player '{PlayerName}' spawned!");
            BLogger.Log($"Try values: @Players.{PlayerName}.health @Players.{PlayerName}.position");
            BLogger.Log($"Try actions: !Players.{PlayerName}.heal(25) or !Players.{PlayerName}.teleport(0,0,0)");
        }

        private void Update()
        {
            // Simulate health decay
            Health = Mathf.Max(0, Health - (int)(Time.deltaTime * 2));
            DamageDealt += (int)(Time.deltaTime * 10);
        }

        // ACTIONS - Methods with parameters that can be invoked via !Players.player1.actionName(args)

        [BLoggerAction("heal")]
        public void Heal(int amount)
        {
            Health = Mathf.Min(100, Health + amount);
            BLogger.Log($"{PlayerName} healed for {amount}. Current health: {Health}");
        }

        [BLoggerAction("takeDamage")]
        public bool TakeDamage(int damage)
        {
            Health = Mathf.Max(0, Health - damage);
            BLogger.Log($"{PlayerName} took {damage} damage. Health: {Health}");
            return Health <= 0;
        }

        [BLoggerAction("teleport")]
        public void Teleport(float x, float y, float z)
        {
            transform.position = new Vector3(x, y, z);
            BLogger.Log($"{PlayerName} teleported to ({x}, {y}, {z})");
        }

        [BLoggerAction("giveItem")]
        public void GiveItem(string itemName, int quantity)
        {
            BLogger.Log($"{PlayerName} received {quantity}x {itemName}");
        }

        [BLoggerAction("setHealth")]
        public void SetHealth(int newHealth)
        {
            Health = Mathf.Clamp(newHealth, 0, 100);
            BLogger.Log($"{PlayerName} health set to {Health}");
        }

        [BLoggerAction("kill")]
        public void Kill()
        {
            Health = 0;
            BLogger.Log($"{PlayerName} was killed!");
        }

        [BLoggerAction("fullHeal")]
        public void FullHeal()
        {
            Health = 100;
            BLogger.Log($"{PlayerName} fully healed!");
        }
    }

    /// <summary>
    /// Example showing enemy instances
    /// Access pattern: @Enemies.boss.health, @Enemies.minion1.health
    /// Action pattern: !Enemies.boss.setPhase(2)
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
            BLogger.Log($"Enemy '{EnemyId}' spawned!");
            BLogger.Log($"Try: @Enemies.{EnemyId}.health @Enemies.{EnemyId}.phase @Enemies.{EnemyId}.threat");
            BLogger.Log($"Try: !Enemies.{EnemyId}.damage(100) or !Enemies.{EnemyId}.enrage()");
            
            // Example log showing all values at once
            InvokeRepeating(nameof(LogStatus), 2f, 2f);
        }

        private void LogStatus()
        {
            BLogger.Log("=== Status Report ===");
            BLogger.Log("P1=@Players.player1.health | Boss=@Enemies.boss.health (Phase @Enemies.boss.phase) | FPS: @fps");
        }

        private void Update()
        {
            Health = Mathf.Max(0, Health - (int)(Time.deltaTime * 5));
        }

        // ACTIONS

        [BLoggerAction("damage")]
        public void Damage(int amount)
        {
            Health = Mathf.Max(0, Health - amount);
            BLogger.Log($"{EnemyId} took {amount} damage. Health: {Health} (Phase {CurrentPhase})");
        }

        [BLoggerAction("enrage")]
        public void Enrage()
        {
            BLogger.Log($"{EnemyId} has enraged! Attack speed increased!");
        }

        [BLoggerAction("summonMinions")]
        public void SummonMinions(int count)
        {
            BLogger.Log($"{EnemyId} summoned {count} minions!");
        }

        [BLoggerAction("setPhase")]
        public void SetPhase(int phase)
        {
            // Calculate health needed for that phase
            Health = phase switch
            {
                1 => 800,
                2 => 500,
                3 => 100,
                _ => Health
            };
            BLogger.Log($"{EnemyId} forced to phase {phase}. Health: {Health}");
        }
    }

    /// <summary>
    /// Static global values and actions accessible anywhere
    /// Values: @fps, @frameCount, @timeScale
    /// Actions: !pause(true), !setTimeScale(0.5), !loadScene(MainMenu)
    /// </summary>
    public static class GameCommands
    {
        // VALUES
        
        [BLoggerValue("fps")]
        public static float FPS => 1f / Time.deltaTime;

        [BLoggerValue("frameCount")]
        public static int FrameCount => Time.frameCount;

        [BLoggerValue("timeScale")]
        public static float TimeScale => Time.timeScale;

        [BLoggerValue("playerCount")]
        public static int PlayerCount => Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length;

        [BLoggerValue("enemyCount")]
        public static int EnemyCount => Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None).Length;

        // ACTIONS

        [BLoggerAction("pause")]
        public static void PauseGame(bool paused)
        {
            Time.timeScale = paused ? 0 : 1;
            BLogger.Log($"Game {(paused ? "paused" : "resumed")}");
        }

        [BLoggerAction("setTimeScale")]
        public static void SetTimeScale(float scale)
        {
            Time.timeScale = scale;
            BLogger.Log($"Time scale set to {scale}");
        }

        [BLoggerAction("slowMotion")]
        public static void SlowMotion()
        {
            Time.timeScale = 0.3f;
            BLogger.Log("Slow motion activated!");
        }

        [BLoggerAction("normalSpeed")]
        public static void NormalSpeed()
        {
            Time.timeScale = 1f;
            BLogger.Log("Normal speed restored");
        }

        [BLoggerAction("logStats")]
        public static void LogStats()
        {
            BLogger.Log("=== Game Statistics ===");
            BLogger.Log($"FPS: @fps | Frame: @frameCount | TimeScale: @timeScale");
            BLogger.Log($"Players: @playerCount | Enemies: @enemyCount");
        }

        [BLoggerAction("testCombat")]
        public static void TestCombat()
        {
            BLogger.Log("=== Combat Test ===");
            BLogger.Log("Before: P1 Health = @Players.player1.health");
            BLogger.Log("!Players.player1.takeDamage(30)");
            BLogger.Log("After: P1 Health = @Players.player1.health");
            BLogger.Log("!Players.player1.heal(50)");
            BLogger.Log("Healed: P1 Health = @Players.player1.health");
        }
    }

    /// <summary>
    /// Example demonstrating various action patterns
    /// Attach this to a GameObject to see automated demos
    /// </summary>
    public class ActionDemoRunner : MonoBehaviour
    {
        private float _timer = 0f;
        private int _demoStep = 0;

        private void Update()
        {
            _timer += Time.deltaTime;

            if (_timer >= 5f)
            {
                _timer = 0f;
                RunNextDemo();
            }
        }

        private void RunNextDemo()
        {
            _demoStep++;

            switch (_demoStep)
            {
                case 1:
                    BLogger.Log("=== DEMO 1: Reading Values ===");
                    BLogger.Log("Player 1 Health: @Players.player1.health");
                    BLogger.Log("Boss Health: @Enemies.boss.health");
                    BLogger.Log("Current FPS: @fps");
                    break;

                case 2:
                    BLogger.Log("=== DEMO 2: Simple Actions ===");
                    BLogger.Log("!Players.player1.heal(25)");
                    break;

                case 3:
                    BLogger.Log("=== DEMO 3: Actions with Multiple Arguments ===");
                    BLogger.Log("!Players.player1.giveItem(Sword, 1)");
                    BLogger.Log("!Players.player1.giveItem(Potion, 5)");
                    break;

                case 4:
                    BLogger.Log("=== DEMO 4: Teleportation ===");
                    BLogger.Log("!Players.player1.teleport(10, 0, 5)");
                    break;

                case 5:
                    BLogger.Log("=== DEMO 5: Combat Simulation ===");
                    BLogger.Log("Boss damages player: !Players.player1.takeDamage(40)");
                    BLogger.Log("Player attacks boss: !Enemies.boss.damage(150)");
                    break;

                case 6:
                    BLogger.Log("=== DEMO 6: Time Control ===");
                    BLogger.Log("!slowMotion()");
                    break;

                case 7:
                    BLogger.Log("!normalSpeed()");
                    break;

                case 8:
                    BLogger.Log("=== DEMO 7: Combining Values and Actions ===");
                    BLogger.Log("Before heal: @Players.player1.health");
                    BLogger.Log("!Players.player1.fullHeal()");
                    BLogger.Log("After heal: @Players.player1.health");
                    break;

                case 9:
                    BLogger.Log("=== DEMO 8: Boss Phase Control ===");
                    BLogger.Log("Current phase: @Enemies.boss.phase");
                    BLogger.Log("!Enemies.boss.setPhase(3)");
                    BLogger.Log("New phase: @Enemies.boss.phase");
                    break;

                case 10:
                    BLogger.Log("=== DEMO 9: Static Actions ===");
                    BLogger.Log("!logStats()");
                    _demoStep = 0; // Reset
                    break;
            }
        }
    }
}
