// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Phoria.Vite.Logging;

namespace Phoria.Vite.DevServer;

/// <summary>
/// WebSocket bi-directional proxy for Vite HMR.
/// </summary>
/// <param name="logger">The logger service.</param>
internal class ViteDevHmrProxy(ILogger<ViteDevHmrProxy> logger)
{
	internal const string SubProtocol = "vite-hmr";
	private readonly ILogger logger = logger;

	/// <summary>
	/// Proxies the HMR WebSocket request to the Vite Dev Server.
	/// </summary>
	/// <param name="context">The <see cref="HttpContext"/> instance.</param>
	/// <param name="targetUri">Vite server HMR WebSocket address. Must use 'ws' or 'wss' <see cref="Uri.Scheme"/>.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	internal async Task ProxyAsync(
		HttpContext context,
		Uri targetUri,
		CancellationToken cancellationToken
	)
	{
		var clientUri = new Uri(context.Request.GetDisplayUrl());
		logger.LogEstablishingWebSocketProxy(clientUri, targetUri);

		ClientWebSocket? targetWebSocket = null;
		WebSocket? clientWebSocket = null;

		try
		{
			targetWebSocket = new ClientWebSocket();
			targetWebSocket.Options.AddSubProtocol(SubProtocol);
			await targetWebSocket.ConnectAsync(targetUri, cancellationToken);
			if (targetWebSocket.State != WebSocketState.Open)
			{
				throw new WebSocketException(
					WebSocketError.InvalidState,
					$"Target WebSocket's state is {targetWebSocket.State}"
				);
			}

			clientWebSocket = await context.WebSockets.AcceptWebSocketAsync(SubProtocol);
			if (clientWebSocket.State != WebSocketState.Open)
			{
				throw new WebSocketException(
					WebSocketError.InvalidState,
					$"Client WebSocket's state is {clientWebSocket.State}"
				);
			}

			// Two parallel tasks will be used to send and receive data in both directions.
			// The direction to be closed first is undefined, so we introduce a special token to stop the other one.
			var transceiveCancellationTokenSource = new CancellationTokenSource();
			CancellationToken transceiveCancellationToken = CancellationTokenSource
				.CreateLinkedTokenSource(cancellationToken, transceiveCancellationTokenSource.Token)
				.Token;

			Task tcTransceiveTask = Transceive(
				targetWebSocket,
				clientWebSocket,
				transceiveCancellationToken
			);
			Task ctTransceiveTask = Transceive(
				clientWebSocket,
				targetWebSocket,
				transceiveCancellationToken
			);

			try
			{
				// Run until any of them finishes
				await Task.WhenAny(tcTransceiveTask, ctTransceiveTask);
				// Stop the second task if it's still running
				transceiveCancellationTokenSource.Cancel();
				// If none reacted to cancellation, though it's hardly possible irl
				await Task.WhenAll(tcTransceiveTask, ctTransceiveTask);
			}
			catch (TaskCanceledException)
			{
				// Not a problem: thrown in case of cancellation
			}
		}
		catch (Exception e)
		{
			logger.LogFailedToEstablishWebSocketProxy(e.Message);
			context.Abort();
		}
		finally
		{
			await CloseIfOpen(targetWebSocket, targetUri, cancellationToken);
			await CloseIfOpen(clientWebSocket, clientUri, cancellationToken);
			targetWebSocket?.Dispose();
			clientWebSocket?.Dispose();
		}
	}

	private async Task CloseIfOpen(WebSocket? ws, Uri uri, CancellationToken cancellationToken)
	{
		if (ws == null || ws.State != WebSocketState.Open)
			return;

		try
		{
			await ws.CloseAsync(
				WebSocketCloseStatus.NormalClosure,
				string.Empty,
				cancellationToken
			);
		}
		catch (Exception e)
		{
			logger.LogFailedToCloseWebSocket(uri, e.Message);
		}
	}

	private static async Task Transceive(
		WebSocket source,
		WebSocket destination,
		CancellationToken cancellationToken
	)
	{
		byte[] buffer = new byte[1024 * 4];

		WebSocketReceiveResult sourceReceiveResult = await Receive(source);
		while (true)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (
				sourceReceiveResult.CloseStatus.HasValue
				|| destination.State != WebSocketState.Open
			)
				return;

			await Send(sourceReceiveResult, destination);
			sourceReceiveResult = await Receive(source);
		}

		Task<WebSocketReceiveResult> Receive(WebSocket webSocket) =>
			webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

		Task Send(WebSocketReceiveResult result, WebSocket webSocket) =>
			webSocket.SendAsync(
				new ArraySegment<byte>(buffer, 0, result.Count),
				result.MessageType,
				result.EndOfMessage,
				cancellationToken
			);
	}
}