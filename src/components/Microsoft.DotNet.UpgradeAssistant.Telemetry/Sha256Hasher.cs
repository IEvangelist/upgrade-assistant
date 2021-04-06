// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    public class Sha256Hasher : IStringHasher
    {
        // We use a Lazy<> as otherwise there's a circular dependency
        private readonly Lazy<IPropertyRetriever> _properties;

        public Sha256Hasher(Lazy<IPropertyRetriever> properties)
        {
            _properties = properties;
        }

        /// <summary>
        /// The hashed mac address needs to be the same hashed value as produced by the other distinct sources given the same input. (e.g. VsCode).
        /// </summary>
        public virtual string Hash(string text)
        {
            using var sha256 = SHA256.Create();
            return HashInFormat(sha256, text);
        }

        public string HashFilePath(string filePath)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            using var incremental = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

#pragma warning disable CA1308 // Normalize strings to uppercase
            var path = Encoding.Unicode.GetBytes(filePath.ToLowerInvariant());
#pragma warning restore CA1308 // Normalize strings to uppercase

            var fromMachineId = GetFileSafeGUID(incremental, path, _properties.Value.MachineId);
            var result = GetFileSafeGUID(incremental, path, fromMachineId);

            return result.ToString();

            static Guid GetFileSafeGUID(IncrementalHash incremental, byte[] path, Guid guid)
            {
                incremental.AppendData(path);
                incremental.AppendData(guid.ToByteArray());

                var hash = incremental.GetHashAndReset();

                // Grab the first 16 bytes of the hash to create a GUID
                var bytes = new byte[16];
                Array.Copy(hash, bytes, 16);

                return new Guid(bytes);
            }
        }

        private static string HashInFormat(SHA256 sha256, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var hash = sha256.ComputeHash(bytes);
            var hashString = new StringBuilder();

            foreach (var x in hash)
            {
                hashString.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", x);
            }

            return hashString.ToString();
        }
    }
}
