﻿namespace SimpleTest
{
    [StaticProxy]
    internal interface IProxy
    {
        void Foo(int x, object y);

        int Bar();
    }
}