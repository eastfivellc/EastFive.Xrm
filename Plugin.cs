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

// This namespace is found in Microsoft.Crm.Sdk.Proxy.dll assembly
// found in the SDK\bin folder.
using Microsoft.Crm.Sdk.Messages;
using EastFive.Xrm;

namespace EastFive.Xrm
{
	public class Plugin : IPlugin
	{
		/// <summary>
        /// A plug-in that auto generates an account number when an
        /// account is created.
		/// </summary>
        /// <remarks>Register this plug-in on the Create message, account entity,
        /// and pre-operation stage.
        /// </remarks>
        //<snippetAccountNumberPlugin2>
        public void Execute(IServiceProvider serviceProvider)
		{
            // Obtain the execution context from the service provider.
            Microsoft.Xrm.Sdk.IPluginExecutionContext context = (Microsoft.Xrm.Sdk.IPluginExecutionContext)
                serviceProvider.GetService(typeof(Microsoft.Xrm.Sdk.IPluginExecutionContext));

            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            tracingService.Trace("Tracing online");
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            tracingService.Trace("Factory online");
            var service = serviceFactory.CreateOrganizationService(context.UserId);
            tracingService.Trace("org online");

            // The InputParameters collection contains all the data passed in the message request.
            if ((!context.InputParameters.Contains("Target")) ||
                (!(context.InputParameters["Target"] is Entity)))
                return;

            tracingService.Trace("Target is Entity");

            // Obtain the target entity from the input parameters.
            var  entity = (Entity)context.InputParameters["Target"];
            //</snippetAccountNumberPlugin2>

            tracingService.Trace("Entity assigned");

            // Verify that the target entity represents an account.
            // If not, this plug-in was not registered correctly.
            if (entity.LogicalName != "new_quoteproduct")
                return;
            
            tracingService.Trace("Entity is quoteproduct");

            var quoteProductEntity = service.Retrieve("new_quoteproduct", entity.Id, new ColumnSet(true));

            // If a quote has not been assigned, do not run yet
            if (!quoteProductEntity.Attributes.Contains("new_quote"))
            {
                tracingService.Trace("Quote not assigned. Attributes are {0}", String.Join(";", quoteProductEntity.Attributes.Select(kvp => kvp.Key)));
                return;
            }

            tracingService.Trace("New quote found");

            // if a quote flow has not been assigned, this is invalid
            if (!quoteProductEntity.Attributes.Contains("new_quoteflow"))
                return;

            tracingService.Trace("New quoteflow found");

            // Get the quoteflow and quote
            var quoteRef = (Microsoft.Xrm.Sdk.EntityReference)quoteProductEntity.Attributes["new_quote"];
            tracingService.Trace("Guid for quote found of type [{0}]", quoteRef.GetType());
            var quoteFlowRef = (Microsoft.Xrm.Sdk.EntityReference)quoteProductEntity.Attributes["new_quoteflow"];
            tracingService.Trace("Guid for quoteflow found");

            tracingService.Trace($"Quote Id = {quoteRef.Id.ToString()}");
            var quoteId = quoteRef.Id;

            tracingService.Trace($"Quote Id = {quoteId}");
            
            var quoteEntity = service.Retrieve("quote", quoteId, new ColumnSet(true));
            tracingService.Trace("quote retrieved online");


            // Get new_producttype from QuoteFlow
            var quoteFlowId = quoteFlowRef.Id;
            tracingService.Trace("Retreiving QuoteFlow");
            var quoteFlowEntity = service.Retrieve("new_quoteflow", quoteFlowId, new ColumnSet(true));
            if (!quoteFlowEntity.Attributes.Contains("new_producttype"))
            {
                tracingService.Trace("No product type on quoteflow");
                return;
            }
            var productTypeRef = (Microsoft.Xrm.Sdk.EntityReference)quoteFlowEntity.Attributes["new_producttype"];
            var productTypeId = productTypeRef.Id;
            tracingService.Trace("Retreiving product");
            var productTypeEntity = service.Retrieve("product", productTypeId, new ColumnSet(true));

            var unitOfMeasureHoursId = Guid.Parse("16796173-ed05-e811-8115-c4346bac894c");

            var opportuntityRef = (Microsoft.Xrm.Sdk.EntityReference)quoteEntity.Attributes["opportunityid"];
            tracingService.Trace($"Opportuntity Ref Id = {opportuntityRef.Id.ToString()}");
            var opportuntityId = opportuntityRef.Id;
            tracingService.Trace($"Opportuntity Id = {opportuntityId.ToString()}");

            if (!GetOpportunityProductMatchingQuoteFlowId(service, opportuntityId, quoteFlowId, out EntityReference unitOfMeasureRef))
            {
                tracingService.Trace($"Unable to determine unit of measure");
                return;
            }

            // Get current product price level
            var quoteProductMrc = GetQuoteDetailMatchingQuoteProductId(service, quoteId, quoteProductEntity.Id,
                    default(Guid),
                (quoteProductMatching) => quoteProductMatching,
                () =>
                {
                    tracingService.Trace($"Trace 8.1");
                    // Create new product price level records
                    var quoteDetailEntity = new Entity("quotedetail");
                    quoteDetailEntity.Id = Guid.NewGuid();
                    tracingService.Trace($"Trace 8.2");
                    quoteDetailEntity.Attributes.Add("new_quoteproduct", new EntityReference("new_quoteproduct", quoteProductEntity.Id));
                    tracingService.Trace($"Trace 8.3");
                    quoteDetailEntity.Attributes.Add("quoteid", new EntityReference("quote", quoteId));
                    quoteDetailEntity.Attributes.Add("productid", productTypeEntity.ToEntityReference());
                    
                    quoteDetailEntity.Attributes.Add("uomid", unitOfMeasureRef);

                    tracingService.Trace($"Unit of measure added");
                    
                    service.Create(quoteDetailEntity);

                    tracingService.Trace($"Quote detail created");

                    return quoteDetailEntity;
                });

            if (null == quoteProductMrc)
            {
                tracingService.Trace("QuoteItem line time could not be created");
                return;
            }

            if(quoteProductMrc.Attributes.ContainsKey("uomid"))
                tracingService.Trace($"Unit of measure = {((EntityReference)quoteProductMrc.Attributes["uomid"]).Id}");

            tracingService.Trace($"Computing total");
            var mrc = (Money)quoteProductEntity.Attributes["new_totalmrc"];

            tracingService.Trace($"Total is {mrc.Value}");
            if (quoteProductMrc.Attributes.ContainsKey("extendedamount"))
                quoteProductMrc.Attributes["extendedamount"] = mrc;
            else
                quoteProductMrc.Attributes.Add("extendedamount", mrc);
            
            tracingService.Trace("atttribute updated");

            service.Update(quoteProductMrc);

            tracingService.Trace("quote updated");

            //var quoteProductsExpression = new QueryExpression("quotedetail");
            //tracingService.Trace($"Trace 1");

            //var quoteProductsFilterExpression = new FilterExpression(LogicalOperator.And);
            //tracingService.Trace($"Trace 2");
            //quoteProductsFilterExpression.Conditions.Add(new ConditionExpression("quoteid", ConditionOperator.Equal, quoteId));
            //tracingService.Trace($"Trace 3");
            //quoteProductsFilterExpression.Conditions.Add(new ConditionExpression("new_quoteproduct", ConditionOperator.Equal, entity.Id));
            //tracingService.Trace($"Trace 4");
            //quoteProductsExpression.ColumnSet = new ColumnSet(true);
            //tracingService.Trace($"Trace 5");
            //quoteProductsExpression.Criteria = quoteProductsFilterExpression;
            //tracingService.Trace($"Trace 6");

            //var quoteproductsMatching = service.RetrieveMultiple(quoteProductsExpression);
            //tracingService.Trace($"Trace 7");

            //if (!quoteproductsMatching.Entities.Any())
            //{

            //}


        }

        public static bool GetOpportunityProductMatchingQuoteFlowId(IOrganizationService service, Guid opportunityId, Guid quoteFlowId, out EntityReference unitOfMeasureRef)
        {
            var unitOfMeasureRef_ = default(EntityReference);
            var result = GetTableEntityWithoutIdByLookup(service, opportunityId, quoteFlowId, "opportunityproduct", "opportunityid", "new_quoteflow",
                (oppProduct) =>
                {
                    unitOfMeasureRef_ = (Microsoft.Xrm.Sdk.EntityReference)oppProduct.Attributes["uomid"];
                    return true;
                },
                () => false);

            unitOfMeasureRef = unitOfMeasureRef_;
            return result;
        }

        public static TResult GetQuoteDetailMatchingQuoteProductId<TResult>(IOrganizationService service,
                Guid quoteId, Guid quoteProductId, Guid? unitOfMeasure,
            Func<Entity, TResult> onFound,
            Func<TResult> onNotFound,
            ITracingService tracingService = default(ITracingService))
        {
            return GetTableEntitiesWithoutIdByLookup<TResult>(service, quoteId, quoteProductId,
                    "quotedetail", "quoteid", "new_quoteproduct",
                (entities) =>
                {
                    if (!entities.Any())
                        return onNotFound();

                    if (!unitOfMeasure.HasValue)
                        return onFound(entities.First());
                    var matchingEntities = entities
                        .Where(entity => entity.Attributes.ContainsKey("uomid"))
                        .Where(entity => ((EntityReference)entity.Attributes["uomid"]).Id == unitOfMeasure.Value);
                    if (!matchingEntities.Any())
                        return onNotFound();
                    return onFound(matchingEntities.First());
                },
                tracingService);
        }

        public static TResult GetTableEntityWithoutIdByLookup<TResult>(IOrganizationService service, Guid tableOwnerId, Guid lookupId,
            string entityLogicalName, string tableOwnerPropertyName, string lookupPropertyName,
            Func<Entity, TResult> onFound,
            Func<TResult> onNotFound,
            ITracingService tracingService = default(ITracingService))
        {
            return GetTableEntitiesWithoutIdByLookup(service, tableOwnerId, lookupId,
                entityLogicalName, tableOwnerPropertyName, lookupPropertyName,
                (entities) =>
                {
                    if (!entities.Any())
                        return onNotFound();


                    var tableEntity = entities.First();
                    return onFound(tableEntity);
                });
        }

        public static TResult GetTableEntitiesWithoutIdByLookup<TResult>(IOrganizationService service, Guid tableOwnerId, Guid lookupId,
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
//</snippetAccountNumberPlugin>
