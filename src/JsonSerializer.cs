using System.Text;
using Newtonsoft.Json;

namespace Yuduan.Redis
{
    public class JsonSerializer : ISerializer
    {
        public byte[] Serialize(object item)
        {
            var jsonString = JsonConvert.SerializeObject(item);
            return Encoding.UTF8.GetBytes(jsonString);
        }

        public T Deserialize<T>(byte[] serializedObject)
        {
            if (serializedObject == null)
                return default;

            var jsonString = Encoding.UTF8.GetString(serializedObject);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }
    }
}
