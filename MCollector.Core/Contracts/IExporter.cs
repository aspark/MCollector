namespace MCollector.Core.Contracts
{
    public interface IExporter
    {

        public string Name { get; }

        public Task Start(Dictionary<string, object> args);

        public Task Stop();
    }
}
