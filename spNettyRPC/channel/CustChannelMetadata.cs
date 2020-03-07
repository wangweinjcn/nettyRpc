// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Transport.Channels
{
    using System.Diagnostics.Contracts;
    using System.Collections.Concurrent;
    using NettyRPC.Util;

    /// <summary>Represents the properties of a <see cref="IChannel" /> implementation.</summary>
    public sealed class CustChannelMetadata
    {
        /// <summary>Create a new instance</summary>
        /// <param name="hasDisconnect">
        ///     <c>true</c> if and only if the channel has the <c>DisconnectAsync()</c> operation
        ///     that allows a user to disconnect and then call <see cref="IChannel.ConnectAsync(System.Net.EndPoint)" />
        ///     again, such as UDP/IP.
        /// </param>
        public CustChannelMetadata(bool hasDisconnect)
            : this(hasDisconnect, 1)
        {
        }
        public DefaultTag tags = new DefaultTag();
        /// <summary>Create a new instance</summary>
        /// <param name="hasDisconnect">
        ///     <c>true</c> if and only if the channel has the <c>DisconnectAsync</c> operation
        ///     that allows a user to disconnect and then call <see cref="IChannel.ConnectAsync(System.Net.EndPoint)" />
        ///     again, such as UDP/IP.
        /// </param>
        /// <param name="defaultMaxMessagesPerRead">
        ///     If a <see cref="IMaxMessagesRecvByteBufAllocator" /> is in use, then this value will be
        ///     set for <see cref="IMaxMessagesRecvByteBufAllocator.MaxMessagesPerRead" />. Must be <c> &gt; 0</c>.
        /// </param>
        public CustChannelMetadata(bool hasDisconnect, int defaultMaxMessagesPerRead)
        {
            Contract.Requires(defaultMaxMessagesPerRead > 0);
            this.HasDisconnect = hasDisconnect;
            this.DefaultMaxMessagesPerRead = defaultMaxMessagesPerRead;
        }

        /// <summary>
        ///     Returns <c>true</c> if and only if the channel has the <c>DisconnectAsync()</c> operation
        ///     that allows a user to disconnect and then call <see cref="IChannel.ConnectAsync(System.Net.EndPoint)" /> again,
        ///     such as UDP/IP.
        /// </summary>
        public bool HasDisconnect { get; }

        /// <summary>
        ///     If a <see cref="IMaxMessagesRecvByteBufAllocator" /> is in use, then this is the default value for
        ///     <see cref="IMaxMessagesRecvByteBufAllocator.MaxMessagesPerRead" />.
        /// </summary>
        public int DefaultMaxMessagesPerRead { get; }
    }
}