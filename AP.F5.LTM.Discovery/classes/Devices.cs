using AP.F5.LTM.Discovery.Classes;
using Lextm.SharpSnmpLib;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;
using System;
using System.Collections.Generic;

namespace AP.F5.LTM.Discovery.classes
{
    class SCOM_Devices
    {
        // Property CONSTANTS
        public const int DEV_PROP_DEVICENAME = 0;
        public const int DEV_PROP_TYPE = 1;
        public const int DEV_PROP_IPADDRESS = 2;
        public const int DEV_PROP_PORT = 3;
        public const int DEV_PROP_COMMUNITY = 4;
        public const int DEV_PROP_SYSTEMOID = 5;
        public const int DEV_PROP_MODEL = 6;
        public const int DEV_PROP_VENDOR = 7;
        public const int DEV_PROP_VERSION = 8;
        public const int DEV_PROP_BUILD = 9;
        public const int DEV_PROP_HOTFIX = 10;
        public const int DEV_PROP_LOCATION = 11;
        public const int DEV_PROP_CONTACT = 12;
        public const int DEV_PROP_SERIAL = 13;
        public const int DEV_PROP_STANDALONE = 14;

        // List of Devices
        public List<EnterpriseManagementObject> Items = new List<EnterpriseManagementObject>();

        /// <summary>
        /// Public Constructor
        /// </summary>
        public SCOM_Devices()
        {

            // Get All Devices
            ManagementPackClass mpc_Devices = SCOM.GetManagementPackClass("AP.F5.Device");
            IObjectReader<EnterpriseManagementObject> DeviceReader;
            //List<Guid> DevicesGuidList;
            DeviceReader = SCOM.m_managementGroup.EntityObjects.GetObjectReader<EnterpriseManagementObject>(mpc_Devices, ObjectQueryOptions.Default);

            foreach (EnterpriseManagementObject dev in DeviceReader)
            {
                Items.Add(dev);
            }
        }
    }

    /// <summary>
    /// Simple Seed Device Used for Initial DeviceGroup Detection
    /// </summary>
    public class f5Device
    {
        // Properties (From CSV File)
        public string Address { get; set; }         // IP Address of Device
        public string Community { get; set; }       // SNMP community string to query with
        public int Port { get; set; }               // SNMP Port to query with
        public string F5usr { get; set; }           // f5 UserName for Device
        public string F5pwd { get; set; }           // F5 Password for Device

        // Properties (From SNMP)
        public string Type { get; set; }            // Type of Device (Physical or Virtual)
        public string SystemNodeName { get; set; }  // Name of Device
        public string SerialNumber { get; set; }    // Serial Number of Device
        public string Active { get; set; }
        public List<string> ActiveTrafficGroups = new List<string>();
        public bool Standalone;                     // Is The Device Standalone

        // SCOM Device Object
        public EnterpriseManagementObject SCOM_DeviceObject;

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="address"></param>
        /// <param name="community"></param>
        /// <param name="port"></param>
        /// <param name="f5usr"></param>
        /// <param name="f5pwd"></param>
        public f5Device(string address, string community, int port, string f5usr, string f5pwd)
        {
            // Properties from Arguments
            Address = address;
            Community = community;
            Port = port;
            F5usr = f5usr;
            F5pwd = f5pwd;


            // Get SysSystem Info
            SystemNodeName = SNMP.GetSNMP(SNMP.sysSystemName, address, port, community)[0].Data.ToString();

            // Get SerialNumber
            SerialNumber = SNMP.GetSNMP(SNMP.sysGeneralChassisSerialNum, address, port, community)[0].Data.ToString();

            // Get Active
            Active = SNMP.GetSNMP(SNMP.sysCmFailoverStatusStatus, address, port, community)[0].Data.ToString();

            // Get Active Traffic Groups
            List<Variable> results = SNMP.WalkSNMP(SNMP.sysCmFailoverStatusDetailsDetails, address, port, community);
            if (results.Count > 0)
            {
                foreach (Variable result in results)
                {
                    ActiveTrafficGroups.Add(result.Data.ToString().Substring(result.Data.ToString().IndexOf("/")));
                }
            }

            // Get Standalone
            int iSynStatus = Convert.ToInt32(SNMP.GetSNMP(SNMP.sysCmSyncStatusId, address, port, community)[0].Data.ToString());
            if (iSynStatus == 6)
            {
                Standalone = true;
            }
            else
            {
                Standalone = false;
            }

        }


    }


}
