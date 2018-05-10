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
using System.Linq;
using Microsoft.Azure.Commands.Common.Authentication;
using Microsoft.Azure.Management.Automation;
using Microsoft.Azure.Test;
using Microsoft.WindowsAzure.Commands.ScenarioTest;
using Microsoft.WindowsAzure.Commands.Test.Utilities.Common;

namespace Microsoft.Azure.Commands.Automation.Test
{
    public abstract class AutomationScenarioTestsBase : RMTestBase
    {
        private EnvironmentSetupHelper helper;

        protected AutomationScenarioTestsBase()
        {
            helper = new EnvironmentSetupHelper();
        }

        protected void SetupManagementClients()
        {
            var automationManagementClient = GetAutomationManagementClient();

            helper.SetupManagementClients(automationManagementClient);
        }

        protected void RunPowerShellTest(params string[] scripts)
        {
            const string RootNamespace = "ScenarioTests";

            using (UndoContext context = UndoContext.Current)
            {
                context.Start(TestUtilities.GetCallingClass(2), TestUtilities.GetCurrentMethodName(2));

                SetupManagementClients();

                helper.SetupEnvironment(AzureModule.AzureResourceManager);


                var psModuleFile = this.GetType().FullName.Contains(RootNamespace) ?
                    this.GetType().FullName.Split(new[] { RootNamespace }, StringSplitOptions.RemoveEmptyEntries).Last().Replace(".", "\\") :
                    $"\\{this.GetType().Name}";

                helper.SetupModules(AzureModule.AzureResourceManager,
                    $"{RootNamespace}{psModuleFile}.ps1",
                    helper.RMProfileModule,
                    helper.GetRMModulePath(@"AzureRM.Automation.psd1"));

                helper.RunPowerShellTest(scripts);
            }
        }

        protected AutomationClient GetAutomationManagementClient()
        {
            return TestBase.GetServiceClient<AutomationClient>(new CSMTestEnvironmentFactory());
        }
    }
}
