// See https://aka.ms/new-console-template for more information

using Marten;
using Marten.Events;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using RetailClient;
using RetailClient.Data;
using Wolverine;
using Wolverine.RabbitMQ;

var host = Host.CreateDefaultBuilder(args)
  .ConfigureServices(
    (
      context,
      services
    ) =>
    {
      services.AddMarten(
        options =>
        {
          options.Connection(
            new NpgsqlConnectionStringBuilder()
            {
              Host = "localhost",
              Password = "123456",
              Database = "client",
              Port = 5403,
              Username = "client",
            }.ToString()
          );
          options.Events.StreamIdentity = StreamIdentity.AsString;
        }
      ).InitializeWith(new InitialData(InitialDatasets.Customers));
    }
  )
  .UseWolverine(
    options =>
    {
      options.UseRabbitMq(
        configure => { configure.ClientProvidedName = "Client"; }
      );
      options.Services.AddHostedService<Worker>();

      options.PublishMessage<PlaceOrder>()
        .ToRabbitQueue("new-orders");
    }
  )
  .Build();

await host.RunAsync();
