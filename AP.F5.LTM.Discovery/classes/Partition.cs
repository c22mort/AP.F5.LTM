using AP.F5.LTM.Discovery.Classes;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;
using System.Collections.Generic;

namespace AP.F5.LTM.Discovery.classes
{
    public class Partition
    {
        // Partition SCOM Object
        public CreatableEnterpriseManagementObject SCOM_Object;

        // List of VirtualServer Objects
        public List<VirtualServer> VirtualServerList = new List<VirtualServer>();

        // List of ClientSslProfiles Objects
        public List<ProfileClientSSL> ClientSslProfileList = new List<ProfileClientSSL>();

        // List of ServerSslProfiles Objects
        public List<ProfileServerSSL> ServerSslProfileList = new List<ProfileServerSSL>();

        // List of VirtualServer Objects
        public List<Certificate> CertificateList = new List<Certificate>();

        // List of Pool Objects
        public List<Pool> PoolList = new List<Pool>();

        // List of Node Objects
        public List<Node> NodeList = new List<Node>();

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="SyncFailoverGroupKey"></param>
        /// <param name="PartitionName"></param>
        public Partition(string SyncFailoverGroupKey, string PartitionName)
        {
            // Create Partition Management Pack Class
            ManagementPackClass mpc_Partition = SCOM.GetManagementPackClass("AP.F5.LTM.Partition");
            // Create New NetworkInterface Object
            SCOM_Object = new CreatableEnterpriseManagementObject(SCOM.m_managementGroup, mpc_Partition);

            // Create Root Entity Class & Key Property
            ManagementPackClass mpc_Entity = SCOM.GetManagementPackClass("System.Entity");
            ManagementPackProperty mpp_EntityDisplayName = mpc_Entity.PropertyCollection["DisplayName"];
            SCOM_Object[mpp_EntityDisplayName].Value = PartitionName;

            // Create SyncFailoverGroup Management Pack Class & Key Property
            ManagementPackClass mpc_SyncFailoverGroup = SCOM.GetManagementPackClass("AP.F5.LTM.SyncFailoverGroup");
            ManagementPackProperty mpp_SyncFailoverGroupKey = mpc_SyncFailoverGroup.PropertyCollection["Key"];
            SCOM_Object[mpp_SyncFailoverGroupKey].Value = SyncFailoverGroupKey;

            // Create Properties of Partition
            ManagementPackProperty mpp_PartitionName = mpc_Partition.PropertyCollection["Name"];
            SCOM_Object[mpp_PartitionName].Value = PartitionName;



        }
    }
}
