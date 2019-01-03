using AP.F5.LTM.Discovery.classes;
using AP.F5.LTM.Discovery.Classes;
using iControl;
using Lextm.SharpSnmpLib;
using LumenWorks.Framework.IO.Csv;
using Microsoft.EnterpriseManagement;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;
using Microsoft.EnterpriseManagement.ConnectorFramework;
using System;
using System.Collections.Generic;
using System.IO;

namespace AP.F5.LTM.Discovery
{
    class Program
    {

        // SCOM Functions Instance
        private static SCOM sf = new SCOM();

        // log4net Instance
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Config File Name
        private static string m_configFileName = "config.csv";
        // Device File Name
        private static string m_deviceFileName = "devices.csv";
        // Management Server Name
        private static string m_managementServer;

        // Create Snapshot Discovery Data Object
        private static SnapshotDiscoveryData discoData = new SnapshotDiscoveryData();
        private static IncrementalDiscoveryData relationshipData = new IncrementalDiscoveryData();


        // Interface to F5 Device
        private static Interfaces m_interfaces = new Interfaces();

        // List of SyncFailover Groups
        private static List<SyncFailoverGroup> SyncFailoverGroupList = new List<SyncFailoverGroup>();

        // List of Devices
        private static List<f5Device> DeviceList = new List<f5Device>();
        // List of SCOM F5 Devices
        private static SCOM_Devices SCOM_DeviceList;

        // CSV Position Indicators
        public static int CSV_ADDRESS = 0;
        public static int CSV_COMMUNITY = 1;
        public static int CSV_PORT = 2;
        public static int CSV_F5USER = 3;
        public static int CSV_F5PASSWORD = 4;


        /// <summary>
        /// Application Entry Point
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // Write Header
            Console.WriteLine("AP.F5.LTM.Discovery, ©A.Patrick 2017-2018");
            Console.WriteLine("Discovers F5 LTM Classes for SCOM.");
            Console.WriteLine("");

            // First Thing is to get the Managment Server Name from the config file (if it exists).
            m_managementServer = GetManagementServer();

            // Get Devices (If Device File Exists
            if (File.Exists(m_deviceFileName))
            {
                // Write Discovered Data to SCOM Database ( For Relationships
                log.Info("Creating Inbound Connector to " + m_managementServer + "...");

                // Get Management Group
                SCOM.m_managementGroup = new ManagementGroup(m_managementServer);

                // Create Our Inbound Connector
                SCOM.CreateConnector();
                log.Info("Inbound Connector Created..." + m_managementServer + "...");

                // Has Conenctor been created successfully
                if (SCOM.m_monitoringConnector.Initialized)
                {
                    // Log Progress
                    log.Info("Starting Device Discovery...");
                    Console.WriteLine();
                    // Get Devices from SCOM
                    SCOM_DeviceList = new SCOM_Devices();
                    // Get Devices from CSV
                    GetDevices();
                    // Log Progress
                    log.Info(SCOM_DeviceList.Items.Count + " Devices Found in SCOM...");
                    log.Info(DeviceList.Count + " Devices Found in CSV...");
                    Console.WriteLine();

                    // Get Device Groups / Traffic Groups
                    log.Info("Get Device Groups / Traffic Groups from...");
                    GetDeviceGroups();

                    // Create Discovery Data
                    Console.WriteLine();
                    log.Info("Creating Discovery Data...");
                    // Add Data to Discovery Data
                    AddDataToDiscoveryData();
                    // Write Discovery Data
                    log.Info("Writing Discovery Data to " + m_managementServer);
                    discoData.Overwrite(SCOM.m_monitoringConnector);

                    // Write Discovered Relationship Data to SCOM Database
                    Console.WriteLine();
                    log.Info("Creating Relationship Data...");
                    CreateRelationships();
                    // Write Relationship Discovery Data
                    log.Info("Writing Relationship Data to " + m_managementServer);
                    relationshipData.Overwrite(SCOM.m_monitoringConnector);


                    Console.WriteLine();


                }
                else
                {
                    log.Fatal("Couldn't Initialize Inbound SCOM Connector!");
                }

                // Free Connector
                SCOM.m_monitoringConnector.Uninitialize();
                SCOM.m_monitoringConnector = null;
            } else
            {
                log.Fatal("Couldn't Initialize Inbound SCOM Connector!");
                Environment.Exit(5);
            }
        }

        /// <summary>
        /// Get Basic Device Info
        /// </summary>
        private static void GetDevices()
        {
            // Get All Devices from SCOM
            ManagementPackClass mpc_Devices = SCOM.GetManagementPackClass("AP.F5.Device");
            IObjectReader<EnterpriseManagementObject> DeviceReader;
            //List<Guid> DevicesGuidList;
            DeviceReader = SCOM.m_managementGroup.EntityObjects.GetObjectReader<EnterpriseManagementObject>(mpc_Devices, ObjectQueryOptions.Default);

            // Load In CSV File
            CsvReader csv = new CsvReader(new StreamReader(m_deviceFileName), true);
            while (csv.ReadNextRecord())
            {
                f5Device newDevice = new f5Device(csv[CSV_ADDRESS], csv[CSV_COMMUNITY], Convert.ToInt32(csv[CSV_PORT]), csv[CSV_F5USER], csv[CSV_F5PASSWORD]);

                // Loop Through SCOM Objects
                foreach (EnterpriseManagementObject scom_dev in DeviceReader)
                {
                    if (scom_dev.DisplayName.ToLower() == newDevice.SystemNodeName.ToLower())
                    {
                        newDevice.SCOM_DeviceObject = scom_dev;
                        break;
                    }
                }

                // Check and Make Sure there's a corresponding SCOM Object
                if (newDevice.SCOM_DeviceObject != null)
                {
                    // Add Device to DeviceList
                    DeviceList.Add(newDevice);
                }
                else
                {
                    log.Warn("Could not Find " + newDevice.SystemNodeName + " in SCOM, skipping this device!");
                }
            }
            // Dispose of CSV File
            csv.Dispose();
        }

        /// <summary>
        /// Get Device Groups
        /// </summary>
        private static void GetDeviceGroups()
        {

            foreach (f5Device device in DeviceList)
            {
                // Show Info
                log.Info(device.Address + "...");

                // Connect to F5 Device via iControl
                m_interfaces.initialize(device.Address, device.F5usr, device.F5pwd);

                // Set active partition to "Common"
                m_interfaces.ManagementPartition.set_active_partition("Common");

                // Get Device Group Names
                string[] devGroupNameList = m_interfaces.ManagementDeviceGroup.get_list();
                ManagementDeviceGroupFailoverStatus failover = m_interfaces.ManagementDeviceGroup.get_failover_status();
                CommonEnabledState[] status = m_interfaces.ManagementDeviceGroup.get_network_failover_enabled_state(devGroupNameList);
                // Check Each Device Group
                foreach (string devGroupFullName in devGroupNameList)
                {
                    // Get array of device names
                    string[] devGroupDeviceNameList = m_interfaces.ManagementDeviceGroup.get_device(new string[] { devGroupFullName })[0];

                    // Sort it
                    Array.Sort(devGroupDeviceNameList, StringComparer.InvariantCulture);

                    // Get Short Name of Current Device
                    string devGroupShortName = devGroupFullName.Substring(devGroupFullName.LastIndexOf("/") + 1);

                    // Ignore gtm, device_trust_group
                    if ((devGroupShortName == "gtm") || (devGroupShortName == "device_trust_group")) { continue; }

                    // Set Key to Short Name of Current Device Group
                    string tempDevGroupKey = devGroupShortName;

                    // Append all Device Names
                    foreach (string devFullName in devGroupDeviceNameList)
                    {
                        tempDevGroupKey += "-" + devFullName.Substring(devFullName.LastIndexOf("/") + 1);
                    }

                    // Set exists to false
                    bool GroupExists = false;

                    // Check Device Group Type (Only Interested in ManagementDeviceGroupType.DGT_FAILOVER)
                    ManagementDeviceGroupType[] devGroupTypes = m_interfaces.ManagementDeviceGroup.get_type(new string[] { devGroupFullName });
                    ManagementDeviceGroupSyncStatus[] devGroupSyncStatus = m_interfaces.ManagementDeviceGroup.get_sync_status(new string[] { devGroupFullName });
                    if (devGroupTypes[0] == ManagementDeviceGroupType.DGT_FAILOVER)
                    {
                        // See if We already have this DeviceGroup
                        foreach (SyncFailoverGroup group in SyncFailoverGroupList)
                        {
                            // It Exists
                            if (group.Key == tempDevGroupKey)
                            {
                                // Set Standalone
                                group.Standalone = (devGroupSyncStatus[0].status == "Standalone") ? true : false;
                                // Add Device to DeviceList
                                group.DeviceList.Add(device);
                                // Add New Traffic Group(s)
                                foreach (string tg in device.ActiveTrafficGroups)
                                {
                                    TrafficGroup newTrafficGroup = new TrafficGroup();
                                    newTrafficGroup.DeviceGroup = group;
                                    newTrafficGroup.FullName = tg;
                                    newTrafficGroup.Name = tg.Substring(tg.LastIndexOf("/") + 1);
                                    group.TrafficGroupList.Add(newTrafficGroup);
                                }
                                GroupExists = true;
                            }
                        }
                        // If Group Doesn't Exist then Create it
                        if (!GroupExists)
                        {
                            // Create Device Group
                            SyncFailoverGroup newGroup = new SyncFailoverGroup();
                            // Add Device to DeviceList
                            newGroup.DeviceList.Add(device);
                            // Set Standalone
                            newGroup.Standalone = (devGroupSyncStatus[0].status == "Standalone") ? true : false;
                            // Set DeviceGroup Type
                            if (devGroupTypes[0] == ManagementDeviceGroupType.DGT_FAILOVER)
                            {
                                newGroup.Type = "Sync-Failover";
                            }
                            else { newGroup.Type = "Sync-Only"; }
                            newGroup.Key = tempDevGroupKey;
                            newGroup.Name = devGroupFullName.Substring(devGroupFullName.LastIndexOf("/") + 1);
                            // Add New Traffic Group(s)
                            foreach (string tg in device.ActiveTrafficGroups)
                            {
                                TrafficGroup newTrafficGroup = new TrafficGroup();
                                newTrafficGroup.DeviceGroup = newGroup;
                                newTrafficGroup.FullName = tg;
                                newTrafficGroup.Name = tg.Substring(tg.LastIndexOf("/") + 1);
                                newGroup.TrafficGroupList.Add(newTrafficGroup);
                            }

                            newGroup.deviceStrings = m_interfaces.ManagementDeviceGroup.get_device(new string[] { devGroupFullName })[0];
                            // Add DeviceGroup to List
                            SyncFailoverGroupList.Add(newGroup);
                        }
                    }
                }

            }

            // Get Device Groups/TrafficGroups for Standalone Devices
            GetGroupsForStandaloneDevices();

            // Log Discoverd Device Groups
            log.Info("Finished Device Group Discovery Found; " + SyncFailoverGroupList.Count.ToString() + " Groups!");

            // Clean up SyncFailover Groups
            foreach (SyncFailoverGroup sfog in SyncFailoverGroupList)
            {
                // Create Comma Seperated Devices String
                foreach (f5Device device in sfog.DeviceList)
                {
                    sfog.Devices += device.SystemNodeName + ',';
                }
                // Trim the last comma from devices
                sfog.Devices = sfog.Devices.TrimEnd(',');

                // Sort Device List by Serial Number (Needed For Making Key)
                sfog.DeviceList.Sort((x, y) => x.SerialNumber.CompareTo(y.SerialNumber));

                // Get Serial Key Suffix
                string KeySuffix = "";

                foreach (string deviceName in sfog.deviceStrings)
                {
                    foreach (EnterpriseManagementObject objDevice in SCOM_DeviceList.Items)
                    {
                        string sFullName = "/common/" + objDevice.DisplayName.ToLower();
                        if (deviceName.ToLower() == sFullName)
                        {
                            sfog.Addresses += objDevice.Values[2].ToString() + ",";
                            sfog.Ports += objDevice.Values[3].ToString() + ",";
                            sfog.Communities += objDevice.Values[4].ToString() + ",";
                            KeySuffix += "-" + objDevice.Values[13].ToString();
                            break;
                        }
                    }
                }


                sfog.Addresses = sfog.Addresses.TrimEnd(',');
                sfog.Ports = sfog.Ports.TrimEnd(',');
                sfog.Communities = sfog.Communities.TrimEnd(',');
                // Recalculate Device Group Key
                sfog.Key = sfog.Name + KeySuffix;


                // Sort Traffic Group List by Serial Number (Needed For Making Key)
                foreach (TrafficGroup tg in sfog.TrafficGroupList)
                {
                    tg.Devices = sfog.Devices;
                    tg.Key += tg.Name + KeySuffix;
                    tg.CreateScomObject();
                }

                // Get Virtual Servers
                log.Info("Collecting Further Information from..." + sfog.Name);
                GetObjectsFromDeviceGroup(sfog);

                // Create SCOM Object for This Device Object
                sfog.CreateDeviceGroupScomObject();

            }

        }

        /// <summary>
        /// Get Traffic Groups
        /// </summary>
        private static void GetGroupsForStandaloneDevices()
        {
            // Go Through Each Device
            foreach (f5Device device in DeviceList)
            {
                if (device.Standalone)
                {
                    // Create a Dummy Device Group (to keep the layout)
                    SyncFailoverGroup newDeviceGroup = new SyncFailoverGroup();
                    newDeviceGroup.Name = device.SystemNodeName;
                    newDeviceGroup.Type = "Sync-Failover";
                    newDeviceGroup.DeviceList.Add(device);
                    newDeviceGroup.Standalone = true;
                    newDeviceGroup.Addresses = device.Address;
                    newDeviceGroup.Communities = device.Community;
                    newDeviceGroup.Ports = device.Port.ToString();
                    foreach (string tg in device.ActiveTrafficGroups)
                    {
                        TrafficGroup newTrafficGroup = new TrafficGroup();
                        newTrafficGroup.Name = tg.Substring(tg.LastIndexOf("/") + 1);
                        newTrafficGroup.FullName = tg;
                        newTrafficGroup.Key = tg + "-" + device.SerialNumber;
                        newTrafficGroup.Devices = device.SystemNodeName;
                        newTrafficGroup.CreateScomObject();
                        newDeviceGroup.TrafficGroupList.Add(newTrafficGroup);

                    }
                    SyncFailoverGroupList.Add(newDeviceGroup);
                }
            }
        }

        /// <summary>
        /// Add Data to Discovery Data (All Objects and Relationship Obects should be sorted)
        /// </summary>
        private static void AddDataToDiscoveryData()
        {

            foreach (SyncFailoverGroup sfog in SyncFailoverGroupList)
            {
                // Add in Device Group
                discoData.Include(sfog.SCOM_Object);

                // Add in Traffic Groups
                foreach (TrafficGroup tg in sfog.TrafficGroupList)
                {
                    discoData.Include(tg.SCOM_Object);
                }

                // Add in Partitions
                foreach (Partition part in sfog.PartitionList)
                {
                    discoData.Include(part.SCOM_Object);
                    
                    // Add in Virtual Servers
                    foreach (VirtualServer vs in part.VirtualServerList)
                    {
                        discoData.Include(vs.SCOM_Object);
                    }

                    // Add Client SSL Profiles
                    foreach (ProfileClientSSL pcs in part.ClientSslProfileList)
                    {
                        discoData.Include(pcs.SCOM_Object);
                    }

                    
                    // Add Server SSL Profiles
                    foreach (ProfileServerSSL pss in part.ServerSslProfileList)
                    {
                        discoData.Include(pss.SCOM_Object);
                    }

                    // Add Certificates
                    foreach (Certificate cert in part.CertificateList)
                    {
                        discoData.Include(cert.SCOM_Object);
                    }

                    // Add in Pools
                    foreach (Pool pool in part.PoolList)
                    {
                        discoData.Include(pool.SCOM_Object);

                        // Add Pool Members
                        foreach (PoolMember member in pool.PoolMembers)
                        {
                            discoData.Include(member.SCOM_Object);
                        }
                    }

                    // Add Nodes
                    foreach (Node node in part.NodeList)
                    {
                        discoData.Include(node.SCOM_Object);
                    }

                }




            }
        }

        private static UInt64 ConvertUlong(CommonULong64 value)
        {
            return ((ulong)value.high << 32) | (uint)value.low;
        }

        /// <summary>
        /// Get OID Suffix
        /// </summary>
        /// <param name="FullName">FullName of F5 Object</param>
        /// <param name="OidWalk">SNMP OID to Walk</param>
        /// <param name="SnmpResults">Results From Previous SNMP WALK</param>
        /// <returns></returns>
        private static string GetOidSuffix(string FullName, string OidWalk, List<Variable> SnmpResults)
        {
            string suffix = "";

            foreach (var result in SnmpResults)
            {
                if (FullName.ToLower() == result.Data.ToString().ToLower())
                {
                    string fulloid = "." + result.Id.ToString();
                    suffix = fulloid.Replace(OidWalk, "");
                    break;
                }
            }

            return suffix;
        }

        /// <summary>
        /// Get Virtual Servers, Pools, PoolMembers, Profiles and Certificates from Group 
        /// </summary>
        /// <param name="devGroup">Group to Collect Info From</param>
        private static void GetObjectsFromDeviceGroup(SyncFailoverGroup sfog)
        {

            // Create Common Partition Holder
            Partition CommonPartition = new Partition("Blank", "Blank");

            // Attach to First Device in Device Group
            m_interfaces.initialize(sfog.DeviceList[0].Address, sfog.DeviceList[0].F5usr, sfog.DeviceList[0].F5pwd);

            if (m_interfaces.initialized)
            {
                // Get SNMP Info (Not Partition Dependant)
                // Virtual Server
                List<Variable> VirtualServerSnmpResults = SNMP.WalkSNMP(SNMP.ltmVsStatusName, sfog.DeviceList[0].Address, sfog.DeviceList[0].Port, sfog.DeviceList[0].Community);
                // Pools
                List<Variable> PoolSnmpResults = SNMP.WalkSNMP(SNMP.ltmPoolStatusName, sfog.DeviceList[0].Address, sfog.DeviceList[0].Port, sfog.DeviceList[0].Community);
                // Pool Members
                List<Variable> PoolMemberSnmpResults = SNMP.WalkSNMP(SNMP.ltmPoolMbrStatusNodeName, sfog.DeviceList[0].Address, sfog.DeviceList[0].Port, sfog.DeviceList[0].Community);
                // Client SSL Profiles
                List<Variable> ClientSslProfileSnmpResults = SNMP.WalkSNMP(SNMP.ltmClientSslName, sfog.DeviceList[0].Address, sfog.DeviceList[0].Port, sfog.DeviceList[0].Community);
                // Server SSL Profiles
                List<Variable> ServerSslProfileSnmpResults = SNMP.WalkSNMP(SNMP.ltmServerSslName, sfog.DeviceList[0].Address, sfog.DeviceList[0].Port, sfog.DeviceList[0].Community);

                // Get List of Partitions
                ManagementPartitionAuthZPartition[] partitionList = m_interfaces.ManagementPartition.get_partition_list();

                // Loop through Partitions
                foreach (ManagementPartitionAuthZPartition partition in partitionList)
                {
                    // Set Active Partition
                    m_interfaces.ManagementPartition.set_active_partition(partition.partition_name);

                    // Create Partition
                    Partition newPartition = new Partition(sfog.Key, partition.partition_name);
                    sfog.PartitionList.Add(newPartition);

                    // Get Certificates
                    ManagementKeyCertificateCertificateInformation_v2[] certs = m_interfaces.ManagementKeyCertificate.get_certificate_list_v2(ManagementKeyCertificateManagementModeType.MANAGEMENT_MODE_DEFAULT);
                    foreach (ManagementKeyCertificateCertificateInformation_v2 cert in certs)
                    {
                        Certificate newCert = new Certificate(cert, sfog, partition.partition_name);
                        newPartition.CertificateList.Add(newCert);
                    }

                    // Get Client SSL Profiles
                    // Get iControl Info
                    string[] ProfileClientSSLList = m_interfaces.LocalLBProfileClientSSL.get_list();
                    string[] ProfileClientSSLDescriptionList = m_interfaces.LocalLBProfileClientSSL.get_description(ProfileClientSSLList);
                    LocalLBProfileString[] ProfileClientSSL_ca_files = m_interfaces.LocalLBProfileClientSSL.get_certificate_file_v2(ProfileClientSSLList);

                    // Loop Through All Client SSL profiles
                    for (int i = 0; i < ProfileClientSSLList.Length; i++)
                    {
                        // Create Profile
                        ProfileClientSSL newProfile = new ProfileClientSSL(sfog, partition.partition_name, ProfileClientSSLList[i], ProfileClientSSLDescriptionList[i], ProfileClientSSL_ca_files[i].value);

                        // Look for Matching Certs in This Partition
                        foreach (Certificate cert in newPartition.CertificateList)
                        {
                            if (ProfileClientSSL_ca_files[i].value == cert.SCOM_Object.Values[3].ToString())
                            {
                                newProfile.CertificateList.Add(cert);
                            }
                        }

                        // Look For Matching Certificates in Common Partition
                        if (partition.partition_name!="Common")
                        {
                            foreach (Certificate cert in CommonPartition.CertificateList)
                            {
                                if (ProfileClientSSL_ca_files[i].value == cert.SCOM_Object.Values[3].ToString())
                                {
                                    newProfile.CertificateList.Add(cert);
                                }
                            }
                        }

                        // Save ClientSslProfile
                        newPartition.ClientSslProfileList.Add(newProfile);
                    }

                    // Get Server SSL Profiles
                    string[] ProfileServerSSLList = m_interfaces.LocalLBProfileServerSSL.get_list();
                    string[] ProfileServerSSLDescriptionList = m_interfaces.LocalLBProfileServerSSL.get_description(ProfileServerSSLList);
                    LocalLBProfileString[] ProfileServerSSL_ca_files = m_interfaces.LocalLBProfileServerSSL.get_certificate_file_v2(ProfileServerSSLList);
                    // Loop Through All Server SSL profiles
                    for (int i = 0; i < ProfileServerSSLList.Length; i++)
                    {
                        // Create Profile
                        ProfileServerSSL newProfile = new ProfileServerSSL(sfog, partition.partition_name, ProfileServerSSLList[i], ProfileServerSSLDescriptionList[i], ProfileServerSSL_ca_files[i].value);
                        // Look for Matching Certs in This Partition
                        foreach (Certificate cert in newPartition.CertificateList)
                        {
                            if (ProfileServerSSL_ca_files[i].value == cert.SCOM_Object.Values[3].ToString())
                            {
                                newProfile.CertificateList.Add(cert);
                            }
                        }

                        // Look For Matching Certificates in Common Partition
                        if (partition.partition_name != "Common")
                        {
                            foreach (Certificate cert in CommonPartition.CertificateList)
                            {
                                if (ProfileServerSSL_ca_files[i].value == cert.SCOM_Object.Values[3].ToString())
                                {
                                    newProfile.CertificateList.Add(cert);
                                }
                            }
                        }
                        newPartition.ServerSslProfileList.Add(newProfile);
                    }

                    // Get Virtual Servers
                    // Get iControl Info
                    string[] vipNames = m_interfaces.LocalLBVirtualServer.get_list();
                    string[] vipDefaultPools = m_interfaces.LocalLBVirtualServer.get_default_pool_name(vipNames);
                    LocalLBVirtualServerVirtualServerType[] vipTypes = m_interfaces.LocalLBVirtualServer.get_type(vipNames);
                    CommonAddressPort[] vipAddressPorts = m_interfaces.LocalLBVirtualServer.get_destination_v2(vipNames);
                    string[] vipDescriptions = m_interfaces.LocalLBVirtualServer.get_description(vipNames);
                    CommonULong64[] vipConnectionLimits = m_interfaces.LocalLBVirtualServer.get_connection_limit(vipNames);
                    LocalLBVirtualServerVirtualServerProfileAttribute[][] vipProfiles = m_interfaces.LocalLBVirtualServer.get_profile(vipNames);

                    // Loop Through All Virtual Servers
                    for (int i = 0; i < vipNames.Length; i++)
                    {
                        // Set Address
                        string vipAddress = vipAddressPorts[i].address;
                        vipAddress = vipAddress.Substring(vipAddress.LastIndexOf("/") + 1);
                        // Get Connection Limit
                        string vipConnectionLimit = ConvertUlong(vipConnectionLimits[i]).ToString();
                        // Get Oid Suffix
                        string vipOidSuffix = GetOidSuffix(vipNames[i], SNMP.ltmVsStatusName, VirtualServerSnmpResults);
                        // Get Client SSL Profiles
                        string vipClientSSLProfiles = "";
                        string vipServerSSLProfiles = "";
                        foreach (LocalLBVirtualServerVirtualServerProfileAttribute profileAttr in vipProfiles[i])
                        {
                            if (profileAttr.profile_type == LocalLBProfileType.PROFILE_TYPE_CLIENT_SSL)
                            {
                                vipClientSSLProfiles += profileAttr.profile_name + ",";
                            }
                            if (profileAttr.profile_type == LocalLBProfileType.PROFILE_TYPE_SERVER_SSL)
                            {
                                vipServerSSLProfiles += profileAttr.profile_name + ",";
                            }
                        }
                        vipClientSSLProfiles = vipClientSSLProfiles.TrimEnd(',');
                        vipServerSSLProfiles = vipServerSSLProfiles.TrimEnd(',');
                        // Get Device Group
                        string vipDeviceGroup = sfog.Name;

                        VirtualServer vs = new VirtualServer(sfog, partition.partition_name, vipNames[i], vipAddress, vipAddressPorts[i].port.ToString(), vipDescriptions[i],
                                                        vipTypes[i].ToString(), vipDefaultPools[i], vipConnectionLimit, vipClientSSLProfiles, vipServerSSLProfiles);

                        // Look For Matching ClientSslProfiles in this Partition
                        foreach (ProfileClientSSL profile in newPartition.ClientSslProfileList)
                        {
                            if (vipClientSSLProfiles.Contains(profile.SCOM_Object.Values[0].ToString()) )
                            {
                                vs.ProfileClientSslList.Add(profile);
                            }
                        }
                        // Look For Matching ClientSslProfiles in Common Partition
                        if (partition.partition_name != "Common")
                        {
                            foreach (ProfileClientSSL profile in CommonPartition.ClientSslProfileList)
                            {
                                if (vipClientSSLProfiles.Contains(profile.SCOM_Object.Values[0].ToString()))
                                {
                                    vs.ProfileClientSslList.Add(profile);
                                }
                            }
                        }
                        // Look For Matching ServerSslProfiles in this Partition
                        foreach (ProfileServerSSL profile in newPartition.ServerSslProfileList)
                        {
                            if (vipServerSSLProfiles.Contains(profile.SCOM_Object.Values[0].ToString()))
                            {
                                vs.ProfileServerSslList.Add(profile);
                            }
                        }
                        // Look For Matching ClientSslProfiles in Common Partition
                        if (partition.partition_name != "Common")
                        {
                            foreach (ProfileServerSSL profile in CommonPartition.ServerSslProfileList)
                            {
                                if (vipServerSSLProfiles.Contains(profile.SCOM_Object.Values[0].ToString()))
                                {
                                    vs.ProfileServerSslList.Add(profile);
                                }
                            }
                        }
                        // Add it to the List
                        newPartition.VirtualServerList.Add(vs);
                    }

                    // Get Pools & Pool Members
                    // Get iControl Info
                    string[] poolNames = m_interfaces.LocalLBPool.get_list();
                    string[] poolDescriptions = m_interfaces.LocalLBPool.get_description(poolNames);
                    long[] poolActiveMemberCount = m_interfaces.LocalLBPool.get_active_member_count(poolNames);
                    CommonAddressPort[][] poolMembers = m_interfaces.LocalLBPool.get_member_v2(poolNames);
                    string[][] poolMemberDescriptions = m_interfaces.LocalLBPool.get_member_description(poolNames, poolMembers);
                    // Loop Through All Pools
                    for (int i = 0; i < poolNames.Length; i++)
                    {
                        // Get Full Name
                        string sPoolName = poolNames[i];

                        // Get Node Short Name
                        string sShortPoolName = sPoolName.Substring(sPoolName.LastIndexOf("/") + 1);

                        // Ignore _auto_ Nodes
                        if (sShortPoolName.ToLower().StartsWith("_auto_")) { continue; }

                        // Get Oid Suffix
                        string OidSuffix = GetOidSuffix(sPoolName, SNMP.ltmPoolStatusName, PoolSnmpResults);

                        // Get Monitor Rule
                        string MonitorRule = SNMP.GetSNMP(SNMP.ltmPoolMonitorRule + OidSuffix, sfog.DeviceList[0].Address, sfog.DeviceList[0].Port, sfog.DeviceList[0].Community)[0].Data.ToString();

                        // Create new Pool
                        Pool newPool = new Pool(sfog, partition.partition_name, sPoolName, sShortPoolName, poolDescriptions[i],MonitorRule, poolMembers[i].Length, poolActiveMemberCount[i]);

                        // Add Pool Members
                        for (int j = 0; j < poolMembers[i].Length; j++)
                        {
                            // Get Oid Suffix
                            string pmOidSuffix = GetOidSuffix(poolMembers[i][j].address, SNMP.ltmPoolMbrStatusNodeName, PoolMemberSnmpResults);
                            // Get Monitor Rule (using SNMP as iControl seems to Error)
                            string pmMonitorRule = SNMP.GetSNMP(SNMP.ltmPoolMemberMonitorRule + pmOidSuffix, sfog.DeviceList[0].Address, sfog.DeviceList[0].Port, sfog.DeviceList[0].Community)[0].Data.ToString();

                            // Create New Pool Member
                            PoolMember newPoolMember = new PoolMember(sfog, partition.partition_name, sPoolName, poolMembers[i][j].address, poolMemberDescriptions[i][j], poolMembers[i][j].port.ToString(), pmMonitorRule);

                            // Add to Pool
                            newPool.PoolMembers.Add(newPoolMember);
                        }

                        // Add it to the List
                        newPartition.PoolList.Add(newPool);

                        // Match To Virtual Servers (DefaultPool)
                        foreach (VirtualServer vs in newPartition.VirtualServerList)
                        {
                            if (vs.DefaultPoolName == sPoolName)
                            {
                                vs.DefaultPool = newPool;
                            }
                        }
                    }


                    // Get Nodes
                    string[] nodeNameList = m_interfaces.LocalLBNodeAddressV2.get_list();
                    string[] nodeAddressList = m_interfaces.LocalLBNodeAddressV2.get_address(nodeNameList);
                    string[] nodeDescriptionList = m_interfaces.LocalLBNodeAddressV2.get_description(nodeNameList);
                    for (int i = 0; i < nodeNameList.Length; i++)
                    {
                        // Get Node Full Name
                        string sNodeName = nodeNameList[i];

                        // Get Node Short Name
                        string sShortNodeName = sNodeName.Substring(sNodeName.LastIndexOf("/") + 1);

                        // Ignore _auto_ Nodes
                        if (sShortNodeName.ToLower().StartsWith("_auto_")) { continue; }

                        string MonitorRules = "";
                        try
                        {
                            LocalLBMonitorRule[] nodeMonitorRuleList = m_interfaces.LocalLBNodeAddressV2.get_monitor_rule(new string[] { sNodeName });
                            for (int j = 0; j < nodeMonitorRuleList[0].monitor_templates.Length; j++)
                            {
                                MonitorRules += nodeMonitorRuleList[0].monitor_templates[j].ToString() + ",";
                            }
                            MonitorRules = MonitorRules.TrimEnd(',');
                        }
                        catch (Exception)
                        {

                        }

                        // Create a New Node
                        Node newNode = new Node(sfog.Key, partition.partition_name, sNodeName, sShortNodeName, nodeAddressList[i],nodeDescriptionList[i], MonitorRules);
                        newPartition.NodeList.Add(newNode);
                    }

                    // Take a Copy of Common Parition
                    if (partition.partition_name=="Common") { CommonPartition = newPartition; }
                }


            }

        }

        /// <summary>
        /// Create Relationships 
        /// </summary>
        private static void CreateRelationships()
        {
            foreach (SyncFailoverGroup sfog in SyncFailoverGroupList)
            {
                // Get SyncFailover Group Object
                EnterpriseManagementObject syncFailoverGroupObject = SCOM.m_managementGroup.EntityObjects.GetObject<EnterpriseManagementObject>(sfog.SCOM_Object.Id, ObjectQueryOptions.Default);

                // Add Device Relationship
                foreach (f5Device dev in sfog.DeviceList)
                {
                    // Create SyncFailoverGroup->Device Containment Relationship Object
                    ManagementPackRelationship mpr_SyncFailoverGroupContainsDevice = SCOM.GetManagementPackRelationship("AP.F5.LTM.SyncFailoverGroupContainsDevices");
                    CreatableEnterpriseManagementRelationshipObject obj_SyncFailoverGroupContainsDevice = new CreatableEnterpriseManagementRelationshipObject(SCOM.m_managementGroup, mpr_SyncFailoverGroupContainsDevice);
                    obj_SyncFailoverGroupContainsDevice.SetSource(syncFailoverGroupObject);
                    obj_SyncFailoverGroupContainsDevice.SetTarget(dev.SCOM_DeviceObject);
                    relationshipData.Add(obj_SyncFailoverGroupContainsDevice);
                }

                // Loop Through Traffic Groups
                foreach (TrafficGroup tg in sfog.TrafficGroupList)
                {
                    // Add Device Relationship
                    foreach (f5Device dev in tg.DeviceGroup.DeviceList)
                    {
                        // Create TrafficGroup->Device Containment Relationship Object
                        ManagementPackRelationship mpr_TrafficGroupContainsDevices = SCOM.GetManagementPackRelationship("AP.F5.LTM.TrafficGroupContainsDevices");
                        CreatableEnterpriseManagementRelationshipObject obj_TrafficGroupContainsDevice = new CreatableEnterpriseManagementRelationshipObject(SCOM.m_managementGroup, mpr_TrafficGroupContainsDevices);
                        obj_TrafficGroupContainsDevice.SetSource(tg.SCOM_Object);
                        obj_TrafficGroupContainsDevice.SetTarget(dev.SCOM_DeviceObject);
                        relationshipData.Add(obj_TrafficGroupContainsDevice);
                    }
                }


                // Loop Through Partitions
                foreach (Partition p in sfog.PartitionList)
                {
                    // Create ClientSslProfile -> Certificate Containment
                    foreach (ProfileClientSSL profile in p.ClientSslProfileList)
                    {
                        if (profile.CertificateList.Count > 0)
                        {
                            // Get Client SSL Object
                            EnterpriseManagementObject profile_Object = SCOM.m_managementGroup.EntityObjects.GetObject<EnterpriseManagementObject>(profile.SCOM_Object.Id, ObjectQueryOptions.Default);

                            foreach (Certificate cert in profile.CertificateList)
                            {
                                // Get Certificate Object
                                EnterpriseManagementObject cert_Object = SCOM.m_managementGroup.EntityObjects.GetObject<EnterpriseManagementObject>(cert.SCOM_Object.Id, ObjectQueryOptions.Default);

                                // Create Relationship
                                ManagementPackRelationship mpr_Containment = SCOM.GetManagementPackRelationship("AP.F5.LTM.ProfileClientSSL.ContainsCertificates");
                                CreatableEnterpriseManagementRelationshipObject obj_Containment = new CreatableEnterpriseManagementRelationshipObject(SCOM.m_managementGroup, mpr_Containment);
                                obj_Containment.SetSource(profile_Object);
                                obj_Containment.SetTarget(cert_Object);
                                relationshipData.Add(obj_Containment);
                            }
                        }
                    }

                    // Create ServerSslProfile -> Certificate Containment
                    foreach (ProfileServerSSL profile in p.ServerSslProfileList)
                    {
                        if (profile.CertificateList.Count > 0)
                        {
                            // Get Client SSL Object
                            EnterpriseManagementObject profile_Object = SCOM.m_managementGroup.EntityObjects.GetObject<EnterpriseManagementObject>(profile.SCOM_Object.Id, ObjectQueryOptions.Default);

                            foreach (Certificate cert in profile.CertificateList)
                            {
                                // Get Certificate Object
                                EnterpriseManagementObject cert_Object = SCOM.m_managementGroup.EntityObjects.GetObject<EnterpriseManagementObject>(cert.SCOM_Object.Id, ObjectQueryOptions.Default);

                                // Create Relationship
                                ManagementPackRelationship mpr_Containment = SCOM.GetManagementPackRelationship("AP.F5.LTM.ProfileServerSSL.ContainsCertificates");
                                CreatableEnterpriseManagementRelationshipObject obj_Containment = new CreatableEnterpriseManagementRelationshipObject(SCOM.m_managementGroup, mpr_Containment);
                                obj_Containment.SetSource(profile_Object);
                                obj_Containment.SetTarget(cert_Object);
                                relationshipData.Add(obj_Containment);
                            }
                        }
                    }

                    // Add Virtual Server Containments
                    foreach (VirtualServer vs in p.VirtualServerList)
                    {
                        // Get Traffic Group Object
                        EnterpriseManagementObject vipObject = SCOM.m_managementGroup.EntityObjects.GetObject<EnterpriseManagementObject>(vs.SCOM_Object.Id, ObjectQueryOptions.Default);

                        // Do Client SSL Profiles
                        foreach (ProfileClientSSL profile in vs.ProfileClientSslList)
                        {
                            // Get Profile Object
                            EnterpriseManagementObject profileObject = SCOM.m_managementGroup.EntityObjects.GetObject<EnterpriseManagementObject>(profile.SCOM_Object.Id, ObjectQueryOptions.Default);
                            // Create VirtualServer->ProfileClientSSL Containment Relationship Object
                            ManagementPackRelationship mpr_VirtualServerContainsProfile = SCOM.GetManagementPackRelationship("AP.F5.LTM.VirtualServerContainsProfileClientSSL");
                            CreatableEnterpriseManagementRelationshipObject obj_VirtualServerContainsProfile = new CreatableEnterpriseManagementRelationshipObject(SCOM.m_managementGroup, mpr_VirtualServerContainsProfile);
                            obj_VirtualServerContainsProfile.SetSource(vipObject);
                            obj_VirtualServerContainsProfile.SetTarget(profileObject);
                            relationshipData.Add(obj_VirtualServerContainsProfile);

                        }
                        // Do Server SSL Profiles
                        foreach (ProfileServerSSL profile in vs.ProfileServerSslList)
                        {
                            // Get Profile Object
                            EnterpriseManagementObject profileObject = SCOM.m_managementGroup.EntityObjects.GetObject<EnterpriseManagementObject>(profile.SCOM_Object.Id, ObjectQueryOptions.Default);
                            // Create VirtualServer->ProfileServerSSL Containment Relationship Object
                            ManagementPackRelationship mpr_VirtualServerContainsProfile = SCOM.GetManagementPackRelationship("AP.F5.LTM.VirtualServerContainsProfileServerSSL");
                            CreatableEnterpriseManagementRelationshipObject obj_VirtualServerContainsProfile = new CreatableEnterpriseManagementRelationshipObject(SCOM.m_managementGroup, mpr_VirtualServerContainsProfile);
                            obj_VirtualServerContainsProfile.SetSource(vipObject);
                            obj_VirtualServerContainsProfile.SetTarget(profileObject);
                            relationshipData.Add(obj_VirtualServerContainsProfile);

                        }


                        // Do Pools
                        if (vs.DefaultPool != null)
                        {
                            // Get Pool Object
                            EnterpriseManagementObject poolObject = SCOM.m_managementGroup.EntityObjects.GetObject<EnterpriseManagementObject>(vs.DefaultPool.SCOM_Object.Id, ObjectQueryOptions.Default);
                            // Create VirtualServer->Pool Containment Relationship Object
                            ManagementPackRelationship mpr_VirtualServerContainsPools = SCOM.GetManagementPackRelationship("AP.F5.LTM.VirtualServerContainsPools");
                            CreatableEnterpriseManagementRelationshipObject obj_VirtualServerContainsPools = new CreatableEnterpriseManagementRelationshipObject(SCOM.m_managementGroup, mpr_VirtualServerContainsPools);
                            obj_VirtualServerContainsPools.SetSource(vipObject);
                            obj_VirtualServerContainsPools.SetTarget(poolObject);
                            relationshipData.Add(obj_VirtualServerContainsPools);

                        }

                    }

                }
            }
        }

        /// <summary>
        /// Get Management Server
        /// </summary>
        /// <returns>Name of Management Server to Conenct to, localhost if config.csv cannot be found or no entry</returns>
        private static string GetManagementServer()
        {
            // Set to default of localhost
            string mserver = "localhost";

            // See if File Exists
            if (!File.Exists(m_configFileName))
            {
                log.Info("Could not find Config File, config.csv, will use locahost as Management Server Name.");
                return mserver;
            }

            // Load In CSV File
            CsvReader csv = new CsvReader(new StreamReader(m_configFileName), true);
            if (csv.FieldCount == 0)
            {
                log.Info("Config File, config.csv, seems to have no fields please check, will use locahost as Management Server Name.");
            }
            else
            {
                // Read First Record
                csv.ReadNextRecord();
                // Get Management Server Name
                mserver = csv[0].ToString();
            }

            // Dispose of CSV Handler
            csv.Dispose();

            // Return Management Server Name
            return mserver;
        }
    }
}
