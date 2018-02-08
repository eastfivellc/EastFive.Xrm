using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Xrm.Entities
{
    [Entity("opportunity")]
    public class Opportunity : EntityBase
    {
        public Opportunity(Entity entity, IOrganizationService service, ITracingService tracingService)
            : base(entity, service, tracingService)
        {
        }
    }
}
