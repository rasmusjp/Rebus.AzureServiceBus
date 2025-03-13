﻿using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Messaging.ServiceBus;
using Rebus.Extensions;

namespace Rebus.AzureServiceBus;

class ConnectionStringParser
{
    readonly Dictionary<string, string> _parts;

    public string ConnectionString { get; }

    public ConnectionStringParser(string endpoint, string sharedAccessKeyName, string sharedAccessKey, string entityPath)
    {
        _parts = new Dictionary<string, string>
        {
            {"Endpoint", endpoint},
            {"SharedAccessKeyName", sharedAccessKeyName},
            {"SharedAccessKey", sharedAccessKey},
            {"EntityPath", entityPath}
        };
    }

    public ConnectionStringParser(string connectionString)
    {
        ConnectionString = connectionString;

        _parts = connectionString.Split(';')
            .Select(token => token.Trim())
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .Select(token =>
            {
                var index = token.IndexOf('=');

                if (index < 0) throw new FormatException($"Could not interpret '{token}' as a key-value pair");

                return new
                {
                    key = token.Substring(0, index),
                    value = token.Substring(index + 1)
                };
            })
            .ToDictionary(a => a.key, a => a.value);
    }

    public bool UseDevelopmentEmulator => _parts.GetValue("UseDevelopmentEmulator") == "true";
    public string Endpoint => _parts.GetValue("Endpoint").TrimEnd('/');
    public string SharedAccessKeyName => _parts.GetValue("SharedAccessKeyName");
    public string SharedAccessKey => _parts.GetValue("SharedAccessKey");
    public string EntityPath => _parts.GetValueOrNull("EntityPath");
    public ServiceBusTransportType Transport => (ServiceBusTransportType)Enum.Parse(typeof(ServiceBusTransportType), _parts.GetValueOrNull("TransportType") ?? nameof(ServiceBusTransportType.AmqpTcp));

    public bool Contains(string name, string value, StringComparison comparison) => _parts.Any(p => string.Equals(p.Key, name, comparison) && string.Equals(p.Value, value, comparison));

    public override string ToString()
    {
        return $@"{ConnectionString}
           Endpoint: {Endpoint}
SharedAccessKeyName: {SharedAccessKeyName}
    SharedAccessKey: {SharedAccessKey}";
    }

    public string GetConnectionStringWithoutEntityPath() => string.Join(";", _parts.Where(p => !string.Equals(p.Key, "EntityPath")).Select(kvp => $"{kvp.Key}={kvp.Value}"));

    public string GetConnectionString() => string.Join(";", _parts.Select(kvp => $"{kvp.Key}={kvp.Value}"));
}
