﻿#==================================================================================
# Script: 	Get-VirtualServerEnabledState.ps1
# Date:		01/08/18
# Author: 	Andi Patrick
# Purpose:	Gets VirtualServer Enabled State via SNMP returns all as Property Bag
#==================================================================================

# Get the named parameters
param($Debug,$DeviceAddresses,$DevicePorts,$DeviceCommunities,$SharpSnmpLocation)


#Constants used for event logging
$SCRIPT_NAME			= 'Get-VirtualServerEnabledState.ps1'
$EVENT_LEVEL_ERROR 		= 1
$VENT_LEVEL_WARNING 	= 2
$EVENT_LEVEL_INFO 		= 4

$SCRIPT_STARTED			= 4601
$PROPERTYBAG_CREATED	= 4602
$SCRIPT_EVENT			= 4603
$SCRIPT_ENDED			= 4604

#==================================================================================
# Sub:		LogDebugEvent
# Purpose:	Logs an informational event to the Operations Manager event log
#			only if Debug argument is true
#==================================================================================
function Log-DebugEvent
{
	param($eventNo,$message)

	$message = "`n" + $message
	if ($Debug -eq $true)
	{
		$api.LogScriptEvent($SCRIPT_NAME,$eventNo,$EVENT_LEVEL_INFO,$message)
	}
}



#Start by setting up API object.
$api = New-Object -comObject 'MOM.ScriptAPI'

# Log Startup Message
$message =	$SCRIPT_NAME + " Started."
Log-DebugEvent $SCRIPT_STARTED " Script Started."

# Get Working All Device Addresses, Ports and Communities
$DeviceAddressList = $DeviceAddresses.Split(",")
$DevicePortList = $DevicePorts.Split(",")
$DeviceCommunityList = $DeviceCommunities.Split(",")

# Load SharpSNMPLib
[reflection.assembly]::LoadFrom( (Resolve-Path $SharpSnmpLocation) )
# Use SNMP v2
$ver = [Lextm.SharpSnmpLib.VersionCode]::V2
# Set Walk Mode to WithinSubtree
$walkMode = [Lextm.SharpSnmpLib.Messaging.WalkMode]::WithinSubtree


# OIDs used
# bigipTrafficMgmt.bigipLocalTM.ltmVirtualServers.ltmVirtualServStatus.ltmVsStatusTable.ltmVsStatusEntry.ltmVsStatusName
[string]$ltmVsStatusName = ".1.3.6.1.4.1.3375.2.2.10.13.2.1.1" 
# bigipTrafficMgmt.bigipLocalTM.ltmVirtualServers.ltmVirtualServStatus.ltmVsStatusTable.ltmVsStatusEntry.ltmVsStatusEnabledState
[string]$ltmVsStatusEnabledState = ".1.3.6.1.4.1.3375.2.2.10.13.2.1.3" 
# bigipTrafficMgmt.bigipLocalTM.ltmVirtualServers.ltmVirtualServStatus.ltmVsStatusTable.ltmVsStatusEntry.ltmVsStatusDetailReason
[string]$ltmVsStatusDetailedReason = ".1.3.6.1.4.1.3375.2.2.10.13.2.1.5" 

# Loop Through All Devices
For ($i=0;$i -lt $DeviceAddressList.Count;$i++) {
	Try {
		# Create endpoint for SNMP server
		$ip = [System.Net.IPAddress]::Parse($DeviceAddressList[$i])
		$svr = New-Object System.Net.IpEndPoint ($ip, $DevicePortList[$i])

		# Get VirtualServer Names from SNMP
		$VirtualServerNameList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
		[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunityList[$i], $ltmVsStatusName, $VirtualServerNameList, 3000, $walkMode)

		# Get VirtualServer Enabled State from SNMP
		$VirtualServerEnabledStateList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
		[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunityList[$i], $ltmVsStatusEnabledState, $VirtualServerEnabledStateList, 3000, $walkMode)

		# Get VirtualServer DetailedReason from SNMP
		$VirtualServerDetailedReasonList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
		[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunityList[$i], $ltmVsStatusDetailedReason, $VirtualServerDetailedReasonList, 3000, $walkMode)

		# Loop Through Results
		For($i=0; $i -lt $VirtualServerNameList.Count; $i++) {
			[string]$VirtualServerName = $VirtualServerNameList[$i].Data.ToString()
			[int]$VirtualServerEnabledState = $VirtualServerEnabledStateList[$i].Data.ToString()
			[string]$VirtualServerDetailedReason = $VirtualServerDetailedReasonList[$i].Data.ToString()

			#Create a property bag.
			Log-DebugEvent $SCRIPT_EVENT " Creating Property bag for $VirtualServerName"
			$bag = $api.CreatePropertyBag()
			$bag.AddValue("VirtualServerName", $VirtualServerName)
			$bag.AddValue("EnabledState", $VirtualServerEnabledState)
			$bag.AddValue("DetailedReason", $VirtualServerDetailedReason)
			#$api.Return($bag)
			$bag
		}
		# All Done So Don't try any more devices
		break	

	}
	Catch {
		Log-DebugEvent $SCRIPT_EVENT "Could not Contact $DeviceAddressList[$i]"
	}
}

# Log Finished Message
Log-DebugEvent $SCRIPT_ENDED " Script Ended."