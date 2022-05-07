# REST API For Access To TURN Services

[A REST API For Access To TURN Services](https://datatracker.ietf.org/doc/html/draft-uberti-behave-turn-rest-00) that can be used with [Coturn](https://github.com/coturn/coturn).

This project assumes the API is hosted via an Ubuntu VM, nginx, and systemd:
https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-6.0

Command line steps I used are found [here](https://github.com/jgcoded/dotfiles/tree/main/coturn).

Coturn must be installed on the same machine and use a sqlite database located at `/var/db/turndb`.

To protect from unauthorized access, OAuth 2.0 is used with OpenId. This project assumes [Azure AD B2C](https://docs.microsoft.com/en-us/azure/active-directory-b2c/tutorial-create-tenant) is the identity management service. Replace the settings in `appsettings.json` with your B2C details.

The project is small and should be easy to enhance to allow the separation of the API and Coturn via a remote DB. I just did it this way so I could pack everything into a cheap VM on Azure.

## Setup

Install VS 2019 with msbuild at least 16.11. Install the .NET 6 SDK. Install VS Code.

Use Visual Studio Code for development with the C# extension. The SQLite Explorer extension may also be helpful.

## Build and Debug

Use Visual Studio code for local testing and debugging.

Use `dotnet build` to build.

## Deployment

```
dotnet publish -r linux-x64 --self-contained false --configuration Release
scp bin\Release\net6.0\linux-x64\publish\* turn@p2p.foo.com:/var/www/p2p-api
ssh turn@p2p.foo.com sudo systemctl restart kestrel-p2p-api.service
ssh turn@p2p.foo.com sudo systemctl status kestrel-p2p-api.service
```
