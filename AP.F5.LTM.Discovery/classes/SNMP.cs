using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace AP.F5.LTM.Discovery.classes
{
    public class SNMP
    {
        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipSystem.sysSystem
        public const string sysSystem = ".1.3.6.1.4.1.3375.2.1.6.0";
        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipSystem.sysSystem.sysSystemNodeName
        public const string sysSystemName = ".1.3.6.1.4.1.3375.2.1.6.2.0";
        //.iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipSystem.sysPlatform.sysGeneral.sysGeneralHwName
        public const string sysGeneralHwName = ".1.3.6.1.4.1.3375.2.1.3.3.1.0";
        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipSystem.sysPlatform.sysPlatformInfo.sysPlatformInfoMarketingName
        public const string sysPlatformInfoMarketingName = ".1.3.6.1.4.1.3375.2.1.3.5.2.0";
        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipSystem.sysProduct
        public const string sysProduct = ".1.3.6.1.4.1.3375.2.1.4.0";
        // .iso.org.dod.internet.mgmt.mib-2.system.sysObjectID
        public const string sysObjectID = ".1.3.6.1.2.1.1.2.0";
        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipSystem.sysPlatform.sysGeneral.sysGeneralChassisSerialNum
        public const string sysGeneralChassisSerialNum = ".1.3.6.1.4.1.3375.2.1.3.3.3.0";

        // Fans
        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipSystem.sysPlatform.sysChassis.sysChassisFan.sysChassisFanNumber
        public const string sysChassisFanNumber = ".1.3.6.1.4.1.3375.2.1.3.2.1.1.0";    // Chassis Fan Count
        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipSystem.sysPlatform.sysChassis.sysChassisFan.sysChassisFanTable.sysChassisFanEntry.sysChassisFanIndex
        public const string sysChassisFanIndex = ".1.3.6.1.4.1.3375.2.1.3.2.1.2.1.1.";  // Fan Index

        // PSU
        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipSystem.sysPlatform.sysChassis.sysChassisPowerSupply.sysChassisPowerSupplyNumber
        public const string sysChassisPowerSupplyNumber = ".1.3.6.1.4.1.3375.2.1.3.2.2.1.0";    // Count of PSU ins PSU Table
        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipSystem.sysPlatform.sysChassis.sysChassisPowerSupply.sysChassisPowerSupplyTable.sysChassisPowerSupplyEntry.sysChassisPowerSupplyIndex
        public const string sysChassisPowerSupplyIndex = ".1.3.6.1.4.1.3375.2.1.3.2.2.2.1.1.";  // PSU Index 

        // Temp Sensors
        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipSystem.sysPlatform.sysChassis.sysChassisTemp.sysChassisTempNumber
        public const string sysChassisTempNumber = ".1.3.6.1.4.1.3375.2.1.3.2.3.1.0";           // Count of Temp Sensors ins Temp Sensor Table
        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipSystem.sysPlatform.sysChassis.sysChassisTemp.sysChassisTempTable.sysChassisTempEntry.sysChassisTempIndex
        public const string sysChassisTempIndex = ".1.3.6.1.4.1.3375.2.1.3.2.3.2.1.1.";         // Temp Sensor Index (Index is appended)

        // CPU
        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipSystem.sysHostInfoStat.sysMultiHostCpu.sysMultiHostCpuNumber
        public const string sysMultiHostCpuNumber = ".1.3.6.1.4.1.3375.2.1.7.5.1.0";
        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipSystem.sysHostInfoStat.sysMultiHostCpu.sysMultiHostCpuTable.sysMultiHostCpuEntry.sysMultiHostCpuIndex
        public const string sysMultiHostCpuIndex = ".1.3.6.1.4.1.3375.2.1.7.5.2.1.2";

        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipSystem.sysCM.sysCmSyncStatus.sysCmSyncStatusId
        public const string sysCmSyncStatusId = ".1.3.6.1.4.1.3375.2.1.14.1.1.0";

        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipSystem.sysCM.sysCmFailoverStatus.sysCmFailoverStatusStatus
        public const string sysCmFailoverStatusStatus = ".1.3.6.1.4.1.3375.2.1.14.3.2.0";

        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipSystem.sysCM.sysCmFailoverStatusDetails.sysCmFailoverStatusDetailsTable.sysCmFailoverStatusDetailsEntry.sysCmFailoverStatusDetailsDetails
        public const string sysCmFailoverStatusDetailsDetails = ".1.3.6.1.4.1.3375.2.1.14.4.2.1.2";

        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipLocalTM.ltmVirtualServers.ltmVirtualServStatus.ltmVsStatusTable.ltmVsStatusEntry.ltmVsStatusName
        public const string ltmVsStatusName = ".1.3.6.1.4.1.3375.2.2.10.13.2.1.1";          // Virtual Server Names

        //.iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipLocalTM.ltmPools.ltmPoolStatus.ltmPoolStatusTable.ltmPoolStatusEntry.ltmPoolStatusName
        public const string ltmPoolStatusName = ".1.3.6.1.4.1.3375.2.2.5.5.2.1.1";   // Pool Names
        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipLocalTM.ltmPools.ltmPool.ltmPoolTable.ltmPoolEntry.ltmPoolMonitorRule
        public const string ltmPoolMonitorRule = ".1.3.6.1.4.1.3375.2.2.5.1.2.1.17";


        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipLocalTM.ltmPools.ltmPoolMemberStatus.ltmPoolMbrStatusTable.ltmPoolMbrStatusEntry.ltmPoolMbrStatusNodeName
        public const string ltmPoolMbrStatusNodeName = ".1.3.6.1.4.1.3375.2.2.5.6.2.1.9"; // Pool Member Names
        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipLocalTM.ltmPools.ltmPoolMember.ltmPoolMemberTable.ltmPoolMemberEntry.ltmPoolMemberMonitorRule
        public const string ltmPoolMemberMonitorRule = ".1.3.6.1.4.1.3375.2.2.5.3.2.1.14";

        // Client SSL Profile
        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipLocalTM.ltmProfiles.ltmClientSsl.ltmClientSslProfile.ltmClientSslTable.ltmClientSslEntry.ltmClientSslName
        public const string ltmClientSslName = ".1.3.6.1.4.1.3375.2.2.6.2.1.2.1.1";          // Client SSL Profile Names

        // Server SSL Profile
        // .iso.org.dod.internet.private.enterprises.f5.bigipTrafficMgmt.bigipLocalTM.ltmProfiles.ltmServerSsl.ltmServerSslProfile.ltmServerSslTable.ltmServerSslEntry.ltmServerSslName
        public const string ltmServerSslName = ".1.3.6.1.4.1.3375.2.2.6.3.1.2.1.1";

        //bigipTrafficMgmt.bigipSystem.sysCM.sysCmTrafficGroupStatus.sysCmTrafficGroupStatusTable.sysCmTrafficGroupStatusEntry.sysCmTrafficGroupStatusTrafficGroup
        public const string sysCmTrafficGroupStatusTrafficGroup = ".1.3.6.1.4.1.3375.2.1.14.5.2.1.1";
            
        #region "SNMP_Functions"
        /// <summary>
        /// Get Single SNMP OID
        /// </summary>
        /// <param name="inputoid"></param>
        /// <returns></returns>
        public static List<Variable> GetSNMP(string inputoid, string address, int port, string community)
        {
            List<Variable> retlist = new List<Variable>();

            try
            {
                var response = Messenger.Get(VersionCode.V2,
                   new IPEndPoint(IPAddress.Parse(address), port),
                   new OctetString(community),
                   new List<Variable> { new Variable(new ObjectIdentifier(inputoid)) },
                   10000);
                retlist = response.ToList();
            }
            catch (Exception)
            {
            }
            return retlist;
        }



        /// <summary>
        /// Get Bulk SNMP
        /// </summary>
        /// <param name="inputoid"></param>
        /// <returns></returns>
        public static List<Variable> BulkSNMP(string inputoid, string address, int port, string community)
        {
            List<Variable> retlist = new List<Variable>();

            try
            {
                GetBulkRequestMessage message = new GetBulkRequestMessage(0,
                                                              VersionCode.V2,
                                                              new OctetString(community),
                                                              0,
                                                              10,
                                                              new List<Variable> { new Variable(new ObjectIdentifier(inputoid)) });
                ISnmpMessage response = message.GetResponse(60000, new IPEndPoint(IPAddress.Parse(address), port));
                if (response.Pdu().ErrorStatus.ToInt32() != 0)
                {
                    throw ErrorException.Create(
                        "error in response",
                        IPAddress.Parse(address),
                        response);

                }
                retlist = response.Pdu().Variables.ToList();

            }
            catch (Exception)
            {
            }
            return retlist;
        }

        /// <summary>
        /// Walk SNMP
        /// </summary>
        /// <param name="inputoid"></param>
        /// <returns></returns>
        public static List<Variable> WalkSNMP(string inputoid, string address, int port, string community)
        {
            List<Variable> retlist = new List<Variable>();

            try
            {
                Messenger.Walk(VersionCode.V2,
                   new IPEndPoint(IPAddress.Parse(address), port),
                   new OctetString(community),
                   new ObjectIdentifier(inputoid),
                   retlist,
                   10000, WalkMode.WithinSubtree);
            }
            catch (Exception)
            {
            }
            return retlist;
        }

        #endregion
    }
}
