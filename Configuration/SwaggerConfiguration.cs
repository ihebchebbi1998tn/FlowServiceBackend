using Microsoft.OpenApi.Models;
using System.Reflection;

namespace MyApi.Configuration
{
    public static class SwaggerConfiguration
    {
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                // API Information
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1.0.0",
                    Title = "Flow Service API",
                    Description = "A comprehensive API for Flow Service application with authentication, contacts, users, roles, skills, and more.",
                    Contact = new OpenApiContact
                    {
                        Name = "Flow Service Development Team",
                        Email = "dev@flowservice.com",
                        Url = new Uri("https://flowservice.com")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://opensource.org/licenses/MIT")
                    }
                });

                // JWT Authentication
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT token in the format: Bearer {your-jwt-token}"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // XML Documentation
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }

                // Enhanced documentation options
                options.EnableAnnotations();
                options.UseInlineDefinitionsForEnums();
                
                // Group endpoints by tags
                options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
                options.DocInclusionPredicate((name, api) => true);

                // Add response examples
                options.OperationFilter<SwaggerResponseExamplesFilter>();
                options.SchemaFilter<SwaggerSchemaExamplesFilter>();

                // Custom ordering
                options.OrderActionsBy(apiDesc => 
                {
                    var controller = apiDesc.ActionDescriptor.RouteValues["controller"];
                    var action = apiDesc.ActionDescriptor.RouteValues["action"];
                    return $"{controller}_{action}";
                });

                // Server information
                var baseUrl = configuration["BaseUrl"] ?? "https://flowservicebackend.onrender.com";
                options.AddServer(new OpenApiServer
                {
                    Url = baseUrl,
                    Description = "Production Server"
                });

                options.AddServer(new OpenApiServer
                {
                    Url = "http://localhost:10000",
                    Description = "Development Server"
                });
            });

            return services;
        }

        public static WebApplication UseSwaggerDocumentation(this WebApplication app, IConfiguration configuration)
        {
            // Enable Swagger in all environments for API testing
            app.UseSwagger(options =>
            {
                options.RouteTemplate = "api-docs/{documentName}/swagger.json";
            });
            
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/api-docs/v1/swagger.json", "Flow Service API v1");
                options.RoutePrefix = "api-docs";
                options.DocumentTitle = "Flow Service API Documentation";
                options.DefaultModelsExpandDepth(-1);
                options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Example);
                options.DisplayRequestDuration();
                options.EnableDeepLinking();
                options.EnableFilter();
                options.ShowExtensions();
                options.EnableValidator();
                
                // Custom CSS for better appearance
                options.InjectStylesheet("/swagger-ui/custom.css");
                
                // Add development token for easy testing
                if (app.Environment.IsDevelopment())
                {
                    var devToken = TokenHelper.GenerateDevelopmentToken(configuration);
                    options.OAuthConfigObject = new
                    {
                        clientId = "swagger-ui",
                        appName = "Flow Service API",
                        additionalQueryStringParams = new Dictionary<string, string>
                        {
                            { "dev_token", devToken }
                        }
                    };
                    
                    // Add custom JavaScript for auto-filling token
                    options.InjectJavascript("/swagger-ui/dev-token.js");
                }
            });

            return app;
        }
    }
}