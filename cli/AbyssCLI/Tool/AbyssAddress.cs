using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices.JavaScript;

namespace AbyssCLI.Tool
{
    internal class AbyssAddress
    {
        public enum EScheme
        {
            WWW,
            Abyss,
            Abyst
        }
        public AbyssAddress(string address)
        {
            String = address;
            if (address.StartsWith("http:") || address.StartsWith("https:") || address.StartsWith("www."))
            {
                if (address.StartsWith("www."))
                    address = "https:" + address;
                Scheme = EScheme.WWW;
                WebAddress = new Uri(address);
                Base = WebAddress.Scheme + ':' + WebAddress.Host;
                return;
            }
            else if (address.StartsWith("abyss:") || address.StartsWith("abyst:"))
            {
                Scheme = address.StartsWith("abyss:") ? EScheme.Abyss : EScheme.Abyst;
                var no_scheme = address[6..];
                var end = no_scheme.IndexOfAny([':', '/']);
                if (end == -1)
                {
                    PeerHash = no_scheme;
                }
                else
                {
                    PeerHash = no_scheme[..end];
                }
                Base = address[..6] + PeerHash;
                return;
            }
            throw new Exception("failed to parse AbyssAddress: invalid scheme");
        }
        public bool TryParseMaybeRelativeAddress(string address, out AbyssAddress result)
        {
            result = null;
            try
            {
                result = new AbyssAddress(address);
                return true;
            }
            catch
            {
                if (address.Contains(':'))
                    return false;

                //consider as local address.
                result = new AbyssAddress(GetRelativeAddress(address));
                return true;
            }
        }
        public string GetRelativeAddress(string path)
        {
            return Base + (path[0] == '/' ? path : ('/' + path));
        }
        public readonly string String;
        public readonly EScheme Scheme;
        public readonly Uri WebAddress;
        public readonly string PeerHash;
        public readonly string Base;
    }
}
