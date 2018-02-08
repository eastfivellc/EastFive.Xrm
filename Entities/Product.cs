using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Xrm.Entities
{
    [Entity("product")]
    public class Product : EntityBase
    {
        public Product(Entity entity, IOrganizationService service, ITracingService tracingService)
            : base(entity, service, tracingService)
        {
        }
    }
}
