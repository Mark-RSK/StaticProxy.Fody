﻿namespace SimpleTest.Integration
{
    using System;

    public class NotProxiedType
    {
        public void TestNoProxy()
        {
            Guid.NewGuid();
        }
    }
}