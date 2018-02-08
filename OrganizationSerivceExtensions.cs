using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EastFive;
using EastFive.Extensions;
using Microsoft.Xrm.Sdk.Query;
using System.Linq.Expressions;
using EastFive.Linq.Expressions;

namespace EastFive.Xrm
{
    public static class OrganizationSerivceExtensions
    {
        public static TResult Retrieve<TEntity, TResult>(this IOrganizationService service, Guid entityId,
            Func<TEntity, TResult> onFound,
            Func<string, TResult> onFailure,
            string[] columns = default(string[]),
            ITracingService tracingService = default(ITracingService))
        {
            return typeof(TEntity).GetCustomAttribute(
                (EntityAttribute entityAttr) =>
                {
                    var columnSet = columns.IsDefault() ?
                        new ColumnSet(true)
                        :
                        new ColumnSet(columns);
                    var xrmEntity = service.Retrieve(entityAttr.LogicalName, entityId, columnSet);
                    var entity = xrmEntity.AsEntity<TEntity>();
                    if (entity.IsDefault())
                        return onFailure("Entity lacks constructors");
                    return onFound(entity);
                },
                () =>
                {
                    var msg = "Requested entity missing EntityAttribute";
                    if (!tracingService.IsDefaultOrNull())
                        tracingService.Trace(msg);
                    return onFailure(msg);
                });
        }

        public static TResult Synchronize<TEntity, TResult>(this TEntity entity, IOrganizationService service,
            Func<TEntity, TResult> onFound,
            Func<string, TResult> onFailure,
            ITracingService tracingService = default(ITracingService))
            where TEntity : EntityBase
        {
            return service.Retrieve(entity.entity.Id, onFound, onFailure,
                default(string[]),
                tracingService);
        }

        public static TEntity AsEntity<TEntity>(this Entity xrmEntity,
            ITracingService tracingService = default(ITracingService))
        {
            var validConstructors = typeof(TEntity).GetConstructors()
                        .Where(constructor => constructor.GetParameters().Length == 1)
                        .Where(constructor => constructor.GetParameters().First().ParameterType.IsAssignableFrom(typeof(Entity)));
            if (!validConstructors.Any())
            {
                if (!tracingService.IsDefaultOrNull())
                    tracingService.Trace($"Type [{typeof(TEntity).FullName}] does not have any valid constructors");
                return default(TEntity);
            }
            var entity = (TEntity)validConstructors.First().Invoke(new object[] { xrmEntity });
            return entity;
        }

        public static TResult GetTableEntitiesWithoutIdByLookup<TResult>(this IOrganizationService service, Guid tableOwnerId, Guid lookupId,
            string entityLogicalName, string tableOwnerPropertyName, string lookupPropertyName,
            Func<Entity[], TResult> onFound,
            ITracingService tracingService = default(ITracingService))
        {
            var opportunityProductsExpression = new QueryExpression(entityLogicalName);
            var opportunityProductsFilterExpression = new FilterExpression(LogicalOperator.And);
            //opportunityProductsFilterExpression.Conditions.Add(new ConditionExpression(tableOwnerPropertyName, ConditionOperator.Equal, tableOwnerId));
            opportunityProductsFilterExpression.Conditions.Add(new ConditionExpression(lookupPropertyName, ConditionOperator.Equal, lookupId));
            opportunityProductsExpression.ColumnSet = new ColumnSet(true);
            opportunityProductsExpression.Criteria = opportunityProductsFilterExpression;

            if (tracingService != null)
                tracingService.Trace($"Querying for '{entityLogicalName}' where `{tableOwnerPropertyName}`=`{tableOwnerId}` AND `{lookupPropertyName}`=`{lookupId}`");
            var opportunityProductsMatching = service.RetrieveMultiple(opportunityProductsExpression);
            var arrayResult = opportunityProductsMatching.Entities.ToArray();
            if (tracingService != null)
                tracingService.Trace($"Found {arrayResult.Length} `{entityLogicalName}`'s");
            return onFound(arrayResult);
        }
        
    }
}
