using AP.F5.LTM.Discovery.Classes;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;
using System;

namespace AP.F5.LTM.Discovery.classes
{
    public class TrafficGroup
    {
        public SyncFailoverGroup DeviceGroup;
        public string Name;
        public string FullName;
        public string Key;
        public string Devices;
        
        // TrafficGroup SCOM Object
        public CreatableEnterpriseManagementObject SCOM_Object;

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="dg">Hosting Device Group</param>
        /// <param name="Name">name of Traffic Group</param>
        public void CreateScomObject()
        {

            // Create Traffic Group Management Pack Class
            ManagementPackClass mpc_Trafficgroup = SCOM.GetManagementPackClass("AP.F5.LTM.TrafficGroup");

            // Create Traffic Group Object
            SCOM_Object = new CreatableEnterpriseManagementObject(SCOM.m_managementGroup, mpc_Trafficgroup);

            // Create Root Entity Class & Display Name Prop
            ManagementPackClass mpc_Entity = SCOM.GetManagementPackClass("System.Entity");
            ManagementPackProperty mpp_EntityDisplayName = mpc_Entity.PropertyCollection["DisplayName"];
            SCOM_Object[mpp_EntityDisplayName].Value = Name;

            // Add in Key Property of Hosting DeviceGroup
            ManagementPackClass mpc_DeviceGroup = SCOM.GetManagementPackClass("AP.F5.LTM.SyncFailoverGroup");
            ManagementPackProperty mpp_SyncFailoverGroupKey = mpc_DeviceGroup.PropertyCollection["Key"];
            // Set parent Cluster Property
            SCOM_Object[mpp_SyncFailoverGroupKey].Value = DeviceGroup.Key;


            // Key (Key Property)
            ManagementPackProperty mpp_TrafficGroupKey = mpc_Trafficgroup.PropertyCollection["Key"];
            SCOM_Object[mpp_TrafficGroupKey].Value = Key;
            // Name 
            ManagementPackProperty mpp_TrafficGroupName = mpc_Trafficgroup.PropertyCollection["Name"];
            SCOM_Object[mpp_TrafficGroupName].Value = Name;
            // FullName 
            ManagementPackProperty mpp_TrafficGroupFullName = mpc_Trafficgroup.PropertyCollection["FullName"];
            SCOM_Object[mpp_TrafficGroupFullName].Value = FullName;
            // Devices
            ManagementPackProperty mpp_TrafficGroupDevices = mpc_Trafficgroup.PropertyCollection["Devices"];
            SCOM_Object[mpp_TrafficGroupDevices].Value = Devices; 
            // DeviceGroup
            ManagementPackProperty mpp_TrafficGroupDeviceGroup = mpc_Trafficgroup.PropertyCollection["DeviceGroup"];
            SCOM_Object[mpp_TrafficGroupDeviceGroup].Value = DeviceGroup.Name;
        }
    }
}
