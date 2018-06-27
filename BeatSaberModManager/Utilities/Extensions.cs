using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BeatSaberModManager.Utilities
{
    public static class Extensions
    {
        public static IEnumerable<IEnumerable<T>> Transpose<T>(this IEnumerable<IEnumerable<T>> source)
        {
            var enumerators = source.Select(e => e.GetEnumerator()).ToArray();
            try
            {
                while (enumerators.All(e => e.MoveNext()))
                {
                    yield return enumerators.Select(e => e.Current).ToArray();
                }
            }
            finally
            {
                Array.ForEach(enumerators, e => e.Dispose());
            }
        }
        public static string FindCommonPrefix(this IEnumerable<string> strings) =>
            new string(strings.Select(s => s.AsEnumerable()).Transpose()
                .TakeWhile(s => s.All(d => d == s.First())).Select(s => s.First()).ToArray());
    }
}
