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
        public string Description { get; set; }
        public List<Field> Fields { get; set; }

        public Entity(string name)
        {
            Name = name;
            Fields = new List<Field>();
        }
        public void AddField(string fieldName, string dataType)
        {
            Fields.Add(new Field(fieldName, dataType));
        }
    }
}
