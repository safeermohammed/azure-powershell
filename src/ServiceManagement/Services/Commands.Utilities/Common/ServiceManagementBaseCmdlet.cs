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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation.Runspaces;
using AutoMapper;
using Hyak.Common;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Commands.Common;
using Microsoft.WindowsAzure.Commands.ServiceManagement.Model;
using Microsoft.WindowsAzure.Commands.Utilities.Properties;
using Microsoft.WindowsAzure.Management;
using Microsoft.WindowsAzure.Management.Compute;
using Microsoft.WindowsAzure.Management.Network;
using Microsoft.WindowsAzure.Management.Storage;

namespace Microsoft.WindowsAzure.Commands.Utilities.Common
{
    public abstract class ServiceManagementBaseCmdlet : CloudBaseCmdlet<IServiceManagement>
    {
        private Lazy<Runspace> runspace;

        private static IMapper _mapper = null;

        private static readonly object _lock = new object();

        private static IMapper Mapper
        {
            get
            {
                lock(_lock)
                {
                    if (_mapper == null)
                    {
                        var config = new MapperConfiguration(cfg =>
                        {
                            cfg.CreateMap<AzureOperationResponse, ManagementOperationContext>()
                                  .ForMember(c => c.OperationId, o => o.MapFrom(r => r.RequestId))
                                  .ForMember(c => c.OperationStatus, o => o.MapFrom(r => r.StatusCode.ToString()));

                            cfg.CreateMap<OperationStatusResponse, ManagementOperationContext>()
                                  .ForMember(c => c.OperationId, o => o.MapFrom(r => r.Id))
                                  .ForMember(c => c.OperationStatus, o => o.MapFrom(r => r.Status.ToString()));
                        });

                        _mapper = config.CreateMapper();
                    }

                    return _mapper;
                }
            }
        }

        protected ServiceManagementBaseCmdlet()
        {
            IClientProvider clientProvider = new ClientProvider(this);
            SetupClients(clientProvider);
        }

        private void SetupClients(IClientProvider clientProvider)
        {
            runspace = new Lazy<Runspace>(() =>
            {
                var localRunspace = RunspaceFactory.CreateRunspace(this.Host);
                localRunspace.Open();
                return localRunspace;
            });
            client = new Lazy<ManagementClient>(clientProvider.CreateClient);
            computeClient = new Lazy<ComputeManagementClient>(clientProvider.CreateComputeClient);
            storageClient = new Lazy<StorageManagementClient>(clientProvider.CreateStorageClient);
            networkClient = new Lazy<NetworkManagementClient>(clientProvider.CreateNetworkClient);
        }

        protected ServiceManagementBaseCmdlet(IClientProvider clientProvider)
        {
            SetupClients(clientProvider);
        }

        private Lazy<ManagementClient> client;
        public ManagementClient ManagementClient 
        { 
            get { return client.Value; }
        }

        private Lazy<ComputeManagementClient> computeClient;
        public ComputeManagementClient ComputeClient 
        {
            get { return computeClient.Value; }
        }

        private Lazy<StorageManagementClient> storageClient;
        public StorageManagementClient StorageClient 
        {
            get { return storageClient.Value; }
        }

        private Lazy<NetworkManagementClient> networkClient;
        public NetworkManagementClient NetworkClient 
        {
            get { return networkClient.Value; }
        }

        protected override void InitChannelCurrentSubscription(bool force)
        {
            // Do nothing for service management based cmdlets
        }

        protected OperationStatusResponse GetOperation(string operationId)
        {
            OperationStatusResponse operation = null;

            try
            {
                operation = this.ManagementClient.GetOperationStatus(operationId);

                if (operation.Status == OperationStatus.Failed)
                {
                    var errorMessage = string.Format(CultureInfo.InvariantCulture, "{0}: {1}", operation.Status, operation.Error.Message);
                    throw new Exception(errorMessage);
                }
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is CloudException)
                {
                    WriteExceptionError(ex.InnerException);
                }
                else
                {
                    WriteExceptionError(ex);
                }
            }
            catch (Exception ex)
            {
                WriteExceptionError(ex);
            }

            return operation;
        }

        protected OperationStatusResponse GetOperation(AzureOperationResponse result)
        {
            OperationStatusResponse operation;

            if (result is OperationStatusResponse)
            {
                operation = result as OperationStatusResponse;
            }
            else
            {
                operation = result == null ? null : GetOperation(result.RequestId);
            }

            return operation;
        }

        //TODO: Input argument is not used and should probably be removed.
        protected void ExecuteClientActionNewSM<TResult>(object input, string operationDescription, Func<TResult> action, Func<OperationStatusResponse, TResult, object> contextFactory) where TResult : AzureOperationResponse
        {
            WriteVerboseWithTimestamp(Resources.ServiceManagementExecuteClientActionInOCSBeginOperation, operationDescription);

            try
            {
                try
                {
                    TResult result = action();
                    OperationStatusResponse operation = GetOperation(result);

                    var context = contextFactory(operation, result);
                    if (context != null)
                    {
                        WriteObject(context, true);
                    }
                }
                catch (CloudException ce)
                {
                    throw new ComputeCloudException(ce);
                }
            }
            catch (Exception ex)
            {
                WriteExceptionError(ex);
            }

            WriteVerboseWithTimestamp(Resources.ServiceManagementExecuteClientActionInOCSCompletedOperation, operationDescription);
        }

        protected void ExecuteClientActionNewSM<TResult>(object input, string operationDescription, Func<TResult> action) where TResult : AzureOperationResponse
        {
            this.ExecuteClientActionNewSM(input, operationDescription, action, (s, response) => this.ContextFactory<AzureOperationResponse, ManagementOperationContext>(response, s));
        }

        protected TDestination ContextFactory<TSource, TDestination>(TSource s1, OperationStatusResponse s2) where TDestination : ManagementOperationContext
        {
            TDestination result = Mapper.Map<TSource, TDestination>(s1);
            result = Mapper.Map(s2, result);
            result.OperationDescription = CommandRuntime.ToString();

            return result;
        }

        protected TDestination ContextFactory<TSource, TDestination>(
            TSource s1, 
            OperationStatusResponse s2, 
            Func<TSource, TDestination> firstMap,
            Func<OperationStatusResponse, TDestination, TDestination> secondMap) where TDestination : ManagementOperationContext
        {
            TDestination result = firstMap(s1);
            result = secondMap(s2, result);
            result.OperationDescription = CommandRuntime.ToString();

            return result;
        }

        protected void ExecuteClientAction(Action action)
        {
            try
            {
                try
                {
                    action();
                }
                catch (CloudException ex)
                {
                    throw new ComputeCloudException(ex);
                }
            }
            catch (Exception ex)
            {
                WriteExceptionError(ex);
            }
        }

        protected Dictionary<string, string> ConvertToDictionary(Hashtable extendedProperty)
        {
            Dictionary<string, string> resultDictionary = null;
            if (extendedProperty != null)
            {
                resultDictionary = new Dictionary<string, string>();

                foreach (DictionaryEntry entry in extendedProperty)
                {
                    if (entry.Value != null)
                    {
                        resultDictionary[entry.Key.ToString()] = entry.Value.ToString();
                    }
                    else
                    {
                        resultDictionary[entry.Key.ToString()] = string.Empty;
                    }
                }
            }

            return resultDictionary;
        }
    }
}