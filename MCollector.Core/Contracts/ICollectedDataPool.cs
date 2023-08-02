namespace MCollector.Core.Contracts
{
    public interface ICollectedDataPool : IObservable<CollectedData>
    {
        IList<CollectedData> GetData();

        //void Subscribe(Action<CollectedData> callback);

        event EventHandler<IEnumerable<CollectedData>> DataChanged;
    }
}
