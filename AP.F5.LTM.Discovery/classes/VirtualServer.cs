using AP.F5.LTM.Discovery.Classes;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;
using System.Collections.Generic;

namespace AP.F5.LTM.Discovery.classes
{
    public class VirtualServer
    {

        // VirtualServer SCOM Object
        public CreatableEnterpriseManagementObject SCOM_Object;

        // List of Client SSL Profiles (used for creating Containment Ralationships later)
        public List<ProfileClientSSL> ProfileClientSslList = new List<ProfileClientSSL>();

        // List of Server SSL Profiles (used for creating Containment Ralationships later)
        public List<ProfileServerSSL> ProfileServerSslList = new List<ProfileServerSSL>();

        public string DefaultPoolName;
        public Pool DefaultPool;

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="Address"></param>
        /// <param name="Port"></param>
        /// <param name="Description"></param>
        /// <param name="Type"></param>
        /// <param name="DefaultPool"></param>
        /// <param name="DeviceGroup"></param>
        /// <param name="ConnectionLimit"></param>
        /// <param name="ClientSslProfiles"></param>
        /// <param name="ServerSslProfiles"></param>
        public VirtualServer(SyncFailoverGroup dg, string PartitionName, string FullName, string Address, string Port, string Description, string Type, string DefaultPool, 
                             string ConnectionLimit, string ClientSslProfiles, string ServerSslProfiles)
        {
            // Set Default Pool
            DefaultPoolName = DefaultPool;
            // Set ShortName
            string ShortName = FullName.Substring(FullName.LastIndexOf("/") + 1);
            // Set Partition
            string Partition = FullName.Substring(1, FullName.LastIndexOf("/") - 1);

            // Create VirtualServer Management Pack Class
            ManagementPackClass mpc_VirtualServer = SCOM.GetManagementPackClass("AP.F5.LTM.VirtualServer");
            // Create New NetworkInterface Object
            SCOM_Object = new CreatableEnterpriseManagementObject(SCOM.m_managementGroup, mpc_VirtualServer);

            // Create Root Entity Class & Key Property
            ManagementPackClass mpc_Entity = SCOM.GetManagementPackClass("System.Entity");
            ManagementPackProperty mpp_EntityDisplayName = mpc_Entity.PropertyCollection["DisplayName"];
            SCOM_Object[mpp_EntityDisplayName].Value = ShortName;

            // Create SyncFailoverGroup Management Pack Class & Key Property
            ManagementPackClass mpc_SyncFailoverGroup = SCOM.GetManagementPackClass("AP.F5.LTM.SyncFailoverGroup");
            ManagementPackProperty mpp_SyncFailoverGroupKey = mpc_SyncFailoverGroup.PropertyCollection["Key"];
            SCOM_Object[mpp_SyncFailoverGroupKey].Value = dg.Key;

            // Create Partition Management Pack Class & Key Property
            ManagementPackClass mpc_Partition = SCOM.GetManagementPackClass("AP.F5.LTM.Partition");
            ManagementPackProperty mpp_PartitionName = mpc_Partition.PropertyCollection["Name"];
            SCOM_Object[mpp_PartitionName].Value = PartitionName;


            // Create our Virtual Server Properties
            ManagementPackProperty mpp_Name = mpc_VirtualServer.PropertyCollection["Name"];
            SCOM_Object[mpp_Name].Value = ShortName;
            ManagementPackProperty mpp_FullName = mpc_VirtualServer.PropertyCollection["FullName"];
            SCOM_Object[mpp_FullName].Value = FullName;
            ManagementPackProperty mpp_Description = mpc_VirtualServer.PropertyCollection["Description"];
            SCOM_Object[mpp_Description].Value = Description;
            ManagementPackProperty mpp_Address = mpc_VirtualServer.PropertyCollection["Address"];
            SCOM_Object[mpp_Address].Value = Address;
            ManagementPackProperty mpp_Port = mpc_VirtualServer.PropertyCollection["Port"];
            SCOM_Object[mpp_Port].Value = Port;
            ManagementPackProperty mpp_Type = mpc_VirtualServer.PropertyCollection["Type"];
            SCOM_Object[mpp_Type].Value = Type;
            ManagementPackProperty mpp_Partition = mpc_VirtualServer.PropertyCollection["Partition"];
            SCOM_Object[mpp_Partition].Value = Partition;
            ManagementPackProperty mpp_DefaultPool = mpc_VirtualServer.PropertyCollection["DefaultPool"];
            SCOM_Object[mpp_DefaultPool].Value = DefaultPool;
            ManagementPackProperty mpp_DeviceGroup = mpc_VirtualServer.PropertyCollection["DeviceGroup"];
            SCOM_Object[mpp_DeviceGroup].Value = dg.Name;
            ManagementPackProperty mpp_ConnectionLimit = mpc_VirtualServer.PropertyCollection["ConnectionLimit"];
            SCOM_Object[mpp_ConnectionLimit].Value = ConnectionLimit;
            ManagementPackProperty mpp_ProfileClientSSL = mpc_VirtualServer.PropertyCollection["ProfileClientSSL"];
            SCOM_Object[mpp_ProfileClientSSL].Value = ClientSslProfiles;
            ManagementPackProperty mpp_ProfileServerSSL = mpc_VirtualServer.PropertyCollection["ProfileServerSSL"];
            SCOM_Object[mpp_ProfileServerSSL].Value = ServerSslProfiles;


        }

    }
}
