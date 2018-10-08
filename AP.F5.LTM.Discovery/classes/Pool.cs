using AP.F5.LTM.Discovery.Classes;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;
using System.Collections.Generic;

namespace AP.F5.LTM.Discovery.classes
{
    public class Pool
    {

        // Pool SCOM Object
        public CreatableEnterpriseManagementObject SCOM_Object;

        // List of Pool Members
        public List<PoolMember> PoolMembers = new List<PoolMember>();


        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="Description"></param>
        /// <param name="MonitorRule"></param>
        /// <param name="TotalMembers"></param>
        /// <param name="ActiveMembers"></param>
        public Pool(SyncFailoverGroup dg, string PartitionName, string FullName, string Description, string MonitorRule, long TotalMembers, long ActiveMembers)
        {
            // Set ShortName
            string ShortName = FullName.Substring(FullName.LastIndexOf("/") + 1);
            // Set Partition
            string Partition = FullName.Substring(1, FullName.LastIndexOf("/") - 1);

            // Create Pool Management Pack Class
            ManagementPackClass mpc_Pool = SCOM.GetManagementPackClass("AP.F5.LTM.Pool");
            // Create New NetworkInterface Object
            SCOM_Object = new CreatableEnterpriseManagementObject(SCOM.m_managementGroup, mpc_Pool);

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

            // Create Properties of Pool
            ManagementPackProperty mpp_PoolFullName = mpc_Pool.PropertyCollection["FullName"];
            SCOM_Object[mpp_PoolFullName].Value = FullName;
            ManagementPackProperty mpp_PoolName = mpc_Pool.PropertyCollection["Name"];
            SCOM_Object[mpp_PoolName].Value = ShortName;
            ManagementPackProperty mpp_PoolPartition = mpc_Pool.PropertyCollection["Partition"];
            SCOM_Object[mpp_PoolPartition].Value = Partition;
            ManagementPackProperty mpp_PoolTotalMembers = mpc_Pool.PropertyCollection["TotalMembers"];
            SCOM_Object[mpp_PoolTotalMembers].Value = TotalMembers;
            ManagementPackProperty mpp_PoolActiveMembers = mpc_Pool.PropertyCollection["ActiveMembers"];
            SCOM_Object[mpp_PoolActiveMembers].Value = ActiveMembers;
            ManagementPackProperty mpp_PoolDescription = mpc_Pool.PropertyCollection["Description"];
            SCOM_Object[mpp_PoolDescription].Value = Description;
            ManagementPackProperty mpp_PoolMonitorRule = mpc_Pool.PropertyCollection["MonitorRule"];
            SCOM_Object[mpp_PoolMonitorRule].Value = MonitorRule;
        }
    }
}
