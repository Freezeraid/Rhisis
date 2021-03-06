﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rhisis.Core.Structures.Configuration;
using Rhisis.Core.Structures.Configuration.World;
using Rhisis.Network.Core;
using Sylver.HandlerInvoker;
using Sylver.Network.Client;
using Sylver.Network.Data;
using System;

namespace Rhisis.World.CoreClient
{
    public sealed class WorldCoreClient : NetClient, IWorldCoreClient
    {
        public const int BufferSize = 128;

        private readonly ILogger<WorldCoreClient> _logger;
        private readonly IHandlerInvoker _handlerInvoker;

        /// <inheritdoc />
        public WorldConfiguration WorldServerConfiguration { get; }

        /// <inheritdoc />
        public CoreConfiguration CoreClientConfiguration { get; }

        /// <inheritdoc />
        public string RemoteEndPoint => Socket.RemoteEndPoint.ToString();

        /// <summary>
        /// Creates a new <see cref="WorldCoreClient"/> instance.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="worldConfiguration">World server configuration.</param>
        /// <param name="coreConfiguration">Core server configuration.</param>
        /// <param name="handlerInvoker">Handler invoker.</param>
        public WorldCoreClient(ILogger<WorldCoreClient> logger, IOptions<WorldConfiguration> worldConfiguration, IOptions<CoreConfiguration> coreConfiguration, IHandlerInvoker handlerInvoker)
        {
            _logger = logger;
            WorldServerConfiguration = worldConfiguration.Value;
            CoreClientConfiguration = coreConfiguration.Value;
            _handlerInvoker = handlerInvoker;
            ClientConfiguration = new NetClientConfiguration(CoreClientConfiguration.Host, CoreClientConfiguration.Port, BufferSize);
        }

        /// <inheritdoc />
        protected override void OnConnected()
        {
            _logger.LogInformation($"{nameof(WorldCoreClient)} connected to core server.");
        }

        /// <inheritdoc />
        protected override void OnDisconnected()
        {
            _logger.LogInformation($"{nameof(WorldCoreClient)} disconnected from core server.");

            // TODO: try to reconnect.
        }

        /// <inheritdoc />
        //protected override void OnSocketError(SocketError socketError)
        //{
        //    this._logger.LogError($"An error occured on {nameof(WorldCoreClient)}: {socketError}");
        //}

        /// <inheritdoc />
        public override void HandleMessage(INetPacketStream packet)
        {
            uint packetHeaderNumber = 0;

            if (Socket == null)
            {
                _logger.LogError($"Skip to handle core packet from server. Reason: {nameof(WorldCoreClient)} is not connected.");
                return;
            }

            try
            {
                packetHeaderNumber = packet.Read<uint>();
                _handlerInvoker.Invoke((CorePacketType)packetHeaderNumber, this, packet);
            }
            catch (ArgumentNullException)
            {
                if (Enum.IsDefined(typeof(CorePacketType), packetHeaderNumber))
                    _logger.LogWarning("Received an unimplemented Core packet {0} (0x{1}) from {2}.", Enum.GetName(typeof(CorePacketType), packetHeaderNumber), packetHeaderNumber.ToString("X4"), RemoteEndPoint);
                else
                    _logger.LogWarning("[SECURITY] Received an unknown Core packet 0x{0} from {1}.", packetHeaderNumber.ToString("X4"), RemoteEndPoint);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"An error occured while handling a core packet.");
                _logger.LogDebug(exception.InnerException?.StackTrace);
            }
        }
    }
}
