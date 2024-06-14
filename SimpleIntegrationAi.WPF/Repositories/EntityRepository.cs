using System.Collections.ObjectModel;
using SimpleIntegrationAi.Domain.Repositories;

namespace SimpleIntegrationAi.WPF.Repositories;

public class EntityRepository<T> : IEntityRepository<T>
{
    public ObservableCollection<T> AddEntity(string userInput)
    {
        throw new NotImplementedException();
    }
}