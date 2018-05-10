function Test-SoftwareUpdateConfiguration {

	$rg = "mo-resources-eus"
	$aa = "mo-aaa-eus2"

	$s = New-AzureRmAutomationSchedule -ResourceGroupName $rg `
                                       -AutomationAccountName $aa `
                                       -Name mo-onetime-01 `
                                       -Description test-OneTime `
                                       -OneTime `
                                       -StartTime ([DateTime]::Now).AddMinutes(10) `
                                       -ForUpdate

    $azureVMIdsW = @(
        "/subscriptions/422b6c61-95b0-4213-b3be-7282315df71d/resourceGroups/mo-compute/providers/Microsoft.Compute/virtualMachines/mo-vm-w-01",
        "/subscriptions/422b6c61-95b0-4213-b3be-7282315df71d/resourceGroups/mo-compute/providers/Microsoft.Compute/virtualMachines/mo-vm-w-02"
    )

    $suc = New-AzureRmAutomationSoftwareUpdateConfiguration  -ResourceGroupName $rg `
                                                             -AutomationAccountName $aa `
                                                             -Schedule $s `
                                                             -Windows `
                                                             -AzureVMResourceIds $azureVMIdsW `
                                                             -Duration (New-TimeSpan -Hours 2)

    Assert-NotNull $suc "New-AzureRmAutomationSoftwareUpdateConfiguration returned null"
}