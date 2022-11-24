using System;
using System.Collections.Generic;

public static class Shuffle
{
    private static readonly Random rng = new Random();

    //Fisher - Yates shuffle
    public static void ShuffleSet<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}