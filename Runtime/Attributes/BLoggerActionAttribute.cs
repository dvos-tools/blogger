using System;

namespace com.DvosTools.blogger.Attributes
{
    /// <summary>
    /// Marks a method as executable via !actionName(args) tokens.
    /// Works on both static methods (global) and instance methods (within aggregates).
    /// Supports parameters: int, float, double, bool, string, Vector2, Vector3, enums.
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
    /// }
    /// 
    /// // Usage: !Players.player1.heal(50)
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
