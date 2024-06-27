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
        public string From { get; set; }
        public string To { get; set; }
        public string ForeignKey { get; set; }
        public string ParentKey { get; set; }
        public RelationshipType Type { get; set; }
    }
}