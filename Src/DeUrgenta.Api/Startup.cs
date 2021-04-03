using System.Reflection;
using DeUrgenta.Backpack.Api.Controllers;
using DeUrgenta.Certifications.Api.Controller;
using DeUrgenta.Api.Extensions;
using DeUrgenta.Common.Swagger;
using DeUrgenta.Domain;
using Hellang.Middleware.ProblemDetails;
using DeUrgenta.Infra.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace DeUrgenta.Api { 
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            WebHostEnvironment = environment;
            _swaggerClientName = configuration.GetValue<string>("SwaggerOidcClientName");
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment WebHostEnvironment { get; }
        public List<ApiAuthenticationScheme> AuthSchemes { get; set; }

        private string _swaggerClientName;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            var identityUrl = Configuration.GetValue<string>("IdentityServerUrl");
            var apiSchemes = new List<ApiAuthenticationScheme>();

            Configuration.GetSection("ApiConfiguration").Bind(apiSchemes);
            var serviceBuilder = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
            foreach (var authScheme in apiSchemes)
            {
                serviceBuilder = serviceBuilder.AddIdentityServerAuthentication(authScheme.SchemeName, options =>
                {
                    options.Authority = identityUrl;
                    options.ApiName = authScheme.ApiName;
                    options.ApiSecret = authScheme.ApiSecret;
                    options.RequireHttpsMetadata = !WebHostEnvironment.IsDevelopment();
                });
            }

            services.AddDatabase(Configuration.GetConnectionString("DbConnectionString"));
            services.AddExceptionHandling(WebHostEnvironment);


            var applicationAssemblies = GetAssemblies();

            services.AddSwaggerFor(applicationAssemblies, Configuration);
            services.AddMediatR(applicationAssemblies);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, DeUrgenta.Domain.DeUrgentaContext dbContext)
        {
            //dbContext.Database.Migrate();
            
            if (WebHostEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseProblemDetails();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.OAuthClientId(_swaggerClientName);
                c.OAuthAppName("Swagger UI");
                c.ConfigObject = new ConfigObject
                {
                    Urls = new[]
                    {
                        new UrlDescriptor{Name = "api", Url = "/swagger/v1/swagger.json"}
                    }
                };

            });

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => endpoints.MapControllers());

        }

        private static Assembly[] GetAssemblies() => new[]
            {
                Assembly.GetAssembly(typeof(Startup)),

                // Application parts
                typeof(BackpackController).GetTypeInfo().Assembly,
                typeof(CertificationController).GetTypeInfo().Assembly,

                // Common

                typeof(ApplicationErrorResponseExample).GetTypeInfo().Assembly
            };
    }
}
