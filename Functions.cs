using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;

namespace ServerlessTelemetry.Functions
{
    public static class Functions
    {

        private static readonly string SqlConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString");


        [FunctionName("negotiate")]
        public static SignalRConnectionInfo GetSignalRInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            [SignalRConnectionInfo(HubName = "telemetryHub")] SignalRConnectionInfo connectionInfo)
        {
            return connectionInfo;
        }

        [FunctionName("messages")]
        public static Task SendMessage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] TelemetryPayload telemetryPayload,
            [SignalR(HubName = "telemetryHub")] IAsyncCollector<SignalRMessage> signalRMessages)
        {
           

            return signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = "telemetryPayload",
                    Arguments = new[] { telemetryPayload }
                });
        }

        [FunctionName("sendDailyFuelConsumption")]
        public static async Task<IActionResult> SendFuelConsumed(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] TelemetryPayload telemetryPayload)
        {
            // Process daily consumption here
            ProcessDailyConsumption(telemetryPayload);

            // Return an appropriate response (e.g., an HTTP status code or a message)
            return new OkResult();
        }


        private static void ProcessDailyConsumption(TelemetryPayload telemetryPayload)
        {
            using (var connection = new SqlConnection(SqlConnectionString))
            {
                connection.Open();
                var query = $"INSERT INTO FuelTanks (Date, [Fuel Left], [Fuel Comsumed/Day], Tank) VALUES (@Date, @FuelLeft,@FuelConsumedPerDay, @Tank)";
                using SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Date", DateTime.UtcNow.Date);
                cmd.Parameters.AddWithValue("@FuelLeft", telemetryPayload.FuelLeft);
                cmd.Parameters.AddWithValue("@FuelConsumedPerDay", telemetryPayload.FuelComsumedDay);
                cmd.Parameters.AddWithValue("@Tank", telemetryPayload.Tank);

                cmd.ExecuteNonQuery();
            }


        }


        
    }


    public class TelemetryPayload
    {
        public int Tank { get; set; }
        public double FuelLeft { get; set; }

        public double FuelComsumedDay { get; set; }

        public DateTime Timestamp { get; set; }

        public double Voltage { get; set; }
        public double Temperature {  get; set; }
        public double Current { get; set; }
        public double FlowRate { get; set; }
        public double Vibration { get; set; }
        public double UndergroundTank1FuelLevel{ get; set; }

        public double UndergroundTank2FuelLevel {  get; set; }

        public double BurnerFuelLevel { get; set; }
        public double GeneratorFuelLevel { get; set; }

        public double BoilerFuelLevel { get; set; }

        public TelemetryPayload()
        {
            Timestamp = DateTime.UtcNow;
        }

    }
}