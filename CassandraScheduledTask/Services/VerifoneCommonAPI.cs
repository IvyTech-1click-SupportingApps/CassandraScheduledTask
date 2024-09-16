using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static CassandraScheduledTask.Program;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Configuration;

namespace CassandraScheduledTask.Services
{
    public class VerifoneCommonAPI
    {
        public readonly IConfiguration _configuration;

        public VerifoneCommonAPI(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string PostAsync(string requestURI, HttpContent httpContent)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_configuration["VerifoneAPILink"] ?? "N/A");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    
                    var httpResponseMessage = client.PostAsync(requestURI, httpContent).Result;                    
                    return httpResponseMessage.Content.ReadAsStringAsync().Result;
                }
            }
            catch (Exception ex)
            {                
                return ex.ToString();
            }
        }
    }
}
