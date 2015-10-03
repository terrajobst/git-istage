using System;

namespace GitIStage
{
    internal static class Whitespace
    {
        public static char[] GetSpaces(int size)
        {
            var result = new char[size];
            for (var i = 0; i < result.Length; i++)
                result[i] = ' ';

            return result;
        }
    }
}