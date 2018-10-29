using AP.F5.LTM.Discovery.Classes;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;
using System.Collections.Generic;

namespace AP.F5.LTM.Discovery.classes
{
    public class SyncFailoverGroup
    {

        // Key Property
        public string Key;
        // Name Property
        public string Name;
        // Type Property (Sync-Only or Sync-Failover)
        public string Type;
        // Standalone Property
        public bool Standalone;

        // Montor Server Info
        public string Addresses;
        public string Ports;
        public string Communities;

        // Devices Propertry (Comma seperated list of Devices in Device Group)
        public string Devices;

        // List of Device Objects
        public List<f5Device> DeviceList = new List<f5Device>();

        // String List of Devices
        public string[] deviceStrings;

        // List of TrafficGroup Objects
        public List<TrafficGroup> TrafficGroupList = new List<TrafficGroup>();

        // List of Partition Objects
        public List<Partition> PartitionList = new List<Partition>();

        // DeviceGroup SCOM Object
        public CreatableEnterpriseManagementObject SCOM_Object;

        /// <summary>
        /// Create SCOM Object
        /// </summary>
        public void CreateDeviceGroupScomObject()
        {

            // Create Root Entity Class & Display Name Prop
            ManagementPackClass mpc_Entity = SCOM.GetManagementPackClass("System.Entity");
            ManagementPackProperty mpp_EntityDisplayName = mpc_Entity.PropertyCollection["DisplayName"];

            // Create Correct Group Management Pack Class
            ManagementPackClass mpc_DeviceGroup;
            if (Type == "Sync-Failover")
            {
                mpc_DeviceGroup = SCOM.GetManagementPackClass("AP.F5.LTM.SyncFailoverGroup");
            }
            else
            {
                mpc_DeviceGroup = SCOM.GetManagementPackClass("AP.F5.LTM.SyncOnlyGroup");

            }

            // Create Sync-Failover Group Object
            SCOM_Object = new CreatableEnterpriseManagementObject(SCOM.m_managementGroup, mpc_DeviceGroup);
            // Display Name of Parent (KEY Property)
            SCOM_Object[mpp_EntityDisplayName].Value = Name;

            // Set Properties of Device Group
            // Name (Key Property)
            ManagementPackProperty mpp_Name = mpc_DeviceGroup.PropertyCollection["Name"];
            SCOM_Object[mpp_Name].Value = Name;
            // Key
            ManagementPackProperty mpp_Key = mpc_DeviceGroup.PropertyCollection["Key"];
            SCOM_Object[mpp_Key].Value = Key;
            // Standalone
            ManagementPackProperty mpp_Standalone = mpc_DeviceGroup.PropertyCollection["Standalone"];
            SCOM_Object[mpp_Standalone].Value = Standalone;
            // Devices 
            ManagementPackProperty mpp_Devices = mpc_DeviceGroup.PropertyCollection["Devices"];
            SCOM_Object[mpp_Devices].Value = Devices;
            // Monitor Address
            ManagementPackProperty mpp_MonitorAddress = mpc_DeviceGroup.PropertyCollection["Addresses"];
            SCOM_Object[mpp_MonitorAddress].Value = Addresses;
            // Monitor Port
            ManagementPackProperty mpp_MonitorPort = mpc_DeviceGroup.PropertyCollection["Ports"];
            SCOM_Object[mpp_MonitorPort].Value = Ports;
            // Monitor Community
            ManagementPackProperty mpp_MonitorCommunity = mpc_DeviceGroup.PropertyCollection["Communities"];
            SCOM_Object[mpp_MonitorCommunity].Value = Communities;

        }
    }
}
