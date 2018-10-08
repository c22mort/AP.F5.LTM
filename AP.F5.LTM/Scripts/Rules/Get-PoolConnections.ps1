#==================================================================================
# Script: 	Get-PoolConnections.ps1
# Date:		01/08/18
# Author: 	Andi Patrick
# Purpose:	Gets Current Pool Connections via SNMP returns all as Property Bag
#==================================================================================

# Get the named parameters
param($Debug,$DeviceAddresses,$DevicePorts,$DeviceCommunities,$SharpSnmpLocation)

#Constants used for event logging
$SCRIPT_NAME			= 'Get-PoolConnections.ps1'
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
# bigipTrafficMgmt.bigipLocalTM.ltmPools.ltmPoolStat.ltmPoolStatTable.ltmPoolStatEntry.ltmPoolStatName
[string]$ltmPoolStatName = ".1.3.6.1.4.1.3375.2.2.5.2.3.1.1" 
# bigipTrafficMgmt.bigipLocalTM.ltmPools.ltmPoolStat.ltmPoolStatTable.ltmPoolStatEntry.ltmPoolStatServerCurConns
[string]$ltmPoolStatServerCurConns = ".1.3.6.1.4.1.3375.2.2.5.2.3.1.8" 

# Initialize Connections Array
$ConnectionsArray = @()

For ($i=0;$i -lt $DeviceAddressList.Count;$i++) {

	Try
	{
		# Create endpoint for SNMP server
		$ip = [System.Net.IPAddress]::Parse($DeviceAddressList[$i])
		$svr = New-Object System.Net.IpEndPoint ($ip, $DevicePortList[$i])

		# Get Pool Names from SNMP
		$PoolNameList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
		[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunityList[$i], $ltmPoolStatName , $PoolNameList, 3000, $walkMode) > $null

		# Get Current Connections from SNMP
		$ConnectionsList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
		[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunityList[$i], $ltmPoolStatServerCurConns , $ConnectionsList, 3000, $walkMode) > $null

		# Create Objects On First Device
		If($ConnectionsArray.Count -eq 0) {
			For ($j=0; $j -lt $PoolNameList.Count; $j++) {
				[string]$PoolName = $PoolNameList[$j].Data.ToString()
				[int]$Connections = [int]$ConnectionsList[$j].Data.ToString()

				# Create Object & Properties
				$NewConnectionObject = New-Object System.Object
				$NewConnectionObject | Add-Member -type NoteProperty -name PoolName -value $PoolName
				$NewConnectionObject | Add-Member -type NoteProperty -name Connections -value $Connections
				# Add to Array
				$ConnectionsArray += $NewConnectionObject
			}
		} else {
			# Loop Through All SNMP Results
			For ($j=0; $j -lt $PoolNameList.Count; $j++) {
				[string]$PoolName = $PoolNameList[$j].Data.ToString()
				[int]$Connections = [int]$ConnectionsList[$j].Data.ToString()

				# Find matching Entry in Array
				For($k=0;$k -lt $ConnectionsArray.Count; $k++){
					If ($PoolName -eq $ConnectionsArray[$k].PoolName) {
						# Check if Connections is Greater
						If ($Connections -gt  $ConnectionsArray[$k].Connections) {
							Write-Host "Found Bigger"
							$ConnectionsArray[$k].Connections = $Connections
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
For($i=0;$i -lt $ConnectionsArray.Count; $i++){
		#Create a property bag.
		Log-DebugEvent $SCRIPT_EVENT " Creating Property bag for $PoolName"
		$bag = $api.CreatePropertyBag()
		$bag.AddValue("PoolName", $ConnectionsArray[$i].PoolName)
		$bag.AddValue("Connections", [int]$ConnectionsArray[$i].Connections)
		#$api.Return($bag)
		$bag		

}
# Log Finished Message
Log-DebugEvent $SCRIPT_ENDED " Script Ended."