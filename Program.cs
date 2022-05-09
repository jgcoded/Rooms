
using Lib.AspNetCore.ServerSentEvents;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

using p2p_api.Extensions;
using p2p_api.Models;
using p2p_api.Providers;
using p2p_api.Services;
using p2p_api.Workers;
using p2p_api.Authentication;
using p2p_api.Authorization;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "P2PApiCorsPolicy";
var allowedCorsOrigins = builder.Configuration["AllowedCorsOrigins"]?.Split(',') ?? new string[] { };
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.AllowAnyMethod();
        policy.AllowAnyHeader();
        policy.WithOrigins(allowedCorsOrigins);
    });
});

// Add services to the container.
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
    options.HandleSameSiteCookieCompatibility();
});

// Adds Microsoft Identity platform (Azure AD B2C) support to protect this Api
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, SSEAuthenticationHandler>(SSEAuthenticationScheme.SchemeName, null)
    .AddMicrosoftIdentityWebApi(options =>
    {
        builder.Configuration.Bind("AzureAdB2C", options);
        options.TokenValidationParameters.NameClaimType = "name";
    },
    options =>
    {
        builder.Configuration.Bind("AzureAdB2C", options);
    });
// End of the Microsoft Identity platform block

const string SSEAuthorizationPolicy = "SSEAuthorizationPolicy";
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(SSEAuthorizationPolicy, policy => {
        policy.AddAuthenticationSchemes(SSEAuthenticationScheme.SchemeName);
        policy.AddRequirements(new SSEAuthorizationRequirement());
    });
});

builder.Services.AddSingleton<RoomsService>();
builder.Services.AddSingleton<ReferenceHolder>();
builder.Services.AddHostedService<DatabaseWorker>();
builder.Services.AddSingleton<IAuthenticationHandler, SSEAuthenticationHandler>();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOptions();
builder.Services.Configure<CredentialsOptions>(builder.Configuration.GetSection(CredentialsOptions.Credentials));
builder.Services.Configure<DatabaseWorkerOptions>(builder.Configuration.GetSection(DatabaseWorkerOptions.DatabaseWorker));

builder.Services.AddServerSentEvents(options =>
{
    options.OnClientConnected = RoomsService.OnClientConnected;
    options.OnClientDisconnected = RoomsService.OnClientDisconnected;
    options.KeepaliveMode = ServerSentEventsKeepaliveMode.Always;
    options.KeepaliveInterval = 60;
});

builder.Services.AddServerSentEventsClientIdProvider<UserClientIdProvider>();
// If this project is scaled out to more than one machine, use
// builder.Services.AddDistributedServerSentEventsNoReconnectClientsIdsStore
builder.Services.AddInMemoryServerSentEventsNoReconnectClientsIdsStore();

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

app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Credentials}/{action=GetCredentials}"
    );

    endpoints.MapServerSentEvents("/rooms/{roomName:required}", new ServerSentEventsOptions
    {
        Authorization = new ServerSentEventsAuthorization
        {
            Policy = SSEAuthorizationPolicy
        },
        OnPrepareAccept = response =>
        {
            response.Headers.Append("Cache-Control", "no-cache");
            response.Headers.Append("X-Accel-Buffering", "no");
        }
    });
});

app.Run();
