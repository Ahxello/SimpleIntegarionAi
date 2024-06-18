using SimpleIntegrationAi.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleIntegrationAi.Domain.Models
{
    public class Relationship
    {
        public string Entity1 { get; set; } 
        public string Entity2 { get; set; } 
        public string Type { get; set; }

        public Relationship(string entity1, string entity2, string type)
        {
            Entity1 = entity1;
            Entity2 = entity2;
            Type = type;
            
        }
    }
}