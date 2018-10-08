using AP.F5.LTM.Discovery.Classes;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;

namespace AP.F5.LTM.Discovery.classes
{
    public class PoolMember
    {
        // Pool SCOM Object
        public CreatableEnterpriseManagementObject SCOM_Object;

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="dg"></param>
        /// <param name="FullName"></param>
        /// <param name="Description"></param>
        /// <param name="Port"></param>
        /// <param name="MonitorRule"></param>
        public PoolMember(SyncFailoverGroup dg, string PartitionName, string PoolFullName, string FullName, string Description, string Port, string MonitorRule)
        {
            // Get ShortName
            string ShortName = FullName.Substring(FullName.LastIndexOf("/") + 1);
            // Get Partition
            string Partition = FullName.Substring(1, FullName.LastIndexOf("/") - 1);

            // Create Pool Management Pack Class
            ManagementPackClass mpc_PoolMember = SCOM.GetManagementPackClass("AP.F5.LTM.PoolMember");
            // Create New NetworkInterface Object
            SCOM_Object = new CreatableEnterpriseManagementObject(SCOM.m_managementGroup, mpc_PoolMember);

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

            // Create Pool Management Pack Class
            ManagementPackClass mpc_Pool = SCOM.GetManagementPackClass("AP.F5.LTM.Pool");
            ManagementPackProperty mpp_PoolKey = mpc_Pool.PropertyCollection["FullName"];
            SCOM_Object[mpp_PoolKey].Value = PoolFullName;

            // Create PoolMember Management Pack Class & Properties
            ManagementPackProperty mpp_PoolMemberFullName = mpc_PoolMember.PropertyCollection["FullName"];
            SCOM_Object[mpp_PoolMemberFullName].Value = FullName;
            ManagementPackProperty mpp_PoolMemberName = mpc_PoolMember.PropertyCollection["Name"];
            SCOM_Object[mpp_PoolMemberName].Value = ShortName;
            ManagementPackProperty mpp_PoolMemberPartition = mpc_PoolMember.PropertyCollection["Partition"];
            SCOM_Object[mpp_PoolMemberPartition].Value = Partition;
            ManagementPackProperty mpp_PoolMemberPort = mpc_PoolMember.PropertyCollection["Port"];
            SCOM_Object[mpp_PoolMemberPort].Value = Port;
            ManagementPackProperty mpp_PoolMemberDescription = mpc_PoolMember.PropertyCollection["Description"];
            SCOM_Object[mpp_PoolMemberDescription].Value = Description;
            ManagementPackProperty mpp_PoolMemberMonitorRule = mpc_PoolMember.PropertyCollection["MonitorRule"];
            SCOM_Object[mpp_PoolMemberMonitorRule].Value = MonitorRule;
            ManagementPackProperty mpp_PoolMemberPoolName = mpc_PoolMember.PropertyCollection["PoolName"];
            SCOM_Object[mpp_PoolMemberPoolName].Value = PoolFullName;


        }
    }
}
