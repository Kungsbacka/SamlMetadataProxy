using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace SamlMetdataProxy;

public static class MetadataHelpers
{
    public static XmlDocument GetMetadataForSingleEntity(string entityId, XmlDocument metadata)
    {
        if (!IsValidEntityId(entityId))
        {
            throw new ArgumentException("EntityId not valid", nameof(entityId));
        }
        var nodeList = metadata.SelectNodes(
            $"/md:EntitiesDescriptor/md:EntityDescriptor[@entityID='{entityId}']",
            GetNameSpaceManager(metadata)
        );
        if (nodeList == null || nodeList.Count == 0)
        {
            throw new ArgumentException($"EntityDescriptor with entityId {entityId} not found");
        }
        if (nodeList.Count > 1)
        {
            throw new ArgumentException($"More than one EntityDescriptor with entityId {entityId} found");
        }
        if (nodeList[0] is not XmlNode entityNode)
        {
            throw new ArgumentException($"EntityDescriptor with entityId {entityId} not found");
        }
        var newMetadata = new XmlDocument();
        newMetadata.AppendChild(
            newMetadata.CreateXmlDeclaration("1.0", "utf-8", "no")
        );
        newMetadata.AppendChild(
            newMetadata.ImportNode(entityNode, true)
        );
        return newMetadata;
    }

    public static X509Certificate2 GetSigningCertificate(XmlDocument metadata)
    {
        var signatureElement = GetSignatureElement(metadata);
        var certElements = signatureElement.GetElementsByTagName("X509Certificate");
        if (certElements.Count == 0)
        {
            certElements = signatureElement.GetElementsByTagName("ds:X509Certificate");
        }
        if (certElements.Count > 1)
        {
            throw new ArgumentException("More than one Certificate Element found", nameof(metadata));
        }
        var certElement = certElements[0] as XmlElement ??
            throw new ArgumentException("Certificate Element not found", nameof(metadata));

        return new X509Certificate2(System.Text.Encoding.ASCII.GetBytes(certElement.InnerText));
    }

    public static DateTime GetValidUntilDateTime(XmlDocument metadata)
    {
        var entitiesDescriptorElements = metadata.SelectNodes("/md:EntitiesDescriptor", GetNameSpaceManager(metadata));
        if (entitiesDescriptorElements == null)
        {
            throw new ArgumentException("Could not find EntitiesDescriptor Element", nameof(metadata));
        }
        if (entitiesDescriptorElements.Count > 1)
        {
            throw new ArgumentException("More than one EntitiesDescriptor found", nameof(metadata));
        }
        var entitiesDescriptorElement = entitiesDescriptorElements[0] as XmlElement ??
            throw new ArgumentException("EntitiesDescriptor Element not found", nameof(metadata));

        var validUntilAttribute = entitiesDescriptorElement.Attributes["validUntil"] ??
            throw new ArgumentException("ValidUntil Attribute not found");

        if (DateTime.TryParse(validUntilAttribute.Value, out DateTime validUntil))
        {
            return validUntil;
        }
        throw new ArgumentException("Unable to parse validUntil Attribute as DateTime");
    }

    public static XmlDocument GetMetadataFromUri(Uri metadataUri)
    {
        XmlDocument metadata = new()
        {
            PreserveWhitespace = true // Important for signature checking
        };
        metadata.Load(metadataUri.AbsoluteUri);
        return metadata;
    }

    public static void ThrowIfCertificatesAreNotEqual(X509Certificate2 cert1, X509Certificate2 cert2)
    {
        if (!new ReadOnlySpan<byte>(cert1.GetCertHash(HashAlgorithmName.SHA256)).SequenceEqual(
        new ReadOnlySpan<byte>(cert2.GetCertHash(HashAlgorithmName.SHA256))))
        {
            throw new ArgumentException("Certificates are not equal");
        }
    }

    public static void ThrowIfSignatureDoesNotMatch(XmlDocument metadata)
    {
        var signatureElement = GetSignatureElement(metadata);
        SignedXml signedXml = new(metadata);
        signedXml.LoadXml(signatureElement);
        if (!signedXml.CheckSignature())
        {
            throw new ArgumentException("Metadata signature does not match");
        }
    }

    private static XmlElement GetSignatureElement(XmlDocument metadata)
    {
        var signatureElements = metadata.GetElementsByTagName("Signature");
        if (signatureElements.Count == 0)
        {
            signatureElements = metadata.GetElementsByTagName("ds:Signature");
        }
        if (signatureElements.Count == 0)
        {
            throw new ArgumentException("No Signature Element found", nameof(metadata));
        }
        if (signatureElements.Count > 1)
        {
            throw new ArgumentException("More than one Signature Element found", nameof(metadata));
        }
        var signatureElement = signatureElements[0] as XmlElement ??
            throw new ArgumentException("Unable to get Signature Element", nameof(metadata));

        return signatureElement;
    }

    private static XmlNamespaceManager GetNameSpaceManager(XmlDocument metadata)
    {
        XmlNamespaceManager xmlNamespaceManager = new(metadata.NameTable);
        xmlNamespaceManager.AddNamespace("md", "urn:oasis:names:tc:SAML:2.0:metadata");
        return xmlNamespaceManager;
    }

    // According to the SAML 2.0 specification an entity ID should be a valid URI.
    // Not all SPs follow this rule and that is why the validation is relaxed.
    //
    // https://docs.oasis-open.org/security/saml/v2.0/saml-metadata-2.0-os.pdf
    // https://docs.oasis-open.org/security/saml/v2.0/saml-core-2.0-os.pdf
    private static bool IsValidEntityId(string entityId)
    {
        if (string.IsNullOrEmpty(entityId))
        {
            return false;
        }
        int len = entityId.Length;
        if (len > 1024)
        {
            return false;
        }
        for (int i = 0; i < len; i++)
        {
            if (entityId[i] < 32 || entityId[i] > 126)
            {
                return false;
            }
        }
        return true;
    }
}
