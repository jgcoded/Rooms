
using System.Text;

using Lib.AspNetCore.ServerSentEvents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

using p2p_api.Extensions;
using p2p_api.Models;
using p2p_api.Providers;
using p2p_api.Services;
using p2p_api.Workers;
using p2p_api.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
    options.HandleSameSiteCookieCompatibility();
});

builder.Services.AddOptions();
builder.Services.Configure<TurnCredentialsOptions>(builder.Configuration.GetSection(TurnCredentialsOptions.TurnCredentials));
builder.Services.Configure<DatabaseWorkerOptions>(builder.Configuration.GetSection(DatabaseWorkerOptions.DatabaseWorker));
builder.Services.Configure<RoomTokenOptions>(builder.Configuration.GetSection(RoomTokenOptions.RoomToken));

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

builder.Services.AddSingleton<IAuthorizationHandler, RoomAuthorizationHandler>();

const string RoomTokenAuthenticationScheme = "RoomToken";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    // This api will mint its own short-lived tokens for auth to SSE api
    .AddJwtBearer(RoomTokenAuthenticationScheme, options => {
        var roomTokenOptions = new RoomTokenOptions();
        builder.Configuration.Bind(RoomTokenOptions.RoomToken, roomTokenOptions);
        options.Events = new JwtBearerEvents()
        {
            OnMessageReceived = RoomTokenService.TokenInUrlOnMessageReceived
        };
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidIssuer = roomTokenOptions.Issuer,
            ValidAudience = roomTokenOptions.Issuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(roomTokenOptions.Key))
        };
    })
    // Adds Microsoft Identity platform (Azure AD B2C) support to protect this Api
    .AddMicrosoftIdentityWebApi(options =>
    {
        builder.Configuration.Bind("AzureAdB2C", options);
    },
    options =>
    {
        builder.Configuration.Bind("AzureAdB2C", options);
    });

const string SSEAuthorizationPolicy = "SSEAuthorizationPolicy";
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(SSEAuthorizationPolicy, policy => {
        policy.AddAuthenticationSchemes(RoomTokenAuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new RoomAuthorizationRequirement());
    });
});

builder.Services.AddSingleton<RoomTokenService>();
builder.Services.AddSingleton<TurnCredentialsService>();
builder.Services.AddSingleton<RoomService>();
builder.Services.AddSingleton<ReferenceHolder>();
builder.Services.AddHostedService<DatabaseWorker>();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddServerSentEvents(options =>
{
    options.OnClientConnected = RoomService.OnClientConnected;
    options.OnClientDisconnected = RoomService.OnClientDisconnected;
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

    endpoints.MapServerSentEvents("/rooms/{roomId:guid:required}", new ServerSentEventsOptions
    {
        //Authorization = ServerSentEventsAuthorization.Default,
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
