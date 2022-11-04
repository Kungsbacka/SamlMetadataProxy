using System.Xml;

namespace SamlMetdataProxy;

public interface IMetadataCache
{
    public XmlDocument GetMetadata(string federationName);

    public void AddFederation(string federationName, string certificatePath, Uri metadataUri, bool skipSignatureValidation);
}
