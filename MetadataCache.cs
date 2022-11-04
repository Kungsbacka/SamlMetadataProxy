namespace SamlMetdataProxy;

using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

public class MetadataCache : IMetadataCache
{
    private readonly IMemoryCache _cache;
    private readonly Dictionary<string, (string CertificatePath, Uri MetadataUri, bool SkipSignatureValidation)> _federations
        = new(StringComparer.OrdinalIgnoreCase);

    public MetadataCache(IMemoryCache memoryCache)
    {
        _cache = memoryCache;
    }

    public void AddFederation(string federationName, string certificatePath, Uri metadataUri, bool skipSignatureValidation)
    {
        if (_federations.ContainsKey(federationName))
        {
            throw new ArgumentException("Metadata cache already contains a federation with the same name.", nameof(federationName));
        }
        _federations.Add(federationName, (certificatePath, metadataUri, skipSignatureValidation));
    }

    public XmlDocument GetMetadata(string federationName)
    {
        if (_federations.TryGetValue(federationName, out var federation))
        {
            return InternalGetMetadata(federation.CertificatePath, federation.MetadataUri, federation.SkipSignatureValidation);
        }
        throw new ArgumentException("Federatíon not registered", nameof(federationName));
    }

    private XmlDocument InternalGetMetadata(string certificatePath, Uri metadataUri, bool skipSignatureValidation)
    {
        if (_cache.TryGetValue(metadataUri.AbsoluteUri, out object cachedMetadata))
        {
            return (XmlDocument)cachedMetadata;
        }
        var metadata = GetAndValidateMetadataFromSource(certificatePath, metadataUri, skipSignatureValidation);
        var validUntil = MetadataHelpers.GetValidUntilDateTime(metadata);
        _cache.Set(metadataUri.AbsoluteUri, metadata, new DateTimeOffset(validUntil));
        return metadata;
    }

    private static XmlDocument GetAndValidateMetadataFromSource(string certificatePath, Uri metadataUri, bool skipSignatureValidation)
    {
        var metadata = MetadataHelpers.GetMetadataFromUri(metadataUri);
        // Make sure signing certificate match the trusted certificate before validating signature.
        var signingCertificate = MetadataHelpers.GetSigningCertificate(metadata);
        X509Certificate2 trustedCertificate = new(certificatePath);
        MetadataHelpers.ThrowIfCertificatesAreNotEqual(trustedCertificate, signingCertificate);
        if (!skipSignatureValidation)
        {
            MetadataHelpers.ThrowIfSignatureDoesNotMatch(metadata);
        }
        return metadata;
    }
}
