using NLog.Web;

namespace Agent.Application
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add NLog
            builder.Logging.ClearProviders();
            builder.Host.UseNLog();

            // Add services to the container.
            var configuration = builder.Configuration;
            var services = builder.Services;

            services.AddRazorPages();
            services.AddControllers();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //app.UseHttpsRedirection();

            app.UseCors();
            app.UseWebSockets();
            app.UseAuthorization();
            app.UseStaticFiles();

            app.MapControllers();

            app.Map("/", () => "Hello Agent Service"); // ื๎ะก API

            app.Run();
        }
    }
}