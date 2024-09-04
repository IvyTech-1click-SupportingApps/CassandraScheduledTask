// See https://aka.ms/new-console-template for more information
using CassandraScheduledTask.DAL;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Reflection;
using static CassandraScheduledTask.Program;
using static System.Net.Mime.MediaTypeNames;
using System.Text;
using Newtonsoft.Json;
using Microsoft.CodeAnalysis;
using System.Runtime.ConstrainedExecution;
using CassandraScheduledTask.Services;
namespace CassandraScheduledTask
{
    public static class Program
    {
        enum ExitCode : int
        {
            Success = 0,
            Error = 1
        }
        //public IConfigurationRoot Configuration { get; private set; }

       static Program()
        {
        }
        static void Main(string[] args)
        {
            Console.WriteLine("-= Start running scheduled job(s)... =-");
            try
            {
                string path = Environment.CurrentDirectory;
                path = path.Replace("bin\\Debug\\net8.0", string.Empty);

                IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().SetBasePath(path);
                configurationBuilder.AddJsonFile(@"appsettings.json");
                IConfiguration configuration = configurationBuilder.Build();

                OrderDetailsRepository or = new OrderDetailsRepository(configuration);
                VerifoneCommonAPI vc = new VerifoneCommonAPI(configuration);
                SendEmail se = new SendEmail();

                var Get_AllCombination = or.Get_AllCombination(1224);
                var Get_VeriFone_OpenedInboundOrders = or.Get_VeriFone_OpenedInboundOrders(1224);
                var DistinctOpenedInboundOrders =
                                    Get_VeriFone_OpenedInboundOrders.Select(i => new
                                    {
                                        APP_ID = string.IsNullOrEmpty(i.APP_ID) ? "NA" : i.APP_ID,
                                        SHIP_TO_SITE_ID = i.SHIP_TO_SITE_ID,
                                        PART_NO = i.PART_NO,
                                        MODEL_NO = i.MODEL_NO,
                                        ADDRESS = i.ADDRESS,
                                        CUSTOMER = i.CUSTOMER,
                                        SHIP_TO_COUNTRY = i.SHIP_TO_COUNTRY,
                                        REPAIR_TYPE = i.REPAIR_TYPE,
                                        REQUEST_TYPE = i.REQUEST_TYPE
                                    }).Distinct().ToList();



                var recordsConfigByTypeID = or.GET_CONFIG_BY_TYPE_ID(9, "ALL");

                //Check Alerts
                foreach (var order in DistinctOpenedInboundOrders)
                { 
                    
                    foreach (var record in recordsConfigByTypeID)
                    {
                        productInfo[] productInfos1 = new productInfo[]
                        {
                            new productInfo
                            {
                                RequestType = order.REQUEST_TYPE ?? "N/A",
                                APP_ID = order.APP_ID ?? "N/A",
                                SN = "dummy",
                                PN = order.PART_NO ?? "N/A",
                                Customer = order.CUSTOMER ?? "N/A",
                                RepairType = order.REPAIR_TYPE ?? "N/A",
                                ShipToCountry = order.SHIP_TO_COUNTRY ?? "N/A",
                                ShipToSiteID = order.SHIP_TO_SITE_ID ?? "N/A",
                                Address = order.ADDRESS ?? "N/A",
                                Model = order.MODEL_NO ?? "N/A",

                            }
                        };

                        filterByCriteria filterByCriteria = new filterByCriteria();
                        filterByCriteria.productInfo = productInfos1;

                        string jsonCriteria = Newtonsoft.Json.JsonConvert.SerializeObject(filterByCriteria);
                        var httpContentCriteria = new StringContent(jsonCriteria, Encoding.UTF8, "application/json");

                        string Criteria = vc.PostAsync("verifone-common/criteria/filterByCriteria", httpContentCriteria);
                        RESPONSE model = JsonConvert.DeserializeObject<RESPONSE>(Criteria);
                        if (model.status == "PASS")
                        {
                            //send mail alerts
                            string strHost = "smtp.corp.ivytech.net";//ConfigurationManager.AppSettings["MailHost"];
                            string strFrom = "donotreply@ivytech.com"; //ConfigurationManager.AppSettings["MailFrom"];
                            string strToAll = "shobhna.parasher2@ivytech.com";  //ConfigurationManager.AppSettings["MailToAll"];
                            string strCcAll = ""; // ConfigurationManager.AppSettings["MailCcAll"];
                            string strBccAll = "";  //ConfigurationManager.AppSettings["MailBccAll"];
                            string strSubject = string.Format("{0} {1} {2}", DateTime.Now, " - ", "Cassandra Notification"); //ConfigurationManager.AppSettings["MailSubject"]);
                            string strMessage = string.Format("{0} {1}", "Cassandra Notification - IncludedValue Field", record.INCLUDEDVALUES);
                            string mailResponse = se.SendEmailAlert(strHost, strFrom, strToAll, strCcAll, strBccAll, strSubject, strMessage, "");
                            /*
                            if (ex.Message == "Unable to connect to the remote server")
                                Program.Terminate(13);
                            else
                                Program.Terminate(1099);*/

                            se.SendEmailAlert(strHost, strFrom, strToAll, strCcAll, strBccAll, strSubject, strMessage, "");
                        }
                    }
                
                    var SelectedOpenedInboundOrders = DistinctOpenedInboundOrders
                                                        .Where(i => i.REPAIR_TYPE.Contains("SPEC") ||
                                                                    i.REPAIR_TYPE.Contains("LOAD") ||
                                                                    i.REPAIR_TYPE.Contains("SOFT"))
                                                        .ToList();


                    foreach (var record1 in SelectedOpenedInboundOrders)
                    {
                        foreach (var record2 in Get_AllCombination)
                        {
                            if (!(record1.APP_ID == (record2.APP_ID)) ||
                                !(record1.SHIP_TO_SITE_ID == (record2.SHIP_TO_SITE_ID)) ||
                                !(record1.PART_NO == (record2.PART_NO)) ||
                                !(record1.MODEL_NO == (record2.MODEL_NO)) ||
                                !(record1.ADDRESS == (record2.ADDRESS)) ||
                                !(record1.CUSTOMER == (record2.CUSTOMER)) ||
                                !(record1.SHIP_TO_COUNTRY == (record2.SHIP_TO_COUNTRY)) ||
                                !(record1.REPAIR_TYPE == (record2.REPAIR_TYPE)) ||
                                !(record1.REQUEST_TYPE == (record2.REQUEST_TYPE)))
                            {
                                //Call API

                                productInfo[] productInfos = new productInfo[]
                                {
                            new productInfo
                            {
                                RequestType = record1.REQUEST_TYPE ?? "N/A",
                                APP_ID = record1.APP_ID ?? "N/A",
                                SN = "dummy",
                                PN = record1.PART_NO ?? "N/A",
                                Customer = record1.CUSTOMER ?? "N/A",
                                RepairType = record1.REPAIR_TYPE ?? "N/A",
                                ShipToCountry = record1.SHIP_TO_COUNTRY ?? "N/A",
                                ShipToSiteID = record1.SHIP_TO_SITE_ID ?? "N/A",
                                Address = record1.ADDRESS ?? "N/A",
                                Model = record1.MODEL_NO ?? "N/A",

                            }
                                };

                                getKeyMethodType getKeyType = new getKeyMethodType();
                                getKeyType.productInfo = productInfos;
                                getKeyType.returnType = "key";

                                getKeyMethodType getMethodType = new getKeyMethodType();
                                getMethodType.productInfo = productInfos;
                                getMethodType.returnType = "method";

                                string jsonKeyType = Newtonsoft.Json.JsonConvert.SerializeObject(getKeyType);
                                string jsonMethodType = Newtonsoft.Json.JsonConvert.SerializeObject(getMethodType);

                                var httpContentKeyType = new StringContent(jsonKeyType, Encoding.UTF8, "application/json");
                                var httpContentMethodType = new StringContent(jsonMethodType, Encoding.UTF8, "application/json");


                                string suggestedKey = vc.PostAsync("/verifone-common/criteria/getKeyMethodType", httpContentKeyType);
                                string suggestedMethod = vc.PostAsync("/verifone-common/criteria/getKeyMethodType", httpContentMethodType);

                                RESPONSE model1 = JsonConvert.DeserializeObject<RESPONSE>(suggestedKey);
                                RESPONSE model2 = JsonConvert.DeserializeObject<RESPONSE>(suggestedMethod);

                                var rowsAffected = or.Insert_SZO_VER_PRODUCT_COMBO(record2, model1.data, model1.data);
                                Console.WriteLine($"{rowsAffected} row(s) inserted.");

                                //send mail alerts
                                string strHost = "smtp.corp.ivytech.net";//ConfigurationManager.AppSettings["MailHost"];
                                string strFrom = "donotreply@ivytech.com"; //ConfigurationManager.AppSettings["MailFrom"];
                                string strToAll = "shobhna.parasher2@ivytech.com";  //ConfigurationManager.AppSettings["MailToAll"];
                                string strCcAll = ""; // ConfigurationManager.AppSettings["MailCcAll"];
                                string strBccAll = "";  //ConfigurationManager.AppSettings["MailBccAll"];
                                string strSubject = string.Format("{0} {1} {2}", DateTime.Now, " - ", "Cassandra Notification"); //ConfigurationManager.AppSettings["MailSubject"]);
                                string strMessage = string.Format("{0} {1}", "Cassandra Notification - No of rows inserted :", rowsAffected);
                                string mailResponse = se.SendEmailAlert(strHost, strFrom, strToAll, strCcAll, strBccAll, strSubject, strMessage, "");
                                /*
                                if (ex.Message == "Unable to connect to the remote server")
                                    Program.Terminate(13);
                                else
                                    Program.Terminate(1099);*/

                                se.SendEmailAlert(strHost, strFrom, strToAll, strCcAll, strBccAll, strSubject, strMessage, "");
                            }
                            }
                        }
                }
                
            }

            catch (Exception ex)
            {
                Environment.ExitCode = (int)ExitCode.Error;
                Console.WriteLine(ex.ToString());
            }
            Console.WriteLine("-= Finished =-");
        }
        public record RESPONSE(String status, String message, String exceptionMessage, String data);
        struct getKeyMethodType
        {
            public productInfo[] productInfo;
            public string returnType;
        }
        struct filterByCriteria
        {
            public productInfo[] productInfo;
            public string criteria;
        }
        struct productInfo
        {
            public string RequestType;
            public string APP_ID;
            public string SN;
            public string PN;
            public string Customer;
            public string RepairType;
            public string ShipToCountry;
            public string ShipToSiteID;
            public string Address;
            public string Model;
        }
    }
}