using System.Xml;

namespace SamlMetdataProxy
{
    public static class AppHelpers
    {
        public static async Task<IResult> GetStreamResultAsync(IMetadataCache cache, string federation, string entityId)
        {
            var aggregatedMetadata = cache.GetMetadata(federation);
            var singleEntityMetadata = MetadataHelpers.GetMetadataForSingleEntity(entityId, aggregatedMetadata);
            MemoryStream ms = new();
            XmlWriterSettings settings = new()
            {
                Async = true
            };
            var xw = XmlWriter.Create(ms, settings);
            singleEntityMetadata.Save(xw);
            await xw.FlushAsync();
            await xw.DisposeAsync();
            ms.Position = 0;
            // MemoryStream is disposed automatically after the response is sent
            return Results.Stream(ms, "application/samlmetadata+xml");
        }

        public static void AddFederationsFromConfig(this IMetadataCache cache, ConfigurationManager config)
        {
            foreach (var md in config.GetSection("Metadata").Get<IEnumerable<MetadataConfigEntry>>())
            {
                string metadataUri = md.MetadataUri ??
                    throw new InvalidOperationException($"MetadataUri is missing in metadata section in config.");
                string name = md.Name ??
                    throw new InvalidOperationException("Name is missing in metadata section in config.");
                string certificatePath = md.TrustedSigningCertificate ??
                    throw new InvalidOperationException($"TrustedSigningCertificate is missing in metadata section in config.");

                if (!Uri.TryCreate(metadataUri, UriKind.Absolute, out Uri? uri))
                {
                    throw new InvalidOperationException($"MetadataUri is not a valid URI in metadata section in config.");
                }
                bool skipSignatureValidation = md.SkipSignatureValidation != null && md.SkipSignatureValidation.Value;
                cache.AddFederation(name, certificatePath, uri, skipSignatureValidation);
            }
        }
    }
}