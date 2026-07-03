using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Collections.Generic;

namespace WebApplication1.Utils
{
    public class ApiExceptionFilter : IExceptionFilter
    {
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;
        private readonly Microsoft.Extensions.Logging.ILogger<ApiExceptionFilter> _logger;

        public ApiExceptionFilter(Microsoft.AspNetCore.Hosting.IWebHostEnvironment env, Microsoft.Extensions.Logging.ILogger<ApiExceptionFilter> logger)
        {
            _env = env;
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            var problemDetails = new ProblemDetails
            {
                Status = 500,
                Title = "An error occurred while processing your request.",
                Detail = _env.EnvironmentName == "Development" ? context.Exception.Message : "An unexpected error occurred.",
                Instance = context.HttpContext.Request.Path
            };
            _logger.LogError(context.Exception, "Unhandled exception on {Path}", context.HttpContext.Request.Path);

            context.Result = new ObjectResult(problemDetails)
            {
                StatusCode = 500
            };
            context.ExceptionHandled = true;
        }
    }

    public static class DrfValidationResponseFactory
    {
        public static IActionResult CreateResponse(ActionContext context)
        {
            var errors = new Dictionary<string, string[]>();
            string firstMessage = "Validation failed.";
            bool firstMessageSet = false;

            foreach (var keyModelStatePair in context.ModelState)
            {
                var key = keyModelStatePair.Key;
                var values = keyModelStatePair.Value.Errors.Select(e => e.ErrorMessage).ToArray();
                
                // Convert PascalCase to snake_case for DRF parity
                var snakeCaseKey = string.Concat(key.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
                
                if (string.IsNullOrEmpty(snakeCaseKey))
                {
                    snakeCaseKey = "non_field_errors";
                }

                errors[snakeCaseKey] = values;

                if (!firstMessageSet && values.Length > 0)
                {
                    firstMessage = $"{snakeCaseKey}: {values[0]}";
                    firstMessageSet = true;
                }
            }

            var response = new
            {
                success = false,
                status = "error",
                message = firstMessage,
                errors = errors
            };

            return new BadRequestObjectResult(response);
        }
    }
}
