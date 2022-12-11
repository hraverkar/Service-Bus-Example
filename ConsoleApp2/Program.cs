using Azure.Messaging.ServiceBus;
using AzureServiceBusDemo.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ConsoleApp2
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddTransient<App>();
            services.AddAzureServiceBusFactory();
            var serviceProvider = services.BuildServiceProvider();
            await serviceProvider.GetService<App>().Run();

        }
    }

    public class App
    {
        private readonly IMessageBusFactory _messageBusFactory;
        public App(IMessageBusFactory messageBusFactory)
        {
            _messageBusFactory = messageBusFactory;
        }

        public async Task Run()
        {
            string connectionString = "Endpoint=sb://hraverkarmyservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=pMSMW/2urwq9t7DEm9F9xzYwOIw5xI6yn9ajc+LjQZM=";
            string queueName = "myqueue";
            var client = this._messageBusFactory.GetClient(connectionString, queueName);
            await client.PublishMessageAsync(new
            {
                FirstName = "Bob",
                LastName = "Smith"
            });
        }
    }

}