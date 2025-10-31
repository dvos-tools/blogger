using System;

namespace com.DvosTools.blogger.Attributes
{
    /// <summary>
    /// Marks a class as a BLogger aggregate, allowing multiple instances to be tracked and accessed via terminal tokens.
    /// The aggregate name groups all instances of this type together, creating a namespace for accessing instance values.
    /// </summary>
    /// <remarks>
    /// <para><b>Access Pattern:</b> @AggregateName.instanceId.valueName</para>
    /// 
    /// <para><b>Requirements:</b></para>
    /// <list type="bullet">
    /// <item>Class must inherit from MonoBehaviour</item>
    /// <item>Class must have at least one field/property marked with [BLoggerAggregateId]</item>
    /// <item>Class should have fields/properties/methods marked with [BLoggerValue]</item>
    /// </list>
    /// 
    /// <para><b>Example Usage:</b></para>
    /// <code>
    /// [BLoggerAggregate("Players")]
    /// public class PlayerController : MonoBehaviour
    /// {
    ///     [BLoggerAggregateId]
    ///     public string PlayerId = "player1";
    ///     
    ///     [BLoggerValue("health")]
    ///     public int Health = 100;
    ///     
    ///     [BLoggerValue("position")]
    ///     public Vector3 Position => transform.position;
    /// }
    /// 
    /// // In your logs:
    /// BLogger.Log("Player health: @Players.player1.health at @Players.player1.position");
    /// // Output: "Player health: 100 at (0.0, 0.0, 0.0)"
    /// </code>
    /// 
    /// <para><b>Multiple Instances:</b></para>
    /// <code>
    /// // GameObject 1: PlayerController with PlayerId = "player1"
    /// // GameObject 2: PlayerController with PlayerId = "player2"
    /// 
    /// BLogger.Log("P1: @Players.player1.health | P2: @Players.player2.health");
    /// // Output: "P1: 100 | P2: 150"
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class BLoggerAggregateAttribute : Attribute
    {
        /// <summary>
        /// Gets the aggregate name that groups all instances of this type.
        /// </summary>
        public string AggregateName { get; }

        /// <summary>
        /// Marks a class as a BLogger aggregate with the specified aggregate name.
        /// </summary>
        /// <param name="aggregateName">The name used to group all instances (e.g., "Players", "Enemies", "Managers")</param>
        public BLoggerAggregateAttribute(string aggregateName)
        {
            AggregateName = aggregateName;
        }
    }
}
