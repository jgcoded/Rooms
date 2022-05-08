
using Lib.AspNetCore.ServerSentEvents;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Builder;

using p2p_api.Extensions;
using p2p_api.Models;
using p2p_api.Providers;
using p2p_api.Services;
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

builder.Services.AddSingleton<RoomsService>();
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

builder.Services.AddServerSentEvents(options =>
{
    options.OnClientConnected = (service, args) =>
    {
        string? roomName = args.Request.RouteValues["roomName"] as string;

        if (roomName is null)
        {
            args.Client.Disconnect();
            return;
        }

        var roomsService = args.Request.HttpContext.RequestServices.GetRequiredService<RoomsService>();
        roomsService.AddUserToRoom(roomName, args.Client.User.UserId());
        service.AddToGroup(roomName, args.Client);
    };

    options.OnClientDisconnected = (service, args) =>
    {
        string? roomName = args.Request.RouteValues["roomName"] as string;

        if (roomName is null)
        {
            return;
        }

        var roomsService = args.Request.HttpContext.RequestServices.GetRequiredService<RoomsService>();
        roomsService.RemoveUserFromRoom(roomName, args.Client.User.UserId());
    };

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
        Authorization = ServerSentEventsAuthorization.Default,
        OnPrepareAccept = response =>
        {
            response.Headers.Append("Cache-Control", "no-cache");
            response.Headers.Append("X-Accel-Buffering", "no");
        }
    }).RequireAuthorization();
});

app.Run();
