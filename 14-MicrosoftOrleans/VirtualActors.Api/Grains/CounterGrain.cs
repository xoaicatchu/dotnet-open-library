namespace VirtualActors.Api.Grains;

public class CounterGrain : Grain, ICounterGrain
{
    private int _count = 0;

    public Task<int> Increment(int value = 1)
    {
        _count += value;
        return Task.FromResult(_count);
    }

    public Task<int> GetCount()
    {
        return Task.FromResult(_count);
    }

    public Task Reset()
    {
        _count = 0;
        return Task.CompletedTask;
    }
}
