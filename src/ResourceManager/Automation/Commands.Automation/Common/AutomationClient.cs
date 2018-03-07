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
using Microsoft.Azure.Commands.Automation.Model;
using Microsoft.Azure.Commands.Automation.Properties;
using Microsoft.Azure.Commands.Common.Authentication;
using Microsoft.Azure.Commands.Common.Authentication.Models;
using Microsoft.Azure.Management.Automation;
using Microsoft.Azure.Management.Automation.Models;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using AutomationAccount = Microsoft.Azure.Commands.Automation.Model.AutomationAccount;
using AutomationManagement = Microsoft.Azure.Management.Automation;
using Certificate = Microsoft.Azure.Commands.Automation.Model.CertificateInfo;
using Connection = Microsoft.Azure.Commands.Automation.Model.Connection;
using Credential = Microsoft.Azure.Commands.Automation.Model.CredentialInfo;
using Job = Microsoft.Azure.Commands.Automation.Model.Job;
using JobSchedule = Microsoft.Azure.Commands.Automation.Model.JobSchedule;
using JobStream = Microsoft.Azure.Commands.Automation.Model.JobStream;
using Module = Microsoft.Azure.Commands.Automation.Model.Module;
using Runbook = Microsoft.Azure.Commands.Automation.Model.Runbook;
using Schedule = Microsoft.Azure.Commands.Automation.Model.Schedule;
using Variable = Microsoft.Azure.Commands.Automation.Model.Variable;
using HybridRunbookWorkerGroup = Microsoft.Azure.Commands.Automation.Model.HybridRunbookWorkerGroup;
using Microsoft.Azure.Commands.Common.Authentication.Abstractions;

namespace Microsoft.Azure.Commands.Automation.Common
{
    public partial class AutomationClient : IAutomationClient
    {
        private readonly AutomationManagement.IAutomationClient automationManagementClient;

        // Injection point for unit tests
        public AutomationClient()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public AutomationClient(IAzureContext context)
            : this(context.Subscription,
                AzureSession.Instance.ClientFactory.CreateArmClient<AutomationManagement.AutomationClient>(context,
                    AzureEnvironment.Endpoint.ResourceManager))
        {
        }

        public AutomationClient(IAzureSubscription subscription,
            AutomationManagement.IAutomationClient automationManagementClient)
        {
            Requires.Argument("automationManagementClient", automationManagementClient).NotNull();

            this.Subscription = subscription;
            this.automationManagementClient = automationManagementClient;
        }

        private void SetClientIdHeader(string clientRequestId)
        {
            var client = ((AutomationManagement.AutomationClient)this.automationManagementClient);
            client.HttpClient.DefaultRequestHeaders.Remove(Constants.ClientRequestIdHeaderName);
            client.HttpClient.DefaultRequestHeaders.Add(Constants.ClientRequestIdHeaderName, clientRequestId);
        }

        public IAzureSubscription Subscription { get; private set; }

        #region Account Operations

        public IEnumerable<Model.AutomationAccount> ListAutomationAccounts(string resourceGroupName, ref string nextLink)
        {
            Rest.Azure.IPage<AutomationManagement.Models.AutomationAccount> response;

            if (string.IsNullOrEmpty(nextLink))
            {
                response = this.GetAutomationClient(resourceGroupName, null).AutomationAccount.List();
            }
            else
            {
                response = this.GetAutomationClient(resourceGroupName, null).AutomationAccount.ListByResourceGroupNext(nextLink);
            }

            nextLink = response.NextPageLink;
            return response.Select(c => new AutomationAccount(resourceGroupName, c));
        }

        public AutomationAccount GetAutomationAccount(string resourceGroupName, string automationAccountName)
        {
            Requires.Argument("ResourceGroupName", resourceGroupName).NotNull();
            Requires.Argument("AutomationAccountName", automationAccountName).NotNull();

            var account = this.GetAutomationClient(resourceGroupName, automationAccountName).AutomationAccount.Get(resourceGroupName, automationAccountName);

            return new Model.AutomationAccount(resourceGroupName, account);
        }

        public AutomationAccount CreateAutomationAccount(string resourceGroupName, string automationAccountName,
            string location, string plan, IDictionary tags)
        {
            Requires.Argument("ResourceGroupName", resourceGroupName).NotNull();
            Requires.Argument("Location", location).NotNull();
            Requires.Argument("AutomationAccountName", automationAccountName).NotNull();

            IDictionary<string, string> accountTags = null;
            if (tags != null)
                accountTags = tags.Cast<DictionaryEntry>()
                    .ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value.ToString());

            var accountCreateOrUpdateParameters = new AutomationAccountCreateOrUpdateParameters()
            {
                Location = location,
                Name = automationAccountName,
                Sku = new Sku()
                {
                    Name = String.IsNullOrWhiteSpace(plan) ? Constants.DefaultPlan : plan,
                },
                Tags = accountTags
            };

            var account = this.GetAutomationClient(resourceGroupName, automationAccountName).AutomationAccount.CreateOrUpdate(resourceGroupName, automationAccountName, accountCreateOrUpdateParameters);

            return new AutomationAccount(resourceGroupName, account);
        }

        public AutomationAccount UpdateAutomationAccount(string resourceGroupName, string automationAccountName,
            string plan, IDictionary tags)
        {
            Requires.Argument("ResourceGroupName", resourceGroupName).NotNull();
            Requires.Argument("AutomationAccountName", automationAccountName).NotNull();

            var automationAccount = GetAutomationAccount(resourceGroupName, automationAccountName);

            IDictionary<string, string> accountTags = null;
            if (tags != null)
            {
                accountTags = tags.Cast<DictionaryEntry>()
                    .ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value.ToString());
            }
            else
            {
                accountTags = automationAccount.Tags.Cast<DictionaryEntry>()
                    .ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value.ToString());
                ;
            }

            var accountUpdateParameters = new AutomationAccountUpdateParameters()
            {
                Name = automationAccountName,
                Sku = new Sku()
                {
                    Name = String.IsNullOrWhiteSpace(plan) ? automationAccount.Plan : plan,
                },
                Tags = accountTags,
            };

            var account = this.GetAutomationClient(resourceGroupName, automationAccountName).AutomationAccount.Update(resourceGroupName, automationAccountName, accountUpdateParameters);

            return new AutomationAccount(resourceGroupName, account);
        }

        public void DeleteAutomationAccount(string resourceGroupName, string automationAccountName)
        {
            try
            {
                this.GetAutomationClient(resourceGroupName, automationAccountName).AutomationAccount.Delete(
                    resourceGroupName,
                    automationAccountName);
            }
            catch (CloudException cloudException)
            {
                if (cloudException.Response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    throw new ResourceNotFoundException(typeof(AutomationAccount),
                        string.Format(CultureInfo.CurrentCulture, Resources.AutomationAccountNotFound,
                            automationAccountName));
                }

                throw;
            }
        }

        #endregion

        #region Module

        public Module CreateModule(string resourceGroupName, string automationAccountName, Uri contentLink,
            string moduleName)
        {
            var createdModule = this.GetAutomationClient(resourceGroupName, automationAccountName).Module.CreateOrUpdate(resourceGroupName,
                automationAccountName,
                new AutomationManagement.Models.ModuleCreateOrUpdateParameters()
                {
                    Name = moduleName,
                    ContentLink = new AutomationManagement.Models.ContentLink()
                    {
                         Uri = contentLink.ToString(),
                         ContentHash = null,
                         Version = null
                    },
                });

            return this.GetModule(resourceGroupName, automationAccountName, moduleName);
        }

        public Module GetModule(string resourceGroupName, string automationAccountName, string name)
        {
            try
            {
                var module =
                    this.GetAutomationClient(resourceGroupName, automationAccountName).Module.Get(automationAccountName, name);
                return new Module(resourceGroupName, automationAccountName, module);
            }
            catch (CloudException cloudException)
            {
                if (cloudException.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new ResourceNotFoundException(typeof(Module),
                        string.Format(CultureInfo.CurrentCulture, Resources.ModuleNotFound, name));
                }

                throw;
            }
        }

        public IEnumerable<Module> ListModules(string resourceGroupName, string automationAccountName,
            ref string nextLink)
        {
            Rest.Azure.IPage<AutomationManagement.Models.Module> response;

            if (string.IsNullOrEmpty(nextLink))
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).Module.ListByAutomationAccount(automationAccountName);
            }
            else
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).Module.ListByAutomationAccountNext(nextLink);
            }

            nextLink = response.NextPageLink;
            return response.Select(c => new Module(resourceGroupName, automationAccountName, c));
        }

        public Module UpdateModule(string resourceGroupName, string automationAccountName, string name,
            Uri contentLinkUri, string contentLinkVersion)
        {
            var moduleModel =
                this.GetAutomationClient(resourceGroupName, automationAccountName).Module.Get(automationAccountName, name);
            if (contentLinkUri != null)
            {
                var moduleUpdateParameters = new AutomationManagement.Models.ModuleUpdateParameters();

                moduleUpdateParameters.Name = name;
                moduleUpdateParameters.ContentLink = new AutomationManagement.Models.ContentLink();
                moduleUpdateParameters.ContentLink.Uri = contentLinkUri.ToString();
                moduleUpdateParameters.ContentLink.Version =
                    (String.IsNullOrWhiteSpace(contentLinkVersion))
                        ? Guid.NewGuid().ToString()
                        : contentLinkVersion;

                moduleUpdateParameters.Tags = moduleModel.Tags;

                this.GetAutomationClient(resourceGroupName, automationAccountName).Module.Update(resourceGroupName, automationAccountName,
                    moduleUpdateParameters);
            }
            var updatedModule =
                this.GetAutomationClient(resourceGroupName, automationAccountName).Module.Get(automationAccountName, name);
            return new Module(resourceGroupName, automationAccountName, updatedModule);
        }

        public void DeleteModule(string resourceGroupName, string automationAccountName, string name)
        {
            try
            {
                this.GetAutomationClient(resourceGroupName, automationAccountName).Module.Delete(automationAccountName, name);
            }
            catch (CloudException cloudException)
            {
                if (cloudException.Response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    throw new ResourceNotFoundException(typeof(Module),
                        string.Format(CultureInfo.CurrentCulture, Resources.ModuleNotFound, name));
                }

                throw;
            }
        }

        #endregion

        #region Schedule Operations

        public Schedule CreateSchedule(string resourceGroupName, string automationAccountName, Schedule schedule)
        {
            var scheduleCreateOrUpdateParameters = new AutomationManagement.Models.ScheduleCreateOrUpdateParameters
            {
               Name = schedule.Name,
               StartTime = schedule.StartTime.DateTime,
               ExpiryTime = schedule.ExpiryTime.DateTime,
               Description = schedule.Description,
               Interval = schedule.Interval,
               Frequency = schedule.Frequency.ToString(),
               AdvancedSchedule = schedule.GetAdvancedSchedule(),
               TimeZone = schedule.TimeZone,
            };

            var scheduleCreateResponse = this.GetAutomationClient(resourceGroupName, automationAccountName).Schedule.CreateOrUpdate(
                resourceGroupName,
                automationAccountName,
                scheduleCreateOrUpdateParameters);

            return this.GetSchedule(resourceGroupName, automationAccountName, schedule.Name);
        }

        public void DeleteSchedule(string resourceGroupName, string automationAccountName, string scheduleName)
        {
            try
            {
                this.GetAutomationClient(resourceGroupName, automationAccountName).Schedule.Delete(automationAccountName, scheduleName);
            }
            catch (CloudException cloudException)
            {
                if (cloudException.Response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    throw new ResourceNotFoundException(typeof(Schedule),
                        string.Format(CultureInfo.CurrentCulture, Resources.ScheduleNotFound, scheduleName));
                }

                throw;
            }
        }

        public Schedule GetSchedule(string resourceGroupName, string automationAccountName, string scheduleName)
        {
            AutomationManagement.Models.Schedule scheduleModel = this.GetScheduleModel(resourceGroupName, automationAccountName,
                scheduleName);
            return this.CreateScheduleFromScheduleModel(resourceGroupName, automationAccountName, scheduleModel);
        }

        public IEnumerable<Schedule> ListSchedules(string resourceGroupName, string automationAccountName, ref string nextLink)
        {
            Rest.Azure.IPage<AutomationManagement.Models.Schedule> response;

            if (string.IsNullOrEmpty(nextLink))
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).Schedule.ListByAutomationAccount(automationAccountName);;
            }
            else
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).Schedule.ListByAutomationAccountNext(nextLink);
            }

            nextLink = response.NextPageLink;
            return response.Select(c => new Schedule(resourceGroupName, automationAccountName, c));
        }

        public Schedule UpdateSchedule(string resourceGroupName, string automationAccountName, string scheduleName, bool? isEnabled,
            string description)
        {
            AutomationManagement.Models.Schedule scheduleModel = this.GetScheduleModel(resourceGroupName, automationAccountName,
                scheduleName);
            isEnabled = (isEnabled.HasValue) ? isEnabled : scheduleModel.IsEnabled;
            description = description ?? scheduleModel.Description;
            return this.UpdateScheduleHelper(resourceGroupName, automationAccountName, scheduleName, isEnabled, description);
        }

        #endregion

        #region Runbook Operations

        public Runbook GetRunbook(string resourceGroupName, string automationAccountName, string runbookName)
        {
            var runbookModel = this.TryGetRunbookModel(resourceGroupName, automationAccountName, runbookName);
            if (runbookModel == null)
            {
                throw new ResourceCommonException(typeof(Runbook),
                    string.Format(CultureInfo.CurrentCulture, Resources.RunbookNotFound, runbookName));
            }

            return new Runbook(resourceGroupName, automationAccountName, runbookModel);
        }

        public IEnumerable<Runbook> ListRunbooks(string resourceGroupName, string automationAccountName, ref string nextLink)
        {
            Rest.Azure.IPage<AutomationManagement.Models.Runbook> response;

            if (string.IsNullOrEmpty(nextLink))
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).Runbook.ListByAutomationAccount(automationAccountName);;
            }
            else
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).Runbook.ListByAutomationAccountNext(nextLink);
            }

            nextLink = response.NextPageLink;
            return response.Select(c => new Runbook(resourceGroupName, automationAccountName, c));
        }

        public Runbook CreateRunbookByName(string resourceGroupName, string automationAccountName, string runbookName, string description,
            IDictionary tags, string type, bool? logProgress, bool? logVerbose, bool overwrite)
        {
            using (var request = new RequestSettings(this.automationManagementClient))
            {
                var runbookModel = this.TryGetRunbookModel(resourceGroupName, automationAccountName, runbookName);
                if (runbookModel != null && overwrite == false)
                {
                    throw new ResourceCommonException(typeof(Runbook),
                        string.Format(CultureInfo.CurrentCulture, Resources.RunbookAlreadyExists, runbookName));
                }

                IDictionary<string, string> runbooksTags = null;
                if (tags != null) runbooksTags = tags.Cast<DictionaryEntry>().ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value.ToString());

                var rdcparam = new RunbookCreateOrUpdateParameters()
                {
                    Name = runbookName,
                    Description = description,
                    RunbookType = String.IsNullOrWhiteSpace(type) ? RunbookTypeEnum.Script : type,
                    LogProgress = logProgress.HasValue && logProgress.Value,
                    LogVerbose = logVerbose.HasValue && logVerbose.Value,
                    Draft = new RunbookDraft(),
                    Tags = runbooksTags,
                    Location = GetAutomationAccount(resourceGroupName, automationAccountName).Location
                };

                this.GetAutomationClient(resourceGroupName, automationAccountName).Runbook.CreateOrUpdate(automationAccountName, runbookName, rdcparam);

                return this.GetRunbook(resourceGroupName, automationAccountName, runbookName);
            }
        }

        public Runbook ImportRunbook(string resourceGroupName, string automationAccountName, string runbookPath, string description, IDictionary tags, string type, bool? logProgress, bool? logVerbose, bool published, bool overwrite, string name)
        {
            var fileExtension = Path.GetExtension(runbookPath);

            if (0 !=
                string.Compare(fileExtension, Constants.SupportedFileExtensions.PowerShellScript,
                    StringComparison.OrdinalIgnoreCase) &&
                0 !=
                string.Compare(fileExtension, Constants.SupportedFileExtensions.Graph,
                    StringComparison.OrdinalIgnoreCase) &&
                0 !=
                string.Compare(fileExtension, Constants.SupportedFileExtensions.Python,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ResourceCommonException(typeof(Runbook),
                        string.Format(CultureInfo.CurrentCulture, Resources.InvalidImportFile, runbookPath));
            }

            // if graph runbook make sure type is not null and has right value
            if (0 == string.Compare(fileExtension, Constants.SupportedFileExtensions.Graph, StringComparison.OrdinalIgnoreCase) 
                && (string.IsNullOrWhiteSpace(type) || !IsGraphRunbook(type)))
            {
                throw new ResourceCommonException(typeof(Runbook),
                        string.Format(CultureInfo.CurrentCulture, Resources.InvalidRunbookTypeForExtension, fileExtension));
            }

            var runbookName = Path.GetFileNameWithoutExtension(runbookPath);

            if (String.IsNullOrWhiteSpace(name) == false)
            {
                if (0 == string.Compare(type, Constants.RunbookType.PowerShellWorkflow, StringComparison.OrdinalIgnoreCase))
                {
                    if (0 != string.Compare(runbookName, name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        throw new ResourceCommonException(typeof(Runbook),
                        string.Format(CultureInfo.CurrentCulture, Resources.FileNameRunbookNameMismatch));
                    }
                }

                runbookName = name;
            }

            using (var request = new RequestSettings(this.automationManagementClient))
            {
                var runbook = this.CreateRunbookByName(resourceGroupName, automationAccountName, runbookName, description, tags, type, logProgress, logVerbose, overwrite);

                this.GetAutomationClient(resourceGroupName, automationAccountName).RunbookDraft.CreateOrUpdate(automationAccountName, runbookName, File.OpenRead(runbookPath));

                if (published)
                {
                    runbook = this.PublishRunbook(resourceGroupName, automationAccountName, runbookName);
                }

                return runbook;
            }
        }

        public void DeleteRunbook(string resourceGroupName, string automationAccountName, string runbookName)
        {
            try
            {
                using (var request = new RequestSettings(this.automationManagementClient))
                {
                    this.GetAutomationClient(resourceGroupName, automationAccountName).Runbook.Delete(automationAccountName, runbookName);
                }
            }
            catch (CloudException cloudException)
            {
                if (cloudException.Response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    throw new ResourceNotFoundException(typeof(Connection),
                        string.Format(CultureInfo.CurrentCulture, Resources.RunbookNotFound, runbookName));
                }

                throw;
            }

        }

        public Runbook UpdateRunbook(string resourceGroupName, string automationAccountName, string runbookName, string description,
            IDictionary tags, bool? logProgress, bool? logVerbose)
        {
            using (var request = new RequestSettings(this.automationManagementClient))
            {
                var runbookModel = this.TryGetRunbookModel(resourceGroupName, automationAccountName, runbookName);
                if (runbookModel == null)
                {
                    throw new ResourceCommonException(typeof(Runbook),
                        string.Format(CultureInfo.CurrentCulture, Resources.RunbookNotFound, runbookName));
                }

                var runbookUpdateParameters = new RunbookUpdateParameters();
                runbookUpdateParameters.Name = runbookName;
                runbookUpdateParameters.Tags = null;

                IDictionary<string, string> runbooksTags = null;
                if (tags != null) runbooksTags = tags.Cast<DictionaryEntry>().ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value.ToString());

                runbookUpdateParameters.Description = description ?? runbookModel.Description;
                runbookUpdateParameters.LogProgress = (logProgress.HasValue)
                    ? logProgress.Value
                    : runbookModel.LogProgress;
                runbookUpdateParameters.LogVerbose = (logVerbose.HasValue)
                    ? logVerbose.Value
                    : runbookModel.LogVerbose;
                runbookUpdateParameters.Tags = runbooksTags ?? runbookModel.Tags;

                var runbook =
                    this.GetAutomationClient(resourceGroupName, automationAccountName).Runbook.Update(resourceGroupName, automationAccountName, runbookUpdateParameters);

                return new Runbook(resourceGroupName, automationAccountName, runbook);
            }
        }

        public DirectoryInfo ExportRunbook(string resourceGroupName, string automationAccountName,
            string runbookName, bool? isDraft, string outputFolder, bool overwrite)
        {
            DirectoryInfo ret = null;
            using (var request = new RequestSettings(this.automationManagementClient))
            {
                var runbook = this.TryGetRunbookModel(resourceGroupName, automationAccountName, runbookName);
                if (runbook == null)
                {
                    throw new ResourceNotFoundException(typeof(Runbook),
                        string.Format(CultureInfo.CurrentCulture, Resources.RunbookNotFound, runbookName));
                }

                var draftContent = String.Empty;
                var publishedContent = String.Empty;

                if (0 !=
                    String.Compare(runbook.State, RunbookState.Published, CultureInfo.InvariantCulture,
                        CompareOptions.IgnoreCase) && (!isDraft.HasValue || isDraft.Value))
                {
                    var stream = this.GetAutomationClient(resourceGroupName, automationAccountName).RunbookDraft.GetContent(automationAccountName, runbookName);
                    if (stream != null)
                    {
                        var reader = new StreamReader(stream);
                        draftContent = reader.ReadToEnd();
                    }
                }
                if (0 !=
                    String.Compare(runbook.State, RunbookState.New, CultureInfo.InvariantCulture,
                        CompareOptions.IgnoreCase) && (!isDraft.HasValue || !isDraft.Value))
                {
                    var stream = this.GetAutomationClient(resourceGroupName, automationAccountName).Runbook.GetContent(automationAccountName, runbookName);
                    if (stream != null)
                    {
                        var reader = new StreamReader(stream);
                        publishedContent = reader.ReadToEnd();
                    }
                }

                // if no slot specified return both draft and publish content
                if (false == isDraft.HasValue)
                {
                    if (false == String.IsNullOrEmpty(publishedContent))
                    {
                        ret = WriteRunbookToFile(outputFolder, runbook.Name, publishedContent, runbook.RunbookType,
                               overwrite);
                    }
                    else if (false == String.IsNullOrEmpty(draftContent))
                    {
                        ret = WriteRunbookToFile(outputFolder, runbook.Name, draftContent, runbook.RunbookType,
                                overwrite);
                    }
                }
                else
                {
                    if (true == isDraft.Value)
                    {

                        if (String.IsNullOrEmpty(draftContent))
                            throw new ResourceCommonException(typeof(Runbook),
                                string.Format(CultureInfo.CurrentCulture, Resources.RunbookHasNoDraftVersion,
                                    runbookName));
                        if (false == String.IsNullOrEmpty(draftContent))
                            ret = WriteRunbookToFile(outputFolder, runbook.Name, draftContent, runbook.RunbookType,
                                overwrite);
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(publishedContent))
                            throw new ResourceCommonException(typeof(Runbook),
                                string.Format(CultureInfo.CurrentCulture, Resources.RunbookHasNoPublishedVersion,
                                    runbookName));

                        if (false == String.IsNullOrEmpty(publishedContent))
                            ret = WriteRunbookToFile(outputFolder, runbook.Name, publishedContent, runbook.RunbookType,
                                overwrite);
                    }
                }
            }

            return ret;
        }

        public Runbook PublishRunbook(string resourceGroupName, string automationAccountName, string runbookName)
        {
            using (var request = new RequestSettings(this.automationManagementClient))
            {
                this.GetAutomationClient(resourceGroupName, automationAccountName).RunbookDraft.Publish(automationAccountName, runbookName);

                return this.GetRunbook(resourceGroupName, automationAccountName, runbookName);
            }
        }

        public Job StartRunbook(string resourceGroupName, string automationAccountName, string runbookName, IDictionary parameters, string runOn)
        {
            IDictionary<string, string> processedParameters = this.ProcessRunbookParameters(resourceGroupName, automationAccountName,
                runbookName, parameters);
            var job = this.GetAutomationClient(resourceGroupName, automationAccountName).Job.Create(
                automationAccountName,
                Guid.NewGuid(),
                new JobCreateParameters
                {
                    Runbook = new RunbookAssociationProperty
                    {
                        Name = runbookName
                    },
                    RunOn = String.IsNullOrWhiteSpace(runOn) ? null : runOn,
                    Parameters = processedParameters ?? null
                });

            return new Job(resourceGroupName, automationAccountName, job);
        }

        #endregion

        #region Variables

        public Variable CreateVariable(Variable variable)
        {
            bool variableExists = true;

            try
            {
                this.GetVariable(variable.ResourceGroupName, variable.AutomationAccountName, variable.Name);
            }
            catch (ResourceNotFoundException)
            {
                variableExists = false;
            }

            if (variableExists)
            {
                throw new AzureAutomationOperationException(string.Format(CultureInfo.CurrentCulture,
                    Resources.VariableAlreadyExists, variable.Name));
            }

            var createParams = new AutomationManagement.Models.VariableCreateOrUpdateParameters()
            {
                Name = variable.Name,
                Value = PowerShellJsonConverter.Serialize(variable.Value),
                Description = variable.Description,
                IsEncrypted = variable.Encrypted
            };

            var sdkCreatedVariable = this.GetAutomationClient(variable.ResourceGroupName, variable.AutomationAccountName).Variable.CreateOrUpdate(variable.AutomationAccountName, variable.Name, createParams);

            return new Variable(sdkCreatedVariable, variable.AutomationAccountName, variable.ResourceGroupName);
        }

        public void DeleteVariable(string resourceGroupName, string automationAccountName, string variableName)
        {
            try
            {
                this.GetAutomationClient(resourceGroupName, automationAccountName).Variable.Delete(automationAccountName, variableName);
            }
            catch (CloudException cloudException)
            {
                if (cloudException.Response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    throw new ResourceNotFoundException(typeof(Variable),
                        string.Format(CultureInfo.CurrentCulture, Resources.VariableNotFound, variableName));
                }

                throw;
            }
        }

        public Variable UpdateVariable(Variable variable, VariableUpdateFields updateFields)
        {
            var existingVariable = this.GetVariable(variable.ResourceGroupName, variable.AutomationAccountName, variable.Name);

            if (existingVariable.Encrypted != variable.Encrypted && updateFields == VariableUpdateFields.OnlyValue)
            {
                throw new ResourceNotFoundException(typeof(Variable),
                    string.Format(CultureInfo.CurrentCulture, Resources.VariableEncryptionCannotBeChanged, variable.Name,
                        existingVariable.Encrypted));
            }

            var updateParams = new AutomationManagement.Models.VariableUpdateParameters()
            {
                Name = variable.Name,
            };

            if (updateFields == VariableUpdateFields.OnlyDescription)
            {
                updateParams.Description = variable.Description;
            }
            else
            {
                updateParams.Value = PowerShellJsonConverter.Serialize(variable.Value);
            }

            this.GetAutomationClient(variable.ResourceGroupName, variable.AutomationAccountName).Variable.Update(variable.AutomationAccountName, variable.Name, updateParams);

            return this.GetVariable(variable.ResourceGroupName, variable.AutomationAccountName, variable.Name);
        }

        public Variable GetVariable(string resourceGroupName, string automationAccountName, string name)
        {
            try
            {
                var sdkVarible = this.GetAutomationClient(resourceGroupName, automationAccountName).Variable.Get(automationAccountName, name);

                if (sdkVarible != null)
                {
                    return new Variable(sdkVarible, automationAccountName, resourceGroupName);
                }

                throw new ResourceNotFoundException(typeof(Variable),
                    string.Format(CultureInfo.CurrentCulture, Resources.VariableNotFound, name));
            }
            catch (CloudException)
            {
                throw new ResourceNotFoundException(typeof(Variable),
                    string.Format(CultureInfo.CurrentCulture, Resources.VariableNotFound, name));
            }
        }

        public IEnumerable<Variable> ListVariables(string resourceGroupName, string automationAccountName, ref string nextLink)
        {
            Rest.Azure.IPage<AutomationManagement.Models.Variable> response;

            if (string.IsNullOrEmpty(nextLink))
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).Variable.ListByAutomationAccount(automationAccountName);
            }
            else
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).Variable.ListByAutomationAccountNext(nextLink);
            }

            nextLink = response.NextPageLink;
            return response.Select(c => new Variable(c, automationAccountName, resourceGroupName));
        }

        #endregion

        #region Credentials

        public CredentialInfo CreateCredential(string resourceGroupName, string automationAccountName, string name, string userName,
            string password,
            string description)
        {
            var credentialCreateParams = new AutomationManagement.Models.CredentialCreateOrUpdateParameters();
            credentialCreateParams.Name = name;
            if (description != null) credentialCreateParams.Description = description;

            Requires.Argument("userName", userName).NotNull();
            Requires.Argument("password", password).NotNull();

            credentialCreateParams.UserName = userName;
            credentialCreateParams.Password = password;

            var createdCredential = this.GetAutomationClient(resourceGroupName, automationAccountName).Credential.CreateOrUpdate(automationAccountName, name, credentialCreateParams);

            return new CredentialInfo(resourceGroupName, automationAccountName, createdCredential);
        }

        public CredentialInfo UpdateCredential(string resourceGroupName, string automationAccountName, string name, string userName,
            string password,
            string description)
        {
            var exisitngCredential = this.GetCredential(resourceGroupName, automationAccountName, name);
            var credentialUpdateParams = new CredentialUpdateParameters();
            credentialUpdateParams.Name = name;
            credentialUpdateParams.Description = description ?? exisitngCredential.Description;

            credentialUpdateParams.UserName = userName;
            credentialUpdateParams.Password = password;

            var credential = this.GetAutomationClient(resourceGroupName, automationAccountName).Credential.Update(resourceGroupName, automationAccountName,
                credentialUpdateParams);

            var updatedCredential = this.GetCredential(resourceGroupName, automationAccountName, name);

            return updatedCredential;
        }

        public CredentialInfo GetCredential(string resourceGroupName, string automationAccountName, string name)
        {
            var credential = this.GetAutomationClient(resourceGroupName, automationAccountName).Credential.Get(automationAccountName, name);
            if (credential == null)
            {
                throw new ResourceNotFoundException(typeof(Credential),
                    string.Format(CultureInfo.CurrentCulture, Resources.CredentialNotFound, name));
            }

            return new CredentialInfo(resourceGroupName, automationAccountName, credential);
        }

        public IEnumerable<Credential> ListCredentials(string resourceGroupName, string automationAccountName, ref string nextLink)
        {
            Rest.Azure.IPage<AutomationManagement.Models.Credential> response;

            if (string.IsNullOrEmpty(nextLink))
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).Credential.ListByAutomationAccount(automationAccountName);
            }
            else
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).Credential.ListByAutomationAccountNext(nextLink);
            }

            nextLink = response.NextPageLink;
            return response.Select(c => new Credential(resourceGroupName, automationAccountName, c));
        }

        public void DeleteCredential(string resourceGroupName, string automationAccountName, string name)
        {
            try
            {
                this.GetAutomationClient(resourceGroupName, automationAccountName).Credential.Delete(automationAccountName, name);
            }
            catch (CloudException cloudException)
            {
                if (cloudException.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new ResourceNotFoundException(typeof(Credential),
                        string.Format(CultureInfo.CurrentCulture, Resources.CredentialNotFound, name));
                }

                throw;
            }
        }

        #endregion

        #region Job

        public IEnumerable<JobStream> GetJobStream(string resourceGroupName, string automationAccountName, Guid jobId, DateTimeOffset? time,
            string streamType, ref string nextLink)
        {
            string listParams = null;
            /* todo: replace with filter
            var listParams = new AutomationManagement.Models.JobStreamListParameter();

            if (time.HasValue)
            {
                listParams.Time = this.FormatDateTime(time.Value);
            }

            if (streamType != null)
            {
                listParams.StreamType = streamType;
            }

            */
            Rest.Azure.IPage<AutomationManagement.Models.JobStream> response;

            if (string.IsNullOrEmpty(nextLink))
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).JobStream.ListByJob(automationAccountName, jobId.ToString(), listParams);
            }
            else
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).JobStream.ListByJobNext(nextLink);
            }

            nextLink = response.NextPageLink;
            return
                response.Select(
                    stream => this.CreateJobStreamFromJobStreamModel(stream, resourceGroupName, automationAccountName, jobId));
        }

        public JobStreamRecord GetJobStreamRecord(string resourceGroupName, string automationAccountName, Guid jobId, string jobStreamId)
        {
            var response = this.GetAutomationClient(resourceGroupName, automationAccountName).JobStream.Get(automationAccountName, jobId.ToString(), jobStreamId);

            return new JobStreamRecord(response, resourceGroupName, automationAccountName, jobId);
        }

        public object GetJobStreamRecordAsPsObject(string resourceGroupName, string automationAccountName, Guid jobId, string jobStreamId)
        {
            var response = this.GetAutomationClient(resourceGroupName, automationAccountName).JobStream.Get(automationAccountName, jobId.ToString(), jobStreamId);

            if (response == null || response.Value == null) return null;

            // PowerShell Workflow runbook jobs would have the below additional properties, remove them from job output
            // we do not know the runbook type, remove will only remove if exists 
            response.Value.Remove("PSComputerName");
            response.Value.Remove("PSShowComputerName");
            response.Value.Remove("PSSourceJobInstanceId");

            var paramTable = new Hashtable();

            foreach (var kvp in response.Value)
            {
                object paramValue;
                try
                {
                    paramValue = ((object)PowerShellJsonConverter.Deserialize(kvp.Value.ToString()));
                }
                catch (CmdletInvocationException exception)
                {
                    if (!exception.Message.Contains("Invalid JSON primitive"))
                        throw;

                    paramValue = kvp.Value;
                }

                // for primitive outputs, the record will be in form "value" : "primitive type value". Return the key and return the primitive type value  
                if (response.Value.Count == 1 && response.Value.ContainsKey("value"))
                {
                    return paramValue;
                }

                paramTable.Add(kvp.Key, paramValue);
            }

            return paramTable;
        }

        public Job GetJob(string resourceGroupName, string automationAccountName, Guid Id)
        {
            var job = this.GetAutomationClient(resourceGroupName, automationAccountName).Job.Get(automationAccountName, Id);
            if (job == null)
            {
                throw new ResourceNotFoundException(typeof(Job),
                    string.Format(CultureInfo.CurrentCulture, Resources.JobNotFound, Id));
            }

            return new Job(resourceGroupName, automationAccountName, job);
        }

        public IEnumerable<Job> ListJobsByRunbookName(string resourceGroupName, string automationAccountName, string runbookName,
            DateTimeOffset? startTime, DateTimeOffset? endTime, string jobStatus, ref string nextLink)
        {
            Rest.Azure.IPage<AutomationManagement.Models.Job> response;

            if (string.IsNullOrEmpty(nextLink))
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).Job.List(
                    automationAccountName,
                    new JobListParameters
                    {
                        StartTime = (startTime.HasValue) ? FormatDateTime(startTime.Value) : null,
                        EndTime = (endTime.HasValue) ? FormatDateTime(endTime.Value) : null,
                        RunbookName = runbookName,
                        Status = jobStatus,
                    });
            }
            else
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).Job.ListByAutomationAccountNext(nextLink);
            }

            nextLink = response.NextPageLink;
            return response.Select(c => new Job(resourceGroupName, automationAccountName, c));
        }

        public IEnumerable<Job> ListJobs(string resourceGroupName, string automationAccountName, DateTimeOffset? startTime,
            DateTimeOffset? endTime, string jobStatus, ref string nextLink)
        {
            Rest.Azure.IPage<AutomationManagement.Models.Job> response;

            if (string.IsNullOrEmpty(nextLink))
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).Job.ListByAutomationAccount(
                    automationAccountName,
                    null
                    /*
                    new JobListParameters
                    {
                        StartTime = (startTime.HasValue) ? FormatDateTime(startTime.Value) : null,
                        EndTime = (endTime.HasValue) ? FormatDateTime(endTime.Value) : null,
                        Status = jobStatus,
                    }
                    */);
            }
            else
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).Job.ListByAutomationAccountNext(nextLink);
            }

            nextLink = response.NextPageLink;
            return response.Select(c => new Job(resourceGroupName, automationAccountName, c));
        }

        public void ResumeJob(string resourceGroupName, string automationAccountName, Guid id)
        {
            this.GetAutomationClient(resourceGroupName, automationAccountName).Job.Resume(automationAccountName, id);
        }

        public void StopJob(string resourceGroupName, string automationAccountName, Guid id)
        {
            this.GetAutomationClient(resourceGroupName, automationAccountName).Job.Stop(automationAccountName, id);
        }

        public void SuspendJob(string resourceGroupName, string automationAccountName, Guid id)
        {
            this.GetAutomationClient(resourceGroupName, automationAccountName).Job.Suspend(automationAccountName, id);
        }

        #endregion

        #region Certificate Operations

        public CertificateInfo CreateCertificate(string resourceGroupName, string automationAccountName, string name, string path,
            SecureString password,
            string description, bool exportable)
        {
            var certificateModel = this.TryGetCertificateModel(resourceGroupName, automationAccountName, name);
            if (certificateModel != null)
            {
                throw new ResourceCommonException(typeof(CertificateInfo),
                    string.Format(CultureInfo.CurrentCulture, Resources.CertificateAlreadyExists, name));
            }

            return CreateCertificateInternal(resourceGroupName, automationAccountName, name, path, password, description, exportable);
        }


        public CertificateInfo UpdateCertificate(string resourceGroupName, string automationAccountName, string name, string path,
            SecureString password,
            string description, bool? exportable)
        {
            if (String.IsNullOrWhiteSpace(path) && (password != null || exportable.HasValue))
            {
                throw new ResourceCommonException(typeof(CertificateInfo),
                    string.Format(CultureInfo.CurrentCulture, Resources.SetCertificateInvalidArgs, name));
            }

            var certificateModel = this.TryGetCertificateModel(resourceGroupName, automationAccountName, name);
            if (certificateModel == null)
            {
                throw new ResourceCommonException(typeof(CertificateInfo),
                    string.Format(CultureInfo.CurrentCulture, Resources.CertificateNotFound, name));
            }

            var createOrUpdateDescription = description ?? certificateModel.Description;
            var createOrUpdateIsExportable = (exportable.HasValue)
                ? exportable.Value
                : certificateModel.IsExportable;

            if (path != null)
            {
                return this.CreateCertificateInternal(resourceGroupName, automationAccountName, name, path, password,
                    createOrUpdateDescription,
                    createOrUpdateIsExportable.Value);
            }

            var cuparam = new CertificateUpdateParameters()
            {
                Name = name,
                Description = createOrUpdateDescription
            };

            this.GetAutomationClient(resourceGroupName, automationAccountName).Certificate.Update(resourceGroupName, automationAccountName, cuparam);

            return new CertificateInfo(resourceGroupName, automationAccountName,
                this.GetAutomationClient(resourceGroupName, automationAccountName).Certificate.Get(automationAccountName, name));
        }

        public CertificateInfo GetCertificate(string resourceGroupName, string automationAccountName, string name)
        {
            var certificateModel = this.TryGetCertificateModel(resourceGroupName, automationAccountName, name);
            if (certificateModel == null)
            {
                throw new ResourceCommonException(typeof(CertificateInfo),
                    string.Format(CultureInfo.CurrentCulture, Resources.CertificateNotFound, name));
            }

            return new Certificate(resourceGroupName, automationAccountName, certificateModel);
        }

        public IEnumerable<CertificateInfo> ListCertificates(string resourceGroupName, string automationAccountName, ref string nextLink)
        {
            Rest.Azure.IPage<AutomationManagement.Models.Certificate> response;

            if (string.IsNullOrEmpty(nextLink))
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).Certificate.ListByAutomationAccount(automationAccountName);
            }
            else
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).Certificate.ListByAutomationAccountNext(nextLink);
            }

            nextLink = response.NextPageLink;
            return response.Select(c => new CertificateInfo(resourceGroupName, automationAccountName, c));
        }

        public void DeleteCertificate(string resourceGroupName, string automationAccountName, string name)
        {
            try
            {
                this.GetAutomationClient(resourceGroupName, automationAccountName).Certificate.Delete(automationAccountName, name);
            }
            catch (CloudException cloudException)
            {
                if (cloudException.Response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    throw new ResourceNotFoundException(typeof(Schedule),
                        string.Format(CultureInfo.CurrentCulture, Resources.CertificateNotFound, name));
                }

                throw;
            }
        }

        #endregion

        #region Connection Operations

        public Connection CreateConnection(string resourceGroupName, string automationAccountName, string name, string connectionTypeName,
            IDictionary connectionFieldValues,
            string description)
        {
            var connectionModel = this.TryGetConnectionModel(resourceGroupName, automationAccountName, name);
            if (connectionModel != null)
            {
                throw new ResourceCommonException(typeof(Connection),
                    string.Format(CultureInfo.CurrentCulture, Resources.ConnectionAlreadyExists, name));
            }

            var ccparam = new ConnectionCreateOrUpdateParameters() { Name = name,
                Description = description,
                ConnectionType = new ConnectionTypeAssociationProperty() { Name = connectionTypeName },
                FieldDefinitionValues =
                    connectionFieldValues.Cast<DictionaryEntry>()
                        .ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value.ToString())
            };

            var connection = this.GetAutomationClient(resourceGroupName, automationAccountName).Connection.CreateOrUpdate(automationAccountName, name, ccparam);

            return new Connection(resourceGroupName, automationAccountName, connection);
        }

        public Connection UpdateConnectionFieldValue(string resourceGroupName, string automationAccountName, string name,
            string connectionFieldName, object value)
        {
            var connectionModel = this.TryGetConnectionModel(resourceGroupName, automationAccountName, name);
            if (connectionModel == null)
            {
                throw new ResourceCommonException(typeof(Connection),
                    string.Format(CultureInfo.CurrentCulture, Resources.ConnectionNotFound, name));
            }

            if (connectionModel.FieldDefinitionValues.ContainsKey(connectionFieldName))
            {
                connectionModel.FieldDefinitionValues[connectionFieldName] =
                    PowerShellJsonConverter.Serialize(value);
            }
            else
            {
                throw new ResourceCommonException(typeof(Connection),
                    string.Format(CultureInfo.CurrentCulture, Resources.ConnectionFieldNameNotFound, name));
            }

            var cuparam = new ConnectionUpdateParameters()
            {
                Name = name,
                Description = connectionModel.Description,
                FieldDefinitionValues = connectionModel.FieldDefinitionValues
            };

            this.GetAutomationClient(resourceGroupName, automationAccountName).Connection.Update(resourceGroupName, automationAccountName, cuparam);

            return new Connection(resourceGroupName, automationAccountName,
                this.GetAutomationClient(resourceGroupName, automationAccountName).Connection.Get(automationAccountName, name));
        }

        public Connection GetConnection(string resourceGroupName, string automationAccountName, string name)
        {
            var connectionModel = this.TryGetConnectionModel(resourceGroupName, automationAccountName, name);
            if (connectionModel == null)
            {
                throw new ResourceCommonException(typeof(Connection),
                    string.Format(CultureInfo.CurrentCulture, Resources.ConnectionNotFound, name));
            }

            return new Connection(resourceGroupName, automationAccountName, connectionModel);
        }

        public IEnumerable<Connection> ListConnectionsByType(string resourceGroupName, string automationAccountName, string typeName)
        {
            var connections = new List<Connection>();
            string nextLink = string.Empty;

            do
            {
                connections.AddRange(this.ListConnections(resourceGroupName, automationAccountName, ref nextLink));

            } while (!string.IsNullOrEmpty(nextLink));

            return
                connections.Where(
                    c => c.ConnectionTypeName.Equals(typeName, StringComparison.InvariantCultureIgnoreCase));
        }

        public IEnumerable<Connection> ListConnections(string resourceGroupName, string automationAccountName, ref string nextLink)
        {
            Rest.Azure.IPage<AutomationManagement.Models.Connection> response;

            if (string.IsNullOrEmpty(nextLink))
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).Connection.ListByAutomationAccount(automationAccountName);
            }
            else
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).Connection.ListByAutomationAccountNext(nextLink);
            }

            nextLink = response.NextPageLink;
            return response.Select(c => new Connection(resourceGroupName, automationAccountName, c));
        }

        public void DeleteConnection(string resourceGroupName, string automationAccountName, string name)
        {
            try
            {
                this.GetAutomationClient(resourceGroupName, automationAccountName).Connection.Delete(automationAccountName, name);
            }
            catch (CloudException cloudException)
            {
                if (cloudException.Response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    throw new ResourceNotFoundException(typeof(Connection),
                        string.Format(CultureInfo.CurrentCulture, Resources.ConnectionNotFound, name));
                }

                throw;
            }
        }

        #endregion

        #region HybridRunbookworkers
        
        public IEnumerable<HybridRunbookWorkerGroup> ListHybridRunbookWorkerGroups(string resourceGroupName, string automationAccountName, ref string nextLink)
        {
            Rest.Azure.IPage<AutomationManagement.Models.HybridRunbookWorkerGroup> response;

            if (string.IsNullOrEmpty(nextLink))
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).HybridRunbookWorkerGroup.ListByAutomationAccount(automationAccountName);;
            }
            else
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).HybridRunbookWorkerGroup.ListByAutomationAccountNext(nextLink);
            }

            nextLink = response.NextPageLink;

            return response.Select(c => new HybridRunbookWorkerGroup(resourceGroupName, automationAccountName, c));
        }

        public HybridRunbookWorkerGroup GetHybridRunbookWorkerGroup(string resourceGroupName, string automationAccountName, string name)
        {
            var hybridRunbookWorkerGroupModel = this.TryGetHybridRunbookWorkerModel(resourceGroupName, automationAccountName, name);
            if (hybridRunbookWorkerGroupModel == null)
            {
                throw new ResourceCommonException(typeof(HybridRunbookWorkerGroup),
                    string.Format(CultureInfo.CurrentCulture, Resources.HybridRunbookWorkerGroupNotFound, name));
            }

            return new HybridRunbookWorkerGroup(resourceGroupName, automationAccountName, hybridRunbookWorkerGroupModel);
            
        }

        #endregion

        #region JobSchedule

        public JobSchedule GetJobSchedule(string resourceGroupName, string automationAccountName, Guid jobScheduleId)
        {
            AutomationManagement.Models.JobSchedule jobScheduleModel = null;

            try
            {
                jobScheduleModel = this.GetAutomationClient(resourceGroupName, automationAccountName).JobSchedule.Get(
                    automationAccountName,
                    jobScheduleId);
            }
            catch (CloudException cloudException)
            {
                if (cloudException.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new ResourceNotFoundException(typeof(JobSchedule),
                        string.Format(CultureInfo.CurrentCulture, Resources.JobScheduleWithIdNotFound, jobScheduleId));
                }

                throw;
            }

            return this.CreateJobScheduleFromJobScheduleModel(resourceGroupName, automationAccountName, jobScheduleModel);
        }

        public JobSchedule GetJobSchedule(string resourceGroupName, string automationAccountName, string runbookName, string scheduleName)
        {
            const bool jobScheduleFound = false;
            var nextLink = string.Empty;

            do
            {
                var schedules = this.ListJobSchedules(resourceGroupName, automationAccountName, ref nextLink);
                var jobSchedule =
                    schedules.FirstOrDefault(
                        js => String.Equals(js.RunbookName, runbookName, StringComparison.OrdinalIgnoreCase) &&
                              String.Equals(js.ScheduleName, scheduleName, StringComparison.OrdinalIgnoreCase));

                if (jobSchedule != null)
                {
                    this.GetJobSchedule(resourceGroupName, automationAccountName, new Guid(jobSchedule.JobScheduleId));
                    return jobSchedule;
                }

            } while (!string.IsNullOrEmpty(nextLink));

            if (!jobScheduleFound)
            {
                throw new ResourceNotFoundException(typeof(Schedule),
                    string.Format(CultureInfo.CurrentCulture, Resources.JobScheduleNotFound, runbookName, scheduleName));
            }
        }

        public IEnumerable<JobSchedule> ListJobSchedules(string resourceGroupName, string automationAccountName, ref string nextLink)
        {
            Rest.Azure.IPage<AutomationManagement.Models.JobSchedule> response;

            if (string.IsNullOrEmpty(nextLink))
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).JobSchedule.ListByAutomationAccount(automationAccountName);;
            }
            else
            {
                response = this.GetAutomationClient(resourceGroupName, automationAccountName).JobSchedule.ListByAutomationAccountNext(nextLink);
            }

            nextLink = response.NextPageLink;
            return response.Select(c => new JobSchedule(resourceGroupName, automationAccountName, c));
        }

        public IEnumerable<JobSchedule> ListJobSchedulesByRunbookName(string resourceGroupName, string automationAccountName, string runbookName)
        {
            var jobSchedulesOfRunbook = new List<JobSchedule>();
            var nextLink = string.Empty;

            do
            {
                var schedules = this.ListJobSchedules(resourceGroupName, automationAccountName, ref nextLink);
                jobSchedulesOfRunbook.AddRange(
                    schedules.Where(js => String.Equals(js.RunbookName, runbookName, StringComparison.OrdinalIgnoreCase)));

            } while (!string.IsNullOrEmpty(nextLink));

            return jobSchedulesOfRunbook;
        }

        public IEnumerable<JobSchedule> ListJobSchedulesByScheduleName(string resourceGroupName, string automationAccountName, string scheduleName)
        {
            var jobSchedulesOfSchedules = new List<JobSchedule>();
            var nextLink = string.Empty;

            do
            {
                var schedules = this.ListJobSchedules(resourceGroupName, automationAccountName, ref nextLink);
                jobSchedulesOfSchedules.AddRange(
                    schedules.Where(
                        js => String.Equals(js.ScheduleName, scheduleName, StringComparison.OrdinalIgnoreCase)));

            } while (!string.IsNullOrEmpty(nextLink));

            return jobSchedulesOfSchedules;
        }

        public JobSchedule RegisterScheduledRunbook(string resourceGroupName, string automationAccountName, string runbookName,
            string scheduleName, IDictionary parameters, string runOn)
        {
            var processedParameters = this.ProcessRunbookParameters(resourceGroupName, automationAccountName, runbookName, parameters);
            var sdkJobSchedule = this.GetAutomationClient(resourceGroupName, automationAccountName).JobSchedule.Create(
                automationAccountName,
                Guid.NewGuid(),
                new JobScheduleCreateParameters
                {
                     Schedule = new ScheduleAssociationProperty { Name = scheduleName },
                     Runbook = new RunbookAssociationProperty { Name = runbookName },
                     Parameters = processedParameters,
                     RunOn = runOn
                });

            return new JobSchedule(resourceGroupName, automationAccountName, sdkJobSchedule);
        }

        public void UnregisterScheduledRunbook(string resourceGroupName, string automationAccountName, Guid jobScheduleId)
        {
            try
            {
                this.GetAutomationClient(resourceGroupName, automationAccountName).JobSchedule.Delete(
                    automationAccountName,
                    jobScheduleId);
            }
            catch (CloudException cloudException)
            {
                if (cloudException.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new ResourceNotFoundException(typeof(Schedule),
                        string.Format(CultureInfo.CurrentCulture, Resources.JobScheduleWithIdNotFound, jobScheduleId));
                }

                throw;
            }
        }

        public void UnregisterScheduledRunbook(string resourceGroupName, string automationAccountName, string runbookName, string scheduleName)
        {
            const bool jobScheduleFound = false;
            var nextLink = string.Empty;

            do
            {
                var schedules = this.ListJobSchedules(resourceGroupName, automationAccountName, ref nextLink);
                var jobSchedule =
                    schedules.FirstOrDefault(
                        js => String.Equals(js.RunbookName, runbookName, StringComparison.OrdinalIgnoreCase) &&
                              String.Equals(js.ScheduleName, scheduleName, StringComparison.OrdinalIgnoreCase));

                if (jobSchedule != null)
                {
                    this.UnregisterScheduledRunbook(resourceGroupName, automationAccountName, new Guid(jobSchedule.JobScheduleId));
                    return;
                }

            } while (!string.IsNullOrEmpty(nextLink));

            if (!jobScheduleFound)
            {
                throw new ResourceNotFoundException(typeof(Schedule),
                    string.Format(CultureInfo.CurrentCulture, Resources.JobScheduleNotFound, runbookName, scheduleName));
            }
        }

        #endregion

        #region ConnectionType

        public void DeleteConnectionType(string resourceGroupName, string automationAccountName, string name)
        {
            try
            {
                this.GetAutomationClient(resourceGroupName, automationAccountName).ConnectionType.Delete(automationAccountName, name);
            }
            catch (CloudException cloudException)
            {
                if (cloudException.Response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    throw new ResourceNotFoundException(typeof(ConnectionType),
                        string.Format(CultureInfo.CurrentCulture, Resources.ConnectionTypeNotFound, name));
                }

                throw;
            }
        }

        #endregion

        #region Private Methods

        private Schedule CreateScheduleFromScheduleModel(string resourceGroupName, string automationAccountName,
            AutomationManagement.Models.Schedule schedule)
        {
            Requires.Argument("schedule", schedule).NotNull();

            return new Schedule(resourceGroupName, automationAccountName, schedule);
        }

        private JobSchedule CreateJobScheduleFromJobScheduleModel(string resourceGroupName, string automationAccountName,
            AutomationManagement.Models.JobSchedule jobSchedule)
        {
            Requires.Argument("jobSchedule", jobSchedule).NotNull();

            return new JobSchedule(resourceGroupName, automationAccountName, jobSchedule);
        }

        private Azure.Management.Automation.Models.Runbook TryGetRunbookModel(string resourceGroupName, string automationAccountName,
            string runbookName)
        {
            Azure.Management.Automation.Models.Runbook runbook = null;
            try
            {
                runbook = this.GetAutomationClient(resourceGroupName, automationAccountName).Runbook.Get(automationAccountName, runbookName);
            }
            catch (CloudException e)
            {
                if (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    runbook = null;
                }
                else
                {
                    throw;
                }
            }
            return runbook;
        }

        private Azure.Management.Automation.Models.HybridRunbookWorkerGroup TryGetHybridRunbookWorkerModel(string resourceGroupName, string automationAccountName, string HybridRunbookWorkerGroupName)
        {
            Azure.Management.Automation.Models.HybridRunbookWorkerGroup hybridRunbookWorkerGroup = null;
            try
            {
                hybridRunbookWorkerGroup = this.GetAutomationClient(resourceGroupName, automationAccountName).HybridRunbookWorkerGroup.Get(automationAccountName, HybridRunbookWorkerGroupName);
            }
            catch (CloudException e)
            {
                if (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    hybridRunbookWorkerGroup = null;
                }
                else
                {
                    throw;
                }
            }
            return hybridRunbookWorkerGroup;
        }
        private Azure.Management.Automation.Models.Certificate TryGetCertificateModel(string resourceGroupName, string automationAccountName, 
            string certificateName)
        {
            Azure.Management.Automation.Models.Certificate certificate = null;
            try
            {
                certificate =
                    this.GetAutomationClient(resourceGroupName, automationAccountName).Certificate.Get(automationAccountName, certificateName);
            }
            catch (CloudException e)
            {
                if (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    certificate = null;
                }
                else
                {
                    throw;
                }
            }
            return certificate;
        }

        private IEnumerable<KeyValuePair<string, RunbookParameter>> ListRunbookParameters(string resourceGroupName, string automationAccountName,
            string runbookName)
        {
            Runbook runbook = this.GetRunbook(resourceGroupName, automationAccountName, runbookName);
            if (0 == String.Compare(runbook.State, RunbookState.New, CultureInfo.InvariantCulture,
                CompareOptions.IgnoreCase))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    Resources.RunbookHasNoPublishedVersion, runbookName));
            }
            return runbook.Parameters.Cast<DictionaryEntry>()
                .ToDictionary(k => k.Key.ToString(), k => (RunbookParameter)k.Value);
        }

        private IDictionary<string, string> ProcessRunbookParameters(string resourceGroupName, string automationAccountName, string runbookName,
            IDictionary parameters)
        {
            parameters = parameters ?? new Dictionary<string, string>();
            IEnumerable<KeyValuePair<string, RunbookParameter>> runbookParameters =
                this.ListRunbookParameters(resourceGroupName, automationAccountName, runbookName);
            var filteredParameters = new Dictionary<string, string>();

            foreach (var runbookParameter in runbookParameters)
            {
                if (parameters.Contains(runbookParameter.Key))
                {
                    object paramValue = parameters[runbookParameter.Key];
                    try
                    {
                        filteredParameters.Add(runbookParameter.Key, PowerShellJsonConverter.Serialize(paramValue));
                    }
                    catch (JsonSerializationException)
                    {
                        throw new ArgumentException(
                            string.Format(
                                CultureInfo.CurrentCulture, Resources.RunbookParameterCannotBeSerializedToJson,
                                runbookParameter.Key));
                    }
                }
                else if (runbookParameter.Value.IsMandatory.HasValue && runbookParameter.Value.IsMandatory.Value)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture, Resources.RunbookParameterValueRequired, runbookParameter.Key));
                }
            }

            if (filteredParameters.Count != parameters.Count)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, Resources.InvalidRunbookParameters));
            }

            return filteredParameters;
        }

        private IDictionary<string, string> ProcessRunbookParameters(IEnumerable<KeyValuePair<string, RunbookParameter>> runbookParameters,
            IDictionary<string, object> parameters)
        {
            parameters = parameters ?? new Dictionary<string, object>();

            var filteredParameters = new Dictionary<string, string>();

            foreach (var runbookParameter in runbookParameters)
            {
                if (parameters.ContainsKey(runbookParameter.Key))
                {
                    object paramValue = parameters[runbookParameter.Key];
                    try
                    {
                        filteredParameters.Add(runbookParameter.Key, PowerShellJsonConverter.Serialize(paramValue));
                    }
                    catch (JsonSerializationException)
                    {
                        throw new ArgumentException(
                            string.Format(
                                CultureInfo.CurrentCulture, Resources.RunbookParameterCannotBeSerializedToJson,
                                runbookParameter.Key));
                    }
                }
                else if (runbookParameter.Value.IsMandatory.HasValue && runbookParameter.Value.IsMandatory.Value)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture, Resources.RunbookParameterValueRequired, runbookParameter.Key));
                }
            }

            if (filteredParameters.Count != parameters.Count)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, Resources.InvalidRunbookParameters));
            }

            return filteredParameters;
        }

        
        private AutomationManagement.Models.Schedule GetScheduleModel(string resourceGroupName, string automationAccountName, string scheduleName)
        {
            AutomationManagement.Models.Schedule scheduleModel;
            try
            {
                scheduleModel = this.GetAutomationClient(resourceGroupName, automationAccountName).Schedule.Get(
                    automationAccountName,
                    scheduleName);
            }
            catch (CloudException cloudException)
            {
                if (cloudException.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new ResourceNotFoundException(typeof(Schedule),
                        string.Format(CultureInfo.CurrentCulture, Resources.ScheduleNotFound, scheduleName));
                }

                throw;
            }

            return scheduleModel;
        }


        private Schedule UpdateScheduleHelper(string resourceGroupName, string automationAccountName,
            string scheduleName, bool? isEnabled, string description)
        {
            var scheduleUpdateParameters = new AutomationManagement.Models.ScheduleUpdateParameters
            {
                Name = scheduleName,
                Description = description,
                IsEnabled = isEnabled
            };

            this.GetAutomationClient(resourceGroupName, automationAccountName).Schedule.Update(
                resourceGroupName,
                automationAccountName,
                scheduleUpdateParameters);

            return this.GetSchedule(resourceGroupName, automationAccountName, scheduleName);
        }

        private Certificate CreateCertificateInternal(string resourceGroupName, string automationAccountName, string name, string path,
            SecureString password, string description, bool exportable)
        {
            var cert = (password == null)
                ? new X509Certificate2(path, String.Empty,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet |
                    X509KeyStorageFlags.MachineKeySet)
                : new X509Certificate2(path, password,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet |
                    X509KeyStorageFlags.MachineKeySet);

            var ccparam = new CertificateCreateOrUpdateParameters()
            {
                Name = name,
                Description = description,
                Base64Value = Convert.ToBase64String(cert.Export(X509ContentType.Pkcs12)),
                Thumbprint = cert.Thumbprint,
                IsExportable = exportable
            };

            var certificate =
                this.GetAutomationClient(resourceGroupName, automationAccountName).Certificate.CreateOrUpdate(automationAccountName, name, ccparam);

            return new Certificate(resourceGroupName, automationAccountName, certificate);
        }

        private Azure.Management.Automation.Models.Connection TryGetConnectionModel(string resourceGroupName, string automationAccountName,
            string connectionName)
        {
            Azure.Management.Automation.Models.Connection connection = null;
            try
            {
                connection =
                    this.GetAutomationClient(resourceGroupName, automationAccountName).Connection.Get(automationAccountName, connectionName);
            }
            catch (CloudException e)
            {
                if (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    connection = null;
                }
                else
                {
                    throw;
                }
            }
            return connection;
        }

        private DirectoryInfo WriteRunbookToFile(string outputFolder, string runbookName, string content, string runbookType, bool overwriteExistingFile)
        {
            string outputFolderFullPath = this.GetCurrentDirectory();

            if (!string.IsNullOrEmpty(outputFolder))
            {
                outputFolderFullPath = this.ValidateAndGetFullPath(outputFolder);
            }

            var fileExtension = IsGraphRunbook(runbookType) ? Constants.SupportedFileExtensions.Graph : Constants.SupportedFileExtensions.PowerShellScript;

            var outputFilePath = outputFolderFullPath + "\\" + runbookName + fileExtension;

            // file exists and overwrite Not specified
            if (File.Exists(outputFilePath) && !overwriteExistingFile)
            {
                throw new ArgumentException(
                        string.Format(CultureInfo.CurrentCulture, Resources.RunbookFileAlreadyExists, outputFilePath));
            }

            // Write to the file
            this.WriteFile(outputFilePath, content);

            return new DirectoryInfo(runbookName + fileExtension);
        }

        private static bool IsGraphRunbook(string runbookType)
        {
            return (string.Equals(runbookType, RunbookTypeEnum.Graph, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(runbookType, RunbookTypeEnum.GraphPowerShell, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(runbookType, RunbookTypeEnum.GraphPowerShellWorkflow, StringComparison.OrdinalIgnoreCase));
        }

        AutomationManagement.IAutomationClient GetAutomationClient(string resourceGroupName, string automationAccountName)
        {
            this.GetAutomationClient(resourceGroupName, automationAccountName).ResourceGroupName = resourceGroupName;
            this.GetAutomationClient(resourceGroupName, automationAccountName).AutomationAccountName = automationAccountName;
            return this.automationManagementClient;
        }

        #endregion
    }
}