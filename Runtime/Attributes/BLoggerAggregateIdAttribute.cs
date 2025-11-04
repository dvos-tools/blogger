using System;

namespace com.DvosTools.blogger.Attributes
{
    /// <summary>
    /// Marks a field or property as the unique instance identifier for a BLogger aggregate.
    /// <br/>
    /// Must be used with <see cref="BLoggerAggregateAttribute"/>. Each instance must have a unique ID.
    /// </summary>
    /// <example>
    /// <code>
    /// [BLoggerAggregate("Players")]
    /// public class PlayerController : MonoBehaviour
    /// {
    ///     [BLoggerAggregateId]
    ///     public string PlayerId = "player1";  // Set unique per instance
    ///     
    ///     [BLoggerValue("health")]
    ///     public int Health = 100;
    /// }
    /// 
    /// // Access: @Players.player1.health
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class BLoggerAggregateIdAttribute : Attribute
    {
    }
}
