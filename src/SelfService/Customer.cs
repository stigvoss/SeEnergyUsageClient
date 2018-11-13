using System.Collections.Generic;
using Newtonsoft.Json;

namespace SelfService
{
    public class Customer
    {
        public string Id { get; }
        public IEnumerable<Installation> Installations { get; } = new Installation[0];
        public string Name { get; }

        [JsonConstructor]
        public Customer(string customerNumber, string fullName, IEnumerable<Installation> addresses)
        {
            Name = fullName;
            Installations = addresses;
            Id = customerNumber;
        }
    }
}