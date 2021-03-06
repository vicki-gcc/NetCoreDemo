﻿namespace ITOps.Shared
{
    using System;
    using Autofac;
    using Marketing.Internal;
    using NServiceBus;
    using NServiceBus.Logging;
    using NServiceBus.Transport;
    using Sales.Internal;

    public static class CommonNServiceBusConfiguration
    {
        static readonly ILog log = LogManager.GetLogger(typeof(CommonNServiceBusConfiguration));

        public static void ApplyCommonNServiceBusConfiguration(this EndpointConfiguration endpointConfiguration,
            IContainer autofacExternalContainer = null, bool enableMonitoring = true,
            Action<TransportExtensions<RabbitMQTransport>> bridgeConfigurator = null)
        {
            // Transport configuration
            var rabbitMqConnectionString = Environment.GetEnvironmentVariable("NetCoreDemoRabbitMQTransport");

            if (string.IsNullOrEmpty(rabbitMqConnectionString))
            {
                log.Info("Using Learning Transport");
                var transport = endpointConfiguration.UseTransport<LearningTransport>();
                ConfigureRouting(transport);
                // Persistence Configuration
                endpointConfiguration.UsePersistence<LearningPersistence>();
            }
            else
            {
                log.Info("Using RabbitMQ Transport");
                var transport = endpointConfiguration.UseTransport<RabbitMQTransport>()
                    .ConnectionString(rabbitMqConnectionString)
                    .UseConventionalRoutingTopology();

                ConfigureRouting(transport);

                bridgeConfigurator?.Invoke(transport);

                // Persistence Configuration
                endpointConfiguration.UsePersistence<InMemoryPersistence>();
            }

            if (autofacExternalContainer != null)
            {
                endpointConfiguration.UseContainer<AutofacBuilder>(
                    customizations => { customizations.ExistingLifetimeScope(autofacExternalContainer); });
            }

            endpointConfiguration.EnableInstallers();

            // JSON Serializer
            endpointConfiguration.UseSerialization<NewtonsoftSerializer>();

            if (enableMonitoring)
            {
                endpointConfiguration.AuditProcessedMessagesTo("audit");

                // Enable Metrics Collection and Reporting
                endpointConfiguration
                    .EnableMetrics()
                    .SendMetricDataToServiceControl("Particular.Monitoring", TimeSpan.FromSeconds(5));

                // Enable endpoint hearbeat reporting
                endpointConfiguration.SendHeartbeatTo("Particular.ServiceControl", TimeSpan.FromSeconds(30));
            }
        }

        static void ConfigureRouting<T>(TransportExtensions<T> transport)
            where T : TransportDefinition
        {
            var routing = transport.Routing();
            routing.RouteToEndpoint(typeof(PlaceOrder), "Sales.Api");
            routing.RouteToEndpoint(typeof(CancelOrder), "Sales.Api");
            routing.RouteToEndpoint(typeof(StoreOrder), "Sales.Api");
            routing.RouteToEndpoint(typeof(AcceptOrder), "Sales.Api");
            routing.RouteToEndpoint(typeof(RecordConsumerBehavior), "Marketing.Api");

            // For transports that do not support publish/subcribe natively, e.g. MSMQ, SqlTransport, call RegisterPublisher
        }
    }
}