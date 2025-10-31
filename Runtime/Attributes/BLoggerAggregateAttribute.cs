using System;

namespace com.DvosTools.blogger.Attributes
{
    /// <summary>
    /// Marks a class as a BLogger aggregate, allowing multiple instances to be tracked.
    /// The aggregate name groups all instances of this type together.
    /// Access pattern: @AggregateName.instanceKey.valueName
    /// </summary>
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
