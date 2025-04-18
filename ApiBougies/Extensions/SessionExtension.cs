using Newtonsoft.Json;

namespace ApiBougies.Extensions
{
    public static class SessionExtension
    {
        public static void SetObject(this ISession session, string key, object value)
        {
            string json = JsonConvert.SerializeObject(value);
            session.SetString(key, json); //guardamos lo q tenemos
        }

        public static T GetObject<T>(this ISession session, string key)
        {
            string data = session.GetString(key);
            if (data == null)
            {
                return default(T);
            }
            else
            {
                return JsonConvert.DeserializeObject<T>(data); //deserializamos el objeto que tenemos para mostrarlo
            }
        }

        public static void SetDecimal(this ISession session, string key, decimal value)
        {
            session.SetString(key, value.ToString()); // Convertimos el decimal a string y lo guardamos
        }

        // Método para recuperar un valor decimal
        public static decimal GetDecimal(this ISession session, string key)
        {
            string value = session.GetString(key);
            return value == null ? 0 : Convert.ToDecimal(value); // Si no existe, retornamos 0
        }

    }
}
