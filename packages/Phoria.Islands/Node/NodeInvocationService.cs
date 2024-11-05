namespace Phoria.Islands.Node;

public interface INodeInvocationService
{
	Task<HttpResponseMessage> Invoke(string function, object[] args, CancellationToken cancellationToken = default);
}
