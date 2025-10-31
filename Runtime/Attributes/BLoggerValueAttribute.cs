using System;

namespace com.DvosTools.blogger.Attributes
{
    /// <summary>
    /// Marks a field, property, or method as accessible via BLogger terminal tokens.
    /// </summary>
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
