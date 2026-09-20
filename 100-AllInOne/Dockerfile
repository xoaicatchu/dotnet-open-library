FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
EXPOSE 5200
ENV ASPNETCORE_HTTP_PORTS=5200

COPY ./publish-linux-chiseled .
ENTRYPOINT ["dotnet", "AllInOne.Api.dll"]
