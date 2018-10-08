After Installation you will need to copy the following files into the installation directory;
Microsoft.EnterpriseManagement.Core.dll
Microsoft.EnterpriseManagement.OperationsManager.dll
Microsoft.EnterpriseManagement.Runtime.dll

The Management Pack AP.F5.Base is located in a folder called ManagementPacks, this will need to be imported into SCOM before you run AP.F5.Base.Discovery.exe

example_config.csv, will need to be renamed to config.csv and filled in with the DNS name of the management server you want to connect to, 
if config.csv does not exist then localhost will be used, useful if you are installing on a Management Server.

example_devices.csv will need to be renamed to devices.csv, and relevant details for your F5 devices filled in;
IP Address, SNMP Community Name, SNMP Port, F5 UserName (for iControl), F5 Password (for iControl)

Make sure the computer that this is installed on is allowed to communicate with the F5 Devices via SNMP.

See Install-Guide.rtf for more information

