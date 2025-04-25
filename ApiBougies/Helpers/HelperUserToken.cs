using System.Security.Claims;
using NugetBougies.Models;
using Newtonsoft.Json;

namespace ApiBougies.Helpers
{
    public class HelperUserToken
    {
        private IHttpContextAccessor contextAccessor;
        public HelperUserToken(IHttpContextAccessor contextAccessor)
        {
            this.contextAccessor = contextAccessor;
        }

        public Usuario GetUser()
        {
            Claim claim = this.contextAccessor.HttpContext.User.FindFirst(x => x.Type == "UserData");
            string json = claim.Value; //-> EmpleadoModel
            string jsonUser = HelperCryptography.DecryptString(json);
            Usuario model = JsonConvert.DeserializeObject<Usuario>(jsonUser);
            return model;
        }

    }
}
