using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Xrm
{
    public class EntityReference<TEntity> where TEntity : EntityBase
    {
        private IOrganizationService service;
        private Guid entityId;

        public Guid Id { get; }

        public TResult Retrieve<TResult>(
            Func<TEntity, TResult> onRetrieved,
            Func<string, TResult> onFailure,
            ITracingService tracingService = default(ITracingService))
        {
            return service.Retrieve(entityId, onRetrieved, onFailure, tracingService: tracingService);
        }
    }
}
