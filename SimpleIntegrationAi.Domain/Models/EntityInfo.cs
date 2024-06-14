using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleIntegrationAi.Domain.Models
{
    public class EntityInfo
    {
        public string Name { get; set; }
        public List<EntityField> Fields { get; set; }
    }
}
