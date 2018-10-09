#==================================================================================
# Script: 	Get-ServerProfileSslStatus.ps1
# Date:		01/07/18
# Author: 	Andi Patrick
# Purpose:	Gets Server SSL Profile Status where ther is no Certificate File;
#			ProfileOkay : No Certificate File Listed
#==================================================================================

# Get the named parameters
param($Debug)

#Constants used for event logging
$SCRIPT_NAME			= 'Get-ServerProfileSslStatus.ps1'
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
Log-DebugEvent $SCRIPT_STARTED $message

# Get All Server SSL Profiles
$instances = Get-SCOMClass –Name "AP.F5.LTM.ProfileServerSSL" | Get-SCOMClassInstance | Select-Object *

# Loop Through Them
foreach ($instance in $instances) {
	$cert = $instance.'[AP.F5.LTM.ProfileServerSSL].CertFileNames'.value
	$name = $instance.'[AP.F5.LTM.ProfileServerSSL].FullName'.value
	if ($cert -eq ""){
		Log-DebugEvent $PROPERTYBAG_CREATED "Created PropertyBag for $name"
		#Create a property bag.
		$bag = $api.CreatePropertyBag()
		$bag.AddValue('ProfileName', $name)
		$bag.AddValue('Status', 'okay')
		#Return the property bag.
		#$api.Return($bag)
		$bag	
	}
}

# Log Finished Message
$message =	$SCRIPT_NAME + " Ended."
Log-DebugEvent $SCRIPT_ENDED $message