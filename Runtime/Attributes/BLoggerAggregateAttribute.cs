using System;

namespace com.DvosTools.blogger.Attributes
{
    /// <summary>
    /// Marks a <see cref="UnityEngine.MonoBehaviour"/> class as a BLogger aggregate, enabling tracking of multiple instances.
    /// <br/>
    /// <br/>
    /// Terminal Access: 
    /// <br/>
    /// <c>/AggregateName.instanceId.valueName</c> - Read values (see <see cref="BLoggerValueAttribute"/>)
    /// <br/>
    /// <c>/AggregateName.instanceId.actionName(args)</c> - Execute actions (see <see cref="BLoggerActionAttribute"/>)
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
    /// // Usage: /Players.player1.health or /Players.player1.heal(50)
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
