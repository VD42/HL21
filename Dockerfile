FROM mcr.microsoft.com/dotnet/runtime:3.1
WORKDIR /app
COPY bin/Release/netcoreapp3.1/publish/hl21.runtimeconfig.json ./
COPY bin/Release/netcoreapp3.1/publish/hl21.dll ./
ENTRYPOINT ["dotnet", "hl21.dll"]