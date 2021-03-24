// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We don't want any errors in telemetry to cause failures in the product.")]
    internal class SerializedQueue<T> : IAsyncDisposable
    {
        private readonly Channel<Action<T>> _channel;
        private readonly Task _task;

        public SerializedQueue(Func<T> initializer)
        {
            _channel = Channel.CreateBounded<Action<T>>(new BoundedChannelOptions(10)
            {
                SingleReader = true,
                SingleWriter = false
            });

            _task = Runner(initializer);
        }

        private async Task Runner(Func<T> contextInitializer)
        {
            await Task.Yield();

            T ctx;

            try
            {
                ctx = contextInitializer();
            }
            catch (Exception)
            {
                return;
            }

            while (await _channel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (_channel.Reader.TryRead(out var result))
                {
                    try
                    {
                        result(ctx);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        public void Add(Action<T> action)
        {
            _channel.Writer.TryWrite(action);
        }

        public async ValueTask DisposeAsync()
        {
            _channel.Writer.TryComplete();
            await _task.ConfigureAwait(false);
            _task.Dispose();
        }
    }
}
