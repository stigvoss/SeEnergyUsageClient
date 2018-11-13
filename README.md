# Syd Energi Self-Service Client
Client for self-service APIs of danish Syd Energi.

# Usage

```c#
using(var client = new SeSelfServiceClient(userName, password))
{
    await client.OpenAsync();

    var from = new DateTime(2018, 10, 1);
    var to = from.AddMonths(1);
    var period = TimePeriods.PeriodType.Days;

    var customers = await client.CustomersAsync();

    foreach (var customer in customers)
    {
        foreach (var installation in customer.Installations)
        {
            var usages = await client.UsageAsync(customer.Id, installation.Id, from, to, period);
            Console.WriteLine(
                $@"{customer.Name} 
                    has used {usages.Sum(e => e.Value):#.00} kWh 
                    at {installation.Street} 
                    in {from.ToString("MMMM", CultureInfo.InvariantCulture)}.");
        }
    }
}
```
