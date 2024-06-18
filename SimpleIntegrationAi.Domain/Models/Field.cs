using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleIntegrationAi.Domain.Models
{
    public class Field
    {
        public string Name { get; set; }
        public string Type { get; set; }

        public Field(string name, string type)
        {
            Name = name;
            Type = type;
        }
    }
}
