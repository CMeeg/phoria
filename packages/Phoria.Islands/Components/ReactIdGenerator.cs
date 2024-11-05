/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Phoria.Islands.Components;

public interface IReactIdGenerator
{
	/// <summary>
	/// Returns a short react identifier starts with "react_".
	/// </summary>
	/// <returns></returns>
	string Generate();
}

/// <summary>
/// React ID generator.
/// </summary>
public sealed class ReactIdGenerator : IReactIdGenerator
{
	private static readonly string encode32Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUV";

	private static long random = DateTime.UtcNow.Ticks;

	private static readonly char[] reactPrefix = "react_".ToCharArray();

	/// <summary>
	/// "react_".Length = 6 + 13 random symbols
	/// </summary>
	private const int ReactIdLength = 19;

	[ThreadStatic]
	private static char[]? chars;

	/// <summary>
	/// Returns a short react identifier starts with "react_".
	/// </summary>
	/// <returns></returns>
	public string Generate()
	{
		char[]? genChars = chars;
		if (genChars == null)
		{
			chars = genChars = new char[ReactIdLength];
			Array.Copy(reactPrefix, 0, genChars, 0, reactPrefix.Length);
		}

		long id = Interlocked.Increment(ref random);

		// from 6 because  "react_".Length == 6, _encode32Chars.Length == 32 (base32),
		// base32 characters are 5 bits in length and from long (64 bits) we can get 13 symbols
		genChars[6] = encode32Chars[(int)(id >> 60) & 31];
		genChars[7] = encode32Chars[(int)(id >> 55) & 31];
		genChars[8] = encode32Chars[(int)(id >> 50) & 31];
		genChars[9] = encode32Chars[(int)(id >> 45) & 31];
		genChars[10] = encode32Chars[(int)(id >> 40) & 31];
		genChars[11] = encode32Chars[(int)(id >> 35) & 31];
		genChars[12] = encode32Chars[(int)(id >> 30) & 31];
		genChars[13] = encode32Chars[(int)(id >> 25) & 31];
		genChars[14] = encode32Chars[(int)(id >> 20) & 31];
		genChars[15] = encode32Chars[(int)(id >> 15) & 31];
		genChars[16] = encode32Chars[(int)(id >> 10) & 31];
		genChars[17] = encode32Chars[(int)(id >> 5) & 31];
		genChars[18] = encode32Chars[(int)id & 31];

		return new string(genChars, 0, ReactIdLength);
	}
}
