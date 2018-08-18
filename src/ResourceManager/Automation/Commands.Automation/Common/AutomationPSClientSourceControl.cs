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

using Microsoft.Azure.Commands.Automation.Model;
using Microsoft.Azure.Commands.Automation.Properties;
using Microsoft.Azure.Management.Automation;
using Microsoft.Azure.Management.Automation.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using AutomationManagement = Microsoft.Azure.Management.Automation;
using SourceControl = Microsoft.Azure.Management.Automation.Models.SourceControl;
using SourceControlSyncJob = Microsoft.Azure.Management.Automation.Models.SourceControlSyncJob;

namespace Microsoft.Azure.Commands.Automation.Common
{
    public partial class AutomationPSClient : IAutomationPSClient
    {
        #region SourceControl

        public Model.SourceControl CreateSourceControl(
            string resourceGroupName,
            string automationAccountName,
            string name,
            string description,
            SecureString accessToken,
            string repoUrl,
            string sourceType,
            string branch,
            string folderPath,
            bool publishRunbook,
            bool autoSync)
        {
            Requires.Argument("resourceGroupName", resourceGroupName).NotNullOrEmpty();
            Requires.Argument("automationAccountName", automationAccountName).NotNullOrEmpty();
            Requires.Argument("name", name).NotNullOrEmpty();
            Requires.Argument("accessToken", accessToken).NotNullOrEmpty();
            Requires.Argument("repoUrl", repoUrl).NotNullOrEmpty();
            Requires.Argument("sourceType", sourceType).NotNullOrEmpty();
            Requires.Argument("folderPath", folderPath).NotNullOrEmpty();

            if (String.Equals(sourceType, Constants.SupportedSourceType.GitHub.ToString()) ||
                String.Equals(sourceType, Constants.SupportedSourceType.VsoGit.ToString()))
            {
                Requires.Argument("branch", branch).NotNullOrEmpty();
            }

            bool sourceControlExists = true;

            try
            {
                this.GetSourceControl(resourceGroupName, automationAccountName, name);
            }
            catch (ResourceNotFoundException)
            {
                sourceControlExists = false;
            }

            if (sourceControlExists)
            {
                throw new AzureAutomationOperationException(
                    string.Format(CultureInfo.CurrentCulture, Resources.SourceControlAlreadyExists, name));
            }

            SourceControl sdkSourceControl = null;
            try
            {
                var decryptedAccessToken = Utils.GetStringFromSecureString(accessToken);

                var createParams = new SourceControlCreateOrUpdateParameters(
                                        repoUrl,
                                        branch,
                                        folderPath,
                                        autoSync,
                                        publishRunbook,
                                        sourceType,
                                        GetAccessTokenProperties(decryptedAccessToken),
                                        description);

                sdkSourceControl = this.automationManagementClient.SourceControl.CreateOrUpdate(
                                            resourceGroupName,
                                            automationAccountName,
                                            name,
                                            createParams);
            }
            catch (ErrorResponseException ex)
            {
                ProcessErrorResponseException<SourceControl>(ex);
            }

            return new Model.SourceControl(sdkSourceControl, automationAccountName, resourceGroupName);
        }

        public Model.SourceControl UpdateSourceControl(
            string resourceGroupName,
            string automationAccountName,
            string name,
            string description,
            SecureString accessToken,
            string branch,
            string folderPath,
            bool? publishRunbook,
            bool? autoSync)
        {
            Requires.Argument("ResourceGroupName", resourceGroupName).NotNullOrEmpty();
            Requires.Argument("AutomationAccountName", automationAccountName).NotNullOrEmpty();
            Requires.Argument("name", name).NotNullOrEmpty();

            Model.SourceControl existingSourceControl = this.GetSourceControl(resourceGroupName, automationAccountName, name);

            var updateParams = new AutomationManagement.Models.SourceControlUpdateParameters();

            if (autoSync.HasValue)
            {
                updateParams.AutoSync = autoSync;
            }

            if (publishRunbook.HasValue)
            {
                updateParams.PublishRunbook = publishRunbook;
            }

            if (!string.IsNullOrEmpty(description))
            {
                updateParams.Description = description;
            }

            if (!string.IsNullOrEmpty(branch))
            {
                updateParams.Branch = branch;
            }

            if (!string.IsNullOrEmpty(folderPath))
            {
                updateParams.FolderPath = folderPath;
            }

            if (accessToken != null)
            {
                var decryptedAccessToken = Utils.GetStringFromSecureString(accessToken);
                updateParams.SecurityToken = GetAccessTokenProperties(decryptedAccessToken);
            }

            SourceControl sdkSourceControl = null;
            try
            {
                sdkSourceControl = this.automationManagementClient.SourceControl.Update(
                                        resourceGroupName,
                                        automationAccountName,
                                        name,
                                        updateParams);
            }
            catch (ErrorResponseException ex)
            {
                ProcessErrorResponseException<SourceControl>(ex);
            }

            return new Model.SourceControl(sdkSourceControl, automationAccountName, resourceGroupName);
        }

        public void DeleteSourceControl(
            string resourceGroupName,
            string automationAccountName,
            string name)
        {
            Requires.Argument("resourceGroupName", resourceGroupName).NotNullOrEmpty();
            Requires.Argument("automationAccountName", automationAccountName).NotNullOrEmpty();
            Requires.Argument("name", name).NotNullOrEmpty();

            try
            {
                this.automationManagementClient.SourceControl.Delete(resourceGroupName, automationAccountName, name);
            }
            catch (ErrorResponseException ex)
            {
                ProcessErrorResponseException<SourceControl>(ex);
            }
        }

        public Model.SourceControl GetSourceControl(
            string resourceGroupName,
            string automationAccountName,
            string name)
        {
            Requires.Argument("resourceGroupName", resourceGroupName).NotNullOrEmpty();
            Requires.Argument("automationAccountName", automationAccountName).NotNullOrEmpty();
            Requires.Argument("name", name).NotNullOrEmpty();

            Model.SourceControl result = null;

            try
            {
                var existingSourceControl = this.automationManagementClient.SourceControl.Get(
                                                resourceGroupName,
                                                automationAccountName,
                                                name);

                if (existingSourceControl != null)
                {
                    result = new Model.SourceControl(existingSourceControl, automationAccountName, resourceGroupName);
                }
            }
            catch (ErrorResponseException ex)
            {
                ProcessErrorResponseException<SourceControl>(ex);
            }

            return result;
        }

        public IEnumerable<Model.SourceControl> ListSourceControl(
            string resourceGroupName,
            string automationAccountName,
            string sourceType,
            ref string nextLink)
        {
            Rest.Azure.IPage<AutomationManagement.Models.SourceControl> response = null;

            if (string.IsNullOrEmpty(nextLink))
            {
                try
                {
                    response = this.automationManagementClient.SourceControl.ListByAutomationAccount(
                                        resourceGroupName,
                                        automationAccountName,
                                        GetSourceControlTypeFilterString(sourceType));
                }
                catch (ErrorResponseException ex)
                {
                    ProcessErrorResponseException<SourceControl>(ex);
                }
            }
            else
            {
                response = this.automationManagementClient.SourceControl.ListByAutomationAccountNext(nextLink);
            }
            nextLink = response.NextPageLink;
            return response.Select(sc => new Model.SourceControl(sc, automationAccountName, resourceGroupName));
        }

        #endregion

        #region SourceControlSyncJob

        public Model.SourceControlSyncJob StartSourceControlSyncJob(
            string resourceGroupName,
            string automationAccountName,
            string sourceControlName,
            Guid syncJobId)
        {
            Requires.Argument("resourceGroupName", resourceGroupName).NotNullOrEmpty();
            Requires.Argument("automationAccountName", automationAccountName).NotNullOrEmpty();
            Requires.Argument("sourceControlName", sourceControlName).NotNullOrEmpty();
            Requires.Argument("syncJobId", syncJobId).NotNullOrEmpty();

            bool syncJobExists = true;
            try
            {
                this.GetSourceControlSyncJob(resourceGroupName, automationAccountName, sourceControlName, syncJobId);
            }
            catch (ResourceNotFoundException)
            {
                syncJobExists = false;
            }

            if (syncJobExists)
            {
                throw new AzureAutomationOperationException(
                    string.Format(CultureInfo.CurrentCulture, Resources.SourceControlSyncJobAlreadyExist, syncJobId.ToString()));
            }

            SourceControlSyncJob sdkSyncJob = null;
            try
            {
                sdkSyncJob = this.automationManagementClient.SourceControlSyncJob.Create(
                                    resourceGroupName,
                                    automationAccountName,
                                    sourceControlName,
                                    syncJobId,
                                    new SourceControlSyncJobCreateParameters(""));
            }
            catch (ErrorResponseException ex)
            {
                ProcessErrorResponseException<SourceControlSyncJob>(ex);
            }

            return new Model.SourceControlSyncJob(resourceGroupName, automationAccountName, sourceControlName, sdkSyncJob);
        }

        public Model.SourceControlSyncJobRecord GetSourceControlSyncJob(
            string resourceGroupName,
            string automationAccountName,
            string sourceControlName,
            Guid syncJobId)
        {
            Requires.Argument("resourceGroupName", resourceGroupName).NotNullOrEmpty();
            Requires.Argument("automationAccountName", automationAccountName).NotNullOrEmpty();
            Requires.Argument("sourceControlName", sourceControlName).NotNullOrEmpty();
            Requires.Argument("syncJobId", syncJobId).NotNullOrEmpty();

            Model.SourceControlSyncJobRecord result = null;

            try
            {
                var existingSyncJob = this.automationManagementClient.SourceControlSyncJob.Get(
                                            resourceGroupName,
                                            automationAccountName,
                                            sourceControlName,
                                            syncJobId);

                if (existingSyncJob != null)
                {
                    result = new Model.SourceControlSyncJobRecord(resourceGroupName, automationAccountName, sourceControlName, existingSyncJob);
                }
            }
            catch (ErrorResponseException ex)
            {
                ProcessErrorResponseException<SourceControlSyncJob>(ex);
            }

            return result;
        }

        public IEnumerable<Model.SourceControlSyncJob> ListSourceControlSyncJobs(
            string resourceGroupName,
            string automationAccountName,
            string sourceControlName,
            ref string nextLink)
        {
            Requires.Argument("resourceGroupName", resourceGroupName).NotNullOrEmpty();
            Requires.Argument("automationAccountName", automationAccountName).NotNullOrEmpty();
            Requires.Argument("sourceControlName", sourceControlName).NotNullOrEmpty();

            Rest.Azure.IPage<AutomationManagement.Models.SourceControlSyncJob> response = null;

            if (string.IsNullOrEmpty(nextLink))
            {
                try
                {
                    response = this.automationManagementClient.SourceControlSyncJob.ListByAutomationAccount(
                                    resourceGroupName,
                                    automationAccountName,
                                    sourceControlName);
                }
                catch (ErrorResponseException ex)
                {
                    ProcessErrorResponseException<SourceControlSyncJob>(ex);
                }
            }
            else
            {
                response = this.automationManagementClient.SourceControlSyncJob.ListByAutomationAccountNext(nextLink);
            }

            nextLink = response.NextPageLink;
            return response.Select(
                    sj => new Model.SourceControlSyncJob(resourceGroupName, automationAccountName, sourceControlName, sj));
        }

        public IEnumerable<Model.SourceControlSyncJobStream> GetSourceControlSyncJobStream(
            string resourceGroupName,
            string automationAccountName,
            string sourceControlName,
            Guid syncJobId,
            string streamType,
            ref string nextLink)
        {
            Requires.Argument("resourceGroupName", resourceGroupName).NotNullOrEmpty();
            Requires.Argument("automationAccountName", automationAccountName).NotNullOrEmpty();
            Requires.Argument("sourceControlName", sourceControlName).NotNullOrEmpty();
            Requires.Argument("syncJobId", syncJobId).NotNullOrEmpty();
            Requires.Argument("streamType", streamType).NotNullOrEmpty();

            Rest.Azure.IPage<AutomationManagement.Models.SourceControlSyncJobStream> response = null;

            if (string.IsNullOrEmpty(nextLink))
            {
                try
                {
                    response = this.automationManagementClient.SourceControlSyncJobStreams.ListBySyncJob(
                                    resourceGroupName,
                                    automationAccountName,
                                    sourceControlName,
                                    syncJobId,
                                    this.GetSyncJobStreamFilterString(streamType));
                }
                catch (ErrorResponseException ex)
                {
                    ProcessErrorResponseException<SourceControlSyncJob>(ex);
                }
            }
            else
            {
                response = this.automationManagementClient.SourceControlSyncJobStreams.ListBySyncJobNext(nextLink);
            }

            nextLink = response.NextPageLink;

            return response.Select(
                       stream => new Model.SourceControlSyncJobStream(stream, resourceGroupName, automationAccountName, sourceControlName, syncJobId));
        }

        public SourceControlSyncJobStreamRecord GetSourceControlSyncJobStreamRecord(
            string resourceGroupName,
            string automationAccountName,
            string sourceControlName,
            Guid syncJobId,
            string syncJobStreamId)
        {
            Requires.Argument("resourceGroupName", resourceGroupName).NotNullOrEmpty();
            Requires.Argument("automationAccountName", automationAccountName).NotNullOrEmpty();
            Requires.Argument("sourceControlName", sourceControlName).NotNullOrEmpty();
            Requires.Argument("syncJobId", syncJobId).NotNullOrEmpty();
            Requires.Argument("syncJobStreamId", syncJobStreamId).NotNullOrEmpty();

            SourceControlSyncJobStreamById response = null;
            try
            {
                response = this.automationManagementClient.SourceControlSyncJobStreams.Get(
                                    resourceGroupName,
                                    automationAccountName,
                                    sourceControlName,
                                    syncJobId,
                                    syncJobStreamId);
            }
            catch (ErrorResponseException ex)
            {
                ProcessErrorResponseException<SourceControlSyncJobStreamById>(ex);
            }

            return new SourceControlSyncJobStreamRecord(
                        response, resourceGroupName, automationAccountName, sourceControlName, syncJobId);
        }

        #region private helper functions

        private string GetSyncJobStreamFilterString(string streamType)
        {
            string filter = null;
            List<string> odataFilter = new List<string>();

            if (!string.IsNullOrWhiteSpace(streamType))
            {
                // By default, to retrieve all the streams, the API does not expect any filters.
                // If streamType is Any, do not add a filter.
                if (!(string.Equals(SourceControlSyncJobStreamType.Any.ToString(), streamType, StringComparison.OrdinalIgnoreCase)))
                {
                    odataFilter.Add("properties/streamType eq '" + Uri.EscapeDataString(streamType) + "'");
                }
            }

            if (odataFilter.Count > 0)
            {
                filter = string.Join(" and ", odataFilter);
            }

            return filter;
        }

        private string GetSourceControlTypeFilterString(string sourceType)
        {
            string filter = null;
            List<string> odataFilter = new List<string>();

            if (!string.IsNullOrWhiteSpace(sourceType))
            {
                odataFilter.Add("properties/sourceType eq '" + Uri.EscapeDataString(sourceType) + "'");
            }

            if (odataFilter.Count > 0)
            {
                filter = string.Join(" and ", odataFilter);
            }

            return filter;
        }

        private SourceControlSecurityTokenProperties GetAccessTokenProperties(string accessToken)
        {
            var securityTokenProperties = new SourceControlSecurityTokenProperties();
            securityTokenProperties.AccessToken = accessToken;
            securityTokenProperties.TokenType = "PersonalAccessToken";

            return securityTokenProperties;
        }

        // Check for ResourceNotFoundException.
        private void ProcessErrorResponseException<T>(ErrorResponseException exception)
        {
            bool isResourceNotFoundException = false;

            if (!string.IsNullOrEmpty(exception.Body.Code) && !string.IsNullOrEmpty(exception.Body.Message))
            {
                if (exception.Body.Code.Equals(System.Net.HttpStatusCode.NotFound.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new ResourceNotFoundException(typeof(T), exception.Body.Message);
                }
            }

            if (!string.IsNullOrEmpty(exception.Response.Content))
            {
                AzureErrorResponseMessage responseContent = null;
                try
                {
                    responseContent = JsonConvert.DeserializeObject<AzureErrorResponseMessage>(exception.Response.Content);
                }
                catch
                {
                    // swallow the exception as we could not parse the error message
                }

                if (responseContent != null)
                {
                    if (responseContent.Error.Code.Equals("ResourceGroupNotFound", StringComparison.InvariantCultureIgnoreCase) || 
                        responseContent.Error.Code.Equals("ResourceNotFound", StringComparison.InvariantCultureIgnoreCase))
                    {
                        isResourceNotFoundException = true;
                        throw new ResourceNotFoundException(typeof(T), responseContent.Error.Message);
                    }
                }
            }

            if (!isResourceNotFoundException)
            {
                throw exception;
            }
        }

        #endregion

        #endregion

    }
}
