using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Helpers
{
    public static int Wrap(int value, int max)
    {
        return (value % (max + 1) + max + 1) % (max + 1);
    }
}
