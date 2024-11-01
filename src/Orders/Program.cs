﻿// See https://aka.ms/new-console-template for more information

using JasperFx.Core;
using Marten;
using Marten.Events;
using Marten.Events.Projections;
using Messages;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Orders;
using Wolverine;
using Wolverine.Marten;
using Wolverine.RabbitMQ;

var hostBuilder = Host.CreateDefaultBuilder(args);
hostBuilder.ConfigureServices(
  services =>
  {
    services.AddMarten(
        options =>
        {
          options.Connection(
            new NpgsqlConnectionStringBuilder()
            {
              Host = "localhost",
              Password = "123456",
              Database = "orders",
              Port = 5401,
              Username = "orders"
            }.ToString()
          );
          options.Events.StreamIdentity = StreamIdentity.AsString;
          options.Projections.Add<PurchaseOrderProjection>(ProjectionLifecycle.Inline);
          options.DisableNpgsqlLogging = true;
        }
      )
      .UseLightweightSessions();
    // .IntegrateWithWolverine();
  }
);
hostBuilder
  .UseWolverine(
    options =>
    {
      options
        .ListenToRabbitQueue(
          "new-orders",
          queue => queue.TimeToLive(15.Seconds())
        );

      options
        .ListenToRabbitQueue(
          "complete-orders"
        );

      var rabbitMq = options.UseRabbitMq(
        configure => { configure.ClientProvidedName = "Orders"; }
      );

      rabbitMq
        .DeclareExchange(
          "new-orders",
          exchange =>
          {
            exchange.ExchangeType = ExchangeType.Direct;
            exchange.BindQueue("new-orders");
          }
        )
        .AutoProvision();


      options
        .UseRabbitMq()
        .DeclareExchange(
          "placed-orders",
          exchange =>
          {
            exchange.ExchangeType = ExchangeType.Fanout;
            exchange.BindQueue("placed-orders-orders");
          }
        )
        .AutoProvision();

      options.PublishMessage<OrderPlaced>()
        .ToRabbitExchange("placed-orders");


      options.ListenToRabbitQueue("placed-orders-orders");
    }
  );
var host = hostBuilder
  .Build();

await host.RunAsync();
