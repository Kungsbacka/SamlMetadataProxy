using System.Xml;

namespace SamlMetdataProxy
{
    public static class AppHelpers
    {
        public static async Task<IResult> GetStreamResult(IMetadataCache cache, string federation, string entityId)
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

        public static void AddFederationFromConfig(this IMetadataCache cache, ConfigurationManager config, string federation)
        {
            string certificatePath = config[$"Metadata:{federation}:TrustedSigningCertificate"] ??
                throw new InvalidOperationException($"TrustedSigningCertificate for federation {federation} is missing in config.");
            string metadataUri = config[$"Metadata:{federation}:MetadataUri"] ??
                throw new InvalidOperationException($"MetadataUri for federation {federation} is missing in config.");

            if (!Uri.TryCreate(metadataUri, UriKind.Absolute, out Uri? uri))
            {
                throw new InvalidOperationException($"MetadataUri for federation {federation} is not a valid URI");
            }
            cache.AddFederation(federation, certificatePath, uri);
        }
    }
}