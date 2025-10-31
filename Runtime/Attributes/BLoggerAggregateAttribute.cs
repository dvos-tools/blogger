using System;

namespace com.DvosTools.blogger.Attributes
{
    /// <summary>
    /// Marks a MonoBehaviour class as a BLogger aggregate, enabling tracking of multiple instances.
    /// Access: @AggregateName.instanceId.valueName or !AggregateName.instanceId.actionName(args)
    /// </summary>
    /// <example>
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
    ///     [BLoggerAction("heal")]
    ///     public void Heal(int amount) => Health += amount;
    /// }
    /// 
    /// // Usage: @Players.player1.health or !Players.player1.heal(50)
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class BLoggerAggregateAttribute : Attribute
    {
        public string AggregateName { get; }

        public BLoggerAggregateAttribute(string aggregateName)
        {
            AggregateName = aggregateName;
        }
    }
}
