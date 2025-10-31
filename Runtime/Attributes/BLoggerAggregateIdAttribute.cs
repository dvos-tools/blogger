using System;

namespace com.DvosTools.blogger.Attributes
{
    /// <summary>
    /// Marks a field or property as the unique instance key for this object.
    /// Used in combination with [BLoggerAggregate] to identify specific instances.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class BLoggerAggregateIdAttribute : Attribute
    {
    }
}
