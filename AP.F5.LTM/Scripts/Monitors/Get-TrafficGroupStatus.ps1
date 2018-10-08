#==================================================================================
# Script: 	Get-TrafficGroupStatus.ps1
# Date:		03/08/18
# Author: 	Andi Patrick
# Purpose:	Uses SharpSnmpLib to get F5 Traffic Group Status returning 
#			as PropertyBag
#==================================================================================
param($TrafficGroupName,$GroupAddresses,$GroupPorts,$GroupCommunities,$SharpSnmpLocation)

# Load SharpSNMPLib
[reflection.assembly]::LoadFrom( (Resolve-Path $SharpSnmpLocation) )

#Start by setting up API object.
$api = New-Object -comObject 'MOM.ScriptAPI'

# Use SNMP v2
$ver = [Lextm.SharpSnmpLib.VersionCode]::V2
$walkMode = [Lextm.SharpSnmpLib.Messaging.WalkMode]::WithinSubtree

# OIDs used
# bigipTrafficMgmt.bigipSystem.sysCM.sysCmTrafficGroupStatus.sysCmTrafficGroupStatusTable.sysCmTrafficGroupStatusEntry.sysCmTrafficGroupStatusTrafficGroup
[string]$sysCmTrafficGroupStatusTrafficGroup = ".1.3.6.1.4.1.3375.2.1.14.5.2.1.1"
# bigipTrafficMgmt.bigipSystem.sysCM.sysCmTrafficGroupStatus.sysCmTrafficGroupStatusTable.sysCmTrafficGroupStatusEntry.sysCmTrafficGroupStatusFailoverStatus
[string]$sysCmTrafficGroupStatusFailoverStatus = ".1.3.6.1.4.1.3375.2.1.14.5.2.1.3"

# Get Addresses, Ports And Communities
$AddressList = $GroupAddresses.Split(",")
$PortList = $GroupPorts.Split(",")
$CommunityList = $GroupCommunities.Split(",")

[int]$TrafficGroupStatus = 0

# Loop Through each Device
For($i=0; $i -lt $AddressList.Count;$i++) {

	# SNMP Return Variables
	$TrafficGroupNameResults = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]' 
	# SNMP Return Variables
	$TrafficGroupStatusResults = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]' 
	
	# Create endpoint for SNMP server
	$ip = [System.Net.IPAddress]::Parse($AddressList[$i])
	$svr = New-Object System.Net.IpEndPoint ($ip, [int]$PortList[$i])
	
	Try {
	    [Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $CommunityList[$i], $sysCmTrafficGroupStatusTrafficGroup , $TrafficGroupNameResults, 2000, $walkMode)
	    [Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $CommunityList[$i], $sysCmTrafficGroupStatusFailoverStatus , $TrafficGroupStatusResults, 2000, $walkMode)

	    # Loop through Results
		For($j=0; $j -lt $TrafficGroupNameResults.Count;$j++) {
			
			# Is this what we are looking for?
			if($TrafficGroupNameResults[$j].Data.ToString() -eq $TrafficGroupName){
				[int]$TestStatus = [int]$TrafficGroupStatusResults[$j].Data.ToString()
				if ($TestStatus -gt $TrafficGroupStatus) {
					$TrafficGroupStatus = $TestStatus
				}
			}
	    }
	} Catch {
	
	}
}
#Create a property bag.
$bag = $api.CreatePropertyBag()
		
# Add Result to Property Bag
$bag.AddValue("TrafficGroupName", $TrafficGroupName)
$bag.AddValue("Value", $TrafficGroupStatus)

#Return the PropertyBag.
#$api.Return($bag)
$bag
