using EastFive.Xrm;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using EastFive.Linq.Expressions;
using System.Reflection;
using EastFive.Extensions;

namespace EastFive.Xrm
{
    public class EntityBase
    {
        public Guid Id { get { return this.entity.Id; } }

        public Entity entity;

        private ITracingService tracingService;

        public EntityBase(Entity entity, IOrganizationService service, ITracingService tracingService)
        {
            this.entity = entity;
            this.tracingService = tracingService;
        }

        public EntityReference ToEntityReference()
        {
            return entity.ToEntityReference();
        }

        protected Money GetAttribute<TEntity>(Expression<Func<TEntity, Money>> attributeRef)
            where TEntity : EntityBase
        {
            return attributeRef.PropertyInfo(
                propInfo =>
                {
                    return propInfo.GetCustomAttribute<EntityAttributeAttribute, Money>(
                        entityAttr =>
                        {
                            return (Money)entity.Attributes[entityAttr.LogicalName];
                        },
                        () =>
                        {
                            tracingService.Trace($"Entity attribute '{propInfo.Name}' does not have EntityAttribute");
                            return default(Money);
                        });
                },
                () =>
                {
                    tracingService.Trace("GetAttribute called without using property reference");
                    return attributeRef.Compile().Invoke((TEntity)this);
                });
        }

        protected void SetAttribute<TEntity>(Expression<Func<TEntity, Money>> attributeRef, Money value)
            where TEntity : EntityBase
        {
            this.SetAttribute(attributeRef, value, m => m.Value.ToString());
        }

        protected void SetAttribute<TEntity, TValue>(Expression<Func<TEntity, TValue>> attributeRef, TValue value,
                Func<TValue, string> extractValue = default(Func<TValue, string>))
            where TEntity : EntityBase
        {
            if (extractValue.IsDefaultOrNull())
                extractValue = (v) => v.ToString();

            var traceResult = attributeRef.PropertyInfo(
                propInfo =>
                {
                    return propInfo.GetCustomAttribute<EntityAttributeAttribute, string>(
                        entityAttr =>
                        {
                            tracingService.Trace($"{propInfo.Name} is being assigned to {extractValue(value)}");
                            if (entity.Attributes.ContainsKey(entityAttr.LogicalName))
                                entity.Attributes[entityAttr.LogicalName] = value;
                            else
                                entity.Attributes.Add(entityAttr.LogicalName, value);
                            return $"{propInfo.Name} updated {extractValue(value)}";
                        },
                        () => $"Entity attribute '{propInfo.Name}' does not have EntityAttribute");
                },
                () => "GetAttribute called without using property reference");
            tracingService.Trace(traceResult);
        }

        public void AssignAttribute<TEntityProperty, TEntity>(Expression<Func<TEntity, TEntityProperty>> entityProperty,
                ITracingService tracingService = default(ITracingService))
            where 
                TEntity : EntityBase
            where
                TEntityProperty : EntityBase
        {
            var entity = this;
            entityProperty.PropertyInfo(
                (propertyInfo) =>
                {
                    return propertyInfo.GetCustomAttribute(
                        (ReferencedEntityAttribute refAttribute) =>
                        {
                            var propertyName = refAttribute.PropertyName;
                            // If a quote has not been assigned, do not run yet
                            if (!entity.entity.Attributes.Contains(propertyName))
                            {
                                if (!tracingService.IsDefaultOrNull())
                                    tracingService.Trace($"Attribute [{propertyName}] not assigned. " +
                                        "Available attributes are {0}", String.Join(";", entity.entity.Attributes.Select(kvp => kvp.Key)));
                                return false;
                            }

                            if (!tracingService.IsDefaultOrNull())
                                tracingService.Trace($"[{propertyName}] attribute found");

                            if (!(entity.entity.Attributes[propertyName] is EntityReference))
                            {
                                if (!tracingService.IsDefaultOrNull())
                                    tracingService.Trace($"Attribute for [{propertyName}] is of type {entity.entity.Attributes[propertyName].GetType().FullName} not EntityReference");
                                return false;
                            }

                            // Get the reference and the ID
                            var reference = (EntityReference)entity.entity.Attributes[propertyName];
                            if (!tracingService.IsDefaultOrNull())
                                tracingService.Trace($"{propertyName} Id = {reference.Id.ToString()}");
                            var referenceId = reference.Id;

                            return true;
                        },
                        () => false);
                },
                () =>
                {
                    throw new ArgumentException();
                });

        }
    }
}
