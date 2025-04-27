using System.Text;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ApiBougies.Helpers
{
    public class HelperActionBougies
    {
        private SecretClient secretclient;

        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string SecretKey { get; set; }

        public HelperActionBougies(IConfiguration configuration, SecretClient client)
        {
            this.secretclient = client;
            KeyVaultSecret secretIssuer = this.secretclient.GetSecret("Issuer");
            this.Issuer = secretIssuer.Value;
            KeyVaultSecret secretAudience = this.secretclient.GetSecret("Audience");
            this.Audience = secretAudience.Value;
            KeyVaultSecret secretKey = this.secretclient.GetSecret("SecretKey");
            this.SecretKey = secretKey.Value;    
        }

        //generar token -> secretKey
        public SymmetricSecurityKey GetKeyToken()
        {
            byte[] data = Encoding.UTF8.GetBytes(this.SecretKey);
            return new SymmetricSecurityKey(data);
        }

        //quitar codigo del program
        public Action<JwtBearerOptions> GetJwtBearerOptions()
        {
            Action<JwtBearerOptions> options = new Action<JwtBearerOptions>(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    //validaciones para el token
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = this.Issuer,
                    ValidAudience = this.Audience,
                    IssuerSigningKey = this.GetKeyToken()
                };
            });
            return options;
        }
        public Action<AuthenticationOptions> GetAuthenticateSchema()
        {
            Action<AuthenticationOptions> options = new Action<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            });
            return options;
        }
    }
}
