// =====================================================================
//  This file is part of the Microsoft CRM SDK Code Samples.
//
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  This source code is intended only as a supplement to Microsoft
//  Development Tools and/or on-line documentation.  See these other
//  materials for detailed information regarding Microsoft code samples.
//
//  THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//  PARTICULAR PURPOSE.
// =====================================================================

//<snippetAccountNumberPlugin>
using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;

// Microsoft Dynamics CRM namespace(s)
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Discovery;
using Microsoft.Crm.Sdk.Messages;

using EastFive;
using EastFive.Xrm;
using EastFive.Extensions;
using EastFive.Linq;

namespace EastFive.Xrm
{
	public class Plugin : IPlugin
	{
        private System.Reflection.MethodInfo GetEntryMethod(ITracingService tracingService)
        {
            var entryMethod = this.GetType()
                .GetMethods()
                .Where(
                    method => method.ContainsCustomAttribute<XrmEntryAttribute>())
                .FirstOrDefault();
            if(entryMethod.IsDefault())
                tracingService.Trace("Entry method not found.");

            return entryMethod;
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(Microsoft.Xrm.Sdk.IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var entryMethod = GetEntryMethod(tracingService);

            var parameters = entryMethod
                .GetParameters()
                .Aggregate(
                    new object[] { },
                    (paramValues, param) =>
                    {
                        if (paramValues.IsDefaultOrNull())
                            return paramValues;

                        if (param.ParameterType.ContainsCustomAttribute<EntityAttribute>())
                            return param.ParameterType.GetCustomAttribute<EntityAttribute>()
                                .SelectTargetEntity(param.ParameterType, context, service, tracingService,
                                    entity => paramValues.Append(entity).ToArray(),
                                    () => default(object[]));

                        if (param.ParameterType.IsAssignableFrom(typeof(ITracingService)))
                            return paramValues.Append(tracingService).ToArray();

                        if (param.ParameterType.IsAssignableFrom(typeof(IPluginExecutionContext)))
                            return paramValues.Append(context).ToArray();

                        if (param.ParameterType.IsAssignableFrom(typeof(IOrganizationService)))
                            return paramValues.Append(service).ToArray();

                        tracingService.Trace($"Cannot populate parameter [{param.Name}] of type [{param.ParameterType.FullName}]");
                        return default(object[]);
                    });

            if(parameters.IsDefault())
            {
                tracingService.Trace($"Error while constructing call to entry method");
                return;
            }
            entryMethod.Invoke(this, parameters);
        }
    }
}
