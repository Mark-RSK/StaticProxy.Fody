﻿namespace IntegrationTests.ProxyWithoutTarget
{
    using System;

    using FluentAssertions;

    using Moq;

    using Ninject;

    using Xunit;

    public class InstanciationTests : ProxyWithoutTargetIntegrationTestBase
    {
        [Fact]
        public void WhenThereIsNoInterceptor_InstanciationMustThrow()
        {
            this.BindInterceptorCollection();

            this.Kernel.Invoking(x => x.Get<IProxy>())
                .ShouldThrow<InvalidOperationException>();
        }

        [Fact(Skip = "implement")]
        public void WhenThereIsAnInterceptor_InstanciationMustNotThrow()
        {
            this.BindInterceptorCollection(Mock.Of<IDynamicInterceptor>());

            this.Kernel.Invoking(x => x.Get<IProxy>())
                .ShouldNotThrow();
        }
    }
}