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

namespace CassandraScheduledTask.Services
{
    public class VerifoneCommonAPI
    {
        public readonly IConfiguration Configuration;

        public VerifoneCommonAPI(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public string PostAsync(string requestURI, HttpContent httpContent)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    Uri uri2 = new Uri("https://apiqa.corp.ivytech.net");

                    client.BaseAddress = new Uri(uri2.ToString());
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
