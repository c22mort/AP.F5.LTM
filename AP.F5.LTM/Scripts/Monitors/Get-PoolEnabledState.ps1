#==================================================================================
# Script: 	Get-PoolEnabledState.ps1
# Date:		01/08/18
# Author: 	Andi Patrick
# Purpose:	Gets Pool Status via SNMP returns all as Property Bag
#==================================================================================

# Get the named parameters
param($Debug,$DeviceAddresses,$DevicePorts,$DeviceCommunities,$SharpSnmpLocation)

# Get Start Time For Script
$StartTime = (GET-DATE)

#Constants used for event logging
$SCRIPT_NAME			= 'Get-PoolEnabledState.ps1'
$EVENT_LEVEL_ERROR 		= 1
$EVENT_LEVEL_WARNING 	= 2
$EVENT_LEVEL_INFO 		= 4

$SCRIPT_STARTED				= 4601
$SCRIPT_PROPERTYBAG_CREATED	= 4602
$SCRIPT_EVENT				= 4603
$SCRIPT_ENDED				= 4604
$SCRIPT_ERROR				= 4605

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
Log-DebugEvent $SCRIPT_STARTED "Script Started."

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
# bigipTrafficMgmt.bigipLocalTM.ltmPools.ltmPoolStatus.ltmPoolStatusTable.ltmPoolStatusEntry.ltmPoolStatusName
[string]$ltmPoolStatusName = ".1.3.6.1.4.1.3375.2.2.5.5.2.1.1"
# bigipTrafficMgmt.bigipLocalTM.ltmPools.ltmPoolStatus.ltmPoolStatusTable.ltmPoolStatusEntry.ltmPoolStatusEnabledState
[string]$ltmPoolStatusEnabledState = ".1.3.6.1.4.1.3375.2.2.5.5.2.1.3" 
# bigipTrafficMgmt.bigipLocalTM.ltmPools.ltmPoolStatus.ltmPoolStatusTable.ltmPoolStatusEntry.ltmPoolStatusDetailReason
[string]$ltmPoolStatusDetailReason = ".1.3.6.1.4.1.3375.2.2.5.5.2.1.5" 

# Loop Through All Devices
For ($i=0;$i -lt $DeviceAddressList.Count;$i++) {
	Try {

		# Create endpoint for SNMP server
		$ip = [System.Net.IPAddress]::Parse($DeviceAddressList[$i])
		$svr = New-Object System.Net.IpEndPoint ($ip, $DevicePortList[$i])

		# Get Pool Names from SNMP
		$PoolNameList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
		[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunityList[$i], $ltmPoolStatusName , $PoolNameList, 3000, $walkMode)

		# Get Pool EnabledState from SNMP
		$PoolEnabledStateList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
		[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunityList[$i], $ltmPoolStatusEnabledState , $PoolEnabledStateList, 3000, $walkMode)

		# Get Pool DetailedReasons from SNMP
		$PoolDetailedReasonList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
		[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunityList[$i], $ltmPoolStatusDetailReason , $PoolDetailedReasonList, 3000, $walkMode)

		# Loop Through Results
		For($i=0; $i -lt $PoolNameList.Count; $i++) {
			[string]$PoolName = $PoolNameList[$i].Data.ToString()
			[string]$PoolEnabledState = $PoolEnabledStateList[$i].Data.ToString()
			[string]$PoolDetailedReason = $PoolDetailedReasonList[$i].Data.ToString()

			#Create a property bag.
			Log-DebugEvent $SCRIPT_PROPERTYBAG_CREATED "Creating Property bag for $PoolName"
			$bag = $api.CreatePropertyBag()
			$bag.AddValue("PoolName", $PoolName)
			$bag.AddValue("EnabledState", $PoolEnabledState)
			$bag.AddValue("DetailedReason", $PoolDetailedReason)
			#$api.Return($bag)
			$bag
		}

		# All Done So Don't try any more devices
		break	
	}
	Catch {
		$message = "SNMP Server : $DeviceAddress" + "`r`nSNMP Port : " + $DevicePort + "`r`nSNMP Community : " + $DeviceCommunity + "`r`nSharpSnmp Location : " + $SharpSnmpLocation + "`r`nError : $_"
		Log-DebugEvent $SCRIPT_ERROR $message
	}
}
# Get End Time For Script
$EndTime = (GET-DATE)
$TimeTaken = NEW-TIMESPAN -Start $StartTime -End $EndTime
$Seconds = [math]::Round($TimeTaken.TotalSeconds, 2)
# Log Finished Message
Log-DebugEvent $SCRIPT_ENDED "Script Finished. Took $Seconds Seconds to Complete!"