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
        public string From { get; set; }
        public string To { get; set; }
        public string Type { get; set; }
    }
}