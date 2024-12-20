﻿// See https://aka.ms/new-console-template for more information
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
using Microsoft.Extensions.Logging;
using System.Configuration;
using Serilog;
using Microsoft.Extensions.Hosting;
using Serilog.Core;
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
            try
            {
                // Configure Serilog for file logging
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
                    .CreateLogger();
           
                Log.Information("-= Start running scheduled job(s)... =-");

                string path = Directory.GetCurrentDirectory();//Environment.CurrentDirectory;
                path = path.Replace("bin\\Debug\\net8.0", string.Empty);

                IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().SetBasePath(path);
                configurationBuilder.AddJsonFile(@"appsettings.json");
                IConfiguration configuration = configurationBuilder.Build();

                OrderDetailsRepository or = new OrderDetailsRepository(configuration);
                VerifoneCommonAPI vc = new VerifoneCommonAPI(configuration);
                SendEmail se = new SendEmail();

                Log.Information("Getting data from Get_AllCombination...");
                var Get_AllCombination = or.Get_AllCombination(1224);
                Log.Information("Total no. of records in Get_AllCombination - " + Get_AllCombination.Count());

                Log.Information("Getting data from Get_VeriFone_OpenedInboundOrders...");
                var Get_VeriFone_OpenedInboundOrders = or.Get_VeriFone_OpenedInboundOrders(1224);
                Log.Information("Total no. of records in Get_VeriFone_OpenedInboundOrders - " + Get_VeriFone_OpenedInboundOrders.Count());
                
                //Getting distinct records
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
                                        REPAIR_TYPE = string.IsNullOrEmpty(i.REPAIR_TYPE) ? "NA" : i.REPAIR_TYPE,
                                        REQUEST_TYPE = i.REQUEST_TYPE
                                    }).Distinct().ToList();


                //Check Alerts

                string strHost = configuration.GetSection("MailSettings")["MailHost"] ?? "N/A";
                string strFrom = configuration.GetSection("MailSettings")["MailFrom"] ?? "N/A";
                string strToAll = configuration.GetSection("MailSettings")["MailToAll"] ?? "N/A";
                string strSubject = string.Format("{0} {1} {2}", "Cassandra Notification", " - ", DateTime.Now);
                string strMessage = string.Empty;
                string mailResponse = string.Empty;
                string FilterByCriteriaURI = configuration.GetSection("VerifoneRequestURI")["FilterByCriteria"] ?? "N/A";
                string GetKeyMethodTypeURI = configuration.GetSection("VerifoneRequestURI")["GetKeyMethodType"] ?? "N/A";

                var recordsConfigByTypeID = or.GET_CONFIG_BY_TYPE_ID(9, "ALL");
                foreach (var order in DistinctOpenedInboundOrders)
                {                    
                    if (recordsConfigByTypeID != null)
                    {
                        foreach (var record in recordsConfigByTypeID)
                        {
                            //execute filterByCriteria
                            Log.Information("Calling filterByCriteria");
                            List<criteriaTable> criteriaTables = JsonConvert.DeserializeObject<List<criteriaTable>>(record.CRITERIA);
                            productInfo[] productInfo = new productInfo[]
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
                            filterByCriteria.productInfo = productInfo;
                            filterByCriteria.criteriaTable = criteriaTables.ToArray();

                            var httpContentCriteria = new StringContent(JsonConvert.SerializeObject(filterByCriteria), Encoding.UTF8, "application/json");
                            string Criteria = vc.PostAsync(FilterByCriteriaURI, httpContentCriteria);
                            RESPONSE responseCriteria = JsonConvert.DeserializeObject<RESPONSE>(Criteria);

                            if (responseCriteria.status.ToUpper() == "TRUE")
                            {
                                if (responseCriteria.message == "Trigger Executed Successfully")
                                {
                                    //send mail alerts
                                    Log.Information("Sending mail alert...");
                                    strMessage = string.Format(record.INCLUDEDVALUES);
                                    mailResponse = se.SendEmailAlert(strHost, strFrom, strToAll, string.Empty, string.Empty, strSubject, strMessage, "");
                                    Log.Information("Mail successfully sent");
                                }
                            }
                        }
                    }
                }

                //Getting selected records based on Repair Type
                
                var rejectList = DistinctOpenedInboundOrders.Where(i => i.REPAIR_TYPE.Contains("SPEC") ||
                                                                        i.REPAIR_TYPE.Contains("LOAD") ||
                                                                        i.REPAIR_TYPE.Contains("SOFT")).ToList();

                var SelectedOpenedInboundOrders = DistinctOpenedInboundOrders.Except(rejectList).ToList();

                //Compare records
                int Totalrows = 0;
                foreach (var record in SelectedOpenedInboundOrders)
                {
                    var recordAllComb = Get_AllCombination.Select(i => i).Where(i => (i.APP_ID == record.APP_ID &&
                                                                               i.SHIP_TO_SITE_ID == record.SHIP_TO_SITE_ID &&
                                                                               i.PART_NO == record.PART_NO &&
                                                                               i.MODEL_NO == record.MODEL_NO &&
                                                                               i.ADDRESS == record.ADDRESS &&
                                                                               i.CUSTOMER == record.CUSTOMER &&
                                                                               i.SHIP_TO_COUNTRY == record.SHIP_TO_COUNTRY &&
                                                                               i.REPAIR_TYPE == record.REPAIR_TYPE &&
                                                                               i.REQUEST_TYPE == record.REQUEST_TYPE)).ToList();

                    //foreach (var record2 in Get_AllCombination)
                    //{
                        if (recordAllComb.Count == 0)
                        {
                            //execute getKeyMethodType
                            Log.Information("Calling getKeyMethodType to obtain suggestedKey and suggestedMethod");

                            productInfo[] productInfos = new productInfo[]
                            {
                                new productInfo
                                {
                                    RequestType = record.REQUEST_TYPE ?? "N/A",
                                    APP_ID = record.APP_ID ?? "N/A",
                                    SN = "dummy",
                                    PN = record.PART_NO ?? "N/A",
                                    Customer = record.CUSTOMER ?? "N/A",
                                    RepairType = record.REPAIR_TYPE ?? "N/A",
                                    ShipToCountry = record.SHIP_TO_COUNTRY ?? "N/A",
                                    ShipToSiteID = record.SHIP_TO_SITE_ID ?? "N/A",
                                    Address = record.ADDRESS ?? "N/A",
                                    Model = record.MODEL_NO ?? "N/A"
                                }
                            };
                            
                            getKeyMethodType getKeyType = new getKeyMethodType();
                            getKeyType.productInfo = productInfos;
                            getKeyType.returnType = "key";
                            
                            getKeyMethodType getMethodType = new getKeyMethodType();
                            getMethodType.productInfo = productInfos;
                            getMethodType.returnType = "method";
                                                        
                            var httpContentKeyType = new StringContent(JsonConvert.SerializeObject(getKeyType), Encoding.UTF8, "application/json");
                            var httpContentMethodType = new StringContent(JsonConvert.SerializeObject(getMethodType), Encoding.UTF8, "application/json");
                            
                            string suggestedKey = vc.PostAsync(GetKeyMethodTypeURI, httpContentKeyType);
                            string suggestedMethod = vc.PostAsync(GetKeyMethodTypeURI, httpContentMethodType);
                            
                            RESPONSE responseKey = JsonConvert.DeserializeObject<RESPONSE>(suggestedKey);
                            RESPONSE responseMethod = JsonConvert.DeserializeObject<RESPONSE>(suggestedMethod);

                            //Insert data in SZO_VER_PRODUCT_COMBO                            
                            Log.Information("Inserting record in SZO_VER_PRODUCT_COMBO...");
                            var rowsAffected = or.Insert_SZO_VER_PRODUCT_COMBO(record, 
                                                                               responseKey.data.Replace("KeyMethodName : ", string.Empty),
                                                                               responseMethod.data.Replace("KeyMethodName : ", string.Empty));
                            Log.Information($"{rowsAffected} row(s) inserted.");

                            Totalrows = Totalrows + rowsAffected; 
                        }
                    //}
                }
                if (Totalrows > 0)
                {
                    //Send mail alerts
                    Log.Information("Sending mail alert...");
                    strMessage = string.Format("{0} {1}", "New combination will arrive : ", Totalrows);
                    mailResponse = se.SendEmailAlert(strHost, strFrom, strToAll, string.Empty, string.Empty, strSubject, strMessage, "");
                    Log.Information("Mail successfully sent");
                }

                Log.Information("-= Finished =-");

            }

            catch (Exception ex)
            {
                Environment.ExitCode = (int)ExitCode.Error;
                Console.WriteLine(ex.ToString());
            }            
        }
        public record RESPONSE(String status, String message, String exceptionMessage, String data, String error);
        struct getKeyMethodType
        {
            public productInfo[] productInfo;
            public string returnType;
        }
        struct filterByCriteria
        {
            public productInfo[] productInfo;
            public criteriaTable[] criteriaTable;
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
        struct criteriaTable
        {
            public string Criteria;
            public string CriteriaOperator;
            public string CriteriaValue;            
        }
    }
}