namespace Yuduan.Redis
{
    public interface ISerializer
    {
        byte[] Serialize(object item);

        T Deserialize<T>(byte[] serializedObject);
    }
}
