using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleIntegrationAi.Domain.Models
{
    public class Product
    {
        public Product(string message)
        {
            Message = message;
        }
        public string Message { get; init; }
    }
}
