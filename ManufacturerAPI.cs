using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;

namespace ManufacturerFUnction
{
    public static class ManufacturerAPI
    {
        [FunctionName("ManufacturerAPI")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string userRequest = req.Query["requestBody"];
            string responseMessage = "";
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            userRequest = userRequest ?? data?.requestBody;

            if(!string.IsNullOrEmpty(userRequest))
            {
                var str = Environment.GetEnvironmentVariable("sqljdbc_connection");
                using (SqlConnection conn = new SqlConnection(str))
                {
                    var text = "";
                    conn.Open();
                    switch(userRequest)
                    {
                        case "manufacturers":
                        {
                            text = "SELECT * from weather where origin = 'JFK' FOR JSON PATH; ";
                            break;
                        }
                        case "manufacturers-flights":
                        {
                             text = "SELECT * from weather where origin = 'JFK' FOR JSON PATH; ";
                            break;
                        }
                        case "airbus-models":
                        {
                            text = "SELECT * from weather where origin = 'JFK' FOR JSON PATH; ";
                            break;
                        }
                        default:
                        {
                            text = "error";
                            break;
                        }
                        
                    }
                    if(text != "error" || text != "")
                    {
                        using (SqlCommand cmd = new SqlCommand(text, conn))
                        {
                            SqlDataReader reader = await cmd.ExecuteReaderAsync();
                            // Execute the command and log the # rows affected.
                            while (reader.Read())
                            {
                                IDataRecord result = (IDataRecord)reader;
                                responseMessage += String.Format("{0},", result[0]);
                            }
                            // Call Close when done reading.
                            reader.Close();

                        }
                    }
                    else 
                    {
                        return new NotFoundObjectResult(userRequest);
                    }
                    
                }
                return new OkObjectResult(responseMessage);
            }
            else
            {
                return new NotFoundObjectResult(userRequest);
            }
            // Get the connection string from app settings and use it to create a connection.
           
        }
    }
}
