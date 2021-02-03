using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Mailer.Mapping;
using Mailer.Api.Auth;
using Mailer.Api.Background;
using Mailer.Api.Middlewares;
using Mailer.API.Middlewares;
using Mailer.Domain.Services;
using Mailer.Infrastructure;
using Mailer.Infrastructure.Connectors;
using Mailer.Infrastructure.Models;
using Mailer.Infrastructure.Security;
using Mailer.Infrastructure.Types;
using Mailer.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace Mailer.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc(options =>
            {
                options.Interceptors.Add<ErrorHandlingInterceptor>();
            });
            services
                .AddSingleton<IPolicyProvider, PolicyProvider>()
                .AddScoped<ICredentialService, CredentialService>()
                .AddSingleton<IDbProtector, DbProtector>()
                .AddMailClient(opts => Configuration.GetSection("SendGrid").Bind(opts))
                .AddMailingMessageBus(opts => Configuration.GetSection("RabbitMq:Mailing").Bind(opts))
                .AddMongoDb(opts => Configuration.GetSection("Mongo").Bind(opts))

                .AddScoped<IRecipientService, RecipientService>()
                .AddScoped<ISenderService, SenderService>()
                .AddScoped<IMailingService, MailingService>()

                .AddHostedService<DatabaseInitializer>();
            if (Configuration.GetValue<bool>("RabbitMq:Mailing:ConsumerOn"))
                services.AddHostedService<MailingSubscriber>();

            var dataProtectionOptions = new DataProtectionOptions();
            Configuration.GetSection("DataProtection").Bind(dataProtectionOptions);
            var dpBuilder = services.AddDataProtection()
                .SetApplicationName(dataProtectionOptions.ApplicationName)
                .SetDefaultKeyLifetime(dataProtectionOptions.KeyLifetime)
                .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionOptions.KeyVaultPath));
            if (dataProtectionOptions.Certificate != null)
                dpBuilder.ProtectKeysWithCertificate(new X509Certificate2(dataProtectionOptions.Certificate.Path, dataProtectionOptions.Certificate.Password));

            services
                .AddMvcCore()
                .AddAuthorization()
                .AddApiExplorer()
                .AddDataAnnotations();

            services.AddMappings((s, cfg) =>
            {
                cfg.AddProfile(new TypesMapping(s.GetRequiredService<IDbProtector>()));

                var profileTypes = new []
                {
                    typeof(ViewModels.MailingVm.Mapping).Assembly,
                    typeof(DbMapping).Assembly
                }.SelectMany(a => a.GetTypes()).Where(t => typeof(BaseMapping).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) != null);
                cfg.AddProfiles(profileTypes.Select(t => (BaseMapping)Activator.CreateInstance(t)!));
            });

            services.AddAuthentication("BasicAuthentication")
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);

            services
                .AddOptions()
                .Configure<UsersList>(opts => Configuration.GetSection("Users").Bind(opts))
                .Configure<HashOptions>(opts => Configuration.GetSection("DataProtection:Hash").Bind(opts));
            services.AddRouting(options => options.LowercaseUrls = true);
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Description = $"Atlas Feeds REST API {GetType().Assembly.GetName().Version}",
                    Title = "LMG Feeds API"
                });
                options.AddSecurityDefinition("basic", new OpenApiSecurityScheme()
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "basic",
                    Description = "Authorization header using the Basic scheme. Example: \"Basic {base64}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "basic"
                            }
                        },
                        new string[0]
                    }
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGrpcServices();
            });
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                c.RoutePrefix = "swagger/ui";
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
                c.DefaultModelsExpandDepth(-1);
                c.DefaultModelExpandDepth(2);
                c.EnableFilter();
                c.DisplayRequestDuration();
            });
        }

        private class DataProtectionOptions
        {
            public string KeyVaultPath { get; set; } = default!;
            public string ApplicationName { get; set; } = default!;
            public TimeSpan KeyLifetime { get; set; } = TimeSpan.FromDays(90);
            public CertificateOptions? Certificate { get; set; }

            public class CertificateOptions
            {
                public string Path { get; set; } = default!;
                public string Password { get; set; } = default!;
            }
        }
    }
}
