namespace SimpleIntegrationAi.Domain.Services;

public interface IDataService<T>
{
    Task <IEnumerable<T>> GetAll();
    Task<T> Get();

}