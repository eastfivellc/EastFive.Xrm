using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EastFive.Xrm;
using Microsoft.Xrm.Sdk;

namespace EastFive.Xrm.Entities
{
    [Entity("quote")]
    public class PriceList : EntityBase
    {
        public PriceList(Entity entity, IOrganizationService service, ITracingService tracingService)
            : base(entity, service, tracingService)
        {
        }
    }
}
