﻿using System.Text.Json;
using Newtonsoft.Json;
using SimpleIntegrationAi.Domain.Models;

namespace SimpleIntegrationAi.Domain.Services;

public interface IResponseParser
{
    (List<Entity>, List<Relationship>) Parse (string text);
    List<EntityGroup> ParseGroups (string text, List<Entity> entities);
}