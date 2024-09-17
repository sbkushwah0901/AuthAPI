using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Prevueit.Db.Models;
using Prevueit.Lib.Implementation;
using Prevueit.Lib.Interface;
using Prevueit.Lib.Models.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Prevueit.Service
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }
        public Startup(IConfiguration configuration, IWebHostEnvironment appEnv)
        {
            Configuration = configuration;
            Environment = appEnv;
        }

        public void ConfigureServices(IServiceCollection services)
        {

            services.AddCors(o => o.AddDefaultPolicy(
                     builder =>
                     {
                         builder.AllowAnyOrigin()
                                .AllowAnyMethod()
                                 .AllowAnyHeader();
                     }));

            //services.AddMvc();
            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue; // In case of multipart
            });

            var section = Configuration.GetSection("ConnectionStrings");

            ConnectionStrings connectionStrings = section.Get<ConnectionStrings>();//new ConnectionStrings();

            System.Environment.SetEnvironmentVariable("SqlConnectionString", connectionStrings.PrevuitContext);

            services.AddSingleton(connectionStrings);
            services.AddDbContextPool<prevuitContext>(options =>
            {
                options.UseSqlServer(
                    Configuration.GetConnectionString("prevueitContext"));
            });

            section = Configuration.GetSection("Jwt");
            Jwt jwt = section.Get<Jwt>();
            services.AddSingleton(jwt);

            section = Configuration.GetSection("AzureStorage");
            AzureStorage azureStorage = section.Get<AzureStorage>();
            services.AddSingleton(azureStorage);

            section = Configuration.GetSection("Email");
            Email email = section.Get<Email>();
            services.AddSingleton(email);

            services.AddSingleton(func =>
            {
                var connectionStrings = func.GetService<ConnectionStrings>();
                var jwt = func.GetService<Jwt>();
                var azureStorage = func.GetService<AzureStorage>();
                var email = func.GetService<Email>();
                return new AppSettingsModel() { Jwt = jwt, ConnectionStrings = connectionStrings, AzureStorage = azureStorage, Email = email};
            });

            services.AddHttpContextAccessor();
            services.AddControllers().AddJsonOptions(options =>
             options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            #region Dependancy

            services.AddScoped<IUserLibrary, UserLibrary>();
            services.AddScoped<IFileLibrary, FileLibrary>();
            services.AddScoped<ICommentLibrary, CommentLibrary>();
            services.AddScoped<ICommonLibrary, CommonLibrary>();
            services.AddScoped<IAdminLibrary, AdminLibrary>();


            #endregion

            #region Swagger Implementation

            //services.AddSwaggerGen(c =>
            //{
            //    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Prevueit Web Service V1", Version = "v1" });
            //    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
            //});
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Prevueit Web Service V1", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type=ReferenceType.SecurityScheme,
                                Id="Bearer"
                            }
                        },
                        new string[]{}
                    }
                });
                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
            });
            #endregion

            services.AddHealthChecks();
            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime)
        {

            #region Swagger
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Prevueit Web Service V1"));
            #endregion

            #region Routes
            app.Use((context, next) =>
            {
                try
                {
                    var headers = context.Request.Headers;
                    if (headers.ContainsKey("customdata"))
                    {
                        var value = headers["customdata"];
                        var parsed = Newtonsoft.Json.Linq.JToken.Parse(value);

                    }
                }
                catch
                {
                }
                return next.Invoke();
            });

            #endregion

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors();
            app.UseAuthorization();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

        }
    }
}
