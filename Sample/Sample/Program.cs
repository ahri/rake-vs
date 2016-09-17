using Library;
using System;

namespace Sample
{
    class Program
    {
        class RealDependency : Dependency
        {
            public void Output(string str)
            {
                Console.WriteLine(str);
            }
        }

        static void Main(string[] args)
        {
            new Library.Library(new RealDependency()).SayHello();
        }
    }
}
