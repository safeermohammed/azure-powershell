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

namespace Microsoft.Azure.Commands.Automation.Model.UpdateManagement
{
    using System;
    using Sdk = Microsoft.Azure.Management.Automation.Models;

    public class SoftwareUpdateConfiguration : BaseArmProperties
    {
        public UpdateConfiguration UpdateConfiguration { get; set; }

        public Schedule ScheduleConfiguration { get; set; }

        public string ProvisioningState { get; set; }

        public ErrorInfo ErrorInfo { get; set; }

        internal SoftwareUpdateConfiguration() { }

        internal SoftwareUpdateConfiguration(string ResourceGroupName, string automationAccountName, Sdk.SoftwareUpdateConfigurationCollectionItem suc)
        {
            this.ResourceGroupName = ResourceGroupName;
            AutomationAccountName = automationAccountName;
            Name = suc.Name;
            CreationTime = suc.CreationTime;
            ScheduleConfiguration = new Schedule
            {
                Frequency = (ScheduleFrequency)Enum.Parse(typeof(ScheduleFrequency), suc.Frequency, true),
                StartTime = suc.StartTime,
                NextRun = suc.NextRun
            };
            UpdateConfiguration = new UpdateConfiguration()
            {
                Duration = suc.UpdateConfiguration.Duration,
                AzureVirtualMachines = suc.UpdateConfiguration.AzureVirtualMachines
            };
            LastModifiedTime = suc.LastModifiedTime;
            ProvisioningState = suc.ProvisioningState;
        }

        internal SoftwareUpdateConfiguration(string resourceGroupName, string automationAccountName, Sdk.SoftwareUpdateConfiguration suc)
        {
            this.ResourceGroupName = resourceGroupName;
            this.AutomationAccountName = automationAccountName;
            this.CreatedBy = suc.CreatedBy;
            this.CreationTime = suc.CreationTime;
            this.Description = suc.ScheduleInfo.Description;
            this.ErrorInfo = suc.Error == null ? null : new ErrorInfo
            {
                Code = ErrorInfo.Code,
                Message = ErrorInfo.Message
            };
            this.LastModifiedBy = suc.LastModifiedBy;
            this.LastModifiedTime = suc.LastModifiedTime;
            this.Name = suc.Name;
            this.ProvisioningState = suc.ProvisioningState;
            this.ScheduleConfiguration = new Schedule
            {
                ResourceGroupName = resourceGroupName,
                AutomationAccountName = automationAccountName,
                CreationTime = suc.ScheduleInfo.CreationTime,
                Description = suc.ScheduleInfo.Description,
                ExpiryTime = suc.ScheduleInfo.ExpiryTime.HasValue ? suc.ScheduleInfo.ExpiryTime.Value : DateTimeOffset.MaxValue,
                Frequency = (ScheduleFrequency)Enum.Parse(typeof(ScheduleFrequency), suc.ScheduleInfo.Frequency, true),
                Interval = suc.ScheduleInfo.Interval.HasValue ? (byte)suc.ScheduleInfo.Interval.Value : (byte?)null,
                IsEnabled = suc.ScheduleInfo.IsEnabled ?? false,
                LastModifiedTime = suc.ScheduleInfo.LastModifiedTime,
                NextRun = suc.ScheduleInfo.NextRun,
                StartTime = suc.ScheduleInfo.StartTime,
                TimeZone = suc.ScheduleInfo.TimeZone,
                WeeklyScheduleOptions = Schedule.CreateWeeklyScheduleOptions(suc.ScheduleInfo.AdvancedSchedule),
                MonthlyScheduleOptions = Schedule.CreateMonthlyScheduleOptions(suc.ScheduleInfo.AdvancedSchedule)
            };
        }
    }
}
