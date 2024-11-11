using System.Collections;

namespace Phoria.Vite;

public interface IViteSsrManifest
	: IEnumerable<string[]>
{
	string[]? this[string key] { get; }
	IEnumerable<string> Keys { get; }
	bool ContainsKey(string key);
}

public sealed class ViteSsrManifest
	: IViteSsrManifest
{
	private readonly IReadOnlyDictionary<string, string[]> files;

	public string[]? this[string key]
	{
		get
		{
			if (!files.TryGetValue(key, out string[]? file))
			{
				return null;
			}

			return file;
		}
	}

	IEnumerable<string> IViteSsrManifest.Keys => files.Keys;

	public ViteSsrManifest()
	{
		files = new Dictionary<string, string[]>();
	}

	public ViteSsrManifest(IReadOnlyDictionary<string, string[]> files)
	{
		this.files = files;
	}

	IEnumerator<string[]> IEnumerable<string[]>.GetEnumerator()
	{
		return files.Values.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() => files.Values.GetEnumerator();

	bool IViteSsrManifest.ContainsKey(string key) => files.ContainsKey(key);
}
