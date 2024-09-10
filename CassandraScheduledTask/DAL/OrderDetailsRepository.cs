using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Dapper;
using Dapper.Oracle;
using System.Net;
using System.Reflection;
using System.Data;
using System.Diagnostics;


namespace CassandraScheduledTask.DAL
{
    public interface IOrderDetailsRepository
    {
        IEnumerable<SZO_VER_PRODUCT_COMBO> Get_AllCombination(int locationId);
        IEnumerable<OPENED_INBOUND_ORDERS> Get_VeriFone_OpenedInboundOrders(int locationId);
        int Insert_SZO_VER_PRODUCT_COMBO(dynamic record, string model1, string model2);
        IEnumerable<VF_CONFIGURATION> GET_CONFIG_BY_TYPE_ID(int p_typeid, string p_workcenter);

    }

    public class OrderDetailsRepository : RepositoryBase, IOrderDetailsRepository
    {
        public OrderDetailsRepository(IConfiguration configuration) : base(configuration)
        {
        }

        public IEnumerable<SZO_VER_PRODUCT_COMBO> Get_AllCombination(int locationId)
        {
            using var connection = GetConnection();
            var query1 =
                    @$"SELECT * FROM CLIENT_EXT.SZO_VER_PRODUCT_COMBO VPC
                    WHERE
                    VPC.LOCATION_ID = 1224";

            var Get_AllCombination = connection.Query<SZO_VER_PRODUCT_COMBO>(query1);
            return Get_AllCombination;
        }

        public IEnumerable<OPENED_INBOUND_ORDERS> Get_VeriFone_OpenedInboundOrders(int locationId)
        {
            using var connection = GetConnection();
            var query2 =
                   $@"SELECT /*+ USE_NL(IO, RO, ROL, EA, AD, P, UFFV, CC, ROFF, ROFF_APPID, ROFF_RT, ROFF_ITN, ROFF_REQ)*/ IO.INBOUND_ORDER_ID,
                    RO.REFERENCE_ORDER_ID, RO.CLIENT_REFERENCE_NO1 AS VF_PR_NUMBER, RO.CLIENT_REFERENCE_NO2 AS VF_SERIAL_NUMBER,
                    AD.ADDRESS_NAME AS CUSTOMER, CC.COUNTRY_NAME AS SHIP_TO_COUNTRY, AD.ADDRESS_NAME AS ADDRESS, ROFF.FLEX_FIELD_VALUE AS
                    SHIP_TO_SITE_ID, ROFF_APPID.FLEX_FIELD_VALUE AS APP_ID, ROFF_RT.FLEX_FIELD_VALUE AS REPAIR_TYPE, ROFF_ITN.FLEX_FIELD_VALUE AS
                    INCOMING_TRACKING_NUMBER, ROFF_REQ.FLEX_FIELD_VALUE AS REQUEST_TYPE, P.PART_NO, UFFV.UNIVERSAL_FF_VALUE AS MODEL_NO,
                    REPORT_NET.LOCAL_TIME(1224, IO.CREATED_TIMESTAMP) AS CREATED_TIMESTAMP
                    FROM SL.INBOUND_ORDER IO
                        INNER JOIN SL.REFERENCE_ORDER RO ON RO.REFERENCE_ORDER_ID = IO.REFERENCE_ORDER_ID
                        INNER JOIN SL.REFERENCE_ORDER_LINE ROL ON ROL.REFERENCE_ORDER_ID = RO.REFERENCE_ORDER_ID
                        INNER JOIN SL.ENTITY_ADDRESS EA ON EA.ENTITY_ID = RO.OUTBOUND_SHIP_TO_PARTY_ID
                        INNER JOIN SL.ADDRESS AD ON AD.ADDRESS_ID = EA.ADDRESS_ID
                        INNER JOIN SL.PART P ON P.PART_ID = ROL.PART_ID
                        INNER JOIN SL.UNIVERSAL_FLEX_FIELD_VALUE UFFV ON UFFV.ENTITY_ID = ROL.PART_ID AND UNIVERSAL_FF_ID = 184 INNER JOIN SL.COUNTRY_CODE
                        CC ON CC.COUNTRY_ID = AD.COUNTRY_ID
                        LEFT OUTER JOIN SL.RO_FLEX_FIELD ROFF ON ROFF.RO_EXT_ID = 22686 /*Ship_to_site_ID*/ AND ROFF.REFERENCE_ORDER_ID =
                        RO.REFERENCE_ORDER_ID
                        LEFT OUTER JOIN SL.RO_FLEX_FIELD ROFF_APPID ON ROFF_APPID.RO_EXT_ID = 22684 /*APP_ID*/ AND ROFF_APPID.REFERENCE_ORDER_ID =
                        RO.REFERENCE_ORDER_ID
                        LEFT OUTER JOIN SL.RO_FLEX_FIELD ROFF_RT ON ROFF_RT.RO_EXT_ID = 32515 /*REPAIR_TYPE*/ AND ROFF_RT.REFERENCE_ORDER_ID =
                        RO.REFERENCE_ORDER_ID
                        LEFT OUTER JOIN SL.RO_FLEX_FIELD ROFF_ITN ON ROFF_ITN.RO_EXT_ID = 60852 /*Incoming Tracking Number*/ AND ROFF_ITN.REFERENCE_ORDER_ID =
                        RO.REFERENCE_ORDER_ID
                        LEFT OUTER JOIN SL.RO_FLEX_FIELD ROFF_REQ ON ROFF_REQ.RO_EXT_ID = 49963 /*Request Type*/ AND ROFF_REQ.REFERENCE_ORDER_ID =
                        RO.REFERENCE_ORDER_ID
                            WHERE IO.ORDER_STATUS_ID = 2 /*Released*/ AND IO.CLIENT_ID = 75867 /*VeriFone*/ AND IO.CONTRACT_ID = 12528 /*Repair Hungary*/ AND
                            IO.LOCATION_ID = 1224 /*Szombathely*/ AND IO.CREATED_TIMESTAMP >= SYSDATE -1 AND RO.BUSINESS_TRX_TYPE_ID = 1 ORDER BY
                            IO.CREATED_TIMESTAMP ASC";

            var Get_VeriFone_OpenedInboundOrders = connection.Query<OPENED_INBOUND_ORDERS>(query2);
            return Get_VeriFone_OpenedInboundOrders;
        }

        public int Insert_SZO_VER_PRODUCT_COMBO(dynamic record, string suggestedKey, string suggestedMethod)
        {
            using var connection = GetConnection();

            var insertQuery = @"INSERT INTO CLIENT_EXT.SZO_VER_PRODUCT_COMBO (LOCATION_ID, PART_NO, MODEL_NO, CUSTOMER, SHIP_TO_COUNTRY, REPAIR_TYPE, APP_ID, SHIP_TO_SITE_ID, ADDRESS, SUGGESTED_KEY, SUGGESTED_METHOD, ACTIVE)  VALUES (1224, @PART_NO, @MODEL_NO, @CUSTOMER, @SHIP_TO_COUNTRY, @REPAIR_TYPE, @APP_ID, @SHIP_TO_SITE_ID, @ADDRESS, @SUGGESTED_KEY, @SUGGESTED_METHOD, 1)";

            insertQuery = insertQuery.Replace("@PART_NO", "'" + record.PART_NO + "'")
                                     .Replace("@MODEL_NO", "'" + record.MODEL_NO + "'")
                                     .Replace("@CUSTOMER", "'" + record.CUSTOMER + "'")
                                     .Replace("@SHIP_TO_COUNTRY", "'" + record.SHIP_TO_COUNTRY + "'")
                                     .Replace("@REPAIR_TYPE", "'" + record.REPAIR_TYPE + "'")
                                     .Replace("@APP_ID", "'" + record.APP_ID + "'")
                                     .Replace("@SHIP_TO_SITE_ID", "'" + record.SHIP_TO_SITE_ID + "'")
                                     .Replace("@ADDRESS", "'" + record.ADDRESS + "'")
                                     .Replace("@SUGGESTED_KEY", "'" + suggestedKey + "'")
                                     .Replace("@SUGGESTED_METHOD", "'" + suggestedMethod + "'");
            var rowsAffected = connection.Execute(insertQuery);
            return rowsAffected;
        }

        public IEnumerable<VF_CONFIGURATION> GET_CONFIG_BY_TYPE_ID(int p_typeid, string p_workcenter)
        {
            using var connection = GetConnection();
            string query3 = @"SELECT
                                VFC.WORKCENTER ,
                                VFC.CRITERIA ,
                                VFC.EXCEPTIONVALUES ,
                                VFC.INCLUDEDVALUES ,
                                VFC.CONFIG_NAME
                                FROM
                                WEBUI.VF_CONFIGURATION VFC
                                    WHERE
                                        VFC.INACTIVE_IND = 0 AND
                                        VFC.CONFIG_TYPE_ID = 9 AND
                                    (VFC.WORKCENTER = 'ALL')";
            var vf_config = connection.Query<VF_CONFIGURATION>(query3);

            /*const string sql = "WEBUI.VERIFONE_CONFIG.GET_CONFIG_BY_TYPE_ID";
            var dynamicParameters = new OracleDynamicParameters();
            dynamicParameters.Add(":P_TYPEID", p_typeid, OracleMappingType.Int16, ParameterDirection.Input);
            dynamicParameters.Add(":P_WORKCENTER", p_workcenter, OracleMappingType.Varchar2, ParameterDirection.Input);
            dynamicParameters.Add(":O_CURSOR", string.Empty, OracleMappingType.RefCursor, ParameterDirection.Output);

                var vf_configuration = connection.Query<VF_CONFIGURATION>(sql,
                dynamicParameters,
                null,
                false, null,
                CommandType.StoredProcedure);*/
            return vf_config;
            
        }
    }
    public record SZO_VER_PRODUCT_COMBO(Decimal VER_PC_ID, Int64 LOCATION_ID, String PART_NO, String MODEL_NO,
    String CUSTOMER, String SHIP_TO_COUNTRY, String REPAIR_TYPE, String APP_ID,
    String SHIP_TO_SITE_ID, String ADDRESS, String CREATED_BY, DateTime CREATED_TS, String UPDATED_BY,
    DateTime UPDATED_TS, Int16 ACTIVE, String REQUEST_TYPE, Int16 CD_MATRIX_APPROVED, Int16 KEY_MANAGER_APPROVED,
    Int16 SW_PACK_APPROVED, Int16 CLOSED, String SUGGESTED_KEY, String NOTES, String SUGGESTED_METHOD);
    public record OPENED_INBOUND_ORDERS(Int64 INBOUND_ORDER_ID, Int64 REFERENCE_ORDER_ID, String VF_PR_NUMBER, String VF_SERIAL_NUMBER,
    String CUSTOMER, String SHIP_TO_COUNTRY, String ADDRESS, String SHIP_TO_SITE_ID, String APP_ID, String REPAIR_TYPE, String INCOMING_TRACKING_NUMBER,
    String REQUEST_TYPE, String PART_NO, String MODEL_NO, DateTime CREATED_TIMESTAMP);

    public record VF_CONFIGURATION(String WORKCENTER, String CRITERIA, String EXCEPTIONVALUES, String INCLUDEDVALUES,
    String CONFIG_NAME);

}
