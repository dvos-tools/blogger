using System;

namespace com.DvosTools.blogger.Attributes
{
    /// <summary>
    /// Marks a method as executable via BLogger terminal actions.
    /// Actions can have parameters and are invoked using the ! prefix followed by arguments.
    /// </summary>
    /// <remarks>
    /// <para><b>Execution Patterns:</b></para>
    /// <list type="bullet">
    /// <item><b>Static actions:</b> !actionName(args) (e.g., !pause(true), !setTimeScale(0.5))</item>
    /// <item><b>Instance actions:</b> !AggregateName.instanceId.actionName(args) (e.g., !Players.player1.heal(50))</item>
    /// </list>
    /// 
    /// <para><b>Requirements:</b></para>
    /// <list type="bullet">
    /// <item>Method must be public</item>
    /// <item>Can be static or instance method</item>
    /// <item>Can have any number of parameters</item>
    /// <item>Can return a value (will be logged) or void</item>
    /// </list>
    /// 
    /// <para><b>Supported Parameter Types:</b></para>
    /// <list type="bullet">
    /// <item>int, float, double, bool, string</item>
    /// <item>Vector2, Vector3 (format: "x,y" or "x,y,z")</item>
    /// <item>Enums (by name or value)</item>
    /// </list>
    /// 
    /// <para><b>Example Usage - Static Actions:</b></para>
    /// <code>
    /// public static class GameCommands
    /// {
    ///     [BLoggerAction("pause")]
    ///     public static void PauseGame(bool paused)
    ///     {
    ///         Time.timeScale = paused ? 0 : 1;
    ///         BLogger.Log($"Game {(paused ? "paused" : "resumed")}");
    ///     }
    ///     
    ///     [BLoggerAction("setTimeScale")]
    ///     public static void SetTimeScale(float scale)
    ///     {
    ///         Time.timeScale = scale;
    ///         BLogger.Log($"Time scale set to {scale}");
    ///     }
    ///     
    ///     [BLoggerAction("loadLevel")]
    ///     public static void LoadLevel(string levelName)
    ///     {
    ///         SceneManager.LoadScene(levelName);
    ///     }
    /// }
    /// 
    /// // Invoke via logs:
    /// BLogger.Log("!pause(true)");  // Pauses the game
    /// BLogger.Log("!setTimeScale(0.5)");  // Slow motion
    /// BLogger.Log("!loadLevel(MainMenu)");  // Load scene
    /// </code>
    /// 
    /// <para><b>Example Usage - Instance Actions (No Parameters):</b></para>
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
    ///     [BLoggerAction("kill")]
    ///     public void Kill()
    ///     {
    ///         Health = 0;
    ///         BLogger.Log($"{PlayerId} was killed");
    ///     }
    ///     
    ///     [BLoggerAction("fullHeal")]
    ///     public void FullHeal()
    ///     {
    ///         Health = 100;
    ///         BLogger.Log($"{PlayerId} fully healed");
    ///     }
    /// }
    /// 
    /// // Invoke via logs:
    /// BLogger.Log("!Players.player1.kill()");
    /// BLogger.Log("!Players.player1.fullHeal()");
    /// </code>
    /// 
    /// <para><b>Example Usage - Instance Actions (With Parameters):</b></para>
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
    ///     public void Heal(int amount)
    ///     {
    ///         Health = Mathf.Min(100, Health + amount);
    ///         BLogger.Log($"{PlayerId} healed for {amount}. Health: {Health}");
    ///     }
    ///     
    ///     [BLoggerAction("teleport")]
    ///     public void Teleport(float x, float y, float z)
    ///     {
    ///         transform.position = new Vector3(x, y, z);
    ///         BLogger.Log($"{PlayerId} teleported to ({x}, {y}, {z})");
    ///     }
    ///     
    ///     [BLoggerAction("takeDamage")]
    ///     public bool TakeDamage(int damage, string source)
    ///     {
    ///         Health -= damage;
    ///         BLogger.Log($"{PlayerId} took {damage} damage from {source}");
    ///         return Health &lt;= 0;
    ///     }
    ///     
    ///     [BLoggerAction("giveItem")]
    ///     public void GiveItem(string itemName, int quantity)
    ///     {
    ///         BLogger.Log($"{PlayerId} received {quantity}x {itemName}");
    ///     }
    /// }
    /// 
    /// // Invoke via logs:
    /// BLogger.Log("!Players.player1.heal(25)");
    /// BLogger.Log("!Players.player1.teleport(10, 0, 5)");
    /// BLogger.Log("!Players.player1.takeDamage(50, boss)");  // Returns: False
    /// BLogger.Log("!Players.player1.giveItem(Sword, 1)");
    /// </code>
    /// 
    /// <para><b>Example Usage - Return Values:</b></para>
    /// <code>
    /// public static class MathCommands
    /// {
    ///     [BLoggerAction("add")]
    ///     public static int Add(int a, int b)
    ///     {
    ///         return a + b;
    ///     }
    ///     
    ///     [BLoggerAction("multiply")]
    ///     public static float Multiply(float a, float b)
    ///     {
    ///         return a * b;
    ///     }
    /// }
    /// 
    /// // Invoke via logs:
    /// BLogger.Log("!add(5, 3)");  // Returns: 8
    /// BLogger.Log("!multiply(2.5, 4)");  // Returns: 10.0
    /// </code>
    /// 
    /// <para><b>Combining with Values:</b></para>
    /// <code>
    /// // Check value, then perform action based on it
    /// BLogger.Log("Health before: @Players.player1.health");
    /// BLogger.Log("!Players.player1.heal(50)");
    /// BLogger.Log("Health after: @Players.player1.health");
    /// 
    /// // Output:
    /// // Health before: 50
    /// // player1 healed for 50. Health: 100
    /// // Health after: 100
    /// </code>
    /// 
    /// <para><b>Error Handling:</b></para>
    /// <code>
    /// BLogger.Log("!unknownAction()");
    /// // Output: !unknownAction()? (shown in red in terminal)
    /// 
    /// BLogger.Log("!Players.player1.heal(invalid)");
    /// // Output: [Error] Failed to convert argument 'invalid' to Int32
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class BLoggerActionAttribute : Attribute
    {
        /// <summary>
        /// Gets the name used to invoke this action.
        /// </summary>
        public string ActionName { get; }

        /// <summary>
        /// Marks a method as executable via BLogger terminal actions.
        /// </summary>
        /// <param name="actionName">The name used to invoke this action (e.g., "heal", "teleport", "pause")</param>
        public BLoggerActionAttribute(string actionName)
        {
            ActionName = actionName;
        }
    }
}
