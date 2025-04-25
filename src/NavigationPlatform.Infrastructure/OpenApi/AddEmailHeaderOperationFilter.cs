using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NavigationPlatform.Infrastructure.OpenApi
{
    public class AddEmailHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
            {
                operation.Parameters = new List<OpenApiParameter>();
            }

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-User-Email",
                In = ParameterLocation.Header,
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = "string"
                },
                Description = "User email for testing purposes",
                Example = new Microsoft.OpenApi.Any.OpenApiString("xhesjana.stambolliu@gmail.com")
            });
        }
    }
} 