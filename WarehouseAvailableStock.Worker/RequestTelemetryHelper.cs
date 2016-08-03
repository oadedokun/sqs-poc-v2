using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace WarehouseAvailableStock.Worker
{
    public static class RequestTelemetryHelper
    {
        private static string SUCCESS_CODE = "200";
        private static string FAILURE_CODE = "500";

        public static RequestTelemetry Start(string name, DateTimeOffset startTime)
        {
            var request = new RequestTelemetry();
            request.Name = name;
            request.Timestamp = startTime;
            return request;
        }

        public static void Dispatch(this TelemetryClient telemetryClient, RequestTelemetry request, TimeSpan duration, bool success)
        {
            request.Duration = duration;
            request.Success = success;
            request.ResponseCode = (success) ? SUCCESS_CODE : FAILURE_CODE;
            telemetryClient.TrackRequest(request);
        }
    }
}
