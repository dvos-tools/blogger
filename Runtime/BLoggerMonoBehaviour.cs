using UnityEngine;

namespace com.DvosTools.blogger
{
    /// <summary>
    /// Base class for <see cref="UnityEngine.MonoBehaviour"/>s that use BLogger attributes.
    /// <br/>
    /// Automatically registers/unregisters with the terminal value system - no manual code needed!
    /// </summary>
    /// <remarks>
    /// <para>
    /// If you need to inherit from a different base class (like <c>NetworkBehaviour</c>),
    /// use <see cref="BLogger.AutoRegister"/> in <see cref="UnityEngine.MonoBehaviour.Awake"/> 
    /// and <see cref="BLogger.AutoUnregister"/> in <see cref="UnityEngine.MonoBehaviour.OnDestroy"/> instead.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [BLoggerAggregate("Player")]
    /// public class PlayerController : BLoggerMonoBehaviour
    /// {
    ///     [BLoggerAggregateId]
    ///     private string playerName = "player1";
    ///     
    ///     [BLoggerValue("health")]
    ///     private int health = 100;
    ///     
    ///     [BLoggerAction("heal")]
    ///     public void Heal(int amount)
    ///     {
    ///         health += amount;
    ///     }
    ///     
    ///     // No Awake or OnDestroy needed - handled automatically!
    /// }
    /// </code>
    /// </example>
    public abstract class BLoggerMonoBehaviour : MonoBehaviour
    {
        protected virtual void Awake()
        {
            // Auto-register this component if it has BLogger attributes
            BLogger.AutoRegister(this);
        }

        protected virtual void OnDestroy()
        {
            // Auto-unregister when destroyed
            BLogger.AutoUnregister(this);
        }
    }
}
