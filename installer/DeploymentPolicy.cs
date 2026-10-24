using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Xml;
using System.Xml.Linq;

namespace AndroidLive.Setup
{
    internal static class DeploymentPolicy
    {
        internal static bool IsServiceAccount()
        {
            using(var identity=WindowsIdentity.GetCurrent())
                return identity.User==null || identity.User.IsWellKnown(WellKnownSidType.LocalSystemSid) ||
                    identity.User.IsWellKnown(WellKnownSidType.LocalServiceSid) || identity.User.IsWellKnown(WellKnownSidType.NetworkServiceSid);
        }
        internal static XDocument ReadManifest(string path)
        {
            using(var reader=XmlReader.Create(path,new XmlReaderSettings{
                DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=2*1024*1024}))
                return XDocument.Load(reader);
        }
        internal static bool MeetsVersion(string path,string minimum)
        {
            try {
                var identity=ReadManifest(path).Root.Elements().FirstOrDefault(e=>e.Name.LocalName=="assemblyIdentity");
                Version actual,expected;
                return identity!=null && Version.TryParse((string)identity.Attribute("version"),out actual) &&
                    Version.TryParse(minimum,out expected) && actual>=expected;
            }catch(IOException){return false;}catch(XmlException){return false;}catch(UnauthorizedAccessException){return false;}
        }
        internal static void RequireManagedTrust(string path)
        {
            // IT must provision trust. Never add certificates or weaken Office security here.
            var certificate=ReadManifest(path).Descendants().FirstOrDefault(e=>e.Name.LocalName=="X509Certificate");
            if(certificate==null)throw new SetupFailure(1625,"Pakken mangler et udgivercertifikat.");
            using(var signer=new X509Certificate2(Convert.FromBase64String(certificate.Value))) {
                bool publisher=false;
                foreach(var location in new[]{StoreLocation.CurrentUser,StoreLocation.LocalMachine})
                using(var store=new X509Store(StoreName.TrustedPublisher,location)) {
                    store.Open(OpenFlags.ReadOnly);
                    publisher|=store.Certificates.Find(X509FindType.FindByThumbprint,signer.Thumbprint,false).Count>0;
                }
                using(var chain=new X509Chain()) {
                    chain.ChainPolicy.RevocationMode=X509RevocationMode.Online;
                    chain.ChainPolicy.UrlRetrievalTimeout=TimeSpan.FromSeconds(15);
                    if(!publisher||!chain.Build(signer))
                        throw new SetupFailure(1625,"IT skal godkende pakkens udgivercertifikat og certifikatkæde før lydløs installation. Den eksisterende udgave er ikke fjernet.");
                }
            }
        }
    }
}