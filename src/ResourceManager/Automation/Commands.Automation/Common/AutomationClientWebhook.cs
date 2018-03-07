// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using Hyak.Common;
using Microsoft.Azure.Commands.Automation.Properties;
using Microsoft.Azure.Management.Automation;
using Microsoft.Azure.Management.Automation.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;

namespace Microsoft.Azure.Commands.Automation.Common
{
    using System.Collections;
    using System.Linq;

    public partial class AutomationClient : IAutomationClient
    {
        public Model.Webhook CreateWebhook(
            string resourceGroupName,
            string automationAccountName,
            string name,
            string runbookName,
            bool isEnabled,
            DateTimeOffset expiryTime,
            IDictionary runbookParameters,
            string runOn)
        {
            Requires.Argument("ResourceGroupName", resourceGroupName).NotNull();
            Requires.Argument("AutomationAccountName", automationAccountName).NotNull();
            using (var request = new RequestSettings(this.automationManagementClient))
            {
                var rbAssociationProperty = new RunbookAssociationProperty { Name = runbookName };

                var webhookCreateOrUpdateParameters = new WebhookCreateOrUpdateParameters {
                    Name = name,
                    IsEnabled = isEnabled,
                    ExpiryTime = expiryTime.DateTime.ToUniversalTime(),
                    Runbook = rbAssociationProperty,
                    Uri = this.GetAutomationClient(resourceGroupName, automationAccountName).Webhook.GenerateUri(automationAccountName),
                    Parameters = (runbookParameters != null) ? this.ProcessRunbookParameters(resourceGroupName, automationAccountName, runbookName, runbookParameters) : null,
                    RunOn = runOn
                };

            var webhook =
                    this.GetAutomationClient(resourceGroupName, automationAccountName).Webhook.CreateOrUpdate(
                        automationAccountName,
                        name,
                        webhookCreateOrUpdateParameters);

                return new Model.Webhook(
                    resourceGroupName,
                    automationAccountName,
                    webhook,
                    webhookCreateOrUpdateParameters.Uri);
            }
        }

        public Model.Webhook GetWebhook(string resourceGroupName, string automationAccountName, string name)
        {
            Requires.Argument("ResourceGroupName", resourceGroupName).NotNull();
            Requires.Argument("AutomationAccountName", automationAccountName).NotNull();
            using (var request = new RequestSettings(this.automationManagementClient))
            {
                try
                {
                    var webhook =
                        this.GetAutomationClient(resourceGroupName, automationAccountName).Webhook.Get(automationAccountName, name);
                    if (webhook == null)
                    {
                        throw new ResourceNotFoundException(
                            typeof(Webhook),
                            string.Format(CultureInfo.CurrentCulture, Resources.WebhookNotFound, name));
                    }

                    return new Model.Webhook(resourceGroupName, automationAccountName, webhook);
                }
                catch (CloudException cloudException)
                {
                    if (cloudException.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        throw new ResourceNotFoundException(
                            typeof(Webhook),
                            string.Format(CultureInfo.CurrentCulture, Resources.WebhookNotFound, name));
                    }

                    throw;
                }
            }
        }

        public IEnumerable<Model.Webhook> ListWebhooks(string resourceGroupName, string automationAccountName, string runbookName, ref string nextLink)
        {
            Requires.Argument("ResourceGroupName", resourceGroupName).NotNull();
            Requires.Argument("AutomationAccountName", automationAccountName).NotNull();

            Rest.Azure.IPage<Webhook> response;

            using (var request = new RequestSettings(this.automationManagementClient))
            {
                if (string.IsNullOrEmpty(nextLink))
                {
                    if (runbookName == null)
                    {
                        response = this.GetAutomationClient(resourceGroupName, automationAccountName).Webhook.ListByAutomationAccount(
                            automationAccountName,
                            null);
                    }
                    else
                    {
                        response = this.GetAutomationClient(resourceGroupName, automationAccountName).Webhook.ListByAutomationAccount(
                            automationAccountName,
                            runbookName);
                    }
                }
                else
                {
                    response = this.GetAutomationClient(resourceGroupName, automationAccountName).Webhook.ListByAutomationAccountNext(nextLink);
                }

                nextLink = response.NextPageLink;
                return
                    response.Select(w => new Model.Webhook(resourceGroupName, automationAccountName, w))
                        .ToList();
            }
        }

        public Model.Webhook UpdateWebhook(
            string resourceGroupName,
            string automationAccountName,
            string name,
            IDictionary parameters,
            bool? isEnabled)
        {
            Requires.Argument("ResourceGroupName", resourceGroupName).NotNull();
            Requires.Argument("AutomationAccountName", automationAccountName).NotNull();
            using (var request = new RequestSettings(this.automationManagementClient))
            {
                var webhookModel =
                    this.GetAutomationClient(resourceGroupName, automationAccountName).Webhook.Get(automationAccountName, name);
                var webhookPatchParameters = new WebhookUpdateParameters
                {
                    Name = name
                };
                if (webhookModel != null)
                {
                    if (isEnabled != null)
                    {
                        webhookPatchParameters.IsEnabled = isEnabled.Value;
                    }
                    if (parameters != null)
                    {
                        webhookPatchParameters.Parameters =
                            this.ProcessRunbookParameters(resourceGroupName, automationAccountName, webhookModel.Runbook.Name, parameters);
                    }
                }
                
                var webhook =
                    this.GetAutomationClient(resourceGroupName, automationAccountName).Webhook.Update(
                        automationAccountName,
                        name,
                        webhookPatchParameters);

                return new Model.Webhook(resourceGroupName, automationAccountName, webhook);
            }
        }

        public void DeleteWebhook(string resourceGroupName, string automationAccountName, string name)
        {
            Requires.Argument("ResourceGroupName", resourceGroupName).NotNull();
            Requires.Argument("AutomationAccountName", automationAccountName).NotNull();
            using (var request = new RequestSettings(this.automationManagementClient))
            {
                try
                {
                    this.GetAutomationClient(resourceGroupName, automationAccountName).Webhook.Delete(automationAccountName, name);
                }
                catch (CloudException cloudException)
                {
                    if (cloudException.Response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    {
                        throw new ResourceNotFoundException(
                            typeof(Webhook),
                            string.Format(CultureInfo.CurrentCulture, Resources.WebhookNotFound, name));
                    }
                    throw;
                }
            }
        }
    }
}
