using Library;
using Xunit;

namespace Tests
{
    public class Tests
    {
        private class FakeDependency : Dependency
        {
            public string str;

            public void Output(string str)
            {
                this.str = str;
            }
        }

        [Fact]
        public void Test()
        {
            var dependency = new FakeDependency();

            new Library.Library(dependency).SayHello();

            Assert.Equal("Hello World", dependency.str);
        }
    }
}
