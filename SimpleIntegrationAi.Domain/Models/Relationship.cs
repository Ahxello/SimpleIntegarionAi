using SimpleIntegrationAi.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleIntegrationAi.Domain.Models
{
    public enum RelationshipType
    {
        OneToOne,
        OneToMany,
        ManyToOne,
        ManyToMany,
    }
    public class Relationship
    {
        public string FromTable { get; set; }
        public string ToTable { get; set; }
        public string FromField { get; set; }
        public string ToField { get; set; }
        public RelationshipType Type { get; set; }
    }
}