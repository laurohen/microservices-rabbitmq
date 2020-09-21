using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using Serilog.Events;

namespace RProducer
{
    class Program
    {
        static void Main(string[] args)
        {
            int caseSwitch;
            string userInput;
            do
            {
                Console.Clear();
                Console.WriteLine("Select a Option:  ");
                Console.WriteLine("1 - Send ");
                Console.WriteLine("2 - Receive ");
                Console.WriteLine("3 - Exit ");

                userInput = Console.ReadLine();
                caseSwitch = Convert.ToInt32(userInput);
                Console.WriteLine(caseSwitch);
            } while (!(caseSwitch < 4));
            
            switch (caseSwitch)
            {
                case 1:
                    Console.Clear();
                    ProduceMsg();
                    break;
                case 2:
                    Console.Clear();
                    ReceiveMsg();
                    break;
            }
        }

        static void ProduceMsg()
        {
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.RollingFile(@"log-{Date}.txt", outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}")
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();
            try
            {
                Log.Information(messageTemplate: "Starting the Program");
                var factory = new ConnectionFactory() { HostName = "localhost" };
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "qwebapp",
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    String UUID = Guid.NewGuid().ToString();
                    while (true)
                    {
                        Random randNum = new Random();

                        string message = $"ID: {UUID} Message: Hello World! Req.: {randNum.Next()} Timestamp: {DateTime.Now}";
                        var body = Encoding.UTF8.GetBytes(message);

                        channel.BasicPublish(exchange: "",
                                             routingKey: "qwebapp",
                                             basicProperties: null,
                                             body: body);
                        Console.WriteLine(" [x] Sent {0}", message);

                        System.Threading.Thread.Sleep(5000);
                    }

                }
            }
            catch (Exception ex)
            {
                Log.Fatal(messageTemplate: "Something went wrong");
                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
        static void ReceiveMsg()
        {
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.RollingFile(@"log-{Date}.txt", outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}")
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

            try
            {
                Log.Information(messageTemplate: "Starting the Program");

                var factory = new ConnectionFactory() { HostName = "localhost" };
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "qwebapp",
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        try
                        {
                            Log.Information(messageTemplate: "Receiving the messages");
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);
                            Console.WriteLine(" [x] Received {0}", message);

                            channel.BasicAck(ea.DeliveryTag, false);
                        }
                        catch (Exception ex)
                        {
                            // log ex
                            Log.Fatal(ex, messageTemplate: "Something went wrong with delivery, doing a Nack");
                            channel.BasicNack(ea.DeliveryTag, false, false);
                        }
                    };
                    channel.BasicConsume(queue: "qwebapp",
                                         autoAck: false,
                                         consumer: consumer);

                    Console.WriteLine(" Press [enter] to exit.");
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                // log ex
                Log.Fatal(messageTemplate: "Something went wrong");
                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
