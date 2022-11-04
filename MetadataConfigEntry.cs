namespace SamlMetdataProxy
{
    public class MetadataConfigEntry
    {
        public string? Name { get; set; }
        public string? MetadataUri { get; set; }
        public string? TrustedSigningCertificate { get; set; }
        public bool? SkipSignatureValidation { get; set; }
    }
}
