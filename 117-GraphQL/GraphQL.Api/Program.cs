using GraphQL.Api.Data;
using GraphQL.Api.Entities;
using GraphQL.Api.Queries;
using GraphQL.Api.Mutations;
using GraphQL.Api.Subscriptions;
using GraphQL.Api.DataLoaders;
using GraphQL.Api.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=graphql.db")); // For DataLoader

builder.Services.AddScoped(sp => 
    sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

builder.Services
    .AddGraphQLServer()
    .AddQueryType()
    .AddTypeExtension<ProductQuery>()
    .AddTypeExtension<OrderQuery>()
    .AddMutationType()
    .AddTypeExtension<ProductMutation>()
    .AddTypeExtension<OrderMutation>()
    .AddSubscriptionType()
    .AddTypeExtension<ProductSubscription>()
    .AddType<ProductType>()
    .AddType<OrderType>()
    .AddDataLoader<ProductByIdDataLoader>()
    .AddInMemorySubscriptions()  // In-memory pub/sub
    .AddProjections()
    .AddFiltering()
    .AddSorting();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    AppDbContext.Seed(db);
}

app.UseWebSockets(); // for Subscriptions
app.MapGraphQL("/graphql");
app.Run();

public partial class Program { }
