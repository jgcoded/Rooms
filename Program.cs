using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;

using Microsoft.AspNetCore.Builder;

using p2p_api.Models;
using p2p_api.Workers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
    options.HandleSameSiteCookieCompatibility();
});

builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAdB2C");

//TODO
builder.Services.AddSingleton<ReferenceHolder>();
builder.Services.AddHostedService<DatabaseWorker>();
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOptions();
builder.Services.Configure<OpenIdConnectOptions>(builder.Configuration.GetSection("AzureAdB2C"));
builder.Services.Configure<CredentialsOptions>(builder.Configuration.GetSection(CredentialsOptions.Credentials));
builder.Services.Configure<DatabaseWorkerOptions>(builder.Configuration.GetSection(DatabaseWorkerOptions.DatabaseWorker));

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCookiePolicy();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Credentials}/{action=GetCredentials}"
    );
});

app.Run();
