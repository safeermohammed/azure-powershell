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

using System;
using System.Management.Automation;
using System.Net;
using Microsoft.Azure.Management.HDInsight.Models;
using Microsoft.WindowsAzure.Commands.Common;
using Microsoft.WindowsAzure.Commands.ScenarioTest;
using Moq;
using Xunit;

namespace Microsoft.Azure.Commands.HDInsight.Test
{
    public class RdpTests : HDInsightTestBase
    {
        private GrantAzureHDInsightRdpServicesAccessCommand grantcmdlet;
        private RevokeAzureHDInsightHttpServicesAccessCommand revokecmdlet;
        private const string ClusterName = "hdicluster";

        private readonly PSCredential _rdpCred;
        
        public RdpTests()
        {
            base.SetupTest();
            _rdpCred = new PSCredential("rdpuser", string.Format("Password1!").ConvertToSecureString());

            grantcmdlet = new GrantAzureHDInsightRdpServicesAccessCommand
            {
                CommandRuntime = commandRuntimeMock.Object,
                HDInsightManagementClient = hdinsightManagementClient.Object,
                ClusterName = ClusterName,
                ResourceGroupName = ResourceGroupName,
                RdpCredential = _rdpCred,
                RdpAccessExpiry = new DateTime(2015, 1, 1)
            };
            revokecmdlet = new RevokeAzureHDInsightHttpServicesAccessCommand
            {
                CommandRuntime = commandRuntimeMock.Object,
                HDInsightManagementClient = hdinsightManagementClient.Object,
                ClusterName = ClusterName,
                ResourceGroupName = ResourceGroupName
            };
        }

        [Fact]
        [Trait(Category.AcceptanceType, Category.CheckIn)]
        public void CanGrantRdpAccess()
        {
            hdinsightManagementClient.Setup(
                c =>
                    c.ConfigureRdp(ResourceGroupName, ClusterName,
                        It.Is<RDPSettingsParameters>(
                            param =>
                                param.OsProfile.LinuxOperatingSystemProfile == null &&
                                param.OsProfile.WindowsOperatingSystemProfile.RdpSettings.ExpiryDate ==
                                grantcmdlet.RdpAccessExpiry &&
                                param.OsProfile.WindowsOperatingSystemProfile.RdpSettings.UserName == _rdpCred.UserName &&
                                param.OsProfile.WindowsOperatingSystemProfile.RdpSettings.Password ==
                                _rdpCred.Password.ConvertToString())))
                .Returns(new HDInsightLongRunningOperationResponse
                {
                    Error = null,
                    StatusCode = HttpStatusCode.OK,
                    Status = OperationStatus.Succeeded
                })
                .Verifiable();

            grantcmdlet.ExecuteCmdlet();

            commandRuntimeMock.VerifyAll();
        }

        [Fact]
        [Trait(Category.AcceptanceType, Category.CheckIn)]
        public void CanRevokeRdpAccess()
        {
            hdinsightManagementClient.Setup(
                c =>
                    c.ConfigureRdp(ResourceGroupName, ClusterName,
                        It.Is<RDPSettingsParameters>(
                            param =>
                                param.OsProfile.LinuxOperatingSystemProfile == null &&
                                param.OsProfile.WindowsOperatingSystemProfile.RdpSettings == null)))
                .Returns(new HDInsightLongRunningOperationResponse
                {
                    Error = null,
                    StatusCode = HttpStatusCode.OK,
                    Status = OperationStatus.Succeeded
                })
                .Verifiable();

            grantcmdlet.ExecuteCmdlet();

            commandRuntimeMock.VerifyAll();
        }
    }
}
