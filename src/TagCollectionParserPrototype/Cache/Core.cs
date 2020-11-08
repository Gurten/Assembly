/// The Tag Collection Parser Prototype Project
/// Author: Gurten
namespace TagCollectionParserPrototype.Cache.Core
{
    public interface ICacheContext
    {
        Blamite.IO.Endian Endian { get; }
        T Get<T>();
    }

    public struct ConfigConstant<T>
    {
        public ConfigConstant(T val)
        {
            Value = val;
        }

        public static implicit operator T(ConfigConstant<T> value)
        {
            return value.Value;
        }

        public T Value { get; private set; }

    }
}
