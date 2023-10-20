using Autodesk.Connectivity.WebServicesTools;
using System;
using VDF = Autodesk.DataManagement.Client.Framework;

namespace VaultDataCrawlerNF
{
    internal class AuthWindows
    {
        public static WebServiceManager loginWindows()
        {
            WebServiceManager mVault = null;
            VDF.Vault.Results.LogInResult result = null;
            try
            {
                result = VDF.Vault.Library.ConnectionManager.LogIn("Vault-B", "VAULT", string.Empty, string.Empty, VDF.Vault.Currency.Connections.AuthenticationFlags.ReadOnly | VDF.Vault.Currency.Connections.AuthenticationFlags.WindowsAuthentication, null);
                //Console.WriteLine(result.Success);
                var mConn = result.Connection;
                mVault = mConn.WebServiceManager;
            }
            catch (Exception)
            {

                throw;
            }
            return mVault;
            //mVault.Dispose();
        }
    }
}
