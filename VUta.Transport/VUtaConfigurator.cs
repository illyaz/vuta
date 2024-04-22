using MassTransit;

namespace VUta.Transport;

public static class VUtaConfigurator
{
    public static void Configure(
        IBusRegistrationConfigurator massTransit)
    {
        massTransit.SetEndpointNameFormatter(VUtaEndpointNameFormatter.Instance);
        massTransit.AddDelayedMessageScheduler();
    }

    public static void Configure(
        IBusRegistrationContext context,
        IRabbitMqBusFactoryConfigurator rabbit)
    {
        rabbit.UseDelayedRedelivery(r => r.Interval(10, TimeSpan.FromSeconds(30)));
        rabbit.UseRetry(r => r.Immediate(3));
        rabbit.UseDelayedMessageScheduler();
        rabbit.ConfigureEndpoints(context, VUtaEndpointNameFormatter.Instance);

        rabbit.MessageTopology.SetEntityNameFormatter(new VUtaEntityNameFormatter());
    }
}