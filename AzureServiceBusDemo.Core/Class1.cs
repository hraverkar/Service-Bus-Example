using Azure.Messaging.ServiceBus;
using AzureServiceBusDemo.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace AzureServiceBusDemo.Core
{
    public interface IMessageBus
    {
        Task PublishMessageAsync<T>(T message);
    }

    public interface IMessageBusFactory
    {

        IMessageBus GetClient(string connectionString, string sender);
    }
    public class AzureServiceBus : IMessageBus
    {
        private readonly ServiceBusSender _serviceBusSender;

        public AzureServiceBus(ServiceBusSender serviceBusSender)
        {
            _serviceBusSender = serviceBusSender;
        }
        public async Task PublishMessageAsync<T>(T message)
        {
            var jsonString = JsonSerializer.Serialize(message);
            var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(jsonString));
            await _serviceBusSender.SendMessageAsync(serviceBusMessage);
        }

        public static IMessageBus Create(ServiceBusSender sender)
        {
            return new AzureServiceBus(sender);
        }
    }

    public class AzureServiceBusFactory : IMessageBusFactory
    {
        private readonly object _lockObject = new object();
        private readonly ConcurrentDictionary<string, ServiceBusClient> _client =
            new ConcurrentDictionary<string, ServiceBusClient>();
        private readonly ConcurrentDictionary<string, ServiceBusSender> _sender =
            new ConcurrentDictionary<string, ServiceBusSender>();

        public IMessageBus GetClient(string connectionString, string senderName)
        {
            var key = $"{connectionString}-{senderName}";
            if (this._sender.ContainsKey(key) && !this._sender[key].IsClosed)
            {
                return AzureServiceBus.Create(this._sender[key]);
            }

            var client = this.GetServiceBusClient(connectionString);

            lock (this._lockObject)
            {
                if (this._sender.ContainsKey(key) && this._sender[key].IsClosed)
                {
                    if (this._sender[key].IsClosed)
                    {
                        this._sender[key].DisposeAsync().GetAwaiter().GetResult();
                    }
                    return AzureServiceBus.Create(this._sender[key]);
                }

                var sender = client.CreateSender(senderName);
                this._sender[key] = sender;
            }
            return AzureServiceBus.Create(this._sender[key]);

        }

        protected virtual ServiceBusClient GetServiceBusClient(string connectionString)
        {
            var key = $"{connectionString}";
            lock(this._lockObject)
            {
                if (this.ClientDoesnotExistOrIsClosed(connectionString))
                {
                    var client = new ServiceBusClient(connectionString, new ServiceBusClientOptions
                    {
                        TransportType = ServiceBusTransportType.AmqpTcp
                    });
                    this._client[key] = client;

                }
                return this._client[key];
            }
        }

        private bool ClientDoesnotExistOrIsClosed(string connectionString)
        {
            return !this._client.ContainsKey(connectionString) 
                || this._client[connectionString].IsClosed;
        }

    }

    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddAzureServiceBusFactory (this IServiceCollection services)
        {
            services.AddSingleton<IMessageBusFactory, AzureServiceBusFactory>();
            return services;
        }
    }
}