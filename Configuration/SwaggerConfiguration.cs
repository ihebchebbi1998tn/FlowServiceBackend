using Microsoft.OpenApi.Models;

namespace MyApi.Configuration
{
    public static class SwaggerConfiguration
    {
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            return services;
        }

        public static WebApplication UseSwaggerDocumentation(this WebApplication app, IConfiguration configuration)
        {
            if (app.Environment.IsDevelopment() || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RENDER")))
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            return app;
        }
    }
}