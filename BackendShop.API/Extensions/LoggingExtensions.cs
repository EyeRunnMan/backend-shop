using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace BackendShop.API.Extensions
{
    public static class LoggingExtensions
    {
        /// <summary>
        /// Logs HTTP requests with additional context information
        /// </summary>
        public static void LogRequest(this ILogger logger, HttpContext context, string message, params object[] args)
        {
            // Add request-specific properties to the log context
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestId"] = context.TraceIdentifier,
                ["RemoteIpAddress"] = context.Connection.RemoteIpAddress,
                ["Method"] = context.Request.Method,
                ["Path"] = context.Request.Path,
                ["UserAgent"] = context.Request.Headers["User-Agent"].ToString(),
                ["StatusCode"] = context.Response.StatusCode
            }))
            {
                logger.LogInformation(message, args);
            }
        }
    }
}