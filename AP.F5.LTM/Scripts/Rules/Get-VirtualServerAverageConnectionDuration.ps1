#==================================================================================
# Script: 	Get-VirtualServerAverageConnectionDuration.ps1
# Date:		03/09/18
# Author: 	Andi Patrick
# Purpose:	Gets Average Connection Duration via SNMP returns all as Property Bag
#==================================================================================

# Get the named parameters
param($Debug,$DeviceAddresses,$DevicePorts,$DeviceCommunities,$SharpSnmpLocation)

#Constants used for event logging
$SCRIPT_NAME			= 'Get-VirtualServerAverageConnectionDuration.ps1'
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
[System.Collections.ArrayList]$DeviceAddressList = $DeviceAddresses.Split(",")
[System.Collections.ArrayList]$DevicePortList = $DevicePorts.Split(",")
[System.Collections.ArrayList]$DeviceCommunityList = $DeviceCommunities.Split(",")

# Load SharpSNMPLib
[reflection.assembly]::LoadFrom( (Resolve-Path $SharpSnmpLocation) )
# Use SNMP v2
$ver = [Lextm.SharpSnmpLib.VersionCode]::V2
# Set Walk Mode to WithinSubtree
$walkMode = [Lextm.SharpSnmpLib.Messaging.WalkMode]::WithinSubtree


# OIDs used
# bigipTrafficMgmt.bigipLocalTM.ltmVirtualServers.ltmVirtualServStat.ltmVirtualServStatTable.ltmVirtualServStatEntry.ltmVirtualServStatName
[string]$ltmVirtualServStatName = ".1.3.6.1.4.1.3375.2.2.10.2.3.1.1" 
# bigipTrafficMgmt.bigipLocalTM.ltmVirtualServers.ltmVirtualServStat.ltmVirtualServStatTable.ltmVirtualServStatEntry.ltmVirtualServStatCsMeanConnDur
[string]$ltmVirtualServStatCsMeanConnDur = ".1.3.6.1.4.1.3375.2.2.10.2.3.1.4" 

# Initialize Connections Array
$ConnectionDurationArray = @()

For ($i=0;$i -lt $DeviceAddressList.Count;$i++) {

	Try
	{
		# Create endpoint for SNMP server
		$ip = [System.Net.IPAddress]::Parse($DeviceAddressList[$i])
		$svr = New-Object System.Net.IpEndPoint ($ip, $DevicePortList[$i])

		# Get VirtualServer Names from SNMP
		$VirtualServerNameList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
		[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunityList[$i], $ltmVirtualServStatName , $VirtualServerNameList, 3000, $walkMode) > $null

		# Get Average Connections Duration from SNMP
		$ConnectionsDurationList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
		[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunityList[$i], $ltmVirtualServStatCsMeanConnDur , $ConnectionsDurationList, 3000, $walkMode) > $null

		# Create Objects On First Device
		If($ConnectionDurationArray.Count -eq 0) {
			For ($j=0; $j -lt $VirtualServerNameList.Count; $j++) {
				[string]$VirtualServerName = $VirtualServerNameList[$j].Data.ToString()
				[int]$ConnectionDuration = [int]$ConnectionsDurationList[$j].Data.ToString()

				# Create Object & Properties
				$NewConnectionObject = New-Object System.Object
				$NewConnectionObject | Add-Member -type NoteProperty -name VirtualServerName -value $VirtualServerName
				$NewConnectionObject | Add-Member -type NoteProperty -name ConnectionDuration -value $ConnectionDuration
				# Add to Array
				$ConnectionDurationArray += $NewConnectionObject
			}
		} else {
			# Loop Through All SNMP Results
			For ($j=0; $j -lt $VirtualServerNameList.Count; $j++) {
				[string]$VirtualServerName = $VirtualServerNameList[$j].Data.ToString()
				[int]$ConnectionDuration = [int]$ConnectionsDurationList[$j].Data.ToString()

				# Find matching Entry in Array
				For($k=0;$k -lt $ConnectionDurationArray.Count; $k++){
					If ($VirtualServerName -eq $ConnectionDurationArray[$k].VirtualServerName) {
						# Check if Connections is Greater
						If ($Connections -gt  $ConnectionDurationArray[$k].Connections) {
							Write-Host "Found Bigger"
							$ConnectionDurationArray[$k].Connections = $ConnectionDuration
						}
						break;
					}
				}
			}
	
		}
	}
	Catch
	{
		Log-DebugEvent $SCRIPT_EVENT "Could not Contact $DeviceAddressList[$i]"		
	}
}

# Create Property bags From the Array
For($i=0;$i -lt $ConnectionDurationArray.Count; $i++){
		#Create a property bag.
		Log-DebugEvent $SCRIPT_EVENT " Creating Property bag for $VirtualServerName"
		#Convert Milliseconds to Seconds
		$MilliSeconds = [int]$ConnectionDurationArray[$i].ConnectionDuration
		[double]$Seconds = [math]::Round(($MilliSeconds / 1000), 2)

		$bag = $api.CreatePropertyBag()
		$bag.AddValue("VirtualServerName", $ConnectionDurationArray[$i].VirtualServerName)
		$bag.AddValue("ConnectionDuration", $Seconds)
		#$api.Return($bag)
		$bag		

}
# Log Finished Message
Log-DebugEvent $SCRIPT_ENDED " Script Ended."