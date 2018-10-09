﻿#==================================================================================
# Script: 	Get-NodeAvailabilityState.ps1
# Date:		01/08/18
# Author: 	Andi Patrick
# Purpose:	Gets Node Status via SNMP returns all as Property Bag
#==================================================================================

# Get the named parameters
param($Debug,$DeviceAddresses,$DevicePorts,$DeviceCommunities,$SharpSnmpLocation)

#Constants used for event logging
$SCRIPT_NAME			= 'Get-NodeAvailabilityState.ps1'
$EVENT_LEVEL_ERROR 		= 1
$VENT_LEVEL_WARNING 	= 2
$EVENT_LEVEL_INFO 		= 4

$SCRIPT_STARTED			= 4601
$PROPERTYBAG_CREATED	= 4602
$SCRIPT_EVENT			= 4603
$SCRIPT_ENDED			= 4604

#==================================================================================
# Function:	LogDebugEvent
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
# bigipTrafficMgmt.bigipLocalTM.ltmNodes.ltmNodeAddrStatus.ltmNodeAddrStatusTable.ltmNodeAddrStatusEntry.ltmNodeAddrStatusName
[string]$ltmNodeAddrStatusName = ".1.3.6.1.4.1.3375.2.2.4.3.2.1.7"
# bigipTrafficMgmt.bigipLocalTM.ltmNodes.ltmNodeAddrStatus.ltmNodeAddrStatusTable.ltmNodeAddrStatusEntry.ltmNodeAddrStatusEnabledState
[string]$ltmNodeAddrStatusEnabledState = ".1.3.6.1.4.1.3375.2.2.4.3.2.1.4" 
# bigipTrafficMgmt.bigipLocalTM.ltmNodes.ltmNodeAddrStatus.ltmNodeAddrStatusTable.ltmNodeAddrStatusEntry.ltmNodeAddrStatusAvailState
[string]$ltmNodeAddrStatusAvailState = ".1.3.6.1.4.1.3375.2.2.4.3.2.1.3"
# bigipTrafficMgmt.bigipLocalTM.ltmNodes.ltmNodeAddrStatus.ltmNodeAddrStatusTable.ltmNodeAddrStatusEntry.ltmNodeAddrStatusDetailReason
[string]$ltmNodeAddrStatusDetailReason = ".1.3.6.1.4.1.3375.2.2.4.3.2.1.6" 

# Loop Through All Devices
For ($i=0;$i -lt $DeviceAddressList.Count;$i++) {

	Try
	{
		# Create endpoint for SNMP server
		$ip = [System.Net.IPAddress]::Parse($DeviceAddressList[$i])
		$svr = New-Object System.Net.IpEndPoint ($ip, $DevicePortList[$i])

		# Get Node Names from SNMP
		$NodeNameList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
		[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunityList[$i], $ltmNodeAddrStatusName , $NodeNameList, 3000, $walkMode)

		# Get Node EnabledState from SNMP
		$NodeEnabledStateList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
		[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunityList[$i], $ltmNodeAddrStatusEnabledState , $NodeEnabledStateList, 3000, $walkMode)

		# Get Node AvailabilityState from SNMP
		$NodeAvailabilityStateList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
		[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunityList[$i], $ltmNodeAddrStatusAvailState , $NodeAvailabilityStateList, 3000, $walkMode)

		# Get Node DetailedReason from SNMP
		$NodeDetailedReasonList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
		[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunityList[$i], $ltmNodeAddrStatusDetailReason , $NodeDetailedReasonList, 3000, $walkMode)

		# Loop Through Results
		For($i=0; $i -lt $NodeNameList.Count; $i++) {
			[int]$NodeEnabledState = $NodeEnabledStateList[$i].Data.ToString()
			# Only Do Enabled Nodes
			If ($NodeEnabledState -eq 1) {			
				[string]$NodeName = $NodeNameList[$i].Data.ToString()
				[int]$NodeAvailabilityState = $NodeAvailabilityStateList[$i].Data.ToString()
				[string]$NodeDetailedReason = $NodeDetailedReasonList[$i].Data.ToString()
	
				#Create a property bag.
				Log-DebugEvent $SCRIPT_EVENT " Creating Property bag for $NodeName"
				$bag = $api.CreatePropertyBag()
				$bag.AddValue("NodeName", $NodeName)
				$bag.AddValue("AvailabilityState", $NodeAvailabilityState)
				$bag.AddValue("DetailedReason", $NodeDetailedReason)
				#$api.Return($bag)
				$bag		
			} 
		}
		# All Done So Don't try any more devices
		break
	}
	Catch
	{
		Log-DebugEvent $SCRIPT_EVENT "Could not Contact $DeviceAddressList[$i]"
	}
}

# Log Finished Message
Log-DebugEvent $SCRIPT_ENDED " Script Ended."