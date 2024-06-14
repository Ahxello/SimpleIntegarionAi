using System.Collections.ObjectModel;

namespace SimpleIntegrationAi.Domain.Repositories;

public interface IEntityRepository<T>
{
    ObservableCollection<T> AddEntity(string userInput);
}