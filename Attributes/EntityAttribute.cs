using Microsoft.Xrm.Sdk;
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

        public TResult SelectTargetEntity<TResult>(Type entityType, IPluginExecutionContext context, 
                IOrganizationService service, ITracingService tracingService,
            Func<EntityBase, TResult> onValid,
            Func<TResult> onInvalid)
        {
            // The InputParameters collection contains all the data passed in the message request.
            if ((!context.InputParameters.Contains("Target")) ||
                (!(context.InputParameters["Target"] is Entity)))
            {
                tracingService.Trace("No target of type Entity");
                return onInvalid();
            }

            // Obtain the target entity from the input parameters.
            var entity = (Entity)context.InputParameters["Target"];
            tracingService.Trace($"Found entity of type [{entity.LogicalName}]");

            // Verify that the target entity represents the correct type.
            // If not, this plug-in was not registered correctly.
            if (entity.LogicalName != this.LogicalName)
            {
                tracingService.Trace($"Did not find entity of type [{this.LogicalName}]");
                return onInvalid();
            }

            if (!typeof(EntityBase).IsAssignableFrom(entityType))
            {
                tracingService.Trace($"Target entity, of type [{entityType.FullName}], is not of type EntityBase.");
                return onInvalid();
            }

            var entityBase = (EntityBase)Activator.CreateInstance(entityType, new object[] { entity, service, tracingService });
            return onValid(entityBase);
        }
    }
}
