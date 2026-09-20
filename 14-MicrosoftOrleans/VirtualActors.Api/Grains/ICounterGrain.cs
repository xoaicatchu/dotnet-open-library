namespace VirtualActors.Api.Grains;

public interface ICounterGrain : IGrainWithStringKey
{
    Task<int> Increment(int value = 1);
    Task<int> GetCount();
    Task Reset();
}
