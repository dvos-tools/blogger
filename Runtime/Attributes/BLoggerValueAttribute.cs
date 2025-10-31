using System;

namespace com.DvosTools.blogger.Attributes
{
    /// <summary>
    /// Marks a field, property, or method as accessible via BLogger terminal tokens.
    /// Values can be accessed in logs using the @ symbol followed by the value name.
    /// </summary>
    /// <remarks>
    /// <para><b>Access Patterns:</b></para>
    /// <list type="bullet">
    /// <item><b>Static values:</b> @valueName (e.g., @fps, @frameCount)</item>
    /// <item><b>Instance values:</b> @AggregateName.instanceId.valueName (e.g., @Players.player1.health)</item>
    /// </list>
    /// 
    /// <para><b>Supported Member Types:</b></para>
    /// <list type="bullet">
    /// <item>Public fields</item>
    /// <item>Public properties (must have getter)</item>
    /// <item>Public methods (must have no parameters and return a value)</item>
    /// </list>
    /// 
    /// <para><b>Example Usage - Static Values:</b></para>
    /// <code>
    /// public static class GameStats
    /// {
    ///     [BLoggerValue("fps")]
    ///     public static float FPS => 1f / Time.deltaTime;
    ///     
    ///     [BLoggerValue("frameCount")]
    ///     public static int FrameCount => Time.frameCount;
    ///     
    ///     [BLoggerValue("isPaused")]
    ///     public static bool IsPaused = false;
    /// }
    /// 
    /// // In your logs:
    /// BLogger.Log("FPS: @fps | Frame: @frameCount | Paused: @isPaused");
    /// // Output: "FPS: 60.5 | Frame: 12345 | Paused: False"
    /// </code>
    /// 
    /// <para><b>Example Usage - Instance Fields:</b></para>
    /// <code>
    /// [BLoggerAggregate("Players")]
    /// public class PlayerController : MonoBehaviour
    /// {
    ///     [BLoggerAggregateId]
    ///     public string PlayerId = "player1";
    ///     
    ///     [BLoggerValue("health")]
    ///     public int Health = 100; // Public field
    ///     
    ///     [BLoggerValue("mana")]
    ///     public float Mana { get; set; } // Property with getter/setter
    /// }
    /// 
    /// // Access via:
    /// BLogger.Log("Health: @Players.player1.health | Mana: @Players.player1.mana");
    /// </code>
    /// 
    /// <para><b>Example Usage - Computed Properties:</b></para>
    /// <code>
    /// [BLoggerAggregate("Players")]
    /// public class PlayerController : MonoBehaviour
    /// {
    ///     [BLoggerAggregateId]
    ///     public string PlayerId = "player1";
    ///     
    ///     public int Health = 100;
    ///     public int MaxHealth = 100;
    ///     
    ///     [BLoggerValue("healthPercent")]
    ///     public float HealthPercent => (float)Health / MaxHealth * 100f;
    ///     
    ///     [BLoggerValue("isAlive")]
    ///     public bool IsAlive => Health > 0;
    ///     
    ///     [BLoggerValue("position")]
    ///     public Vector3 Position => transform.position;
    /// }
    /// 
    /// // Access via:
    /// BLogger.Log("Health: @Players.player1.healthPercent% at @Players.player1.position");
    /// // Output: "Health: 75.5% at (10.0, 0.0, 5.0)"
    /// </code>
    /// 
    /// <para><b>Example Usage - Methods:</b></para>
    /// <code>
    /// [BLoggerAggregate("Enemies")]
    /// public class EnemyController : MonoBehaviour
    /// {
    ///     [BLoggerAggregateId]
    ///     public string EnemyId = "boss";
    ///     
    ///     public int Health = 1000;
    ///     
    ///     [BLoggerValue("threat")]
    ///     public string GetThreatLevel() // Parameterless method
    ///     {
    ///         return Health > 500 ? "HIGH" : "LOW";
    ///     }
    ///     
    ///     [BLoggerValue("distance")]
    ///     public float GetDistanceToPlayer()
    ///     {
    ///         var player = FindObjectOfType&lt;PlayerController&gt;();
    ///         return Vector3.Distance(transform.position, player.transform.position);
    ///     }
    /// }
    /// 
    /// // Access via:
    /// BLogger.Log("Boss threat: @Enemies.boss.threat | Distance: @Enemies.boss.distance");
    /// </code>
    /// 
    /// <para><b>Mixing Static and Instance Values:</b></para>
    /// <code>
    /// BLogger.Log("P1 Health: @Players.player1.health | P2 Health: @Players.player2.health | FPS: @fps");
    /// // Output: "P1 Health: 100 | P2 Health: 85 | FPS: 60.3"
    /// </code>
    /// 
    /// <para><b>Unknown Value Handling:</b></para>
    /// <code>
    /// BLogger.Log("Value: @unknownValue");
    /// // Output: "Value: @unknownValue?" (shown in red in terminal)
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false)]
    public class BLoggerValueAttribute : Attribute
    {
        /// <summary>
        /// Gets the name used to reference this value in terminal tokens.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Marks a field, property, or method as accessible via BLogger terminal tokens.
        /// </summary>
        /// <param name="name">The name used to reference this value (e.g., "health", "position", "fps")</param>
        public BLoggerValueAttribute(string name)
        {
            Name = name;
        }
    }
}
