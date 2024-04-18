using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Settings;

internal partial class SettingsManager
{
    private static BuilderOutput Setup(
        ILogger logger,
        SettingsTypeInformation[] settingsTypeInformationArray,
        IBaseSettingsStorageBackend[] baseStorageBackendArray,
        IBaseSettingsStorageBackend? defaultBaseStorageBackend)
    {
        var builder = new SettingsBuilder();

        var objectCreationInformationList = new List<ObjectCreationInformation>();
        var storageBackendMappings = new Dictionary<Type, ISettingsStorageBackend>();
        var asyncStorageBackendMappings = new Dictionary<Type, IAsyncSettingsStorageBackend>();
        var propertyBuilderOutputs = new List<PropertyBuilderOutput>();

        foreach (var settingsTypeInformation in settingsTypeInformationArray)
        {
            var (objectType, defaultValue, configureLambda) = settingsTypeInformation;

            try
            {
                configureLambda.Invoke(builder);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception while configuring {Type}", objectType);
            }

            propertyBuilderOutputs.AddRange(builder.PropertyBuilderOutputs);
            var defaultValueFactory = builder.DefaultValueFactory;
            var storageBackendValues = builder.StorageBackendBuilderValues;

            objectCreationInformationList.Add(new ObjectCreationInformation(objectType, defaultValue, defaultValueFactory));
            if (storageBackendValues is not null)
            {
                IBaseSettingsStorageBackend? backend = null;
                if (storageBackendValues.BackendId != SettingsStorageBackendId.DefaultValue)
                {
                    backend = baseStorageBackendArray.FirstOrDefault(x => x.Id == storageBackendValues.BackendId);
                } else if (storageBackendValues.BackendType is not null)
                {
                    backend = baseStorageBackendArray.FirstOrDefault(x => x.GetType().IsAssignableTo(storageBackendValues.BackendType));
                }
                else if (!storageBackendValues.IsDisabled)
                {
                    backend = defaultBaseStorageBackend;
                }

                if (backend is not null)
                {
                    AddBackend(objectType, backend);
                }
            }
            else if (defaultBaseStorageBackend is not null)
            {
                AddBackend(objectType, defaultBaseStorageBackend);
            }

            builder.Reset();
        }

        var objectCreationMappings = objectCreationInformationList.ToImmutableDictionary(x => x.ObjectType, x => x);

        return new BuilderOutput
        {
            ObjectCreationMappings = objectCreationMappings,
            StorageBackendMappings = storageBackendMappings.ToImmutableDictionary(),
            AsyncStorageBackendMappings = asyncStorageBackendMappings.ToImmutableDictionary(),
            PropertyBuilderOutputs = propertyBuilderOutputs.ToArray(),
        };

        void AddBackend(Type objectType, IBaseSettingsStorageBackend backend)
        {
            switch (backend)
            {
                case ISettingsStorageBackend settingsStorageBackend:
                    storageBackendMappings[objectType] = settingsStorageBackend;
                    break;
                case IAsyncSettingsStorageBackend asyncSettingsStorageBackend:
                    asyncStorageBackendMappings[objectType] = asyncSettingsStorageBackend;
                    break;
                default:
                    throw new UnreachableException();
            }
        }
    }

    internal record BuilderOutput
    {
        public required ImmutableDictionary<Type, ObjectCreationInformation> ObjectCreationMappings { get; init; }
        public required ImmutableDictionary<Type, ISettingsStorageBackend> StorageBackendMappings { get; init; }
        public required ImmutableDictionary<Type, IAsyncSettingsStorageBackend> AsyncStorageBackendMappings { get; init; }
        public required PropertyBuilderOutput[] PropertyBuilderOutputs { get; init; }
    }
}
