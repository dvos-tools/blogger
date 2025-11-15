using System;

namespace com.DvosTools.blogger.Attributes
{
    /// <summary>
    /// Marks a field, property, or parameterless method as accessible via <c>/valueName</c> tokens in the terminal.
    /// <br/>
    /// Works on both static members (global) and instance members (within <see cref="BLoggerAggregateAttribute"/> classes).
    /// </summary>
    /// <example>
    /// <code>
    /// // Instance values (within aggregate)
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
    /// // Usage: /Players.player1.health
    /// 
    /// // Static values (global)
    /// public static class GameStats
    /// {
    ///     [BLoggerValue("fps")]
    ///     public static float FPS => 1f / Time.deltaTime;
    /// }
    /// // Usage: /fps
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false)]
    public class BLoggerValueAttribute : Attribute
    {
        public string Name { get; }

        public BLoggerValueAttribute(string name)
        {
            Name = name;
        }
    }
}
