using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Hemo.Pdf.Api.Swagger;

public sealed class ReportPreviewOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!string.Equals(context.ApiDescription.ActionDescriptor.RouteValues["action"], "Preview", StringComparison.Ordinal))
            return;

        if (operation.RequestBody?.Content is null
            || !operation.RequestBody.Content.TryGetValue("application/json", out var mediaType))
            return;

        mediaType.Example = new OpenApiObject
        {
            ["reportTemplateId"] = new OpenApiString("clinical-07-lab"),
            ["tenantCode"] = new OpenApiString("tenant-demo-a"),
            ["entityId"] = new OpenApiString("test-entity-1"),
            ["data"] = new OpenApiObject
            {
                ["patientName"] = new OpenApiString("Test Patient"),
                ["value"] = new OpenApiInteger(42),
            },
        };
    }
}
