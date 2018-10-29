#==================================================================================
# Script: 	Get-TrafficGroupStatus.ps1
# Date:		03/08/18
# Author: 	Andi Patrick
# Purpose:	Uses SharpSnmpLib to get F5 Traffic Group Status returning 
#			as PropertyBag
#==================================================================================
param($Debug,$TrafficGroupName,$GroupAddresses,$GroupPorts,$GroupCommunities,$SharpSnmpLocation)

# Get Start Time For Script
$StartTime = (GET-DATE)

#Constants used for event logging
$SCRIPT_NAME			= 'Get-TrafficGroupStatus.ps1'
$EVENT_LEVEL_ERROR 		= 1
$EVENT_LEVEL_WARNING 	= 2
$EVENT_LEVEL_INFO 		= 4

$SCRIPT_STARTED				= 4601
$SCRIPT_PROPERTYBAG_CREATED	= 4602
$SCRIPT_EVENT				= 4603
$SCRIPT_ENDED				= 4604
$SCRIPT_ERROR				= 4605

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
Log-DebugEvent $SCRIPT_STARTED "Script Started."

# Get Addresses, Ports And Communities
$AddressList = $GroupAddresses.Split(",")
$PortList = $GroupPorts.Split(",")
$CommunityList = $GroupCommunities.Split(",")

# Load SharpSNMPLib
[reflection.assembly]::LoadFrom( (Resolve-Path $SharpSnmpLocation) )

# Use SNMP v2
$ver = [Lextm.SharpSnmpLib.VersionCode]::V2
$walkMode = [Lextm.SharpSnmpLib.Messaging.WalkMode]::WithinSubtree

# OIDs used
# bigipTrafficMgmt.bigipSystem.sysCM.sysCmTrafficGroupStatus.sysCmTrafficGroupStatusTable.sysCmTrafficGroupStatusEntry.sysCmTrafficGroupStatusTrafficGroup
[string]$sysCmTrafficGroupStatusTrafficGroup = ".1.3.6.1.4.1.3375.2.1.14.5.2.1.1"
# bigipTrafficMgmt.bigipSystem.sysCM.sysCmTrafficGroupStatus.sysCmTrafficGroupStatusTable.sysCmTrafficGroupStatusEntry.sysCmTrafficGroupStatusFailoverStatus
[string]$sysCmTrafficGroupStatusFailoverStatus = ".1.3.6.1.4.1.3375.2.1.14.5.2.1.3"



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
		$message = "SNMP Server : $DeviceAddress" + "`r`nSNMP Port : " + $DevicePort + "`r`nSNMP Community : " + $DeviceCommunity + "`r`nSharpSnmp Location : " + $SharpSnmpLocation + "`r`nError : $_"
		Log-DebugEvent $SCRIPT_ERROR $message	
	}
}
#Create a property bag.
Log-DebugEvent $SCRIPT_PROPERTYBAG_CREATED "Creating Property bag for $TrafficGroupName"
$bag = $api.CreatePropertyBag()
		
# Add Result to Property Bag
$bag.AddValue("TrafficGroupName", $TrafficGroupName)
$bag.AddValue("Value", $TrafficGroupStatus)

#Return the PropertyBag.
#$api.Return($bag)
$bag

# Get End Time For Script
$EndTime = (GET-DATE)
$TimeTaken = NEW-TIMESPAN -Start $StartTime -End $EndTime
$Seconds = [math]::Round($TimeTaken.TotalSeconds, 2)
# Log Finished Message
Log-DebugEvent $SCRIPT_ENDED "Script Finished. Took $Seconds Seconds to Complete!"
