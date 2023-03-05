using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GDC = Godot.Collections;

namespace NakamaWebRTC
{
    public static class Utils
    {
        public static StreamPeerBuffer ToBuffer(this byte[] byteArray)
        {
            var buffer = new StreamPeerBuffer();
            buffer.DataArray = byteArray;
            return buffer;
        }

        public static byte[] Serialize(this IEnumerable<IBufferSerializable> serializableArray)
        {
            var buffer = new StreamPeerBuffer();
            buffer.Put32(serializableArray.Count());
            foreach (var serializable in serializableArray)
                buffer.PutData(serializable.Serialize());
            return buffer.DataArray;
        }

        public static byte[] Serialize(this IBufferSerializable serializable)
        {
            var buffer = new StreamPeerBuffer();
            serializable.Serialize(buffer);
            return buffer.DataArray;
        }

        public static T[] DeserializeArray<T>(this byte[] byteArray) where T : IBufferSerializable, new()
        {
            var buffer = new StreamPeerBuffer();
            buffer.DataArray = byteArray;
            T[] array = new T[buffer.Get32()];
            for (int i = 0; i < array.Length; i++)
                array[i] = buffer.GetSerializable<T>();
            return array;
        }

        public static T Deserialize<T>(this byte[] byteArray) where T : IBufferSerializable, new()
        {
            var buffer = new StreamPeerBuffer();
            buffer.DataArray = byteArray;
            return buffer.GetSerializable<T>();
        }

        public static void PutSerializable(this StreamPeerBuffer buffer, IBufferSerializable serializable)
        {
            serializable.Serialize(buffer);
        }

        public static T GetSerializable<T>(this StreamPeerBuffer buffer) where T : IBufferSerializable, new()
        {
            T inst = new T();
            inst.Deserialize(buffer);
            return inst;
        }

        public static T Get<T>(this GDC.Dictionary dictionary, object key, T defaultReturn = default)
        {
            if (dictionary.Contains(key))
                return (T)dictionary[key];
            return defaultReturn;
        }

        public static T Get<T>(this GDC.Dictionary dictionary, string key, T defaultReturn = default)
        {
            var keys = key.Split(".");
            for (int i = 0; i < keys.Length; i++)
            {
                if (i == keys.Length - 1)
                {
                    if (dictionary.Contains(key))
                        return (T)dictionary[key];
                    return defaultReturn;
                }
                dictionary = dictionary.Get<GDC.Dictionary>(keys[i]);
                if (dictionary == null)
                    return defaultReturn;
            }
            return defaultReturn;
        }

        public static GDC.Dictionary ToGDDict(this object obj)
        {
            GDC.Dictionary dict = new GDC.Dictionary();
            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                dict[prop.Name] = prop.GetValue(obj, null);
            }
            return dict;
        }

        public static GDC.Array<T> ToGDArray<T>(this IEnumerable<T> array)
        {
            var gdArray = new GDC.Array<T>();
            foreach (var eleme in array)
                gdArray.Add(eleme);
            return gdArray;
        }

        public static GDC.Array GDParams(params object[] array)
        {
            var gdArray = new GDC.Array();
            foreach (var eleme in array)
                gdArray.Add(eleme);
            return gdArray;
        }

        public static object[] Params(params object[] array) => array;
    }
}