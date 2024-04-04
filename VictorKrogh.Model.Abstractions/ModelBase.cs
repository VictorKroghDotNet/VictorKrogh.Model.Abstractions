using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace VictorKrogh.Model;

public interface IModel
{
}

public abstract class ModelBase : IModel
{
    private static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>> Properties = new();

    private static IEnumerable<PropertyInfo> GetProperties(Type type)
    {
        return Properties.GetOrAdd(type.TypeHandle, typeHandle =>
        {
            return [.. type.GetProperties()];
        });
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        foreach (var propertyInfo in GetProperties(GetType()))
        {
            var thisValue = propertyInfo.GetValue(this);
            if (thisValue == null)
            {
                continue;
            }

            if (thisValue is IEnumerable thisEnumerableValue && thisValue is not string)
            {
                foreach (var propertyValue in thisEnumerableValue)
                {
                    hashCode.Add(propertyValue);
                }
            }
            else
            {
                hashCode.Add(thisValue);
            }
        }

        return hashCode.ToHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ModelBase model)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        var type = GetType();
        if (type != model.GetType())
        {
            return false;
        }

        foreach (var propertyInfo in GetProperties(type))
        {
            var thisValue = propertyInfo.GetValue(this);
            var objValue = propertyInfo.GetValue(model);

            if (thisValue == null && objValue == null)
            {
                continue;
            }

            if (thisValue == null && objValue != null)
            {
                return false;
            }

            if (thisValue is IEnumerable thisEnumerableValue)
            {
                if (objValue is IEnumerable objEnumerableValue)
                {
                    var enumerableNew = thisEnumerableValue.Cast<object>();
                    var enumerableOld = objEnumerableValue.Cast<object>();

                    if (enumerableOld.SequenceEqual(enumerableNew))
                    {
                        continue;
                    }
                }

                return false;
            }

            if (!thisValue?.Equals(objValue) ?? false)
            {
                return false;
            }
        }

        return true;
    }
}

