#==================================================================================
# Script: 	Get-NodeConnections.ps1
# Date:		01/08/18
# Author: 	Andi Patrick
# Purpose:	Gets Current Node Connections via SNMP returns all as Property Bag
#==================================================================================

# Get the named parameters
param($Debug,$DeviceAddresses,$DevicePorts,$DeviceCommunities,$SharpSnmpLocation)

# Get Start Time For Script
$StartTime = (GET-DATE)

#Constants used for event logging
$SCRIPT_NAME			= 'Get-NodeConnections.ps1'
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
# bigipTrafficMgmt.bigipLocalTM.ltmNodes.ltmNodeAddrStat.ltmNodeAddrStatTable.ltmNodeAddrStatEntry.ltmNodeAddrStatNodeName
[string]$ltmNodeAddrStatNodeName = ".1.3.6.1.4.1.3375.2.2.4.2.3.1.20" 
# bigipTrafficMgmt.bigipLocalTM.ltmNodes.ltmNodeStat.ltmNodeStatTable.ltmNodeStatEntry.ltmNodeStatServerCurConns
[string]$ltmNodeStatServerCurConns = ".1.3.6.1.4.1.3375.2.2.4.2.3.1.9" 

# Initialize Connections Array
$ConnectionsArray = @()

For ($i=0;$i -lt $DeviceAddressList.Count;$i++) {
	Try
	{
		# Create endpoint for SNMP server
		$ip = [System.Net.IPAddress]::Parse($DeviceAddressList[$i])
		$svr = New-Object System.Net.IpEndPoint ($ip, $DevicePortList[$i])

		# Get Node Names from SNMP
		$NodeNameList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
		[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunityList[$i], $ltmNodeAddrStatNodeName , $NodeNameList, 3000, $walkMode) > $null

		# Get Current Connections from SNMP
		$ConnectionsList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
		[Lextm.SharpSnmpLib.Messaging.Messenger]::Walk($ver, $svr, $DeviceCommunityList[$i], $ltmNodeStatServerCurConns , $ConnectionsList, 3000, $walkMode) > $null

		# Create Objects On First Device
		If($ConnectionsArray.Count -eq 0) {
			For ($j=0; $j -lt $NodeNameList.Count; $j++) {
				[string]$NodeName = $NodeNameList[$j].Data.ToString()
				[int]$Connections = [int]$ConnectionsList[$j].Data.ToString()

				# Create Object & Properties
				$NewConnectionObject = New-Object System.Object
				$NewConnectionObject | Add-Member -type NoteProperty -name NodeName -value $NodeName
				$NewConnectionObject | Add-Member -type NoteProperty -name Connections -value $Connections
				# Add to Array
				$ConnectionsArray += $NewConnectionObject
			}
		} else {
			# Loop Through All SNMP Results
			For ($j=0; $j -lt $NodeNameList.Count; $j++) {
				[string]$NodeName = $NodeNameList[$j].Data.ToString()
				[int]$Connections = [int]$ConnectionsList[$j].Data.ToString()

				# Find matching Entry in Array
				For($k=0;$k -lt $ConnectionsArray.Count; $k++){
					If ($NodeName -eq $ConnectionsArray[$k].NodeName) {
						# Check if Connections is Greater
						If ($Connections -gt  $ConnectionsArray[$k].Connections) {
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
		$message = "SNMP Server : " + $DeviceAddressList[$i] + "`r`nSNMP Port : " + $DevicePortList[$i] + "`r`nSNMP Community : " + $DeviceCommunityList[$i] + "`r`nSharpSnmp Location : " + $SharpSnmpLocation + "`r`nError : $_"
		Log-DebugEvent $SCRIPT_ERROR $message
	}
}

# Create Property bags From the Array
For($i=0;$i -lt $ConnectionsArray.Count; $i++){
		# Get Conenctions
		$Connections = [int]$ConnectionsArray[$i].Connections
		# Get Node Name
		$Name = $ConnectionsArray[$i].NodeName
		#Create a property bag.
		[string] $message = "Created Node Property bag for $Name`r`n" + "Connections : " + $Connections
		Log-DebugEvent $SCRIPT_PROPERTYBAG_CREATED $message
		$bag = $api.CreatePropertyBag()
		$bag.AddValue("NodeName", $Name)
		$bag.AddValue("Connections", $Connections)
		#$api.Return($bag)
		$bag		

}
# Get End Time For Script
$EndTime = (GET-DATE)
$TimeTaken = NEW-TIMESPAN -Start $StartTime -End $EndTime
$Seconds = [math]::Round($TimeTaken.TotalSeconds, 2)
# Log Finished Message
Log-DebugEvent $SCRIPT_ENDED "Script Finished. Took $Seconds Seconds to Complete!"