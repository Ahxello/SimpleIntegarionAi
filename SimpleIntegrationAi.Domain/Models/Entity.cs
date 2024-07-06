using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleIntegrationAi.Domain.Models
{
    public class Entity
    {
        public string Name { get; set; }
        public List<string> Fields { get; set; }
        public List<Dictionary<string, string>> Data { get; set; }

        public string GroupName { get; set; }
        public Relationship Relationship { get; set; }
    }
}
