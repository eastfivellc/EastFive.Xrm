using System;

namespace EastFive.Xrm
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ReferencedEntityAttribute : Attribute
    {
        public ReferencedEntityAttribute(string propertyName)
        {
            this.PropertyName = propertyName;
        }
        
        /// <summary>
        /// What is provided by this value
        /// </summary>
        public virtual string PropertyName { get; private set; }
    }
}
