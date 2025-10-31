using System;

namespace com.DvosTools.blogger.Attributes
{
    /// <summary>
    /// Marks a field or property as the unique instance identifier for a BLogger aggregate.
    /// The value of this field/property is used to distinguish between multiple instances of the same aggregate type.
    /// </summary>
    /// <remarks>
    /// <para><b>Must be used with:</b> [BLoggerAggregate] on the class level</para>
    /// 
    /// <para><b>Requirements:</b></para>
    /// <list type="bullet">
    /// <item>Field or property must be public</item>
    /// <item>Value must be convertible to string (via ToString())</item>
    /// <item>Each instance should have a unique ID within the same aggregate</item>
    /// <item>Only one [BLoggerAggregateId] per class</item>
    /// </list>
    /// 
    /// <para><b>Example Usage - Static ID:</b></para>
    /// <code>
    /// [BLoggerAggregate("Players")]
    /// public class PlayerController : MonoBehaviour
    /// {
    ///     [BLoggerAggregateId]
    ///     public string PlayerId = "player1"; // Hardcoded unique ID
    ///     
    ///     [BLoggerValue("health")]
    ///     public int Health = 100;
    /// }
    /// 
    /// // Access via: @Players.player1.health
    /// </code>
    /// 
    /// <para><b>Example Usage - Dynamic ID:</b></para>
    /// <code>
    /// [BLoggerAggregate("Players")]
    /// public class PlayerController : MonoBehaviour
    /// {
    ///     [BLoggerAggregateId]
    ///     public string PlayerId { get; private set; } // Dynamic ID set at runtime
    ///     
    ///     [BLoggerValue("health")]
    ///     public int Health = 100;
    ///     
    ///     private void Awake()
    ///     {
    ///         PlayerId = $"player_{GetInstanceID()}"; // Unique per instance
    ///     }
    /// }
    /// 
    /// // Access via: @Players.player_12345.health
    /// </code>
    /// 
    /// <para><b>Example Usage - Multiple Aggregates:</b></para>
    /// <code>
    /// [BLoggerAggregate("Enemies")]
    /// public class EnemyController : MonoBehaviour
    /// {
    ///     [BLoggerAggregateId]
    ///     public string EnemyType = "boss"; // "boss", "minion1", "minion2"
    ///     
    ///     [BLoggerValue("health")]
    ///     public int Health = 500;
    /// }
    /// 
    /// // Access different enemies:
    /// BLogger.Log("Boss: @Enemies.boss.health | Minion: @Enemies.minion1.health");
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class BLoggerAggregateIdAttribute : Attribute
    {
    }
}
