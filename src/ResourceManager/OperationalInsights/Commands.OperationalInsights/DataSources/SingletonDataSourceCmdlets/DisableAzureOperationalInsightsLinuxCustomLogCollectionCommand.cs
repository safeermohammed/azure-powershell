﻿// ----------------------------------------------------------------------------------
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

using Microsoft.Azure.Commands.OperationalInsights.Models;
using Microsoft.Azure.Commands.OperationalInsights.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Microsoft.Azure.Commands.OperationalInsights
{
    [Cmdlet(VerbsLifecycle.Disable, "AzureRmOperationalInsightsLinuxCustomLogCollection", SupportsShouldProcess = true,
        DefaultParameterSetName = ByWorkspaceName), OutputType(typeof(PSDataSource))]
    public class DisableAzureOperationalInsightsLinuxCustomLogCollectionCommand : AzureOperationalInsightsDataSourceBaseCmdlet
    {
        public override void ExecuteCmdlet()
        {
            PSDataSource dataSource = OperationalInsightsClient.GetSingletonDataSource(
                this.ResourceGroupName, 
                this.WorkspaceName, 
                PSDataSourceKinds.CustomLogCollection);
            if (null == dataSource)
            {
                var dsProperties = new PSCustomLogCollectionDataSourceProperties
                {
                    State = CustomLogState.LinuxLogsDisabled
                };

                CreatePSDataSourceWithProperties(dsProperties, Resources.SingletonDataSourceCustomLogCollectionDefaultName);
            }
            else
            {
                PSCustomLogCollectionDataSourceProperties dsProperties = dataSource.Properties as PSCustomLogCollectionDataSourceProperties;
                dsProperties.State = CustomLogState.LinuxLogsDisabled;
                UpdatePSDataSourceParameters parameters = new UpdatePSDataSourceParameters
                {
                    ResourceGroupName = dataSource.ResourceGroupName,
                    WorkspaceName = dataSource.WorkspaceName,
                    Name = dataSource.Name,
                    Properties = dsProperties
                };
                WriteObject(OperationalInsightsClient.UpdatePSDataSource(parameters));
            }
        }
    }
}