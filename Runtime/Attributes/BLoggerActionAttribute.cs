using System;

namespace com.DvosTools.blogger.Attributes
{
    /// <summary>
    /// Marks a method as executable via <c>/actionName(args)</c> tokens in the terminal.
    /// <br/>
    /// Works on both static methods (global) and instance methods (within <see cref="BLoggerAggregateAttribute"/> classes).
    /// <br/>
    /// <br/>
    /// Supported parameter types: <c>int</c>, <c>float</c>, <c>double</c>, <c>bool</c>, <c>string</c>, 
    /// <see cref="UnityEngine.Vector2"/>, <see cref="UnityEngine.Vector3"/>, enums.
    /// <br/>
    /// Vector2 and Vector3 use bracket notation: <c>[1,2]</c> for Vector2, <c>[1,2,3]</c> for Vector3.
    /// </summary>
    /// <example>
    /// <code>
    /// [BLoggerAggregate("Players")]
    /// public class PlayerController : MonoBehaviour
    /// {
    ///     [BLoggerAggregateId]
    ///     public string PlayerId = "player1";
    ///     
    ///     public int Health = 100;
    ///     
    ///     [BLoggerAction("heal")]
    ///     public void Heal(int amount)
    ///     {
    ///         Health += amount;
    ///     }
    ///     
    ///     [BLoggerAction("spawn")]
    ///     public void SpawnAt(string name, Vector3 position)
    ///     {
    ///         // Spawn logic
    ///     }
    /// }
    /// 
    /// // Usage: /Players.player1.heal(50)
    /// // Usage: /Players.player1.spawn("enemy", [1,2,3])
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class BLoggerActionAttribute : Attribute
    {
        public string ActionName { get; }

        public BLoggerActionAttribute(string actionName)
        {
            ActionName = actionName;
        }
    }
}
