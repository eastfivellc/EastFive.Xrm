using Microsoft.Xrm.Sdk;
using System;

namespace EastFive.Xrm
{
    [AttributeUsage(AttributeTargets.Property)]
    public class EntityAttributeAttribute : Attribute
    {
        public EntityAttributeAttribute(string logicalName)
        {
            this.LogicalName = logicalName;
        }
        
        /// <summary>
        /// What is provided by this value
        /// </summary>
        public virtual string LogicalName { get; private set; }
        
    }
}
