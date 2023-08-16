#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore

FROM build AS publish
#���õĲ�������ˣ�prometheus oauth2.0 es
#��Ҫ����Ѷ�ƵĲ������
RUN dotnet publish ./MCollector/MCollector.csproj -c Release -o /app/publish/MCollector \
 && dotnet publish ./MCollector.Plugins.TencentCloud/MCollector.Plugins.TencentCloud.csproj -c Release -o /app/publish/MCollector.Plugins.TencentCloud \
 && mkdir -p /app/publish/MCollector/Plugins/TencentCloud/ \
 && cp -r /app/publish/MCollector.Plugins.TencentCloud/*.* /app/publish/MCollector/Plugins/TencentCloud/

#RUN dotnet publish ./MCollector/MCollector.csproj -c Release -o /app/publish/MCollector \

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish/MCollector .
EXPOSE 18086 1234
ENTRYPOINT ["dotnet", "MCollector.dll"]