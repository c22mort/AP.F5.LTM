using AP.F5.LTM.Discovery.Classes;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;
using System.Collections.Generic;

namespace AP.F5.LTM.Discovery.classes
{
    public class ProfileServerSSL
    {
        // Profile SCOM Object
        public CreatableEnterpriseManagementObject SCOM_Object;

        // List of Certificates
        public List<Certificate> CertificateList = new List<Certificate>();

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="dg"></param>
        /// <param name="FullName"></param>
        /// <param name="Description"></param>
        /// <param name="CertFileName"></param>
        public ProfileServerSSL(SyncFailoverGroup dg, string PartitionName, string FullName, string Description, string CertFileNames)
        {
            // Get ShortName
            string ShortName = FullName.Substring(FullName.LastIndexOf("/") + 1);
            // Get Partition
            string Partition = FullName.Substring(1, FullName.LastIndexOf("/") - 1);

            // Create Profile Management Pack Class
            ManagementPackClass mpc_Profile = SCOM.GetManagementPackClass("AP.F5.LTM.ProfileServerSSL");
            // Create New NetworkInterface Object
            SCOM_Object = new CreatableEnterpriseManagementObject(SCOM.m_managementGroup, mpc_Profile);

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

            // Now We Can Create Profile Properties
            ManagementPackProperty mpp_ProfileName = mpc_Profile.PropertyCollection["Name"];
            SCOM_Object[mpp_ProfileName].Value = ShortName;
            ManagementPackProperty mpp_ProfileFullName = mpc_Profile.PropertyCollection["FullName"];
            SCOM_Object[mpp_ProfileFullName].Value = FullName;
            ManagementPackProperty mpp_ProfilePartition = mpc_Profile.PropertyCollection["Partition"];
            SCOM_Object[mpp_ProfilePartition].Value = Partition;
            ManagementPackProperty mpp_ProfileDeviceGroup = mpc_Profile.PropertyCollection["DeviceGroup"];
            SCOM_Object[mpp_ProfileDeviceGroup].Value = dg.Name;
            ManagementPackProperty mpp_ProfileDescription = mpc_Profile.PropertyCollection["Description"];
            SCOM_Object[mpp_ProfileDescription].Value = Description;
            ManagementPackProperty mpp_ProfileCertFileName = mpc_Profile.PropertyCollection["CertFileNames"];
            SCOM_Object[mpp_ProfileCertFileName].Value = CertFileNames;

        }
    }
}
