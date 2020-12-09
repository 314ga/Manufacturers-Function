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
            log.LogInformation("C# HTTP API requests for manufacturer");

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
                        case "planes-per-manufacturer":
                        {
                            text = "SELECT * FROM(SELECT manufacturer, count(manufacturer) AS plane_count FROM dbo.planes GROUP by manufacturer) " +
                                "AS result_table WHERE plane_count> 200 FOR JSON PATH";
                            break;
                        }
                        case "flights-per-manufacturer":
                        {
                             text = "DECLARE @Looper INT = 1, @Manufacturers INT, @ManufacturerName NVARCHAR(100), @ManufacturerNames NVARCHAR(300), " +
                                "@ManufacturerFlights NVARCHAR(100) SELECT @Manufacturers = count(*) FROM(SELECT * FROM(SELECT manufacturer, count(manufacturer) AS plane_count " +
                                "FROM dbo.planes GROUP by manufacturer) AS result_table WHERE plane_count > 200) as manufact; " +
                                "WHILE(@Looper <= @Manufacturers) BEGIN WITH cte_customers AS(SELECT ROW_NUMBER() OVER(ORDER BY manufacturer ) row_num, manufacturer " +
                                "FROM(SELECT * FROM(SELECT manufacturer, count(manufacturer) AS plane_count FROM dbo.planes GROUP by manufacturer) AS result_table WHERE plane_count > 200) as manufact) " +
                                "SELECT @ManufacturerName = manufacturer FROM cte_customers WHERE row_num = @Looper;" +
                                "SELECT @ManufacturerFlights = count(*) FROM(SELECT flights.id, planes.manufacturer FROM dbo.flights " +
                                "INNER JOIN planes ON flights.tailnum = planes.tailnum AND planes.manufacturer = @ManufacturerName) as s; " +
                                "SET @Looper = @Looper + 1 IF (@ManufacturerNames IS NULL or @ManufacturerNames = '') " +
                                "SELECT @ManufacturerNames = CONCAT('[{\"manufacturer\":\"',@ManufacturerName,'\",\"flights\":',@ManufacturerFlights, '},') " +  
                                "ELSE SELECT @ManufacturerNames = CONCAT(@ManufacturerNames,'{\"manufacturer\":\"',@ManufacturerName,'\",\"flights\":',@ManufacturerFlights, '},') " +
                                "END SELECT @ManufacturerNames = CONCAT(@ManufacturerNames,']') SELECT @ManufacturerNames AS company; ";
                            break;
                        }
                        case "airbus-per-manufaturer":
                        {
                            text = "SELECT manufacturer AS model, count(*) as airbus_model FROM dbo.planes " +
                                 "WHERE  manufacturer = 'AIRBUS' GROUP BY manufacturer UNION ALL SELECT manufacturer AS model, " +
                                 "count(*) as airbus_model FROM dbo.planes WHERE  manufacturer = 'AIRBUS INDUSTRIE' GROUP BY manufacturer FOR JSON PATH; ";
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
                if(responseMessage.EndsWith(","))
                    responseMessage.Substring(0, responseMessage.Length - 1);
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
