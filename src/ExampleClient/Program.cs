using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SelfService;

namespace ExampleClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var userName = args.FirstOrDefault(e => e.StartsWith("--username="))?.Split('=')[1];
            var password = args.FirstOrDefault(e => e.StartsWith("--password="))?.Split('=')[1];

            try
            {
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
                            Console.WriteLine($"{customer.Name} has used {usages.Sum(e => e.Value):#.00} kWh at {installation.Street} in {from.ToString("MMMM", CultureInfo.InvariantCulture)}.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}