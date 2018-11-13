using Newtonsoft.Json;

namespace SelfService
{
    public class Installation
    {
        public string Id { get; }
        public string Street { get; }
        public string PostalCode { get; }
        public string City { get; }

        [JsonConstructor]
        public Installation(
            string installationId, 
            string street, 
            string number, 
            string letter, 
            string postalCode, 
            string city)
        {
            Id = installationId;
            Street = $"{street} {number}{letter ?? string.Empty}";
            PostalCode = postalCode;
            City = city;
        }
    }
}