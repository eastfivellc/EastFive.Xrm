using System;

namespace EastFive.Xrm
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EntityAttribute : Attribute
    {
        public EntityAttribute(string logicalName)
        {
            this.LogicalName = logicalName;
        }
        
        /// <summary>
        /// What is provided by this value
        /// </summary>
        public virtual string LogicalName { get; private set; }
    }
}
