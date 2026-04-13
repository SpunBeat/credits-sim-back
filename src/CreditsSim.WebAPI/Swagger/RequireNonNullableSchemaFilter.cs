using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CreditsSim.WebAPI.Swagger;

/// <summary>
/// Marks all non-nullable properties as required in the OpenAPI schema.
/// This eliminates unnecessary '?' in generated TypeScript interfaces.
/// </summary>
public class RequireNonNullableSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties == null || context.Type == null)
            return;

        var nonNullableProperties = context.Type
            .GetProperties()
            .Where(p =>
            {
                // Nullable<T> (int?, decimal?, etc.) → not required
                if (Nullable.GetUnderlyingType(p.PropertyType) != null)
                    return false;

                // Nullable reference types: check NullabilityInfoContext
                var nullability = new NullabilityInfoContext().Create(p);
                return nullability.WriteState != NullabilityState.Nullable;
            })
            .Select(p =>
            {
                // Convert PascalCase property name to camelCase (matching JSON serialization)
                var name = p.Name;
                return char.ToLowerInvariant(name[0]) + name[1..];
            })
            .ToHashSet();

        schema.Required = nonNullableProperties
            .Where(name => schema.Properties.ContainsKey(name))
            .ToHashSet();
    }
}
