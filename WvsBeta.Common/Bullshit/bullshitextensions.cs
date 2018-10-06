using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MySql.Data.MySqlClient;

namespace WvsBeta.Common
{
    public static class BullshitExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (T element in enumerable)
                action(element);
        }

        public static bool TrueForAll<T>(this IEnumerable<T> enumerable, Predicate<T> predicate)
        {
            foreach (T element in enumerable)
            {
                if (!predicate(element))
                    return false;
            }

            return true;
        }

        public static bool Exists<T>(this IEnumerable<T> enumerable, Predicate<T> predicate)
        {
            foreach (T element in enumerable)
            {
                if (predicate(element))
                    return true;
            }

            return false;
        }

        public static T Reduce<T>(this IEnumerable<T> enumerable, Func<T, T, T> reducer)
        {
            return enumerable.Aggregate(enumerable.First(), reducer);
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

        private static Random listRandomizer = new Random();
        public static T RandomElement<T>(this List<T> list)
        {
            return list[(int)(list.Count * listRandomizer.NextDouble())];
        }

        public static T FirstRandom<T>(this IEnumerable<T> enumerable)
        {
            T ret = default(T);

            var list = enumerable.ToList();
            if (list.Count != 0)
                ret = RandomElement(list);

            return ret;
        }

        public static IEnumerable<T> FlatMap<T>(this MySqlDataReader reader, Func<MySqlDataReader, T> mapper)
        {
            List<T> ret = new List<T>();

            while (reader.Read())
            {
                ret.Add(mapper(reader));
            }

            return ret;
        }

        public static T Map<T>(this MySqlDataReader reader, Func<MySqlDataReader, T> mapper)
        {
            if (reader.Read())
            {
                var ret = mapper(reader);
                reader.Close();
                return ret;
            }
            else
                return default(T);
        }
        
        public static bool TryFind<T>(this IEnumerable<T> enumerable, Predicate<T> predicate, Action<T> onFound, Action onNotFound)
        {
            foreach (T element in enumerable)
            {
                if (predicate(element))
                {
                    onFound(element);
                    return true;
                }
            }

            onNotFound();
            return false;
        }
        
    }
}
// joren wuz heer