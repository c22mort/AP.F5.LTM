using AP.F5.LTM.Discovery.Classes;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;
using System.Collections.Generic;

namespace AP.F5.LTM.Discovery.classes
{
    public class Node
    {
        // Node SCOM Object
        public CreatableEnterpriseManagementObject SCOM_Object;

        // Pool Member List
        public List<PoolMember> PoolMemberList = new List<PoolMember>();

        public Node(string SyncFailoverGroupKey, string PartitionName, string NodeFullName, string Address, string Description, string MonitorRules)
        {
            // Set ShortName
            string ShortName = NodeFullName.Substring(NodeFullName.LastIndexOf("/") + 1);
            // Set Partition
            string Partition = NodeFullName.Substring(1, NodeFullName.LastIndexOf("/") - 1);

            // Create Node Management Pack Class
            ManagementPackClass mpc_Node = SCOM.GetManagementPackClass("AP.F5.LTM.Node");
            // Create New NetworkInterface Object
            SCOM_Object = new CreatableEnterpriseManagementObject(SCOM.m_managementGroup, mpc_Node);

            // Create Root Entity Class & Key Property
            ManagementPackClass mpc_Entity = SCOM.GetManagementPackClass("System.Entity");
            ManagementPackProperty mpp_EntityDisplayName = mpc_Entity.PropertyCollection["DisplayName"];
            SCOM_Object[mpp_EntityDisplayName].Value = ShortName;

            // Create SyncFailoverGroup Management Pack Class & Key Property
            ManagementPackClass mpc_SyncFailoverGroup = SCOM.GetManagementPackClass("AP.F5.LTM.SyncFailoverGroup");
            ManagementPackProperty mpp_SyncFailoverGroupKey = mpc_SyncFailoverGroup.PropertyCollection["Key"];
            SCOM_Object[mpp_SyncFailoverGroupKey].Value = SyncFailoverGroupKey;

            // Create Partition Management Pack Class & Key Property
            ManagementPackClass mpc_Partition = SCOM.GetManagementPackClass("AP.F5.LTM.Partition");
            ManagementPackProperty mpp_PartitionName = mpc_Partition.PropertyCollection["Name"];
            SCOM_Object[mpp_PartitionName].Value = PartitionName;

            // Set Node Properties
            // Create our Virtual Server Properties
            ManagementPackProperty mpp_Name = mpc_Node.PropertyCollection["Name"];
            SCOM_Object[mpp_Name].Value = ShortName;
            ManagementPackProperty mpp_FullName = mpc_Node.PropertyCollection["FullName"];
            SCOM_Object[mpp_FullName].Value = NodeFullName;
            ManagementPackProperty mpp_Description = mpc_Node.PropertyCollection["Description"];
            SCOM_Object[mpp_Description].Value = Description;
            ManagementPackProperty mpp_Address = mpc_Node.PropertyCollection["Address"];
            SCOM_Object[mpp_Address].Value = Address;
            ManagementPackProperty mpp_Partition = mpc_Node.PropertyCollection["Partition"];
            SCOM_Object[mpp_Partition].Value = Partition;
            ManagementPackProperty mpp_MonitorRule = mpc_Node.PropertyCollection["MonitorRules"];
            SCOM_Object[mpp_MonitorRule].Value = MonitorRules;


        }
    }
}
