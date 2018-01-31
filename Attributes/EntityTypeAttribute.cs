using System;

namespace EastFive.Xrm
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class EntityTypeAttribute : Attribute
    {
        public EntityTypeAttribute(string logicalName)
        {
            this.LogicalName = logicalName;
        }
        
        /// <summary>
        /// What is provided by this value
        /// </summary>
        public virtual string LogicalName { get; private set; }
    }
}
